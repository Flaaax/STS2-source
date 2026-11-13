using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

/// <summary>
/// Pretty much a copy of the character select screen.
/// </summary>
public partial class NDreadScreen : NSubmenu, IStartRunLobbyListener, ICharacterSelectButtonDelegate
{
	public static readonly StringName shaderParameterBrightness = new StringName("brightness");

	public static readonly StringName shaderParameterContrast = new StringName("contrast");

	private NBackButton _backButton;

	private NConfirmButton _embarkButton;

	private Control _underConstruction;

	private NDreadNameText _nameText;

	private NCharacterSelectButton? _selectedButton;

	private CharacterModel? _rotatingDisplayCharacterOverride;

	private CharacterModel? _rotatingPostAnimationCharacterSelect;

	private CharacterModel? _rotatingCharacter;

	private NChainVisualization _chainVisualization;

	private Control _charButtonContainer;

	private Control _readyAndWaitingContainer;

	private NRemoteLobbyPlayerContainer _remotePlayerContainer;

	private NCharacterSelectButton _rotatingCharacterButton;

	private Control _rotatingImage;

	private NDreadBeginRotationTickbox _startRotationTickbox;

	private MegaRichTextLabel _startRotationLabel;

	private NBackButton _unreadyButton;

	private const string _sceneCharSelectButtonPath = "screens/char_select/char_select_button";

	private NDreadInfoButton _dreadInfoButton;

	private Control _bgContainer;

	private ShaderMaterial _grayscaleBg;

	private Control _scoreContainer;

	private MegaRichTextLabel _scoreLabels;

	private MegaRichTextLabel _scoreValues;

	private IBootstrapSettings? _settings;

	private StartRunLobby _lobby;

	private List<NCharacterSelectButton> _charSelectButtons = new List<NCharacterSelectButton>();

	private bool _skipNextCharSelectLogic;

	private bool _doingChainAnimation;

	private bool? _lastOpenWasSingleplayer;

	private Control? _overriddenFocusedControl;

	private const int _ascension = 10;

	public StartRunLobby Lobby => _lobby;

	protected override Control? InitialFocusedControl => _overriddenFocusedControl ?? _charButtonContainer.GetChild<Control>(0);

