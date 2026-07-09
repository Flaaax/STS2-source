using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Nodes.Events;

public partial class NEventLayout : Control
{
	public const string defaultScenePath = "res://scenes/events/default_event_layout.tscn";

	protected Tween? _descriptionTween;

	protected VBoxContainer _optionsContainer;

	private TextureRect? _portrait;

	private Texture2D? _currentPortraitTex;

	private Texture2D? _currentPhobiaPortraitTex;

	private MegaLabel? _title;

	protected EventModel _event;

	/// <summary>
	/// The label to show when an event is "shared" (see <see cref="P:MegaCrit.Sts2.Core.Models.EventModel.IsShared" /> for details).
	/// Can be null for event types that can't be shared (like <see cref="T:MegaCrit.Sts2.Core.Models.AncientEventModel" />).
	/// </summary>
	protected MegaLabel? _sharedEventLabel;

	private static readonly LocString _sharedEventLoc = new LocString("events", "SHARED_EVENT_INFO");

	/// <summary>
	/// The event description label. Some events (like Ancients) have no descriptions, in which case this will be null.
	/// </summary>
	protected MegaRichTextLabel? _description;

	private static bool _isDebugUiVisible;

	/// <summary>
	/// Container for event VFX nodes (ambient particles, etc.).
	/// Can be null in some subclasses.
	/// </summary>
	public Control? VfxContainer { get; private set; }

	public IEnumerable<NEventOptionButton> OptionButtons => _optionsContainer.GetChildren().OfType<NEventOptionButton>();

	public virtual Control? DefaultFocusedControl => OptionButtons.FirstOrDefault();

	public override void _Ready()
	{
		_portrait = GetNodeOrNull<TextureRect>("%Portrait");
		_title = GetNodeOrNull<MegaLabel>("%Title");
		_description = GetNodeOrNull<MegaRichTextLabel>("%EventDescription");
		VfxContainer = GetNodeOrNull<Control>("%VfxContainer");
		_sharedEventLabel = GetNodeOrNull<MegaLabel>("%SharedEventLabel");
		_sharedEventLabel?.SetTextAutoSize(_sharedEventLoc.GetFormattedText());
		_optionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
		_description?.SetText(string.Empty);
		ApplyDebugUiVisibility();
	}

	public override void _EnterTree()
	{
		RunManager.Instance.EventSynchronizer.PlayerVoteChanged += OnPlayerVoteChanged;
		NGame.Instance?.Connect(NGame.SignalName.PhobiaModeToggled, Callable.From(UpdatePhobiaMode));
	}

	public override void _ExitTree()
	{
		RunManager.Instance.EventSynchronizer.PlayerVoteChanged -= OnPlayerVoteChanged;
		NGame.Instance?.Disconnect(NGame.SignalName.PhobiaModeToggled, Callable.From(UpdatePhobiaMode));
	}

	public virtual void SetEvent(EventModel eventModel)
	{
		_event = eventModel;
		InitializeVisuals();
		_event.OnRoomEnter();
	}

	protected virtual void InitializeVisuals()
	{
		if (_event.HasPhobiaModePortrait)
		{
			SetPortrait(_event.CreateInitialPortrait(), _event.CreateInitialPhobiaModePortrait());
		}
		else
		{
			SetPortrait(_event.CreateInitialPortrait());
		}
		if (_event.HasVfx)
		{
			Node2D node2D = _event.CreateVfx();
			NEventRoom.Instance.Layout.AddVfxAnchoredToPortrait(node2D);
			node2D.Position = EventModel.VfxOffset;
		}
	}

	private void UpdatePhobiaMode()
	{
		if (_currentPhobiaPortraitTex != null)
		{
			if (_portrait == null)
			{
				throw new InvalidOperationException("Trying to set a portrait in an event layout that doesn't have one.");
			}
			if (SaveManager.Instance.PrefsSave.PhobiaMode)
			{
				_portrait.Texture = _currentPhobiaPortraitTex;
			}
			else
			{
				_portrait.Texture = _currentPortraitTex;
			}
		}
	}

	public void SetPortrait(Texture2D portrait, Texture2D? phobiaModePortrait = null)
	{
		if (_portrait == null)
		{
			throw new InvalidOperationException("Trying to set a portrait in an event layout that doesn't have one.");
		}
		_currentPortraitTex = portrait;
		_currentPhobiaPortraitTex = phobiaModePortrait;
		_portrait.Texture = ((SaveManager.Instance.PrefsSave.PhobiaMode && _currentPhobiaPortraitTex != null) ? _currentPhobiaPortraitTex : _currentPortraitTex);
	}

	/// <summary>
	/// Adds a child node that's anchored to the portrait. Usually you'd want to add it to VfxContainer, but you can use
	/// this method instead if your VFX's position is dependent on the portrait (usually due to resolution stuff)
	/// </summary>
	/// <param name="vfx">VFX to add</param>
	public void AddVfxAnchoredToPortrait(Node? vfx)
	{
		_portrait.AddChildSafely(vfx);
	}

	/// <summary>
	/// Removes ALL child nodes from the Portrait, vfx or not.
	/// Useful for swapping out vfx if an event has multiple portraits.
	/// </summary>
	public void RemoveNodesOnPortrait()
	{
		foreach (Node child in _portrait.GetChildren())
		{
			_portrait.RemoveChildSafely(child);
		}
	}

	public void SetTitle(string title)
	{
		if (_title != null)
		{
			_title.Text = title;
		}
	}

