using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MegaCrit.Sts2.Core.Nodes.Combat;

public partial class NControllerCardPlay : NCardPlay
{
	[Signal]
	public delegate void ConfirmedEventHandler();

	[Signal]
	public delegate void CanceledEventHandler();

	private Callable _onCreatureHoverCallable;

	private Callable _onCreatureUnhoverCallable;

	private bool _signalsConnected;

	public override void _Input(InputEvent inputEvent)
	{
		if (!(inputEvent is InputEventAction inputEventAction))
		{
			return;
		}
		if (inputEventAction.IsActionPressed(MegaInput.select))
		{
			EmitSignal(SignalName.Confirmed);
			GetViewport()?.SetInputAsHandled();
		}
		if (inputEventAction.IsActionPressed(MegaInput.cancel) || inputEventAction.IsActionPressed(MegaInput.topPanel))
		{
			EmitSignal(SignalName.Canceled);
			if (inputEvent.IsActionPressed(MegaInput.cancel))
			{
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public static NControllerCardPlay Create(NHandCardHolder holder)
	{
		NControllerCardPlay nControllerCardPlay = new NControllerCardPlay();
		nControllerCardPlay.Holder = holder;
		nControllerCardPlay.Player = holder.CardModel.Owner;
		return nControllerCardPlay;
	}

	public override void Start()
	{
		if (base.Card == null || base.CardNode == null)
		{
			return;
		}
		NDebugAudioManager.Instance?.Play("card_select.mp3");
		NHoverTipSet.Remove(base.Holder);
		_onCreatureHoverCallable = Callable.From<NCreature>(base.OnCreatureHover);
		_onCreatureUnhoverCallable = Callable.From<NCreature>(base.OnCreatureUnhover);
		if (!base.Card.CanPlay(out UnplayableReason reason, out AbstractModel preventer))
		{
			CannotPlayThisCardFtueCheck(base.Card);
			CancelPlayCard();
			LocString playerDialogueLine = reason.GetPlayerDialogueLine(preventer);
			if (playerDialogueLine != null)
			{
				NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(NThoughtBubbleVfx.Create(playerDialogueLine.GetFormattedText(), base.Card.Owner.Creature, 1.0));
			}
			return;
		}
		TryShowEvokingOrbs();
		base.CardNode.CardHighlight.AnimFlash();
		CenterCard();
		TargetType targetType = base.Card.TargetType;
		if ((targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly) ? true : false)
		{
			TaskHelper.RunSafely(SingleCreatureTargeting(base.Card.TargetType));
		}
		else
		{
			MultiCreatureTargeting();
		}
	}

	private async Task SingleCreatureTargeting(TargetType targetType)
	{
		Creature owner = base.Card.Owner.Creature;
		List<Creature> list = new List<Creature>();
		switch (targetType)
		{
		case TargetType.AnyEnemy:
			list = (from c in owner.CombatState.GetOpponentsOf(owner)
				where c.IsHittable
				select c).ToList();
			break;
		case TargetType.AnyAlly:
			list = base.Card.CombatState.PlayerCreatures.Where((Creature c) => c.IsHittable && c != owner).ToList();
			break;
		}
		if (list.Count == 0)
		{
			CancelPlayCard();
			return;
		}
		List<NCreature> list2 = list.Select((Creature c) => NCombatRoom.Instance.GetCreatureNode(c)).OfType<NCreature>().ToList();
		if (list2.Count == 0)
		{
			CancelPlayCard();
			return;
		}
		NTargetManager instance = NTargetManager.Instance;
		instance.Connect(NTargetManager.SignalName.CreatureHovered, _onCreatureHoverCallable);
		instance.Connect(NTargetManager.SignalName.CreatureUnhovered, _onCreatureUnhoverCallable);
		_signalsConnected = true;
		try
		{
			instance.StartTargeting(targetType, base.CardNode, TargetMode.Controller, () => !GodotObject.IsInstanceValid(this) || !NControllerManager.Instance.IsUsingController, null);
			NCombatRoom.Instance.RestrictControllerNavigation(list2.Select((NCreature n) => n.Hitbox));
			NCreature nCreature = list2.First();
			if (NCombatRoom.Instance.LastTargetedCreature != null && NCombatRoom.Instance.LastTargetedCreature.IsHittable)
			{
				NCreature nCreature2 = list2.FirstOrDefault((NCreature c) => c.Entity == NCombatRoom.Instance.LastTargetedCreature);
				if (nCreature2 != null)
				{
					nCreature = nCreature2;
				}
			}
			nCreature.Hitbox.TryGrabFocus();
			NCreature nCreature3 = (NCreature)(await instance.SelectionFinished());
			NCombatRoom.Instance.EnableControllerNavigation();
			if (GodotObject.IsInstanceValid(this))
			{
				if (nCreature3 != null)
				{
					TryPlayCard(nCreature3.Entity);
				}
				else
				{
					CancelPlayCard();
				}
			}
		}
		finally
		{
			DisconnectTargetingSignals();
		}
	}

	public override void _ExitTree()
	{
		DisconnectTargetingSignals();
	}

	private void DisconnectTargetingSignals()
	{
		if (_signalsConnected)
		{
			_signalsConnected = false;
			if (NRun.Instance != null)
			{
				NTargetManager instance = NTargetManager.Instance;
				instance.Disconnect(NTargetManager.SignalName.CreatureHovered, _onCreatureHoverCallable);
				instance.Disconnect(NTargetManager.SignalName.CreatureUnhovered, _onCreatureUnhoverCallable);
			}
		}
	}

	private void MultiCreatureTargeting()
	{
		NCombatRoom.Instance.RestrictControllerNavigation(Array.Empty<Control>());
		ShowMultiCreatureTargetingVisuals();
		Connect(SignalName.Confirmed, Callable.From(delegate
		{
			NCombatRoom.Instance.EnableControllerNavigation();
			TryPlayCard(null);
		}));
		Connect(SignalName.Canceled, Callable.From(base.CancelPlayCard));
	}

	protected override void OnCancelPlayCard()
	{
		NCombatRoom.Instance.EnableControllerNavigation();
		base.Holder.TryGrabFocus();
	}
}