	public override void _Ready()
	{
		ConnectSignals();
		_underConstruction = GetNode<Control>("%UnderConstruction");
		_underConstruction.OffsetLeft = 24f;
		_underConstruction.OffsetTop = 82f;
		_nameText = GetNode<NDreadNameText>("%DreadNameText");
		_rotatingImage = GetNode<Control>("%RotatingImage");
		_startRotationTickbox = GetNode<NDreadBeginRotationTickbox>("%RotatingTickbox");
		_startRotationLabel = GetNode<MegaRichTextLabel>("%RotatingExplanation");
		_bgContainer = GetNode<Control>("%AnimatedBg");
		_grayscaleBg = (ShaderMaterial)GetNode<Control>("%GrayScaler").Material;
		_charButtonContainer = GetNode<Control>("%ButtonContainer");
		_remotePlayerContainer = GetNode<NRemoteLobbyPlayerContainer>("%RemotePlayerContainer");
		_readyAndWaitingContainer = GetNode<Control>("%ReadyAndWaitingPanel");
		GetNode<MegaRichTextLabel>("%WaitingForPlayers").Text = new LocString("main_menu_ui", "CHARACTER_SELECT.waitingForPlayers").GetFormattedText();
		_backButton = GetNode<NBackButton>("%BackButton");
		_unreadyButton = GetNode<NBackButton>("%UnreadyButton");
		_embarkButton = GetNode<NConfirmButton>("%ConfirmButton");
		_embarkButton.OverrideHotkeys(new string[1] { MegaInput.select });
		_embarkButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnEmbarkPressed));
		_unreadyButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnUnreadyPressed));
		_unreadyButton.Disable();
		_dreadInfoButton = GetNode<NDreadInfoButton>("%DreadInfoButton");
		_dreadInfoButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenDreadInfo));
		_chainVisualization = GetNode<NChainVisualization>("%DreadWinStreak");
		_scoreLabels = GetNode<MegaRichTextLabel>("%ScoreLabels");
		_scoreValues = GetNode<MegaRichTextLabel>("%ScoreValues");
		Tween tween = CreateTween();
		tween.TweenProperty(_rotatingImage, "rotation", (float)Math.PI * 2f, 10.0).From(0f).SetTrans(Tween.TransitionType.Linear);
		tween.SetLoops();
		_rotatingImage.Visible = false;
		InitCharacterButtons();
		Type type = BootstrapSettingsUtil.Get();
		if (type != null)
		{
			_settings = (IBootstrapSettings)Activator.CreateInstance(type);
			PreloadManager.Enabled = _settings.DoPreloading;
		}
	}

	public void InitializeSingleplayer()
	{
		_lobby = new StartRunLobby(GameMode.Dread, isCustom: false, new NetSingleplayerGameService(), this, 1);
		_lobby.AddLocalHostPlayer(new UnlockState(SaveManager.Instance.Progress), 0, SaveManager.Instance.Progress.MaxBlitzAscension);
		AfterInitialized();
	}

	public void InitializeMultiplayerAsHost(INetGameService gameService, int maxPlayers)
	{
		if (gameService.Type != NetGameType.Host)
		{
			throw new InvalidOperationException($"Initialized Dread with GameService of type {gameService.Type} when hosting!");
		}
		_lobby = new StartRunLobby(GameMode.Dread, isCustom: false, gameService, this, maxPlayers);
		_lobby.AddLocalHostPlayer(new UnlockState(SaveManager.Instance.Progress), 10, SaveManager.Instance.Progress.MaxBlitzAscension);
		AfterInitialized();
	}

	public void InitializeMultiplayerAsClient(INetGameService gameService, ClientLobbyJoinResponseMessage message)
	{
		if (gameService.Type != NetGameType.Client)
		{
			throw new InvalidOperationException($"Initialized character select screen with GameService of type {gameService.Type} when joining!");
		}
		_lobby = new StartRunLobby(GameMode.Dread, isCustom: false, gameService, this, -1);
		_lobby.InitializeFromMessage(message);
		AfterInitialized();
	}

	private void InitCharacterButtons()
	{
		foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
		{
			NCharacterSelectButton nCharacterSelectButton = SceneHelper.Instantiate<NCharacterSelectButton>("screens/char_select/char_select_button");
			_charButtonContainer.AddChildSafely(nCharacterSelectButton);
			nCharacterSelectButton.Init(allCharacter, this);
			_charSelectButtons.Add(nCharacterSelectButton);
		}
		_rotatingCharacterButton = SceneHelper.Instantiate<NCharacterSelectButton>("screens/char_select/char_select_button");
		_charButtonContainer.AddChildSafely(_rotatingCharacterButton);
		_charSelectButtons.Add(_rotatingCharacterButton);
		List<NCharacterSelectButton> list = _charSelectButtons.Where((NCharacterSelectButton c) => c.Visible).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			list[num].FocusNeighborTop = list[num].GetPath();
			list[num].FocusNeighborBottom = list[num].GetPath();
			NCharacterSelectButton nCharacterSelectButton2 = list[num];
			NodePath path;
			if (num <= 0)
			{
				path = list[list.Count - 1].GetPath();
			}
			else
			{
				path = list[num - 1].GetPath();
			}
			nCharacterSelectButton2.FocusNeighborLeft = path;
			list[num].FocusNeighborRight = ((num < list.Count - 1) ? list[num + 1].GetPath() : list[0].GetPath());
		}
	}

	public override void _Process(double delta)
	{
		if (_lobby != null && _lobby.NetService.IsConnected)
		{
			_lobby.NetService.Update();
		}
	}

	public void PlayerConnected(StartRunLobbyPlayer player)
	{
		_remotePlayerContainer.OnPlayerConnected(player);
		RefreshButtonSelectionForPlayer(player);
		UpdateRichPresence();
	}

	private void RefreshButtonSelectionForPlayer(StartRunLobbyPlayer player)
	{
		if (player.id == _lobby.LocalPlayer.id)
		{
			return;
		}
		CharacterModel character = player.character;
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			if (charSelectButton.RemoteSelectedPlayers.Contains(player.id) && character != charSelectButton.Character)
			{
				charSelectButton.OnRemotePlayerDeselected(player.id);
			}
			else if (character == charSelectButton.Character)
			{
				charSelectButton.OnRemotePlayerSelected(player.id);
			}
		}
	}

	public void PlayerChanged(StartRunLobbyPlayer player)
	{
		_remotePlayerContainer.OnPlayerChanged(player);
		RefreshButtonSelectionForPlayer(player);
	}

	public void AscensionChanged()
	{
	}

	public void SeedChanged()
	{
		throw new NotImplementedException("Seed should not be changed in Dread mode!");
	}

	public void ModifiersChanged()
	{
		throw new NotImplementedException("Modifiers should not be changed in Dread mode!");
	}

	public void GameModeChanged()
	{
		throw new NotImplementedException("Game mode should not be changed in Dread mode!");
	}

	public void MaxAscensionChanged()
	{
	}

	public void RemotePlayerDisconnected(StartRunLobbyPlayer player)
	{
		_remotePlayerContainer.OnPlayerDisconnected(player);
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			if (charSelectButton.RemoteSelectedPlayers.Contains(player.id) && player.character == charSelectButton.Character)
			{
				charSelectButton.OnRemotePlayerDeselected(player.id);
			}
		}
		UpdateRichPresence();
	}

	public void BeginRun(string seed, List<ActModel> acts, IReadOnlyList<ModifierModel> modifiers)
	{
		if (modifiers.Count > 0)
		{
			Log.Error("Modifiers list is not empty while starting a Dread run, ignoring!");
		}
		NAudioManager.Instance?.StopMusic();
		_embarkButton.Disable();
		_unreadyButton.Disable();
		_startRotationTickbox.Disable();
		if (_lobby.NetService.Type == NetGameType.Singleplayer)
		{
			TaskHelper.RunSafely(StartNewSingleplayerRun(seed, acts));
		}
		else
		{
			TaskHelper.RunSafely(StartNewMultiplayerRun(seed, acts));
		}
	}

	private async Task StartNewSingleplayerRun(string seed, List<ActModel> acts)
	{
		Log.Info("Embarking on a solo Dread run " + _lobby.LocalPlayer.character.Id.Entry + ". Seed: " + seed);
		Player player = _lobby.GeneratePlayers(seed)[0];
		player.ExtraFields.IsPlayingRotatingCharacter = _lobby.LocalPlayer.isRotatingDread;
		UpdateProgressForRotation(player.Character);
		try
		{
			SfxCmd.Play(player.Character.CharacterTransitionSfx);
			await NGame.Instance.Transition.FadeOut(0.8f, player.Character.CharacterSelectTransitionPath);
			await NGame.Instance.StartNewSingleplayerRun(player, shouldSave: true, acts, Array.Empty<ModifierModel>(), seed, GameMode.Dread, isCustomGame: false, 10);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception starting a solo Dread run: {ex}");
			CleanUpLobby(disconnectSession: true, NetError.InternalError);
			await NGame.Instance.ReturnToMainMenuWithInternalError(ex);
			return;
		}
		CleanUpLobby(disconnectSession: false);
	}

	private async Task StartNewMultiplayerRun(string seed, List<ActModel> acts)
	{
		Log.Info("Embarking on a multiplayer Dread run. Players: " + string.Join(",", _lobby.Players) + ". Seed: " + seed);
		List<Player> players = _lobby.GeneratePlayers(seed);
		Player player = players.First((Player p) => p.NetId == _lobby.LocalPlayer.id);
		UpdateProgressForRotation(player.Character);
		SfxCmd.Play(player.Character.CharacterTransitionSfx);
		await NGame.Instance.Transition.FadeOut(0.8f, player.Character.CharacterSelectTransitionPath);
		if (Bootstrapper.Settings?.BootstrapInMultiplayer ?? false)
		{
			using (new NetLoadingHandle(_lobby.NetService))
			{
				Bootstrapper bootstrapper = new Bootstrapper().WithActs(acts).WithPlayers(players).WithRunManagerSetup(delegate(RunState r, bool s)
				{
					RunManager.Instance.SetUpNewMultiplayer(r, _lobby, s);
				})
					.WithSeed(seed);
				try
				{
					await bootstrapper.SetupRun();
				}
				catch (Exception ex)
				{
					Log.Error($"Exception starting bootstrap multiplayer run : {ex}");
					CleanUpLobby(disconnectSession: true, NetError.InternalError);
					await NGame.Instance.ReturnToMainMenuWithInternalError(ex);
					return;
				}
				CleanUpLobby(disconnectSession: false);
				await bootstrapper.EnterFirstRoom();
			}
		}
		else
		{
			try
			{
				await NGame.Instance.StartNewMultiplayerRun(players, _lobby, shouldSave: true, acts, Array.Empty<ModifierModel>(), seed, 10);
			}
			catch (Exception ex2)
			{
				Log.Error($"Exception starting multiplayer run : {ex2}");
				CleanUpLobby(disconnectSession: true, NetError.InternalError);
				await NGame.Instance.ReturnToMainMenuWithInternalError(ex2);
				return;
			}
			CleanUpLobby(disconnectSession: false);
		}
	}

	/// <summary>
	/// Updates the progress state with the preferred Dread character and whether the "begin rotation" tickbox should
	/// be ticked on or not.
	/// This is called when a run is started (singleplayer or multiplayer).
	/// </summary>
	private void UpdateProgressForRotation(CharacterModel embarkingCharacter)
	{
		bool flag = !_lobby.NetService.Type.IsMultiplayer();
		bool flag2 = IsBeginningRotation();
		UpdatePreferredDreadCharacter();
		if (flag)
		{
			SaveManager.Instance.Progress.PreferredDreadRotatingTickboxOnSingleplayer = flag2 || _rotatingCharacterButton.IsSelected;
			if (flag2)
			{
				SaveManager.Instance.Progress.CurrentSingleplayerDreadStartingCharacterForRotation = embarkingCharacter.Id;
				SaveManager.Instance.Progress.PreferredDreadCharacter = ModelDb.GetId<RotatingCharacter>();
			}
		}
		else
		{
			SaveManager.Instance.Progress.PreferredDreadRotatingTickboxOnMultiplayer = flag2 || _rotatingCharacterButton.IsSelected;
			if (flag2)
			{
				SaveManager.Instance.Progress.CurrentMultiplayerDreadStartingCharacterForRotation = embarkingCharacter.Id;
				SaveManager.Instance.Progress.PreferredMultiplayerDreadCharacter = ModelDb.GetId<RotatingCharacter>();
			}
		}
		SaveManager.Instance.SaveProgressFile();
	}

	/// <summary>
	/// Updates the preferred Dread character to the one the local player is embarking with, so that it's selected the
	/// next time this screen opens. Does not save the progress file; the caller is responsible for that.
	/// </summary>
	private void UpdatePreferredDreadCharacter()
	{
		if (_selectedButton == null)
		{
			return;
		}
		ModelId id = _selectedButton.Character.Id;
		ProgressState progress = SaveManager.Instance.Progress;
		if (_lobby.NetService.Type == NetGameType.Singleplayer)
		{
			if (!(progress.PreferredDreadCharacter == id))
			{
				Log.Info($"Setting preferred Dread character to {id}");
				progress.PreferredDreadCharacter = id;
			}
		}
		else if (!(progress.PreferredMultiplayerDreadCharacter == id))
		{
			Log.Info($"Setting preferred multiplayer Dread character to {id}");
			progress.PreferredMultiplayerDreadCharacter = id;
		}
	}

	/// <summary>
	/// Checks if the player is beginning a rotating streak.
	/// This is false if the player is continuing a rotating streak.
	/// </summary>
	private bool IsBeginningRotation()
	{
		bool flag = _lobby.NetService.Type.IsMultiplayer();
		int currentRotatingDreadWinStreak = AscensionHelper.GetCurrentRotatingDreadWinStreak(flag);
		if (!flag && currentRotatingDreadWinStreak == 0)
		{
			return _startRotationTickbox.IsTicked;
		}
		return false;
	}

	public override void OnSubmenuClosed()
	{
		base.OnSubmenuClosed();
		_embarkButton.Disable();
		_remotePlayerContainer.Cleanup();
		if (_lobby.NetService.Type.IsMultiplayer())
		{
			PlatformUtil.SetRichPresence("MAIN_MENU", null, null);
		}
		CleanUpLobby(disconnectSession: true);
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

	/// <summary>
	/// Called when the player selects a Character! Also called when you open the character select screen.
	/// </summary>
	public void SelectCharacter(NCharacterSelectButton charSelectButton)
	{
		_selectedButton = charSelectButton;
		if (_skipNextCharSelectLogic)
		{
			_skipNextCharSelectLogic = false;
			return;
		}
		CharacterModel character = charSelectButton.Character;
		SetSelectedCharacterVisuals(character);
		InitChainForCharacter(character);
		RefreshDreadRecordsUi();
		foreach (NCharacterSelectButton charSelectButton2 in _charSelectButtons)
		{
			if (charSelectButton2 != _selectedButton)
			{
				charSelectButton2.Deselect();
			}
		}
		if (charSelectButton.IsLocked)
		{
			_embarkButton.Disable();
		}
		else if (!_doingChainAnimation)
		{
			_embarkButton.Enable();
		}
		bool flag = _lobby.NetService.Type == NetGameType.Singleplayer;
		bool isRotatingDread = character is RotatingCharacter || IsBeginningRotation();
		CharacterModel character2 = ((!(character is RotatingCharacter)) ? character : ((_rotatingPostAnimationCharacterSelect == null) ? AscensionHelper.GetNextRotatingDreadCharacter(!flag) : _rotatingPostAnimationCharacterSelect));
		_lobby.SetLocalCharacter(character2, isRotatingDread);
	}

	/// <summary>
	/// Does a subset of the character selection
	/// </summary>
	private void SetSelectedCharacterVisuals(CharacterModel characterModel)
	{
		bool flag = _lobby.NetService.Type == NetGameType.Singleplayer;
		int currentRotatingDreadWinStreak = AscensionHelper.GetCurrentRotatingDreadWinStreak(!flag);
		bool flag2 = characterModel is RotatingCharacter;
		_startRotationTickbox.Visible = flag && !flag2 && currentRotatingDreadWinStreak <= 0;
		if (_startRotationTickbox.Visible && !_doingChainAnimation)
		{
			_startRotationTickbox.Enable();
		}
		else
		{
			_startRotationTickbox.Disable();
		}
		_rotatingImage.Visible = flag2;
		_chainVisualization.Visible = !_startRotationTickbox.Visible || !_startRotationTickbox.IsTicked || _chainVisualization.IsBreaking;
		NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short, 90f);
		foreach (Node child2 in _bgContainer.GetChildren())
		{
			_bgContainer.RemoveChildSafely(child2);
			child2.QueueFreeSafely();
		}
		CharacterModel characterModel2;
		if (characterModel is RotatingCharacter)
		{
			characterModel2 = _rotatingDisplayCharacterOverride ?? _rotatingCharacter ?? characterModel;
			LocString locString = new LocString("main_menu_ui", "DREAD_ROTATION_CHARACTER");
			locString.Add("CharacterName", characterModel2.ShortTitle);
			_nameText.SetNameText(locString, characterModel2.DreadNameColor);
		}
		else
		{
			characterModel2 = characterModel;
			_nameText.SetNameText(characterModel2);
		}
		SfxCmd.Play(characterModel2.CharacterSelectSfx);
		Control child = PreloadManager.Cache.GetScene(characterModel2.CharacterSelectBg).Instantiate<Control>(PackedScene.GenEditState.Disabled);
		_bgContainer.AddChildSafely(child);
		_grayscaleBg.SetShaderParameter(shaderParameterBrightness, characterModel2.DreadBgBrightness);
		_grayscaleBg.SetShaderParameter(shaderParameterContrast, characterModel2.DreadBgContrast);
	}

	public void BeginRotationTickboxChanged(bool beginRotation)
	{
		bool flag = !_lobby.NetService.Type.IsMultiplayer();
		if (!beginRotation && _selectedButton != null)
		{
			CharacterStats statsForCharacter = SaveManager.Instance.Progress.GetStatsForCharacter(_selectedButton.Character.Id);
			int count = ((!flag) ? (statsForCharacter?.CurrentMultiplayerDreadWinStreak ?? 0) : (statsForCharacter?.CurrentSingleplayerDreadWinStreak ?? 0));
			_chainVisualization.Initialize(count);
		}
		RefreshDreadRecordsUi();
		_chainVisualization.Visible = !beginRotation || _chainVisualization.IsBreaking;
		if (_selectedButton != null && _lobby.LocalPlayer.isRotatingDread != beginRotation)
		{
			_lobby.SetLocalCharacter(_selectedButton.Character, beginRotation);
		}
	}

	private void OnEmbarkPressed(NButton _)
	{
		_embarkButton.Disable();
		_startRotationTickbox.Disable();
		_lobby.SetReady(ready: true);
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			charSelectButton.Disable();
		}
		_backButton.Disable();
		if (_lobby.NetService.Type.IsMultiplayer() && !_lobby.IsAboutToBeginGame())
		{
			_readyAndWaitingContainer.Visible = true;
			_unreadyButton.Enable();
		}
	}

	/// <summary>
	/// Is called after the screen is opened.
	/// </summary>
	private void AfterInitialized()
	{
		NGame.Instance.RemoteCursorContainer.Initialize(_lobby.InputSynchronizer, _lobby.Players.Select((StartRunLobbyPlayer p) => p.id));
		NGame.Instance.ReactionContainer.InitializeNetworking(_lobby.NetService);
		NGame.Instance.TimeoutOverlay.Initialize(_lobby.NetService, isGameLevel: true);
		_lobby.PlayerFailedToConnect += RemoteClientFailedToConnectToLocalHost;
		UpdateRichPresence();
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Network] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.Debug);
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Actions] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.VeryDebug);
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.GameSync] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.VeryDebug);
		if (_lobby.NetService.Type != NetGameType.Singleplayer)
		{
			IBootstrapSettings? settings = _settings;
			if (settings != null && settings.BootstrapInMultiplayer)
			{
				NGame.Instance.DebugSeedOverride = _settings.Seed;
				return;
			}
		}
		NGame.Instance.DebugSeedOverride = null;
	}

	private void InitRotatingCharacterButton()
	{
		if (_lobby.NetService.Type.IsMultiplayer())
		{
			_rotatingCharacterButton.Visible = false;
		}
		else if (_rotatingDisplayCharacterOverride != null)
		{
			_rotatingCharacterButton.Visible = true;
			_rotatingCharacterButton.InitAsRotating(_rotatingDisplayCharacterOverride, this);
		}
		else if (_rotatingCharacter != null)
		{
			_rotatingCharacterButton.Visible = true;
			_rotatingCharacterButton.InitAsRotating(_rotatingCharacter, this);
		}
		else
		{
			_rotatingCharacterButton.Visible = false;
		}
	}

	/// <summary>
	/// Called when the player presses the unready button in multiplayer. You can't unready in singleplayer.
	/// </summary>
	private void OnUnreadyPressed(NButton _)
	{
		_lobby.SetReady(ready: false);
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			charSelectButton.Enable();
		}
		_selectedButton?.TryGrabFocus();
		_readyAndWaitingContainer.Visible = false;
		_embarkButton.Enable();
		_backButton.Enable();
		_startRotationTickbox.Enable();
		_unreadyButton.Disable();
	}

	private void OpenDreadInfo(NButton _)
	{
		this.AddChildSafely(NDreadInfoDisclaimer.Create(isDelayedOpen: false));
	}

	private void UpdateRichPresence()
	{
		if (_lobby.NetService.Type.IsMultiplayer())
		{
			PlatformUtil.SetRichPresence("DREAD_MP_LOBBY", _lobby.NetService.GetRawLobbyIdentifier(), _lobby.Players.Count);
		}
	}

	public override void OnSubmenuOpened()
	{
		base.OnSubmenuOpened();
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			if (!charSelectButton.IsLocked)
			{
				charSelectButton.Enable();
			}
			else
			{
				charSelectButton.UnlockIfPossible();
			}
			charSelectButton.Reset();
		}
		_rotatingCharacter = null;
		_selectedButton = null;
		_rotatingDisplayCharacterOverride = null;
		_rotatingPostAnimationCharacterSelect = null;
		_embarkButton.Enable();
		bool flag = _lobby.NetService.Type == NetGameType.Singleplayer;
		ModelId preferredCharacterId = (flag ? SaveManager.Instance.Progress.PreferredDreadCharacter : SaveManager.Instance.Progress.PreferredMultiplayerDreadCharacter);
		PendingDreadWinStreakResult pendingDreadWinStreakResult = (flag ? SaveManager.Instance.Progress.PendingSingleplayerDreadWinStreakResult : SaveManager.Instance.Progress.PendingMultiplayerDreadWinStreakResult);
		bool isTicked = (flag ? SaveManager.Instance.Progress.PreferredDreadRotatingTickboxOnSingleplayer : SaveManager.Instance.Progress.PreferredDreadRotatingTickboxOnMultiplayer);
		int currentRotatingDreadWinStreak = AscensionHelper.GetCurrentRotatingDreadWinStreak(!flag);
		if (pendingDreadWinStreakResult?.CharacterId == ModelDb.GetId<RotatingCharacter>())
		{
			ModelId startingCharacter = ((pendingDreadWinStreakResult.InitialRotatingCharacterBeforeReset == ModelId.none) ? ModelDb.GetId<Ironclad>() : pendingDreadWinStreakResult.InitialRotatingCharacterBeforeReset);
			ModelId rotatingCharacterIdForStreak = AscensionHelper.GetRotatingCharacterIdForStreak(startingCharacter, pendingDreadWinStreakResult.WinStreakBeforeReset + 1);
			if (pendingDreadWinStreakResult.WasReset && pendingDreadWinStreakResult.WinStreakBeforeReset == 0)
			{
				preferredCharacterId = rotatingCharacterIdForStreak;
				if (flag)
				{
					SaveManager.Instance.Progress.PreferredDreadCharacter = rotatingCharacterIdForStreak;
				}
				else
				{
					SaveManager.Instance.Progress.PreferredMultiplayerDreadCharacter = rotatingCharacterIdForStreak;
				}
				SaveManager.Instance.SaveProgressFile();
			}
			else
			{
				_rotatingDisplayCharacterOverride = ModelDb.GetById<CharacterModel>(AscensionHelper.GetRotatingCharacterIdForStreak(startingCharacter, pendingDreadWinStreakResult.WinStreakBeforeReset));
				_rotatingPostAnimationCharacterSelect = ModelDb.GetById<CharacterModel>(rotatingCharacterIdForStreak);
			}
		}
		if (currentRotatingDreadWinStreak > 0)
		{
			ModelId nextRotatingDreadCharacterId = AscensionHelper.GetNextRotatingDreadCharacterId(!flag);
			_rotatingCharacter = ModelDb.GetById<CharacterModel>(nextRotatingDreadCharacterId);
		}
		if (flag != _lastOpenWasSingleplayer)
		{
			_startRotationTickbox.InitTickedState(isTicked);
		}
		_startRotationTickbox.Enable();
		InitRotatingCharacterButton();
		NCharacterSelectButton overriddenFocusedControl = _charSelectButtons.FirstOrDefault((NCharacterSelectButton button) => !button.IsLocked && button.Visible && button.Character.Id == preferredCharacterId);
		_overriddenFocusedControl = overriddenFocusedControl;
		_remotePlayerContainer.Visible = _lobby.NetService.Type != NetGameType.Singleplayer;
		_remotePlayerContainer.Initialize(_lobby, displayLocalPlayer: true);
		_readyAndWaitingContainer.Visible = false;
		_underConstruction.Visible = !_remotePlayerContainer.Visible;
		if (!SaveManager.Instance.SeenPopup("dread_disclaimer"))
		{
			this.AddChildSafely(NDreadInfoDisclaimer.Create(isDelayedOpen: true));
		}
		foreach (StartRunLobbyPlayer player in _lobby.Players)
		{
			RefreshButtonSelectionForPlayer(player);
		}
		NControllerManager? instance = NControllerManager.Instance;
		if (instance != null && !instance.IsUsingDirectionalNavigation)
		{
			Callable.From(delegate
			{
				(InitialFocusedControl as NCharacterSelectButton)?.Select();
			}).CallDeferred();
		}
		_lastOpenWasSingleplayer = flag;
	}

	private async Task PlayDreadWinStreakResultAnimation(int currentStreak, PendingDreadWinStreakResult winStreakResult, bool isSingleplayer)
	{
		_embarkButton.Disable();
		_backButton.Disable();
		foreach (NCharacterSelectButton charSelectButton in _charSelectButtons)
		{
			charSelectButton.Disable();
		}
		try
		{
			_doingChainAnimation = true;
			if (winStreakResult.WasReset)
			{
				_chainVisualization.Initialize(winStreakResult.WinStreakBeforeReset);
				await _chainVisualization.Break(0.5f);
			}
			else if (winStreakResult.WinStreakIncrease > 0)
			{
				_chainVisualization.Initialize(currentStreak);
				TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
				TaskHelper.RunSafely(_chainVisualization.AnimateInLastLink(0.5f, taskCompletionSource));
				await taskCompletionSource.Task;
			}
		}
		finally
		{
			_doingChainAnimation = false;
			if (isSingleplayer)
			{
				SaveManager.Instance.Progress.PendingSingleplayerDreadWinStreakResult = null;
			}
			else
			{
				SaveManager.Instance.Progress.PendingMultiplayerDreadWinStreakResult = null;
			}
			if (winStreakResult.CharacterId == ModelDb.GetId<RotatingCharacter>())
			{
				_rotatingDisplayCharacterOverride = null;
				CharacterModel nextCharacter = _rotatingPostAnimationCharacterSelect ?? ModelDb.Character<Ironclad>();
				if (winStreakResult.WasReset)
				{
					_skipNextCharSelectLogic = true;
					_charSelectButtons.FirstOrDefault((NCharacterSelectButton b) => b.Character == nextCharacter)?.Select();
					_rotatingCharacterButton.Deselect();
					if (isSingleplayer)
					{
						SaveManager.Instance.Progress.PreferredDreadCharacter = nextCharacter.Id;
					}
					else
					{
						SaveManager.Instance.Progress.PreferredMultiplayerDreadCharacter = nextCharacter.Id;
					}
					SetSelectedCharacterVisuals(nextCharacter);
				}
				else
				{
					SetSelectedCharacterVisuals(ModelDb.Character<RotatingCharacter>());
				}
				_rotatingPostAnimationCharacterSelect = null;
				InitRotatingCharacterButton();
			}
			SaveManager.Instance.SaveProgressFile();
			foreach (NCharacterSelectButton charSelectButton2 in _charSelectButtons)
			{
				charSelectButton2.Enable();
			}
			if (_startRotationTickbox.Visible)
			{
				_startRotationTickbox.Enable();
			}
			await Cmd.Wait(0.5f);
			_embarkButton.Enable();
			_backButton.Enable();
		}
	}

	private void InitChainForCharacter(CharacterModel characterModel)
	{
		bool flag = _lobby.NetService.Type == NetGameType.Singleplayer;
		ProgressState progress = SaveManager.Instance.Progress;
		CharacterStats statsForCharacter = progress.GetStatsForCharacter(characterModel.Id);
		int num = ((!flag) ? (statsForCharacter?.CurrentMultiplayerDreadWinStreak ?? 0) : (statsForCharacter?.CurrentSingleplayerDreadWinStreak ?? 0));
		PendingDreadWinStreakResult pendingDreadWinStreakResult = (flag ? progress.PendingSingleplayerDreadWinStreakResult : progress.PendingMultiplayerDreadWinStreakResult);
		if (pendingDreadWinStreakResult != null && pendingDreadWinStreakResult.WasReset && pendingDreadWinStreakResult.WinStreakBeforeReset == 0)
		{
			if (flag)
			{
				SaveManager.Instance.Progress.PendingSingleplayerDreadWinStreakResult = null;
			}
			else
			{
				SaveManager.Instance.Progress.PendingMultiplayerDreadWinStreakResult = null;
			}
			pendingDreadWinStreakResult = null;
		}
		if (pendingDreadWinStreakResult == null || pendingDreadWinStreakResult.CharacterId != characterModel.Id)
		{
			_chainVisualization.Initialize(num);
			_chainVisualization.Visible = !_startRotationTickbox.Visible || !_startRotationTickbox.IsTicked || _chainVisualization.IsBreaking;
		}
		else
		{
			TaskHelper.RunSafely(PlayDreadWinStreakResultAnimation(num, pendingDreadWinStreakResult, flag));
		}
	}

	private void RefreshDreadRecordsUi()
	{
		if (_selectedButton == null)
		{
			return;
		}
		Log.Info("Updating Dread Records ui...");
		CharacterModel characterModel = (IsBeginningRotation() ? ModelDb.Character<RotatingCharacter>() : _selectedButton.Character);
		bool flag = _lobby.NetService.Type == NetGameType.Singleplayer;
		CharacterStats statsForCharacter = SaveManager.Instance.Progress.GetStatsForCharacter(characterModel.Id);
		string gameVersion = NGame.GetGameVersion();
		int num;
		int num2;
		string variable;
		if (flag)
		{
			(num, num2, variable) = ProgressSaveManager.ReconcileDreadWinStreakVersion(gameVersion, statsForCharacter?.BestSingleplayerDreadWinStreakThisVersion ?? 0, statsForCharacter?.BestSingleplayerDreadWinStreakThisVersionTag, statsForCharacter?.BestSingleplayerDreadWinStreakOldVersion ?? 0, statsForCharacter?.BestSingleplayerDreadWinStreakOldVersionTag);
		}
		else
		{
			(num, num2, variable) = ProgressSaveManager.ReconcileDreadWinStreakVersion(gameVersion, statsForCharacter?.BestMultiplayerDreadWinStreakThisVersion ?? 0, statsForCharacter?.BestMultiplayerDreadWinStreakThisVersionTag, statsForCharacter?.BestMultiplayerDreadWinStreakOldVersion ?? 0, statsForCharacter?.BestMultiplayerDreadWinStreakOldVersionTag);
		}
		if (num == 0 && num2 == 0)
		{
			_scoreLabels.Text = "";
			_scoreValues.Text = "";
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (num > 0)
		{
			LocString locString = new LocString("main_menu_ui", (characterModel is RotatingCharacter) ? "DREAD_BEST_ROTATING_STREAK" : "DREAD_BEST_STREAK");
			list.Add(locString.GetFormattedText());
			list2.Add(num.ToString());
		}
		if (num2 > num)
		{
			LocString locString2 = new LocString("main_menu_ui", (characterModel is RotatingCharacter) ? "DREAD_BEST_ROTATING_STREAK_OLD" : "DREAD_BEST_STREAK_OLD");
			locString2.Add("Version", variable);
			list.Add(locString2.GetFormattedText());
			list2.Add(num2.ToString());
		}
		_scoreLabels.Text = string.Join("\n", list);
		_scoreValues.Text = string.Join("\n", list2);
	}

	private void CleanUpLobby(bool disconnectSession, NetError error = NetError.Quit)
	{
		_lobby.PlayerFailedToConnect -= RemoteClientFailedToConnectToLocalHost;
		_lobby.CleanUp(disconnectSession, error);
		_lobby = null;
	}

	private void RemoteClientFailedToConnectToLocalHost(ulong sender, NetErrorInfo info)
	{
		bool showReportBugButton;
		string formattedText = NErrorPopup.LocStringFromNetError(info, out showReportBugButton).GetFormattedText();
		LocString locString = new LocString("main_menu_ui", "NETWORK_ERROR.HOST.PREFIX.body");
		locString.Add("playerName", PlatformUtil.GetPlayerName(_lobby.NetService.Platform, sender));
		locString.Add("info", formattedText);
		this.AddChildSafely(NFailedJoinVfx.Create(locString.GetFormattedText()));
	}
}
