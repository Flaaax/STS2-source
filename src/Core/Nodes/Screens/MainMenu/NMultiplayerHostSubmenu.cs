using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

public partial class NMultiplayerHostSubmenu : NSubmenu
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/multiplayer_host_submenu");

	private NSubmenuButton _standardButton;

	private NSubmenuButton _dailyButton;

	private NSubmenuButton _dreadButton;

	private NSubmenuButton _blitzButton;

	private NSubmenuButton _customButton;

	private const string _keyStandard = "STANDARD_MP";

	private const string _keyDaily = "DAILY_MP";

	private const string _keyDread = "DREAD_MP";

	private const string _keyBlitz = "BLITZ_MP";

	private const string _keyCustom = "CUSTOM_MP";

	private const string _iconStandard = "standard";

	private const string _iconDaily = "daily";

	private const string _iconBlitz = "blitz";

	private const string _iconDread = "dread";

	private const string _iconCustom = "custom";

	private Control _loadingOverlay;

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

	public static NMultiplayerHostSubmenu? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NMultiplayerHostSubmenu>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		ConnectSignals();
		_loadingOverlay = GetNode<Control>("%LoadingOverlay");
		_standardButton = GetNode<NSubmenuButton>("%StandardButton");
		_standardButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnStandardPressed));
		_standardButton.SetLocalization("STANDARD_MP");
		_standardButton.SetIcon("standard");
		_dailyButton = GetNode<NSubmenuButton>("%DailyButton");
		_dailyButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnDailyPressed));
		_dailyButton.SetLocalization("DAILY_MP");
		_dailyButton.SetIcon("daily");
		_blitzButton = GetNode<NSubmenuButton>("%BlitzButton");
		_blitzButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnBlitzPressed));
		_blitzButton.SetLocalization("BLITZ_MP");
		_blitzButton.SetIcon("blitz");
		_dreadButton = GetNode<NSubmenuButton>("%DreadButton");
		_dreadButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnDreadPressed));
		_dreadButton.SetLocalization("DREAD_MP");
		_dreadButton.SetIcon("dread");
		_customButton = GetNode<NSubmenuButton>("%CustomRunButton");
		_customButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnCustomPressed));
		_customButton.SetLocalization("CUSTOM_MP");
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
		_dailyButton.RefreshLabels("DAILY");
		_blitzButton.RefreshLabels("BLITZ");
		_customButton.RefreshLabels("CUSTOM");
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

	private void OnStandardPressed(NButton _)
	{
		StartHost(GameMode.Standard, isCustom: false);
	}

	private void OnDailyPressed(NButton _)
	{
		StartHost(GameMode.Daily, isCustom: false);
	}

	private void OnBlitzPressed(NButton _)
	{
		StartHost(GameMode.Blitz, isCustom: false);
	}

	private void OnDreadPressed(NButton _)
	{
		StartHost(GameMode.Dread, isCustom: false);
	}

	private void OnCustomPressed(NButton _)
	{
		StartHost(GameMode.Standard, isCustom: true);
	}

	public void StartHost(GameMode gameMode, bool isCustom)
	{
		TaskHelper.RunSafely(StartHostAsync(gameMode, isCustom, _loadingOverlay, _stack));
	}

	public static async Task StartHostAsync(GameMode gameMode, bool isCustom, Control loadingOverlay, NSubmenuStack stack)
	{
		PlatformType platformType = ((SteamInitializer.Initialized && !CommandLineHelper.HasArg("fastmp")) ? PlatformType.Steam : PlatformType.None);
		loadingOverlay.Visible = true;
		try
		{
			NetHostGameService netService = new NetHostGameService(PeerVersionInfo.LocalDefault());
			NetErrorInfo? netErrorInfo = null;
			if (platformType == PlatformType.Steam)
			{
				netErrorInfo = await netService.StartSteamHost(4);
			}
			else
			{
				netService.StartENetHost(33771, 4);
			}
			if (!netErrorInfo.HasValue)
			{
				if (isCustom)
				{
					NCustomRunScreen submenuType = stack.GetSubmenuType<NCustomRunScreen>();
					submenuType.InitializeMultiplayerAsHost(netService, 4);
					stack.Push(submenuType);
					return;
				}
				switch (gameMode)
				{
				case GameMode.Standard:
				{
					NCharacterSelectScreen submenuType5 = stack.GetSubmenuType<NCharacterSelectScreen>();
					submenuType5.InitializeMultiplayerAsHost(netService, 4);
					stack.Push(submenuType5);
					break;
				}
				case GameMode.Daily:
				{
					NDailyRunScreen submenuType4 = stack.GetSubmenuType<NDailyRunScreen>();
					submenuType4.InitializeMultiplayerAsHost(netService);
					stack.Push(submenuType4);
					break;
				}
				case GameMode.Blitz:
				{
					NBlitzScreen submenuType3 = stack.GetSubmenuType<NBlitzScreen>();
					submenuType3.InitializeMultiplayerAsHost(netService, 4);
					stack.Push(submenuType3);
					break;
				}
				case GameMode.Dread:
				{
					NDreadScreen submenuType2 = stack.GetSubmenuType<NDreadScreen>();
					submenuType2.InitializeMultiplayerAsHost(netService, 4);
					stack.Push(submenuType2);
					break;
				}
				default:
					throw new InvalidOperationException();
				}
			}
			else
			{
				NErrorPopup nErrorPopup = NErrorPopup.Create(netErrorInfo.Value);
				if (nErrorPopup != null)
				{
					NModalContainer.Instance.Add(nErrorPopup);
				}
			}
		}
		catch
		{
			NErrorPopup nErrorPopup2 = NErrorPopup.Create(new NetErrorInfo(NetError.InternalError, selfInitiated: false));
			if (nErrorPopup2 != null)
			{
				NModalContainer.Instance.Add(nErrorPopup2);
			}
			throw;
		}
		finally
		{
			loadingOverlay.Visible = false;
		}
	}
}
