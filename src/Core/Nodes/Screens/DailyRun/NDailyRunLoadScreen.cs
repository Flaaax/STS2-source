using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

/// <summary>
/// Screen that displays when loading a multiplayer daily run, allowing other players to join the game before it starts.
/// </summary>
public partial class NDailyRunLoadScreen : NSubmenu, ILoadRunLobbyListener
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/daily_run/daily_run_load_screen");

	private static readonly LocString _ascensionLoc = new LocString("main_menu_ui", "DAILY_RUN_MENU.ASCENSION");

	public static readonly string dateFormat = LocManager.Instance.GetTable("main_menu_ui").GetRawText("DAILY_RUN_MENU.DATE_FORMAT");

	private MegaRichTextLabel _dateLabel;

	private NConfirmButton _embarkButton;

	private NBackButton _backButton;

	private NBackButton _unreadyButton;

	private NDailyRunCharacterContainer _characterContainer;

	private NDailyRunLeaderboard _leaderboard;

	private MegaLabel _modifiersTitleLabel;

	private Control _modifiersContainer;

	private readonly List<NDailyRunScreenModifier> _modifierContainers = new List<NDailyRunScreenModifier>();

	private NRemoteLoadLobbyPlayerContainer _remotePlayerContainer;

	private Control _readyAndWaitingContainer;

	private LoadRunLobby? _lobby;

	public static string[] AssetPaths => new string[1] { _scenePath };

	protected override Control? InitialFocusedControl => null;

	public static NDailyRunLoadScreen? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NDailyRunLoadScreen>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		ConnectSignals();
		_embarkButton = GetNode<NConfirmButton>("%ConfirmButton");
		_backButton = GetNode<NBackButton>("%BackButton");
		_unreadyButton = GetNode<NBackButton>("%UnreadyButton");
		_dateLabel = GetNode<MegaRichTextLabel>("%Date");
		_leaderboard = GetNode<NDailyRunLeaderboard>("%Leaderboards");
		_modifiersTitleLabel = GetNode<MegaLabel>("%ModifiersLabel");
		_modifiersContainer = GetNode<Control>("%ModifiersContainer");
		_characterContainer = GetNode<NDailyRunCharacterContainer>("ChallengeContainer/CenterContainer/HBoxContainer/CharacterContainer");
		_remotePlayerContainer = GetNode<NRemoteLoadLobbyPlayerContainer>("%RemotePlayerLoadContainer");
		_readyAndWaitingContainer = GetNode<Control>("%ReadyAndWaitingPanel");
		foreach (NDailyRunScreenModifier item in _modifiersContainer.GetChildren().OfType<NDailyRunScreenModifier>())
		{
			_modifierContainers.Add(item);
		}
		_readyAndWaitingContainer.Visible = false;
		_embarkButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnEmbarkPressed));
		_unreadyButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnUnreadyPressed));
		_unreadyButton.Disable();
		_leaderboard.Cleanup();
	}

	public void InitializeAsHost(INetGameService gameService, SerializableRun run)
	{
		if (gameService.Type != NetGameType.Host)
		{
			throw new InvalidOperationException($"Initialized daily run load screen with net service of type {gameService.Type} when hosting!");
		}
		_lobby = new LoadRunLobby(gameService, this, run);
		try
		{
			_lobby.AddLocalHostPlayer();
			AfterMultiplayerStarted();
		}
		catch
		{
			CleanUpLobby(disconnectSession: true, NetError.InternalError);
			throw;
		}
	}

	public void InitializeAsClient(INetGameService gameService, ClientLoadJoinResponseMessage message)
	{
		if (gameService.Type != NetGameType.Client)
		{
			throw new InvalidOperationException($"Initialized daily run load screen with net service of type {gameService.Type} when joining!");
		}
		_lobby = new LoadRunLobby(gameService, this, message);
		AfterMultiplayerStarted();
	}

	public override void OnSubmenuOpened()
	{
		base.OnSubmenuOpened();
		_leaderboard.Initialize(_lobby.Run.DailyTime.Value, _lobby.Run.Players.Select((SerializablePlayer p) => p.NetId), allowPagination: true);
		_embarkButton.Enable();
		_remotePlayerContainer.Initialize(_lobby, displayLocalPlayer: false);
	}

	public override void OnSubmenuClosed()
	{
		_embarkButton.Disable();
		_remotePlayerContainer.Cleanup();
		_leaderboard.Cleanup();
		LoadRunLobby? lobby = _lobby;
		if (lobby != null && lobby.NetService.Type.IsMultiplayer())
		{
			PlatformUtil.SetRichPresence("MAIN_MENU", null, null);
		}
		CleanUpLobby(disconnectSession: true);
	}

	private void InitializeDisplay()
	{
		if (_lobby == null)
		{
			throw new InvalidOperationException("Tried to initialize daily run display before lobby was initialized!");
		}
		_ascensionLoc.Add("ascension", _lobby.Run.Ascension);
		DateTimeOffset value = _lobby.Run.DailyTime.Value;
		SerializablePlayer serializablePlayer = _lobby.Run.Players.First((SerializablePlayer p) => p.NetId == _lobby.NetService.NetId);
		CharacterModel byId = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId);
		_characterContainer.Fill(byId, serializablePlayer.NetId, _lobby.Run.Ascension, _lobby.NetService);
		_dateLabel.SetTextAutoSize(value.ToString(dateFormat));
		_embarkButton.Enable();
		for (int num = 0; num < _lobby.Run.Modifiers.Count; num++)
		{
			ModifierModel modifier = ModifierModel.FromSerializable(_lobby.Run.Modifiers[num]);
			_modifierContainers[num].Fill(modifier);
		}
	}

	private void OnEmbarkPressed(NButton _)
	{
		_embarkButton.Disable();
		_backButton.Disable();
		_lobby.SetReady(ready: true);
		if (!_lobby.IsAboutToBeginGame())
		{
			_readyAndWaitingContainer.Visible = true;
			_unreadyButton.Enable();
		}
	}

	private void OnUnreadyPressed(NButton _)
	{
		_embarkButton.Enable();
		_unreadyButton.Disable();
		_backButton.Enable();
		_lobby.SetReady(ready: false);
		_readyAndWaitingContainer.Visible = false;
	}

	private void UpdateRichPresence()
	{
		LoadRunLobby? lobby = _lobby;
		if (lobby != null && lobby.NetService.Type.IsMultiplayer())
		{
			PlatformUtil.SetRichPresence("LOADING_MP_LOBBY", _lobby.NetService.GetRawLobbyIdentifier(), _lobby.PlayerCount);
		}
	}

	public override void _Process(double delta)
	{
		LoadRunLobby? lobby = _lobby;
		if (lobby != null && lobby.NetService.IsConnected)
		{
			_lobby.NetService.Update();
		}
	}

	private void CleanUpLobby(bool disconnectSession, NetError error = NetError.Quit)
	{
		if (_lobby != null)
		{
			_lobby.PlayerFailedToConnect -= RemoteClientFailedToConnectToLocalHost;
			_lobby.CleanUp(disconnectSession, error);
			_lobby = null;
		}
	}

	public async Task<bool> ShouldAllowRunToBegin()
	{
		if (_lobby.PlayerCount >= _lobby.Run.Players.Count)
		{
			return true;
		}
		LocString locString = new LocString("gameplay_ui", "CONFIRM_LOAD_SAVE.body");
		locString.Add("MissingCount", _lobby.Run.Players.Count - _lobby.PlayerCount);
		NGenericPopup nGenericPopup = NGenericPopup.Create();
		NModalContainer.Instance.Add(nGenericPopup);
		return await nGenericPopup.WaitForConfirmation(locString, new LocString("gameplay_ui", "CONFIRM_LOAD_SAVE.header"), new LocString("gameplay_ui", "CONFIRM_LOAD_SAVE.cancel"), new LocString("gameplay_ui", "CONFIRM_LOAD_SAVE.confirm"));
	}

	private async Task StartRun()
	{
		try
		{
			Log.Info("Loading a multiplayer run. Players: " + string.Join(",", _lobby.PlayerIds) + ".");
			SerializablePlayer serializablePlayer = _lobby.Run.Players.First((SerializablePlayer p) => p.NetId == _lobby.NetService.NetId);
			SfxCmd.Play(ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId).CharacterTransitionSfx);
			await NGame.Instance.Transition.FadeOut(0.8f, ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId).CharacterSelectTransitionPath);
			RunState runState = RunState.FromSerializable(_lobby.Run);
			await RunManager.Instance.SetUpSavedMultiplayer(runState, _lobby);
			await NGame.Instance.LoadRun(runState, _lobby.Run.PreFinishedRoom);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception loading daily multiplayer run : {ex}");
			CleanUpLobby(disconnectSession: true, NetError.InternalError);
			await NGame.Instance.ReturnToMainMenuWithInternalError(ex);
			return;
		}
		CleanUpLobby(disconnectSession: false);
		await NGame.Instance.Transition.FadeIn();
	}

	public void PlayerConnected(LoadRunLobbyPlayer player)
	{
		Log.Info($"Player connected: {player.id}");
		_remotePlayerContainer.OnPlayerConnected(player.id);
		UpdateRichPresence();
	}

	public void PlayerReadyChanged(ulong playerId)
	{
		Log.Info($"Player ready changed: {playerId}");
		_remotePlayerContainer.OnPlayerChanged(playerId);
		if (playerId == _lobby.NetService.NetId && !_lobby.IsPlayerReady(playerId))
		{
			_embarkButton.Enable();
		}
		if (playerId == _lobby.NetService.NetId && _lobby.NetService.Type.IsMultiplayer())
		{
			_characterContainer.SetIsReady(_lobby.IsPlayerReady(playerId));
		}
	}

	public void RemotePlayerDisconnected(ulong playerId)
	{
		Log.Info($"Player disconnected: {playerId}");
		_remotePlayerContainer.OnPlayerDisconnected(playerId);
		UpdateRichPresence();
	}

	public void BeginRun()
	{
		NAudioManager.Instance?.StopMusic();
		_embarkButton.Disable();
		_unreadyButton.Disable();
		TaskHelper.RunSafely(StartRun());
	}

	public void LocalPlayerDisconnected(NetErrorInfo info)
	{
		if ((info.SelfInitiated && info.GetReason() == NetError.Quit) || !this.IsValid() || _stack == null)
		{
			return;
		}
		if (_stack.Peek() == this)
		{
			_stack.Pop();
		}
		if (TestMode.IsOff)
		{
			NErrorPopup nErrorPopup = NErrorPopup.Create(info);
			if (nErrorPopup != null)
			{
				NModalContainer.Instance.Add(nErrorPopup);
			}
		}
	}

	private void RemoteClientFailedToConnectToLocalHost(ClientConnectionFailedMessage message, ulong sender)
	{
		string formattedText = message.GetLocString(PeerVersionInfo.LocalDefault()).GetFormattedText();
		LocString locString = new LocString("main_menu_ui", "NETWORK_ERROR.HOST.PREFIX.body");
		locString.Add("playerName", PlatformUtil.GetPlayerName(_lobby.NetService.Platform, sender));
		locString.Add("info", formattedText);
		this.AddChildSafely(NFailedJoinVfx.Create(locString.GetFormattedText()));
	}

	private void AfterMultiplayerStarted()
	{
		NGame.Instance.RemoteCursorContainer.Initialize(_lobby.InputSynchronizer, _lobby.PlayerIds);
		NGame.Instance.ReactionContainer.InitializeNetworking(_lobby.NetService);
		_lobby.PlayerFailedToConnect += RemoteClientFailedToConnectToLocalHost;
		InitializeDisplay();
		UpdateRichPresence();
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Network] = LogLevel.Debug;
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Actions] = LogLevel.VeryDebug;
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.GameSync] = LogLevel.VeryDebug;
	}
}
