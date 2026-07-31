using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// A popup to let the player know a terrible bug has occurred, and to upload a bug report.
/// The wording should be changed before we release.
/// Renders above the capstone screens (above top bar).
/// </summary>
public partial class NErrorPopup : NVerticalPopup, IScreenContext
{
	private NVerticalPopup _verticalPopup;

	private string _title;

	private string _body;

	private LocString? _cancel;

	private bool _showReportBugButton;

	private static readonly string _scenePath = SceneHelper.GetScenePath("ui/error_popup");

	public Control? DefaultFocusedControl => null;

	public new static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public override void _Ready()
	{
		_verticalPopup = GetNode<NVerticalPopup>("VerticalPopup");
		_verticalPopup.SetText(_title, _body);
		if (_showReportBugButton)
		{
			_verticalPopup.InitYesButton(new LocString("main_menu_ui", "NETWORK_ERROR.report_bug"), OnReportBugButtonPressed);
		}
		else
		{
			_verticalPopup.InitYesButton(new LocString("main_menu_ui", "GENERIC_POPUP.ok"), OnOkButtonPressed);
		}
		if (_cancel != null)
		{
			_verticalPopup.InitNoButton(_cancel, OnCancelButtonPressed);
		}
		else
		{
			_verticalPopup.HideNoButton();
		}
	}

	public static NErrorPopup? Create(NetErrorInfo info)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		if (info.SelfInitiated && info.GetReason() == NetError.Quit)
		{
			return null;
		}
		bool showReportBugButton;
		return Create(new LocString("main_menu_ui", "NETWORK_ERROR.header"), LocStringFromNetError(info, out showReportBugButton), null, showReportBugButton);
	}

	public static NErrorPopup? Create(LocString title, LocString body, LocString? cancel, bool showReportBugButton)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NErrorPopup nErrorPopup = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NErrorPopup>(PackedScene.GenEditState.Disabled);
		nErrorPopup._title = title.GetFormattedText();
		nErrorPopup._body = body.GetFormattedText();
		nErrorPopup._showReportBugButton = showReportBugButton;
		nErrorPopup._cancel = cancel;
		return nErrorPopup;
	}

	/// <summary>
	/// Creates an error popup with hardcoded English text (bypassing localization).
	/// Use this when localization may be broken (e.g., showing localization errors).
	/// </summary>
	public static NErrorPopup? Create(string title, string body, bool showReportBugButton)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NErrorPopup nErrorPopup = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NErrorPopup>(PackedScene.GenEditState.Disabled);
		nErrorPopup._title = title;
		nErrorPopup._body = body;
		nErrorPopup._showReportBugButton = showReportBugButton;
		return nErrorPopup;
	}

	public static LocString LocStringFromNetError(NetErrorInfo info, out bool showReportBugButton)
	{
		NetError reason = info.GetReason();
		LocString locString = GetLocStringForReason(reason, info.ConnectionExtraInfo?.localIsHost ?? false);
		if (locString == null)
		{
			ConnectionFailureExtraInfo? connectionExtraInfo = info.ConnectionExtraInfo;
			if ((object)connectionExtraInfo != null && connectionExtraInfo.localIsHost)
			{
				locString = GetLocStringForReason(reason, isHost: false);
			}
		}
		bool flag = !info.IsModded;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = ((reason == NetError.None || reason == NetError.StateDivergence || (uint)(reason - 17) <= 1u) ? true : false);
			flag2 = flag3;
		}
		showReportBugButton = flag2;
		if (locString == null)
		{
			Log.Error($"Invalid net error passed to {"NErrorPopup"}: {info}!");
			locString = new LocString("main_menu_ui", "NETWORK_ERROR.INTERNAL_ERROR.body");
			showReportBugButton = !info.IsModded;
		}
		locString.Add("info", info.GetErrorString());
		return locString;
	}

	public static LocString? GetLocStringForReason(NetError reason, bool isHost)
	{
		string text = (isHost ? "NETWORK_ERROR.HOST." : "NETWORK_ERROR.");
		string text2 = default(string);
		switch (reason)
		{
		case NetError.None:
			text2 = null;
			break;
		case NetError.QuitGameOver:
			text2 = null;
			break;
		case NetError.CancelledJoin:
			text2 = null;
			break;
		case NetError.LobbyFull:
			text2 = "LOBBY_FULL.body";
			break;
		case NetError.Quit:
			text2 = "QUIT.body";
			break;
		case NetError.HostAbandoned:
			text2 = "HOST_ABANDONED.body";
			break;
		case NetError.Kicked:
			text2 = "KICKED.body";
			break;
		case NetError.InvalidJoin:
			text2 = "INVALID_JOIN.body";
			break;
		case NetError.RunInProgress:
			text2 = "RUN_IN_PROGRESS.body";
			break;
		case NetError.StateDivergence:
			text2 = "STATE_DIVERGENCE.body";
			break;
		case NetError.ModMismatch:
			text2 = "MOD_MISMATCH.body";
			break;
		case NetError.JoinBlockedByUser:
			text2 = "JOIN_BLOCKED_BY_USER.body";
			break;
		case NetError.NoInternet:
			text2 = "NO_INTERNET.body";
			break;
		case NetError.Timeout:
			text2 = "TIMEOUT.body";
			break;
		case NetError.HandshakeTimeout:
			text2 = "TIMEOUT.body";
			break;
		case NetError.InternalError:
			text2 = "INTERNAL_ERROR.body";
			break;
		case NetError.UnknownNetworkError:
			text2 = "UNKNOWN_ERROR.body";
			break;
		case NetError.RateLimited:
			text2 = "RATE_LIMITED.body";
			break;
		case NetError.TryAgainLater:
			text2 = "TRY_AGAIN_LATER.body";
			break;
		case NetError.SecureConnectionFailed:
			text2 = "SECURE_CONNECTION_FAILED.body";
			break;
		case NetError.FailedToHost:
			text2 = "FAILED_TO_HOST.body";
			break;
		case NetError.NotInSaveGame:
			text2 = "NOT_IN_SAVE_GAME.body";
			break;
		case NetError.VersionMismatch:
			text2 = "VERSION_MISMATCH.body";
			break;
		default:
			throw new System.Runtime.CompilerServices.SwitchExpressionException(reason);
			break;
		}
		string text3 = text2;
		string text4 = ((text3 != null) ? (text + text3) : null);
		if (text4 != null)
		{
			LocString locString = new LocString("main_menu_ui", text4);
			if (locString.Exists())
			{
				return locString;
			}
		}
		return null;
	}

	private void OnOkButtonPressed(NButton _)
	{
		this.QueueFreeSafely();
	}

	private void OnCancelButtonPressed(NButton _)
	{
		this.QueueFreeSafely();
	}

	private void OnReportBugButtonPressed(NButton _)
	{
		TaskHelper.RunSafely(OpenFeedbackScreen());
	}

	private async Task OpenFeedbackScreen()
	{
		SceneTree sceneTree = GetTree();
		this.QueueFreeSafely();
		await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
		await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
		await NFeedbackScreenOpener.Instance.OpenFeedbackScreen();
	}
}
