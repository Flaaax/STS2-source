using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NByrdpipReadyToEvolveVfx : Node2D
{
	private Creature _byrdpip;

	private MegaSprite _sprite;

	private CombatId? _combatId;

	private MegaTrackEntry? _attackEntry;

	private bool _finished;

	public static NByrdpipReadyToEvolveVfx? Create(Player player)
	{
		if (TestMode.IsOn || NonInteractiveMode.IsActive)
		{
			return null;
		}
		MegaCrit.Sts2.Core.Models.Relics.Byrdpip relic = player.GetRelic<MegaCrit.Sts2.Core.Models.Relics.Byrdpip>();
		if (relic == null || relic.SwoopsPlayed != 5)
		{
			return null;
		}
		Creature creature = player.PlayerCombatState?.GetPet<MegaCrit.Sts2.Core.Models.Monsters.Byrdpip>();
		MegaSprite megaSprite = creature?.GetCreatureNode()?.Visuals.SpineBody;
		if (creature == null || megaSprite == null)
		{
			return null;
		}
		return new NByrdpipReadyToEvolveVfx
		{
			_byrdpip = creature,
			_sprite = megaSprite,
			_combatId = CombatManager.Instance.CurrentCombatId
		};
	}

	public override void _Ready()
	{
		_sprite.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnStarted));
		_sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>(OnCompleted));
		_sprite.ConnectAnimationInterrupted(Callable.From<GodotObject, GodotObject, GodotObject>(OnInterrupted));
		_sprite.ConnectAnimationEnded(Callable.From<GodotObject, GodotObject, GodotObject>(OnEnded));
	}

	private void OnStarted(GodotObject _, GodotObject __, GodotObject trackEntry)
	{
		if (!_finished && IsAttackAnimation(trackEntry))
		{
			if (_attackEntry != null)
			{
				Finish();
			}
			else
			{
				_attackEntry = _sprite.GetAnimationState().GetCurrent(0);
			}
		}
	}

	private void OnCompleted(GodotObject _, GodotObject __, GodotObject trackEntry)
	{
		if (!_finished && _attackEntry != null && IsAttackAnimation(trackEntry) && _attackEntry.IsComplete())
		{
			Finish();
			if (CombatManager.Instance.IsCurrentLiveCombat(_combatId) && !CombatManager.Instance.IsEnding)
			{
				TalkCmd.Play(new LocString("relics", "BYRDPIP.readyToEvolve"), _byrdpip, VfxColor.Green);
			}
		}
	}

	private void OnInterrupted(GodotObject _, GodotObject __, GodotObject trackEntry)
	{
		if (_attackEntry != null && !_finished && IsAttackAnimation(trackEntry) && !new MegaTrackEntry(trackEntry).IsComplete())
		{
			Finish();
		}
	}

	private void OnEnded(GodotObject _, GodotObject __, GodotObject trackEntry)
	{
		if (!_finished && _attackEntry != null && IsAttackAnimation(trackEntry) && !(_sprite.GetAnimationState().GetCurrentAnimationName() == "attack"))
		{
			Finish();
		}
	}

	private static bool IsAttackAnimation(GodotObject trackEntry)
	{
		return new MegaTrackEntry(trackEntry).GetAnimationName() == "attack";
	}

	private void Finish()
	{
		_finished = true;
		this.QueueFreeSafely();
	}

	public override void _ExitTree()
	{
		_finished = true;
		_attackEntry?.Dispose();
		_attackEntry = null;
		if (GodotObject.IsInstanceValid(_sprite.BoundObject))
		{
			_sprite.DisconnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnStarted));
			_sprite.DisconnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>(OnCompleted));
			_sprite.DisconnectAnimationInterrupted(Callable.From<GodotObject, GodotObject, GodotObject>(OnInterrupted));
			_sprite.DisconnectAnimationEnded(Callable.From<GodotObject, GodotObject, GodotObject>(OnEnded));
		}
	}
}
