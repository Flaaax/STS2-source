using System.Linq;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// A Node script which contains references to all systems which reside in the Top Bar, the bar at the top
/// of the screen which displays gold, health, potions, and various buttons in the top right corner during a run.
/// </summary>
public partial class NTopBar : Control
{
	private NCapstoneContainer _capstoneContainer;

	private static readonly StringName _fontOutlineTheme = "font_outline_color";

	private static readonly StringName _h = new StringName("h");

	private static readonly StringName _v = new StringName("v");

	private static readonly Color _redLabelOutline = new Color("593400");

	private static readonly Color _blueLabelOutline = new Color("004759");

	private Control _modifiersContainer;

	private Control _achievementLock;

	private Control _ascensionIcon;

	private MegaLabel _ascensionLabel;

	private ShaderMaterial _ascensionHsv;

	private Tween? _hideTween;

	private bool _isDebugHidden;

	private Player? _player;

	public NTopBarMapButton Map { get; private set; }

	public NTopBarDeckButton Deck { get; private set; }

	public NTopBarPauseButton Pause { get; private set; }

	public NPotionContainer PotionContainer { get; private set; }

	public NTopBarRoomIcon RoomIcon { get; private set; }

	public NTopBarFloorIcon FloorIcon { get; private set; }

	public NTopBarBossIcon BossIcon { get; private set; }

	public NTopBarGold Gold { get; private set; }

	public NTopBarHp Hp { get; private set; }

	public NTopBarPortrait Portrait { get; private set; }

	public NTopBarPortraitTip PortraitTip { get; private set; }

	public NRunTimer Timer { get; private set; }

	public Node TrailContainer { get; private set; }

	/// <summary>
	/// A control node that will automatically try to refocus on the current ScreenContext's FocusedControlFromTopBar.
	/// Used by things like the nodes in see <see cref="T:MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventory" /> to dynamically controller navigate from the relic
	/// nodes back to the current active screen. Doing it this way circumvents us needing to call ActiveScreenContext.Update
	/// whenever we need update the top bar elements navigation back to the active screen.
	/// </summary>
	public Control ActiveScreenProxy { get; private set; }

