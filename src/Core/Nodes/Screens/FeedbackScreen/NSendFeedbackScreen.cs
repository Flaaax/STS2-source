using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using Sentry;

namespace MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;

public partial class NSendFeedbackScreen : Control, IScreenContext
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/feedback_screen/feedback_screen");

	private const float _superWiggleTime = 0.25f;

	private const string _defaultUrl = "https://feedback.sts2.megacrit.com/feedback";

	private static readonly string _url = System.Environment.GetEnvironmentVariable("STS2_FEEDBACK_URL") ?? "https://feedback.sts2.megacrit.com/feedback";

	private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10L)
	};

	private const int _maxDescriptionChars = 8000;

	private NBackButton _backButton;

	private Control _mainPanel;

	private NMegaTextEdit _descriptionInput;

	private MegaLabel _emojiLabel;

	private NButton _sendButton;

	private MegaLabel _sendLabel;

	private MegaLabel _categoryLabel;

	private NButton _returnToGameButton;

	private MegaLabel _returnToGameLabel;

	private MegaLabel _returnToGameHoverLabel;

	private NFeedbackCategoryDropdown _categoryDropdown;

	private Control _sendBackstop;

	private Control _sendPanel;

	private MegaLabel _successLabel;

	private MegaLabel _failedLabel;

	private MegaRichTextLabel _sendingLabel;

	private List<NSendFeedbackCartoon> _cartoons = new List<NSendFeedbackCartoon>();

	private NSendFeedbackFlower _flower;

	private CancellationTokenSource? _screenClosedCancelToken;

	private TaskCompletionSource? _runInBackgroundTaskSource;

	private readonly List<NSendFeedbackEmojiButton> _emojiButtons = new List<NSendFeedbackEmojiButton>();

	private NSendFeedbackEmojiButton? _selectedEmoteButton;

	private byte[]? _screenshotBytes;

	private Vector2 _originalSuccessPosition;

	private ulong _lastClosedMsec;

	private string _descriptionText = string.Empty;

	private int _descriptionCaretLine;

	private int _descriptionCaretColumn;

	private Tween? _wiggleTween;

	public Control DefaultFocusedControl => _descriptionInput;

	public static NSendFeedbackScreen? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NSendFeedbackScreen>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_mainPanel = GetNode<Control>("%MainPanel");
		_descriptionInput = GetNode<NMegaTextEdit>("%DescriptionInput");
		_emojiLabel = GetNode<MegaLabel>("%EmojiLabel");
		_sendButton = GetNode<NButton>("%SendButton");
		_returnToGameButton = GetNode<NButton>("%ReturnToGameButton");
		_returnToGameLabel = _returnToGameButton.GetNode<MegaLabel>("Label");
		_returnToGameHoverLabel = _returnToGameButton.GetNode<MegaLabel>("%ReturnToGameHoverLabel");
		_sendLabel = _sendButton.GetNode<MegaLabel>("Label");
		_categoryLabel = GetNode<MegaLabel>("%CategoryLabel");
		_categoryDropdown = GetNode<NFeedbackCategoryDropdown>("%CategoryDropdown");
		_sendBackstop = GetNode<Control>("%SendBackstop");
		_sendPanel = GetNode<Control>("%SendPanel");
		_successLabel = GetNode<MegaLabel>("%SuccessLabel");
		_failedLabel = GetNode<MegaLabel>("%FailedLabel");
		_sendingLabel = GetNode<MegaRichTextLabel>("%SendingLabel");
		_backButton = GetNode<NBackButton>("BackButton");
		_successLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_SEND_SUCCESS_LABEL").GetFormattedText());
		_failedLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_SEND_FAILED_LABEL").GetFormattedText());
		_sendingLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_SENDING_LABEL").GetFormattedText());
		_returnToGameLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_RUN_IN_BACKGROUND_LABEL").GetFormattedText());
		_returnToGameHoverLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_RUN_IN_BACKGROUND_HOVER_LABEL").GetFormattedText());
		_originalSuccessPosition = _sendPanel.Position;
		int num = 3;
		List<NSendFeedbackCartoon> list = new List<NSendFeedbackCartoon>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<NSendFeedbackCartoon> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = GetNode<NSendFeedbackCartoon>("Sun");
		num2++;
		span[num2] = GetNode<NSendFeedbackCartoon>("Cupcake");
		num2++;
		span[num2] = GetNode<NSendFeedbackCartoon>("FlowerContainer/Flower");
		_cartoons = list;
		_flower = GetNode<NSendFeedbackFlower>("FlowerContainer");
		foreach (Node child in GetNode("%EmojiButtonContainer").GetChildren())
		{
			if (child is NSendFeedbackEmojiButton nSendFeedbackEmojiButton)
			{
				_emojiButtons.Add(nSendFeedbackEmojiButton);
				nSendFeedbackEmojiButton.PivotOffset = nSendFeedbackEmojiButton.Size * 0.5f;
				nSendFeedbackEmojiButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(EmojiButtonSelected));
			}
		}
		_sendButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(SendButtonSelected));
		_sendButton.Connect(NClickableControl.SignalName.Focused, Callable.From<NClickableControl>(SendButtonFocused));
		_sendButton.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NClickableControl>(SendButtonUnfocused));
		_returnToGameButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(ReturnToGameSelected));
		_returnToGameButton.Connect(NClickableControl.SignalName.Focused, Callable.From<NClickableControl>(ReturnToGameFocused));
		_returnToGameButton.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NClickableControl>(ReturnToGameUnfocused));
		_descriptionInput.Connect(TextEdit.SignalName.TextChanged, Callable.From(OnDescriptionChanged));
		_returnToGameHoverLabel.Visible = false;
		_sendButton.FocusNeighborTop = _categoryDropdown.GetPath();
		_sendButton.FocusNeighborLeft = _emojiButtons.Last().GetPath();
		_sendButton.FocusNeighborBottom = _sendButton.GetPath();
		_sendButton.FocusNeighborRight = _sendButton.GetPath();
		_emojiButtons.Last().FocusNeighborRight = _sendButton.GetPath();
		foreach (NSendFeedbackEmojiButton emojiButton in _emojiButtons)
		{
			emojiButton.FocusNeighborTop = _categoryDropdown.GetPath();
			emojiButton.FocusNeighborBottom = emojiButton.GetPath();
		}
		_categoryDropdown.FocusNeighborRight = _sendButton.GetPath();
		_categoryDropdown.FocusNeighborBottom = _emojiButtons.First().GetPath();
		_categoryDropdown.FocusNeighborTop = _descriptionInput.GetPath();
		_descriptionInput.FocusNeighborTop = _descriptionInput.GetPath();
		_descriptionInput.FocusNeighborLeft = _descriptionInput.GetPath();
		_descriptionInput.FocusNeighborRight = _descriptionInput.GetPath();
		_descriptionInput.FocusNeighborBottom = _categoryDropdown.GetPath();
		_backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			Close();
		}));
		base.Visible = false;
		base.MouseFilter = MouseFilterEnum.Ignore;
		_backButton.Disable();
	}

	public void Relocalize()
	{
		_descriptionInput.PlaceholderText = new LocString("settings_ui", "FEEDBACK_DESCRIPTION_PLACEHOLDER").GetFormattedText();
		_categoryLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_CATEGORY_LABEL").GetFormattedText());
		_emojiLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_EMOJI_LABEL").GetFormattedText());
		_sendLabel.SetTextAutoSize(new LocString("settings_ui", "FEEDBACK_SEND_BUTTON_LABEL").GetFormattedText());
		_descriptionInput.RefreshFont();
		_categoryLabel.RefreshFont();
		_emojiLabel.RefreshFont();
		_sendLabel.RefreshFont();
	}

	/// <summary>
	/// Used to limit the number of characters that can be typed into the description box.
	/// </summary>
	private void OnDescriptionChanged()
	{
		if (_descriptionInput.Text.Length > 8000)
		{
			_descriptionInput.Text = _descriptionText;
			_descriptionInput.SetCaretLine(_descriptionCaretLine);
			_descriptionInput.SetCaretColumn(_descriptionCaretColumn);
		}
		else
		{
			_descriptionText = _descriptionInput.Text;
			_descriptionCaretLine = _descriptionInput.GetCaretLine();
			_descriptionCaretColumn = _descriptionInput.GetCaretColumn();
		}
	}

	/// <summary>
	/// Set the screenshot that will be uploaded with the feedback. The screenshot will be automatically scaled to a
	/// smaller size so that the upload isn't massive.
	/// </summary>
	/// <param name="screenshot"></param>
	public void SetScreenshot(Image screenshot)
	{
		int width = screenshot.GetWidth();
		int height = screenshot.GetHeight();
		float num = (float)width / (float)height;
		if (width > 1280)
		{
			screenshot.Resize(1280, Mathf.RoundToInt(1280f / num), Image.Interpolation.Bilinear);
		}
		if (height > 720)
		{
			screenshot.Resize(Mathf.RoundToInt(720f * num), 720, Image.Interpolation.Bilinear);
		}
		_screenshotBytes = screenshot.SavePngToBuffer();
	}

	private void EmojiButtonSelected(NButton button)
	{
		SetSelectedEmoji((NSendFeedbackEmojiButton)button);
	}

	private void SendButtonFocused(NClickableControl _)
	{
		_flower.SetState(NSendFeedbackFlower.State.Anticipation);
	}

	private void SendButtonUnfocused(NClickableControl _)
	{
		if (_flower.MyState == NSendFeedbackFlower.State.Anticipation && !_sendBackstop.Visible)
		{
			_flower.SetState(NSendFeedbackFlower.State.None);
		}
	}

	public void Open()
	{
		if (!base.Visible)
		{
			Log.Info("Feedback screen opened");
			if (Time.GetTicksMsec() - _lastClosedMsec > 60000)
			{
				ClearInput();
			}
			_screenClosedCancelToken = new CancellationTokenSource();
			base.Visible = true;
			_flower.SetState(NSendFeedbackFlower.State.None);
			_sendBackstop.Visible = false;
			_sendButton.Enable();
			base.MouseFilter = MouseFilterEnum.Stop;
			NHotkeyManager.Instance.AddBlockingScreen(this);
			ActiveScreenContext.Instance.Update();
			_backButton.Enable();
		}
	}

	private void Close()
	{
		Log.Info("Feedback screen closed");
		_flower.SetState(NSendFeedbackFlower.State.None);
		_sendBackstop.Visible = false;
		_mainPanel.Modulate = Colors.White;
		_wiggleTween?.Kill();
		base.Visible = false;
		_lastClosedMsec = Time.GetTicksMsec();
		_screenClosedCancelToken?.Cancel();
		base.MouseFilter = MouseFilterEnum.Ignore;
		_backButton.Disable();
		NHotkeyManager.Instance.RemoveBlockingScreen(this);
		ActiveScreenContext.Instance.Update();
	}

	private void ClearInput()
	{
		_descriptionInput.Text = string.Empty;
		_descriptionText = string.Empty;
		SetSelectedEmoji(null);
	}

	private void SetSelectedEmoji(NSendFeedbackEmojiButton? button)
	{
		NSendFeedbackEmojiButton selectedEmoteButton = _selectedEmoteButton;
		_selectedEmoteButton?.SetSelected(isSelected: false);
		if (selectedEmoteButton != button)
		{
			_selectedEmoteButton = button;
			_selectedEmoteButton?.SetSelected(isSelected: true);
		}
	}

	private void SendButtonSelected(NButton _)
	{
		TaskHelper.RunSafely(SendFeedbackWrapper());
		_sendButton.Disable();
	}

	private void ReturnToGameSelected(NButton _)
	{
		_runInBackgroundTaskSource?.TrySetResult();
	}

	private void ReturnToGameFocused(NClickableControl _)
	{
		_returnToGameHoverLabel.Visible = true;
	}

	private void ReturnToGameUnfocused(NClickableControl _)
	{
		_returnToGameHoverLabel.Visible = false;
	}

	/// <summary>
	/// Send feedback, allowing the player to exit the menu if they want to get back to playing.
	/// </summary>
	private async Task SendFeedbackWrapper()
	{
		if (string.IsNullOrEmpty(_descriptionText))
		{
			return;
		}
		Log.Info("Beginning asynchronous feedback send at " + Log.Timestamp + ": " + _descriptionText);
		ReleaseInfo releaseInfo = ReleaseInfoManager.Instance.ReleaseInfo;
		string text = releaseInfo?.Commit ?? GitHelper.ShortCommitId;
		FeedbackData data = new FeedbackData
		{
			description = _descriptionText,
			category = _categoryDropdown.CurrentCategory,
			gameVersion = (releaseInfo?.Version ?? "v0.0.1"),
			uniqueId = SaveManager.Instance.Progress.UniqueId,
			commit = (text ?? "unknown"),
			platformBranch = PlatformUtil.GetPlatformBranch().ToName(),
			sessionId = SentryService.SessionId,
			isModded = (ModManager.IsRunningModded() || ModManager.HasHarmonyPatches()),
			isFullConsole = SaveManager.Instance.SettingsSave.FullConsole,
			lang = LocManager.Instance.Language
		};
		MemoryStream screenshotStream = new MemoryStream(_screenshotBytes);
		int currentProfileId = SaveManager.Instance.CurrentProfileId;
		MemoryStream memoryStream = new MemoryStream();
		GetLogsConsoleCmd.ZipFeedbackLogs(memoryStream, currentProfileId);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		_runInBackgroundTaskSource = new TaskCompletionSource();
		Task<bool> sendTask = SendFeedback(data, screenshotStream, memoryStream);
		_sendBackstop.Visible = true;
		_sendingLabel.Visible = true;
		_failedLabel.Visible = false;
		_successLabel.Visible = false;
		_sendPanel.Modulate = Colors.Transparent;
		Control sendPanel = _sendPanel;
		Vector2 position = _sendPanel.Position;
		position.Y = _originalSuccessPosition.Y + 20f;
		sendPanel.Position = position;
		Tween tween = GetTree().CreateTween().Parallel();
		tween.TweenProperty(_mainPanel, "modulate", new Color(0.1f, 0.1f, 0.1f), 0.15000000596046448);
		tween.TweenProperty(_sendPanel, "modulate", Colors.White, 0.15000000596046448);
		tween.TweenProperty(_sendPanel, "position:y", _originalSuccessPosition.Y, 0.15000000596046448).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		tween.Chain().TweenProperty(_successLabel, "position:y", _successLabel.Position.Y - 10f, 0.10000000149011612).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		await TaskHelper.WhenAny(sendTask, _runInBackgroundTaskSource.Task);
		if (this.IsValid())
		{
			if (_runInBackgroundTaskSource.Task.IsCompleted)
			{
				Close();
			}
			else if (await sendTask)
			{
				await OnFeedbackSuccess();
			}
			else
			{
				await OnFeedbackFailed();
			}
		}
	}

	/// <summary>
	/// Send feedback and log after close.
	/// This is purposefully static so that we ensure that nothing from the screen is used, as it can be deallocated
	/// before the method completes.
	/// </summary>
	private static async Task<bool> SendFeedback(FeedbackData data, Stream screenshotStream, Stream logsMemoryStream)
	{
		_ = 2;
		try
		{
			using MultipartFormDataContent formContent = BuildMultipartContent(data, screenshotStream, logsMemoryStream);
			int[] delaysMs = new int[3] { 500, 1000, 2000 };
			string sentryMessage = null;
			for (int attempt = 0; attempt < 3; attempt++)
			{
				try
				{
					using HttpResponseMessage response = await _httpClient.PutAsync(_url, formContent);
					if (response.IsSuccessStatusCode)
					{
						Log.Info("Feedback successfully posted!");
						return true;
					}
					int statusCode = (int)response.StatusCode;
					if (statusCode >= 400 && statusCode < 500 && statusCode != 429)
					{
						string value = await response.Content.ReadAsStringAsync();
						Log.Warn($"Feedback rejected ({response.StatusCode}): {value}");
						SentrySdk.CaptureMessage($"Feedback rejected: Response status code {response.StatusCode}");
						return false;
					}
					sentryMessage = $"Response status code {response.StatusCode}";
					Log.Warn($"Feedback attempt {attempt + 1}/{3} failed: {response.StatusCode}");
				}
				catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException) ? 1 : 0) != 0)
				{
					string text = $"Feedback attempt {attempt + 1}/{3} network error: {ex}\n Inner messages: {ExceptionMessageWithInner(ex)}";
					if (ex is HttpRequestException { HttpRequestError: not HttpRequestError.NameResolutionError })
					{
						sentryMessage = ex.GetType().Name + ": " + ExceptionMessageWithInner(ex);
					}
					Log.Warn(text);
				}
				if (attempt < 2)
				{
					await Task.Delay(delaysMs[attempt]);
					screenshotStream.Seek(0L, SeekOrigin.Begin);
					logsMemoryStream.Seek(0L, SeekOrigin.Begin);
				}
			}
			Log.Warn("Feedback send failed after all retry attempts");
			if (sentryMessage != null)
			{
				SentrySdk.CaptureMessage("Feedback failed to send: " + sentryMessage);
			}
			return false;
		}
		finally
		{
			screenshotStream.Close();
			logsMemoryStream.Close();
		}
	}

	private static string ExceptionMessageWithInner(Exception ex)
	{
		if (ex.InnerException == null)
		{
			return ex.Message;
		}
		return ex.Message + " | " + ExceptionMessageWithInner(ex.InnerException);
	}

	private static MultipartFormDataContent BuildMultipartContent(FeedbackData data, Stream screenshotStream, Stream logsStream)
	{
		string content = JsonSerializer.Serialize(data, JsonSerializationUtility.GetTypeInfo<FeedbackData>());
		MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
		StringContent stringContent = new StringContent(content);
		stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		stringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
		{
			Name = "payload_json"
		};
		multipartFormDataContent.Add(stringContent);
		StreamContent streamContent = new StreamContent(logsStream);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
		streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
		{
			Name = "logs"
		};
		multipartFormDataContent.Add(streamContent);
		StreamContent streamContent2 = new StreamContent(screenshotStream);
		streamContent2.Headers.ContentType = new MediaTypeHeaderValue("image/png");
		streamContent2.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
		{
			Name = "screenshot"
		};
		multipartFormDataContent.Add(streamContent2);
		return multipartFormDataContent;
	}

	private async Task OnFeedbackFailed()
	{
		_sendingLabel.Visible = false;
		_failedLabel.Visible = true;
		_successLabel.Visible = false;
		Tween tween = GetTree().CreateTween().Chain();
		tween.TweenProperty(_failedLabel, "position:x", _successLabel.Position.X - 10f, 0.02500000037252903).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(_failedLabel, "position:x", _successLabel.Position.X + 10f, 0.05000000074505806).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(_failedLabel, "position:x", _successLabel.Position.X - 10f, 0.05000000074505806).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(_failedLabel, "position:x", _successLabel.Position.X + 10f, 0.05000000074505806).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(_failedLabel, "position:x", _successLabel.Position.X, 0.02500000037252903).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		_wiggleTween?.Kill();
		_flower.SetState(NSendFeedbackFlower.State.None);
		await Task.Delay(2000, _screenClosedCancelToken?.Token ?? default(CancellationToken));
		_sendBackstop.Visible = false;
		_mainPanel.Modulate = Colors.White;
	}

	private async Task OnFeedbackSuccess()
	{
		ClearInput();
		_screenshotBytes = null;
		_sendingLabel.Visible = false;
		_failedLabel.Visible = false;
		_successLabel.Visible = true;
		MegaLabel successLabel = _successLabel;
		Color modulate = _successLabel.Modulate;
		modulate.A = 0f;
		successLabel.Modulate = modulate;
		Tween tween = GetTree().CreateTween().Parallel();
		tween.TweenProperty(_successLabel, "modulate:a", 1f, 0.15000000596046448);
		tween.TweenProperty(_successLabel, "position:y", _successLabel.Position.Y - 10f, 0.10000000149011612).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		tween.Chain().TweenProperty(_successLabel, "position:y", _successLabel.Position.Y, 0.10000000149011612).SetEase(Tween.EaseType.In)
			.SetTrans(Tween.TransitionType.Quad);
		_wiggleTween?.Kill();
		_wiggleTween = CreateTween();
		_wiggleTween.TweenCallback(Callable.From(WiggleCartoons1));
		_wiggleTween.TweenInterval(0.25);
		_wiggleTween.TweenCallback(Callable.From(WiggleCartoons2));
		_wiggleTween.TweenInterval(0.25);
		_wiggleTween.SetLoops();
		string scenePath = SceneHelper.GetScenePath("vfx/vfx_dramatic_entrance_fullscreen");
		Node2D node2D = PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		this.AddChildSafely(node2D);
		this.MoveChildSafely(node2D, 1);
		node2D.GlobalPosition = NGame.Instance.GetViewportRect().Size * 0.5f;
		_flower.SetState(NSendFeedbackFlower.State.NoddingFast);
		await Task.Delay(2000, _screenClosedCancelToken?.Token ?? default(CancellationToken));
		Close();
	}

	private void WiggleCartoons1()
	{
		foreach (NSendFeedbackCartoon cartoon in _cartoons)
		{
			if (_flower.MyState == NSendFeedbackFlower.State.None || cartoon != _flower.Cartoon)
			{
				cartoon.SetRotation1();
			}
		}
	}

	private void WiggleCartoons2()
	{
		foreach (NSendFeedbackCartoon cartoon in _cartoons)
		{
			if (_flower.MyState == NSendFeedbackFlower.State.None || cartoon != _flower.Cartoon)
			{
				cartoon.SetRotation2();
			}
		}
	}
}
