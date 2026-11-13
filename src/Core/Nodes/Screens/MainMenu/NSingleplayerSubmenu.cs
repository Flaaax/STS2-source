using System;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

public partial class NSingleplayerSubmenu : NSubmenu
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/singleplayer_submenu");

	private NSubmenuButton _standardButton;

	private NSubmenuButton _dailyButton;

	private NSubmenuButton _dreadButton;

	private NSubmenuButton _blitzButton;

	private NSubmenuButton _customButton;

	private const string _keyStandard = "STANDARD";

	private const string _keyDaily = "DAILY";

	private const string _keyDread = "DREAD";

	private const string _keyBlitz = "BLITZ";

	private const string _keyCustom = "CUSTOM";

	private const string _iconStandard = "standard";

	private const string _iconDaily = "daily";

	private const string _iconBlitz = "blitz";

	private const string _iconDread = "dread";

	private const string _iconCustom = "custom";

	public static string[] AssetPaths => new string[6]
	{
		_scenePath,
		NSubmenuPortrait.GetIconPath("standard"),
		NSubmenuPortrait.GetIconPath("daily"),
		NSubmenuPortrait.GetIconPath("blitz"),
		NSubmenuPortrait.GetIconPath("dread"),
		NSubmenuPortrait.GetIconPath("custom")
	};

	protected override Control InitialFocusedControl => _standardButton;

	public static NSingleplayerSubmenu? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NSingleplayerSubmenu>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		ConnectSignals();
		_standardButton = GetNode<NSubmenuButton>("%StandardButton");
		_standardButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenCharacterSelect));
		_standardButton.SetLocalization("STANDARD");
		_standardButton.SetIcon("standard");
		_dailyButton = GetNode<NSubmenuButton>("%DailyButton");
		_dailyButton.Connect(NClickableControl.SignalName.Released, Callable.From((Action<NButton>)OpenDailyScreen));
		_dailyButton.SetLocalization("DAILY");
		_dailyButton.SetIcon("daily");
		_blitzButton = GetNode<NSubmenuButton>("%BlitzButton");
		_blitzButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenBlitzScreen));
		_blitzButton.SetLocalization("BLITZ");
		_blitzButton.SetIcon("blitz");
		_dreadButton = GetNode<NSubmenuButton>("%DreadButton");
		_dreadButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenDreadScreen));
		_dreadButton.SetLocalization("DREAD");
		_dreadButton.SetIcon("dread");
		_customButton = GetNode<NSubmenuButton>("%CustomRunButton");
		_customButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenCustomScreen));
		_customButton.SetLocalization("CUSTOM");
		_customButton.SetIcon("custom");
	}

	/// <summary>
	/// Checks the unlock state of Custom and Daily to determine whether they should be locked or not,
	/// and the unlock state of Dread to determine whether it should be hidden and unnavigable or not.
	/// Is not checked only at _Ready() because the MainMenu isn't recreated upon returning from the Timeline.
	/// </summary>
	private void RefreshButtons()
	{
		_dailyButton.SetEnabled(SaveManager.Instance.IsEpochRevealed<DailyRunEpoch>());
		_blitzButton.SetEnabled(SaveManager.Instance.IsEpochRevealed<BlitzEpoch>());
		_customButton.SetEnabled(SaveManager.Instance.IsEpochRevealed<CustomAndSeedsEpoch>());
		_dailyButton.RefreshLabels();
		_blitzButton.RefreshLabels();
		_customButton.RefreshLabels();
		bool flag = SaveManager.Instance.IsEpochRevealed<DreadEpoch>();
		_dreadButton.SetEnabled(flag);
		_dreadButton.Visible = flag;
		if (_dreadButton.Visible)
		{
			GetNode<Control>("%ButtonContainer").Scale = Vector2.One * 0.85f;
		}
	}

	public override void OnSubmenuOpened()
	{
		RefreshButtons();
	}

	private void OpenCharacterSelect(NButton _)
	{
		NCharacterSelectScreen submenuType = _stack.GetSubmenuType<NCharacterSelectScreen>();
		submenuType.InitializeSingleplayer();
		_stack.Push(submenuType);
	}

	private void OpenDailyScreen(NButton _)
	{
		OpenDailyScreen();
	}

	private void OpenDailyScreen()
	{
		NDailyRunScreen submenuType = _stack.GetSubmenuType<NDailyRunScreen>();
		submenuType.InitializeSingleplayer();
		_stack.Push(submenuType);
	}

	private void OpenBlitzScreen(NButton _)
	{
		NBlitzScreen submenuType = _stack.GetSubmenuType<NBlitzScreen>();
		submenuType.InitializeSingleplayer();
		_stack.Push(submenuType);
	}

	private void OpenDreadScreen(NButton _)
	{
		NDreadScreen submenuType = _stack.GetSubmenuType<NDreadScreen>();
		submenuType.InitializeSingleplayer();
		_stack.Push(submenuType);
	}

	private void OpenCustomScreen(NButton _)
	{
		NCustomRunScreen submenuType = _stack.GetSubmenuType<NCustomRunScreen>();
		submenuType.InitializeSingleplayer();
		_stack.Push(submenuType);
	}
}
