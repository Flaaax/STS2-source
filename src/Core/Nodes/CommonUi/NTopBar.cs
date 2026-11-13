using System;
using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.Nodes.Relics;
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
	private Control _modifiersContainer;

	private ShaderMaterial _topBarMat;

	private NCapstoneContainer _capstoneContainer;

	private static readonly StringName _fontColorTheme = "font_color";

	private static readonly StringName _fontOutlineTheme = "font_outline_color";

	private static readonly StringName _h = new StringName("h");

	private static readonly StringName _s = new StringName("s");

	private static readonly StringName _v = new StringName("v");

	private Control _achievementLock;

	private TextureRect _ascensionIcon;

	private MegaLabel _ascensionLabel;

	private ShaderMaterial _ascensionIconHsv;

	private Tween? _hideTween;

	private bool _isDebugHidden;

	private Player? _player;

	public NTopBarPortrait Portrait { get; private set; }

	public NTopBarPortraitTip PortraitTip { get; private set; }

	public NTopBarHp Hp { get; private set; }

	public NTopBarGold Gold { get; private set; }

	public NPotionContainer PotionContainer { get; private set; }

	public NTopBarRoomIcon RoomIcon { get; private set; }

	public Control RoomIconsContainer { get; private set; }

	public NBlitzTopBarMap BlitzMap { get; private set; }

	public NTopBarFloorIcon FloorIcon { get; private set; }

	public NTopBarBossIcon BossIcon { get; private set; }

	public NRunTimer Timer { get; private set; }

	public NTopBarMapButton Map { get; private set; }

	public NTopBarDeckButton Deck { get; private set; }

	public NTopBarPauseButton Pause { get; private set; }

	public Container LeftContainer { get; private set; }

	public Container RightContainer { get; private set; }

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
		RoomIconsContainer = GetNode<Control>("%RoomIcons");
		BlitzMap = GetNode<NBlitzTopBarMap>("%BlitzMap");
		FloorIcon = GetNode<NTopBarFloorIcon>("%FloorIcon");
		BossIcon = GetNode<NTopBarBossIcon>("%BossIcon");
		Gold = GetNode<NTopBarGold>("%TopBarGold");
		Hp = GetNode<NTopBarHp>("%TopBarHp");
		Portrait = GetNode<NTopBarPortrait>("%TopBarPortrait");
		PortraitTip = GetNode<NTopBarPortraitTip>("%TopBarPortraitTip");
		Timer = GetNode<NRunTimer>("%TimerContainer");
		ActiveScreenProxy = GetNode<Control>("%ActiveScreenProxy");
		LeftContainer = GetNode<Container>("LeftAlignedStuff");
		RightContainer = GetNode<Container>("RightAlignedStuff");
		_achievementLock = GetNode<Control>("%AchievementLock");
		_ascensionIcon = GetNode<TextureRect>("%AscensionIcon");
		_ascensionLabel = GetNode<MegaLabel>("%AscensionLabel");
		_ascensionIconHsv = (ShaderMaterial)_ascensionIcon.Material;
		_modifiersContainer = GetNode<Control>("%Modifiers");
		_topBarMat = (ShaderMaterial)GetNode<Control>("%BgImage").GetMaterial();
		ActiveScreenProxy.Connect(Control.SignalName.FocusEntered, Callable.From(delegate
		{
			ActiveScreenContext.Instance.GetCurrentScreen()?.FocusedControlFromTopBar?.TryGrabFocus();
		}));
		_capstoneContainer = GetParent().GetNode<NCapstoneContainer>("%CapstoneScreenContainer");
		_capstoneContainer.Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(ToggleAnimState));
		_capstoneContainer.Connect(Node.SignalName.ChildExitingTree, Callable.From<Node>(ToggleAnimState));
	}

	/// <summary>
	/// Called at the start of Run (or loading a save).
	/// The top bar is NOT recreated between rooms/acts/etc.
	/// </summary>
	/// <param name="runState"></param>
	public void Initialize(IRunState runState)
	{
		if (runState.GameMode == GameMode.Dread)
		{
			int dreadStreak = AscensionHelper.GetDreadStreak(runState);
			ApplyAscensionIconConfig(new DreadIconConfig(), (dreadStreak > 0) ? dreadStreak.ToString() : null);
		}
		else if (runState.GameMode == GameMode.Blitz)
		{
			ApplyAscensionIconConfig(new BlitzIconConfig(), (runState.AscensionLevel > 0) ? runState.AscensionLevel.ToString() : null);
		}
		else if (runState.AscensionLevel > 0)
		{
			ApplyAscensionIconConfig(new AscensionIconConfig(runState.Players.Count > 1), runState.AscensionLevel.ToString());
		}
		else
		{
			_ascensionIcon.Visible = false;
		}
		if (runState.GameMode == GameMode.Blitz)
		{
			Gold.Visible = false;
			RoomIconsContainer.Visible = false;
			Map.Visible = false;
			Map.Disable();
			BlitzMap.Initialize(runState, this);
		}
		_achievementLock.Visible = GameModeHelper.AreAchievementsAndEpochsLockedForRun(runState);
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
		_topBarMat.SetShaderParameter(_s, (runState.GameMode == GameMode.Dread) ? 0.25f : 1f);
		_player.RelicObtained += OnRelicsUpdated;
		_player.RelicRemoved += OnRelicsUpdated;
		_player.MaxPotionCountChanged += MaxPotionsChanged;
		Callable.From(UpdateNavigation).CallDeferred();
	}

	/// <summary>
	/// Shows the AscensionIcon styled with the given config. A null labelText hides the number and shows only the icon.
	/// </summary>
	private void ApplyAscensionIconConfig(TopBarAscensionIconConfig config, string? labelText)
	{
		_ascensionIcon.Texture = config.Texture;
		_ascensionIconHsv.SetShaderParameter(_h, config.Hue);
		_ascensionIconHsv.SetShaderParameter(_s, config.Saturation);
		_ascensionIconHsv.SetShaderParameter(_v, config.Value);
		_ascensionLabel.AddThemeColorOverride(_fontColorTheme, config.FontColor);
		_ascensionLabel.AddThemeColorOverride(_fontOutlineTheme, config.FontOutlineColor);
		_ascensionLabel.Visible = labelText != null;
		if (labelText != null)
		{
			_ascensionLabel.SetTextAutoSize(labelText);
		}
		_ascensionIcon.Visible = true;
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
		base.FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Inherited;
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

	public List<Control> GetAllNavigableElements()
	{
		List<Control> list = new List<Control>();
		list.Add(PortraitTip);
		list.Add(Hp);
		list.Add(Gold);
		list.AddRange(PotionContainer.Holders);
		list.Add(RoomIcon);
		list.Add(FloorIcon);
		list.Add(BossIcon);
		list.AddRange(BlitzMap.MapPoints);
		list.AddRange(_modifiersContainer.GetChildren().OfType<Control>());
		return (from c in list.ToArray()
			where c.IsVisibleInTree()
			select c).ToList();
	}

	public void UpdateNavigation()
	{
		IReadOnlyList<NRelicInventoryHolder> readOnlyList = NRun.Instance?.GlobalUi.RelicInventory.RelicNodes ?? Array.Empty<NRelicInventoryHolder>();
		List<Control> allNavigableElements = GetAllNavigableElements();
		for (int i = 0; i < allNavigableElements.Count; i++)
		{
			Control control = allNavigableElements[i];
			Control control2 = null;
			for (int j = 1; j <= allNavigableElements.Count; j++)
			{
				control2 = allNavigableElements[(i - j + allNavigableElements.Count) % allNavigableElements.Count];
				if (control2.IsVisibleInTree())
				{
					break;
				}
			}
			Control control3 = null;
			for (int k = 1; k <= allNavigableElements.Count; k++)
			{
				control3 = allNavigableElements[(i + k) % allNavigableElements.Count];
				if (control3.IsVisibleInTree())
				{
					break;
				}
			}
			Control control4 = null;
			float num = 0f;
			Vector2 vector = control.GlobalPosition + control.Size * 0.5f;
			foreach (NRelicInventoryHolder item in readOnlyList)
			{
				Vector2 to = item.GlobalPosition + item.Size * 0.5f;
				float num2 = vector.DistanceSquaredTo(to);
				if (control4 == null || num2 < num)
				{
					control4 = item;
					num = num2;
				}
			}
			control.FocusNeighborLeft = control2?.GetPath() ?? control.GetPath();
			control.FocusNeighborRight = control3?.GetPath() ?? control.GetPath();
			control.FocusNeighborTop = control.GetPath();
			control.FocusNeighborBottom = control4?.GetPath() ?? control.GetPath();
		}
		NRun.Instance?.GlobalUi.RelicInventory.UpdateNavigation();
		Viewport viewport = GetViewport();
		if (viewport != null && viewport.GuiGetFocusOwner() == ActiveScreenProxy)
		{
			ActiveScreenContext.Instance.FocusOnDefaultControl();
		}
	}
}