	public void SetDescription(string description)
	{
		if (_description != null)
		{
			_description.SetTextAutoSize(description);
			AnimateIn();
		}
	}

	protected virtual void AnimateIn()
	{
		if (_sharedEventLabel != null)
		{
			_sharedEventLabel.Modulate = StsColors.transparentWhite;
		}
		if (_description != null)
		{
			_description.Modulate = StsColors.transparentWhite;
			bool flag = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast;
			_descriptionTween?.Kill();
			_descriptionTween = CreateTween().SetParallel();
			_descriptionTween.TweenInterval(flag ? 0.2 : 0.5);
			_descriptionTween.Chain();
			if (_title != null)
			{
				_descriptionTween.TweenProperty(_title, "modulate", Colors.White, flag ? 0.25 : 0.5);
			}
			_descriptionTween.TweenProperty(_description, "modulate", Colors.White, flag ? 0.5 : 1.0).SetDelay(0.25);
			_descriptionTween.TweenProperty(_description, "visible_ratio", 1f, flag ? 0.5 : 1.0).SetDelay(0.25).From(0f)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Sine);
			if (_sharedEventLabel != null)
			{
				_descriptionTween.TweenProperty(_sharedEventLabel, "modulate", Colors.White, flag ? 0.25 : 0.5).SetDelay(0.25);
			}
		}
	}

	public void ClearOptions()
	{
		foreach (Node item in _optionsContainer.GetChildren().ToList())
		{
			_optionsContainer.RemoveChildSafely(item);
			item.QueueFreeSafely();
		}
	}

	public void AddOptions(IEnumerable<EventOption> options)
	{
		if (_sharedEventLabel != null)
		{
			MegaLabel? sharedEventLabel = _sharedEventLabel;
			EventModel eventModel = _event;
			sharedEventLabel.Visible = eventModel != null && eventModel.IsShared && !eventModel.IsFinished && _event.Owner.RunState.Players.Count > 1;
		}
		foreach (EventOption option in options)
		{
			NEventOptionButton nEventOptionButton = NEventOptionButton.Create(_event, option, _optionsContainer.GetChildCount());
			_optionsContainer.AddChildSafely(nEventOptionButton);
			nEventOptionButton.RefreshVotes();
		}
		int childCount = _optionsContainer.GetChildCount();
		if (childCount != 0)
		{
			NodePath path = _optionsContainer.GetChild<Control>(0).GetPath();
			NodePath path2 = _optionsContainer.GetChild<Control>(childCount - 1).GetPath();
			for (int i = 0; i < childCount; i++)
			{
				Control child = _optionsContainer.GetChild<Control>(i);
				NodePath focusNeighborRight = (child.FocusNeighborLeft = child.GetPath());
				child.FocusNeighborRight = focusNeighborRight;
				child.FocusNeighborTop = ((i > 0) ? _optionsContainer.GetChild<Control>(i - 1).GetPath() : path2);
				child.FocusNeighborBottom = ((i < childCount - 1) ? _optionsContainer.GetChild<Control>(i + 1).GetPath() : path);
			}
			AnimateButtonsIn();
		}
	}

	/// <summary>
	/// Called when this layout is finished being set up.
	/// At this point, all the buttons for the initial page should be populated.
	/// </summary>
	public virtual void OnSetupComplete()
	{
	}

	protected virtual void AnimateButtonsIn()
	{
		foreach (NEventOptionButton button in OptionButtons)
		{
			Callable.From(delegate
			{
				button.AnimateIn();
			}).CallDeferred();
		}
	}

	public async Task BeforeSharedOptionChosen(EventOption option)
	{
		NEventOptionButton chosenButton = null;
		foreach (NEventOptionButton optionButton in OptionButtons)
		{
			optionButton.Disable();
			if (optionButton.Option == option)
			{
				chosenButton = optionButton;
			}
		}
		if (chosenButton == null)
		{
			return;
		}
		EventSplitVoteAnimation eventSplitVoteAnimation = new EventSplitVoteAnimation(this, _event.Owner.RunState);
		await eventSplitVoteAnimation.TryPlay(chosenButton);
		foreach (NEventOptionButton optionButton2 in OptionButtons)
		{
			if (optionButton2.Option != option)
			{
				optionButton2.GrayOut();
			}
		}
		await chosenButton.FlashConfirmation();
	}

	/// <summary>
	/// Called during a shared event when a player changes the option they voted on.
	/// </summary>
	private void OnPlayerVoteChanged(Player player)
	{
		foreach (NEventOptionButton optionButton in OptionButtons)
		{
			optionButton.RefreshVotes();
		}
	}

	public void DisableEventOptions()
	{
		foreach (NEventOptionButton optionButton in OptionButtons)
		{
			optionButton.Disable();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionReleased(DebugHotkey.hideEventUi))
		{
			_isDebugUiVisible = !_isDebugUiVisible;
			ApplyDebugUiVisibility();
			NGame.Instance.AddChildSafely(NFullscreenTextVfx.Create(_isDebugUiVisible ? "Hide Event UI" : "Show Event UI"));
		}
	}

	private void ApplyDebugUiVisibility()
	{
		if (_isDebugUiVisible)
		{
			_optionsContainer.Visible = false;
			if (_title != null)
			{
				_title.Modulate = Colors.Transparent;
			}
			if (_description != null)
			{
				_description.Visible = false;
			}
		}
		else
		{
			_optionsContainer.Visible = true;
			if (_description != null)
			{
				_description.Visible = true;
			}
		}
	}
}