	public override void _Ready()
	{
		TrailContainer = GetNode<Node>("%TrailContainer");
		Map = GetNode<NTopBarMapButton>("%Map");
		Deck = GetNode<NTopBarDeckButton>("%Deck");
		Pause = GetNode<NTopBarPauseButton>("%PauseButton");
		PotionContainer = GetNode<NPotionContainer>("%PotionContainer");
		RoomIcon = GetNode<NTopBarRoomIcon>("%RoomIcon");
		FloorIcon = GetNode<NTopBarFloorIcon>("%FloorIcon");
		BossIcon = GetNode<NTopBarBossIcon>("%BossIcon");
		Gold = GetNode<NTopBarGold>("%TopBarGold");
		Hp = GetNode<NTopBarHp>("%TopBarHp");
		Portrait = GetNode<NTopBarPortrait>("%TopBarPortrait");
		PortraitTip = GetNode<NTopBarPortraitTip>("%TopBarPortraitTip");
		Timer = GetNode<NRunTimer>("%TimerContainer");
		ActiveScreenProxy = GetNode<Control>("%ActiveScreenProxy");
		_achievementLock = GetNode<Control>("%AchievementLock");
		_ascensionIcon = GetNode<Control>("%AscensionIcon");
		_ascensionLabel = GetNode<MegaLabel>("%AscensionLabel");
		_ascensionHsv = (ShaderMaterial)_ascensionIcon.Material;
		_modifiersContainer = GetNode<Control>("%Modifiers");
		ActiveScreenProxy.Connect(Control.SignalName.FocusEntered, Callable.From(delegate
		{
			ActiveScreenContext.Instance.GetCurrentScreen()?.FocusedControlFromTopBar?.TryGrabFocus();
		}));
		_capstoneContainer = GetParent().GetNode<NCapstoneContainer>("%CapstoneScreenContainer");
		_capstoneContainer.Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(ToggleAnimState));
		_capstoneContainer.Connect(Node.SignalName.ChildExitingTree, Callable.From<Node>(ToggleAnimState));
	}

	public void Initialize(IRunState runState)
	{
		if (runState.AscensionLevel > 0)
		{
			if (runState.Players.Count > 1)
			{
				_ascensionHsv.SetShaderParameter(_h, 0.52f);
				_ascensionHsv.SetShaderParameter(_v, 1.2f);
				_ascensionLabel.AddThemeColorOverride(_fontOutlineTheme, _blueLabelOutline);
			}
			else
			{
				_ascensionHsv.SetShaderParameter(_h, 1f);
				_ascensionHsv.SetShaderParameter(_v, 1f);
				_ascensionLabel.AddThemeColorOverride(_fontOutlineTheme, _redLabelOutline);
			}
			_ascensionIcon.Visible = true;
			_ascensionLabel.SetTextAutoSize(runState.AscensionLevel.ToString());
		}
		_achievementLock.Visible = runState.GameMode.AreAchievementsAndEpochsLocked();
		_modifiersContainer.Visible = runState.Modifiers.Count > 0;
		foreach (ModifierModel modifier in runState.Modifiers)
		{
			NTopBarModifier child = NTopBarModifier.Create(modifier);
			_modifiersContainer.AddChildSafely(child);
		}
		_player = LocalContext.GetMe(runState);
		Deck.Initialize(_player);
		RoomIcon.Initialize(runState);
		FloorIcon.Initialize(runState);
		BossIcon.Initialize(runState);
		Gold.Initialize(_player);
		Hp.Initialize(_player);
		Pause.Initialize(runState);
		Portrait.Initialize(_player);
		PortraitTip.Initialize(runState);
		PotionContainer.Initialize(runState);
		_player.RelicObtained += OnRelicsUpdated;
		_player.RelicRemoved += OnRelicsUpdated;
		_player.MaxPotionCountChanged += MaxPotionsChanged;
		Callable.From(UpdateNavigation).CallDeferred();
	}

	public override void _EnterTree()
	{
		ActiveScreenContext.Instance.Updated += UpdateNavigation;
	}

	public override void _ExitTree()
	{
		ActiveScreenContext.Instance.Updated -= UpdateNavigation;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1 && _player != null)
		{
			_player.RelicObtained -= OnRelicsUpdated;
			_player.RelicRemoved -= OnRelicsUpdated;
			_player.MaxPotionCountChanged -= MaxPotionsChanged;
		}
	}

	private void ToggleAnimState(Node _)
	{
		Pause.ToggleAnimState();
		Deck.ToggleAnimState();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionReleased(DebugHotkey.hideTopBar))
		{
			DebugHideTopBar();
		}
	}

	private void DebugHideTopBar()
	{
		if (!_isDebugHidden)
		{
			NGame.Instance.AddChildSafely(NFullscreenTextVfx.Create("Hide Top Bar"));
			AnimHide();
		}
		else
		{
			NGame.Instance.AddChildSafely(NFullscreenTextVfx.Create("Show Top Bar"));
			AnimShow();
		}
		_isDebugHidden = !_isDebugHidden;
	}

	public void AnimHide()
	{
		base.FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Disabled;
		_hideTween?.Kill();
		_hideTween = CreateTween();
		_hideTween.TweenProperty(this, "position:y", -100f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	public void AnimShow()
	{
		base.FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Enabled;
		_hideTween?.Kill();
		_hideTween = CreateTween();
		_hideTween.TweenProperty(this, "position:y", 0f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	private void OnRelicsUpdated(RelicModel _)
	{
		Callable.From(UpdateNavigation).CallDeferred();
	}

	private void MaxPotionsChanged(int _)
	{
		Callable.From(UpdateNavigation).CallDeferred();
	}

	private void UpdateNavigation()
	{
		Control control = NRun.Instance?.GlobalUi.RelicInventory.RelicNodes.FirstOrDefault();
		if (control == null)
		{
			return;
		}
		Gold.FocusNeighborBottom = control.GetPath();
		Hp.FocusNeighborBottom = control.GetPath();
		FloorIcon.FocusNeighborBottom = control.GetPath();
		RoomIcon.FocusNeighborBottom = control.GetPath();
		BossIcon.FocusNeighborBottom = control.GetPath();
		Gold.FocusNeighborTop = Gold.GetPath();
		Hp.FocusNeighborTop = Hp.GetPath();
		FloorIcon.FocusNeighborTop = FloorIcon.GetPath();
		RoomIcon.FocusNeighborTop = RoomIcon.GetPath();
		BossIcon.FocusNeighborTop = BossIcon.GetPath();
		Hp.FocusNeighborLeft = Hp.GetPath();
		Hp.FocusNeighborRight = Gold.GetPath();
		Gold.FocusNeighborLeft = Hp.GetPath();
		Gold.FocusNeighborRight = PotionContainer.FirstPotionControl?.GetPath();
		RoomIcon.FocusNeighborLeft = PotionContainer.LastPotionControl?.GetPath();
		RoomIcon.FocusNeighborRight = FloorIcon.GetPath();
		FloorIcon.FocusNeighborLeft = RoomIcon.GetPath();
		FloorIcon.FocusNeighborRight = BossIcon.GetPath();
		BossIcon.FocusNeighborLeft = FloorIcon.GetPath();
		BossIcon.FocusNeighborRight = BossIcon.GetPath();
		if (PortraitTip.ShowTip)
		{
			PortraitTip.FocusNeighborRight = Hp.GetPath();
			PortraitTip.FocusNeighborLeft = PortraitTip.GetPath();
			PortraitTip.FocusNeighborTop = PortraitTip.GetPath();
			PortraitTip.FocusNeighborBottom = PortraitTip.GetPath();
			Hp.FocusNeighborLeft = PortraitTip.GetPath();
		}
		Viewport viewport = GetViewport();
		if (viewport != null && viewport.GuiGetFocusOwner() == ActiveScreenProxy)
		{
			ActiveScreenContext.Instance.FocusOnDefaultControl();
		}
		if (!_modifiersContainer.Visible)
		{
			return;
		}
		Control[] array = _modifiersContainer.GetChildren().OfType<Control>().ToArray();
		BossIcon.FocusNeighborRight = array.First().GetPath();
		if (!BossIcon.IsVisible())
		{
			FloorIcon.FocusNeighborRight = array.First().GetPath();
		}
		for (int i = 0; i < array.Length; i++)
		{
			Control control2 = array[i];
			control2.FocusNeighborTop = control2.GetPath();
			control2.FocusNeighborBottom = control.GetPath();
			control2.FocusNeighborRight = ((i < array.Length - 1) ? array[i + 1].GetPath() : control2.GetPath());
			if (i > 0)
			{
				control2.FocusNeighborLeft = array[i - 1].GetPath();
			}
			else
			{
				control2.FocusNeighborLeft = (BossIcon.IsVisible() ? BossIcon.GetPath() : FloorIcon.GetPath());
			}
		}
	}
}
