using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Multiplayer.Game;

/// <summary>
/// Object which manages the join flow for the local player, who joins as a client.
/// </summary>
public class JoinFlow
{
	private TaskCompletionSource<InitialGameInfoMessage>? _connectCompletion;

	private TaskCompletionSource<ClientRejoinResponseMessage>? _rejoinCompletion;

	private TaskCompletionSource<ClientLoadJoinResponseMessage>? _loadJoinCompletion;

	private TaskCompletionSource<ClientLobbyJoinResponseMessage>? _joinCompletion;

	private readonly MegaCrit.Sts2.Core.Logging.Logger _logger = new MegaCrit.Sts2.Core.Logging.Logger("JoinFlow", LogType.Network);

	private readonly PeerVersionInfo? _mockInfo;

	public INetClientGameService NetService { get; }

	public CancellationTokenSource CancelToken { get; } = new CancellationTokenSource();

	public JoinFlow(INetClientGameService netService, PeerVersionInfo? mockInfo = null)
	{
		_mockInfo = mockInfo;
		NetService = netService;
	}

	/// <summary>
	/// Begins the join flow.
	/// This task returns either a JoinResult with the final message in the join flow, for use initializing the run or
	/// the Lobby in the case of a rejoin or a join, respectively.
	/// </summary>
	/// <param name="initializer">The object to use when initializing the connection.</param>
	/// <param name="sceneTree">Scene tree to use for hooking into update.
	/// If null, NetService.Update is NOT called and the caller needs to do it manually.</param>
	/// <throws>ClientConnectionFailedException if the join fails. In this case, you should not use NetService. An error
	/// should be shown to the user and they may try to join again.</throws>
	public async Task<JoinResult> Begin(IClientConnectionInitializer initializer, SceneTree? sceneTree)
	{
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Network] = LogLevel.Debug;
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Actions] = LogLevel.VeryDebug;
		MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.GameSync] = LogLevel.VeryDebug;
		if (_connectCompletion != null)
		{
			throw new InvalidOperationException("JoinFlow object can only be used once!");
		}
		_logger.Info($"Beginning join with initializer {initializer}");
		CancelToken.Token.Register(Cancel);
		CancellationTokenSource updateLoopCancelSource = new CancellationTokenSource();
		if (sceneTree != null)
		{
			TaskHelper.RunSafely(NetServiceUpdateLoop(updateLoopCancelSource, sceneTree));
		}
		JoinResult result;
		try
		{
			_ = 4;
			try
			{
				NetService.RegisterMessageHandler<InitialGameInfoMessage>(HandleInitialGameInfoMessage);
				NetService.RegisterMessageHandler<ClientLobbyJoinResponseMessage>(HandleJoinResponseMessage);
				NetService.RegisterMessageHandler<ClientLoadJoinResponseMessage>(HandleLoadJoinResponseMessage);
				NetService.RegisterMessageHandler<ClientRejoinResponseMessage>(HandleRejoinResponseMessage);
				NetService.Disconnected += OnDisconnected;
				_connectCompletion = new TaskCompletionSource<InitialGameInfoMessage>();
				NetErrorInfo? value = await initializer.Connect(NetService, CancelToken.Token);
				if (value.HasValue)
				{
					_logger.Info($"Connection failed: {value}");
					throw new ClientConnectionFailedException("Could not connect", ConnectionFailureReason.None, null);
				}
				_logger.Info("Initializer connection completed, awaiting initial game info message");
				InitialGameInfoMessage initialMessage = await _connectCompletion.Task;
				PeerVersionInfo versionInfo = initialMessage.versionInfo;
				PeerVersionInfo localInfo = _mockInfo ?? PeerVersionInfo.LocalDefault();
				ConnectionFailureExtraInfo connectionFailureExtraInfo = new ConnectionFailureExtraInfo
				{
					localInfo = localInfo,
					remoteInfo = versionInfo,
					localIsHost = false
				};
				if (initialMessage.connectionFailureReason.HasValue)
				{
					_logger.Info($"Received initial join message with failure: {initialMessage.connectionFailureReason}");
					throw new ClientConnectionFailedException("Got connection failure from host", initialMessage.connectionFailureReason.Value, connectionFailureExtraInfo);
				}
				RunSessionState state = initialMessage.sessionState;
				_logger.Info($"Got initial game info message. Version: {versionInfo.version} Hash: {versionInfo.idDatabaseHash} Type: {initialMessage.gameMode} State: {state}");
				if (versionInfo.version != localInfo.version)
				{
					throw new ClientConnectionFailedException($"Version mismatch. Host: {versionInfo.version} Ours: {localInfo.version} Host branch: {versionInfo.branch}", ConnectionFailureReason.VersionMismatch, connectionFailureExtraInfo);
				}
				List<string> missingModsOnRemote = connectionFailureExtraInfo.GetMissingModsOnRemote(nonGameplay: false);
				List<string> missingModsOnLocal = connectionFailureExtraInfo.GetMissingModsOnLocal(nonGameplay: false);
				if (missingModsOnLocal.Count > 0 || missingModsOnRemote.Count > 0)
				{
					_logger.Warn($"Mismatch in gameplay-relevant mods with the host!\nMods that host has that we don't: {string.Join(",", missingModsOnLocal)}.\nMods that we have that host doesn't: {string.Join(",", missingModsOnRemote)}.");
					throw new ClientConnectionFailedException("Mod mismatch. Host mods: " + string.Join(",", versionInfo.gameplayAffectingMods ?? new List<string>()) + " Local mods: " + string.Join(",", localInfo.gameplayAffectingMods ?? new List<string>()), ConnectionFailureReason.ModMismatch, connectionFailureExtraInfo);
				}
				if (versionInfo.idDatabaseHash != localInfo.idDatabaseHash)
				{
					_logger.Warn("Our version " + localInfo.version + " matches the host's, but our Model ID hash does not! Disconnecting");
					throw new ClientConnectionFailedException($"ModelDb hash mismatch. Host: {versionInfo.idDatabaseHash} Ours: {ModelIdSerializationCache.Hash}", ConnectionFailureReason.VersionMismatch, connectionFailureExtraInfo);
				}
				List<string> missingModsOnRemote2 = connectionFailureExtraInfo.GetMissingModsOnRemote(nonGameplay: true);
				List<string> missingModsOnLocal2 = connectionFailureExtraInfo.GetMissingModsOnLocal(nonGameplay: true);
				if (missingModsOnRemote2.Count > 0 || missingModsOnLocal2.Count > 0)
				{
					_logger.Warn($"Mismatch in non-gameplay relevant mods. This is allowed, but it's up to the mod authors to guarantee that it doesn't break anything.\nNon-gameplay relevant mods that host has that we don't: {string.Join(",", missingModsOnLocal2)}.\nNon-gameplay relevant mods that we have that host doesn't: {string.Join(",", missingModsOnRemote2)}.");
				}
				switch (state)
				{
				case RunSessionState.InLobby:
				{
					ClientLobbyJoinResponseMessage value4 = await AttemptJoin();
					result = new JoinResult
					{
						gameMode = initialMessage.gameMode,
						sessionState = state,
						joinResponse = value4
					};
					break;
				}
				case RunSessionState.InLoadedLobby:
				{
					ClientLoadJoinResponseMessage value3 = await AttemptLoadJoin();
					result = new JoinResult
					{
						gameMode = initialMessage.gameMode,
						sessionState = state,
						loadJoinResponse = value3
					};
					break;
				}
				case RunSessionState.Running:
				{
					ClientRejoinResponseMessage value2 = await AttemptRejoin();
					result = new JoinResult
					{
						gameMode = initialMessage.gameMode,
						sessionState = state,
						rejoinResponse = value2
					};
					break;
				}
				default:
					NetService.Disconnect(NetError.InternalError, now: true);
					throw new InvalidOperationException($"Received invalid state {state} from connection!");
				}
			}
			catch (Exception ex)
			{
				if (NetService.IsConnected)
				{
					NetError reason;
					if (ex is ClientConnectionFailedException ex2)
					{
						ClientConnectionFailedMessage message = new ClientConnectionFailedMessage
						{
							disconnectionReason = ex2.rawReason,
							versionInfo = (ex2.info.ConnectionExtraInfo?.localInfo ?? PeerVersionInfo.LocalDefault())
						};
						NetService.SendMessage(message);
						reason = ex2.info.GetReason();
					}
					else
					{
						reason = ((ex is OperationCanceledException) ? NetError.CancelledJoin : NetError.InternalError);
					}
					NetService.Disconnected -= OnDisconnected;
					SetDisconnectionException(ex);
					NetService.Disconnect(reason);
				}
				MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Network] = LogLevel.Info;
				MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Actions] = LogLevel.Info;
				MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.GameSync] = LogLevel.Info;
				throw;
			}
		}
		finally
		{
			await updateLoopCancelSource.CancelAsync();
			NetService.UnregisterMessageHandler<InitialGameInfoMessage>(HandleInitialGameInfoMessage);
			NetService.UnregisterMessageHandler<ClientLobbyJoinResponseMessage>(HandleJoinResponseMessage);
			NetService.UnregisterMessageHandler<ClientLoadJoinResponseMessage>(HandleLoadJoinResponseMessage);
			NetService.UnregisterMessageHandler<ClientRejoinResponseMessage>(HandleRejoinResponseMessage);
			NetService.Disconnected -= OnDisconnected;
		}
		return result;
	}

	private async Task NetServiceUpdateLoop(CancellationTokenSource token, SceneTree sceneTree)
	{
		while (!token.IsCancellationRequested)
		{
			try
			{
				NetService.Update();
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<ClientLobbyJoinResponseMessage> AttemptJoin()
	{
		_joinCompletion = new TaskCompletionSource<ClientLobbyJoinResponseMessage>();
		_logger.Info("Sending ClientLobbyJoinRequestMessage and waiting for response message");
		UnlockState unlockState = SaveManager.Instance.GenerateUnlockStateFromProgress();
		ClientLobbyJoinRequestMessage message = new ClientLobbyJoinRequestMessage
		{
			maxAscensionUnlocked = SaveManager.Instance.Progress.MaxMultiplayerAscension,
			unlockState = unlockState.ToSerializable(),
			versionInfo = PeerVersionInfo.LocalDefault()
		};
		NetService.SendMessage(message);
		ClientLobbyJoinResponseMessage clientLobbyJoinResponseMessage = await _joinCompletion.Task;
		_logger.Info($"Received {"ClientLobbyJoinResponseMessage"}: {clientLobbyJoinResponseMessage}");
		return clientLobbyJoinResponseMessage;
	}

	private async Task<ClientLoadJoinResponseMessage> AttemptLoadJoin()
	{
		_loadJoinCompletion = new TaskCompletionSource<ClientLoadJoinResponseMessage>();
		_logger.Info("Sending ClientLoadJoinRequestMessage and waiting for rejoin response message");
		ClientLoadJoinRequestMessage message = new ClientLoadJoinRequestMessage
		{
			versionInfo = PeerVersionInfo.LocalDefault()
		};
		NetService.SendMessage(message);
		ClientLoadJoinResponseMessage clientLoadJoinResponseMessage = await _loadJoinCompletion.Task;
		_logger.Info($"Received ClientLoadJoinResponseMessage: {clientLoadJoinResponseMessage}");
		return clientLoadJoinResponseMessage;
	}

	private async Task<ClientRejoinResponseMessage> AttemptRejoin()
	{
		_rejoinCompletion = new TaskCompletionSource<ClientRejoinResponseMessage>();
		_logger.Info("Sending ClientRequestRejoinMessage and waiting for rejoin response message");
		ClientRejoinRequestMessage message = new ClientRejoinRequestMessage
		{
			versionInfo = PeerVersionInfo.LocalDefault()
		};
		NetService.SendMessage(message);
		ClientRejoinResponseMessage clientRejoinResponseMessage = await _rejoinCompletion.Task;
		_logger.Info($"Received ClientRejoinResponseMessage: {clientRejoinResponseMessage}");
		return clientRejoinResponseMessage;
	}

	private void HandleInitialGameInfoMessage(InitialGameInfoMessage message, ulong _)
	{
		if (_connectCompletion == null || _connectCompletion.Task.IsCompleted)
		{
			_logger.Warn($"Received {"InitialGameInfoMessage"} when we weren't expecting it! Completion status: {_connectCompletion}");
		}
		else
		{
			_connectCompletion.SetResult(message);
		}
	}

	private void HandleRejoinResponseMessage(ClientRejoinResponseMessage message, ulong senderId)
	{
		if (_rejoinCompletion == null || _rejoinCompletion.Task.IsCompleted)
		{
			_logger.Warn($"Received {"ClientRejoinResponseMessage"} when we weren't expecting it! Completion status: {_connectCompletion}");
		}
		else
		{
			_rejoinCompletion.SetResult(message);
		}
	}

	private void HandleLoadJoinResponseMessage(ClientLoadJoinResponseMessage message, ulong senderId)
	{
		if (_loadJoinCompletion == null || _loadJoinCompletion.Task.IsCompleted)
		{
			_logger.Warn($"Received {"ClientLoadJoinResponseMessage"} when we weren't expecting it! Completion status: {_connectCompletion}");
		}
		else
		{
			_loadJoinCompletion.SetResult(message);
		}
	}

	private void HandleJoinResponseMessage(ClientLobbyJoinResponseMessage message, ulong senderId)
	{
		if (_joinCompletion == null || _joinCompletion.Task.IsCompleted)
		{
			_logger.Warn($"Received {"ClientLobbyJoinResponseMessage"} when we weren't expecting it! Completion status: {_connectCompletion}");
		}
		else
		{
			_joinCompletion.SetResult(message);
		}
	}

	private void OnDisconnected(NetErrorInfo info)
	{
		_logger.Info($"Disconnected during join flow, reason: {info.GetReason()}. Failing with an exception");
		ClientConnectionFailedException disconnectionException = new ClientConnectionFailedException($"Unexpectedly disconnected from host while joining. Reason: {info.GetReason()}", info);
		SetDisconnectionException(disconnectionException);
	}

	private void SetDisconnectionException(Exception exception)
	{
		TaskCompletionSource<InitialGameInfoMessage> connectCompletion = _connectCompletion;
		if (connectCompletion != null)
		{
			Task<InitialGameInfoMessage> task = connectCompletion.Task;
			if (task != null && !task.IsCompleted)
			{
				_connectCompletion.SetException(exception);
			}
		}
		TaskCompletionSource<ClientLobbyJoinResponseMessage> joinCompletion = _joinCompletion;
		if (joinCompletion != null)
		{
			Task<ClientLobbyJoinResponseMessage> task2 = joinCompletion.Task;
			if (task2 != null && !task2.IsCompleted)
			{
				_joinCompletion?.SetException(exception);
			}
		}
		TaskCompletionSource<ClientLoadJoinResponseMessage> loadJoinCompletion = _loadJoinCompletion;
		if (loadJoinCompletion != null)
		{
			Task<ClientLoadJoinResponseMessage> task3 = loadJoinCompletion.Task;
			if (task3 != null && !task3.IsCompleted)
			{
				_loadJoinCompletion?.SetException(exception);
			}
		}
		TaskCompletionSource<ClientRejoinResponseMessage> rejoinCompletion = _rejoinCompletion;
		if (rejoinCompletion != null)
		{
			Task<ClientRejoinResponseMessage> task4 = rejoinCompletion.Task;
			if (task4 != null && !task4.IsCompleted)
			{
				_rejoinCompletion?.SetException(exception);
			}
		}
	}

	private void Cancel()
	{
		TaskCompletionSource<InitialGameInfoMessage> connectCompletion = _connectCompletion;
		if (connectCompletion != null)
		{
			Task<InitialGameInfoMessage> task = connectCompletion.Task;
			if (task != null && !task.IsCompleted)
			{
				_connectCompletion.SetCanceled();
			}
		}
		TaskCompletionSource<ClientLobbyJoinResponseMessage> joinCompletion = _joinCompletion;
		if (joinCompletion != null)
		{
			Task<ClientLobbyJoinResponseMessage> task2 = joinCompletion.Task;
			if (task2 != null && !task2.IsCompleted)
			{
				_joinCompletion?.SetCanceled();
			}
		}
		TaskCompletionSource<ClientLoadJoinResponseMessage> loadJoinCompletion = _loadJoinCompletion;
		if (loadJoinCompletion != null)
		{
			Task<ClientLoadJoinResponseMessage> task3 = loadJoinCompletion.Task;
			if (task3 != null && !task3.IsCompleted)
			{
				_loadJoinCompletion?.SetCanceled();
			}
		}
		TaskCompletionSource<ClientRejoinResponseMessage> rejoinCompletion = _rejoinCompletion;
		if (rejoinCompletion != null)
		{
			Task<ClientRejoinResponseMessage> task4 = rejoinCompletion.Task;
			if (task4 != null && !task4.IsCompleted)
			{
				_rejoinCompletion?.SetCanceled();
			}
		}
	}
}
