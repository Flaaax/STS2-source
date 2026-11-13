using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Blitz;

/// <summary>
/// Disclaimer which explains how Blitz works.
/// Appears the first time you enter the Blitz character select screen but can also be
/// accessed by selecting the "?" button in the top right corner.
/// </summary>
public partial class NBlitzInfoDisclaimer : Control, IScreenContext
{
	public const string id = "blitz_disclaimer";

	private const string _disclaimerScenePath = "screens/blitz/blitz_info_disclaimer";

	private Tween? _openTween;

	private readonly OwnedTween _fadeOut;

	private Control _image;

	private Control _bgRect;

	private Control _popup;

	private NButton _confirmButton;

	private bool _isDelayedOpen;

	public Control? DefaultFocusedControl => null;

	public NBlitzInfoDisclaimer()
	{
		_fadeOut = new OwnedTween(this);
	}

	/// <summary>
	/// Created as needed (not preloaded) because it shows up rarely.
	/// </summary>
	public static NBlitzInfoDisclaimer Create(bool isDelayedOpen)
	{
		NBlitzInfoDisclaimer nBlitzInfoDisclaimer = SceneHelper.Instantiate<NBlitzInfoDisclaimer>("screens/blitz/blitz_info_disclaimer");
		NHotkeyManager.Instance?.AddBlockingScreen(nBlitzInfoDisclaimer);
		nBlitzInfoDisclaimer._isDelayedOpen = isDelayedOpen;
		return nBlitzInfoDisclaimer;
	}

	public override void _Ready()
	{
		string formattedText = new LocString("main_menu_ui", "BLITZ_DISCLAIMER.title").GetFormattedText();
		_confirmButton = GetNode<NDreadDisclaimerConfirmButton>("%ConfirmButton");
		_confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnConfirmPressed));
		GetNode<MegaLabel>("%Header").SetTextAutoSize(formattedText);
		_image = GetNode<TextureRect>("Image");
		UpdateDisclaimerDescription();
		AnimIn();
	}

	private void OnConfirmPressed(NButton obj)
	{
		TaskHelper.RunSafely(CloseScreen());
	}

	private void AnimIn()
	{
		_bgRect = GetNode<Control>("%Bg");
		Control bgRect = _bgRect;
		Color modulate = _bgRect.Modulate;
		modulate.A = (_isDelayedOpen ? 0.8f : 0f);
		bgRect.Modulate = modulate;
		_popup = GetNode<Control>("%Image");
		Control popup = _popup;
		modulate = _popup.Modulate;
		modulate.A = 0f;
		popup.Modulate = modulate;
		_openTween?.Kill();
		_openTween = CreateTween().SetParallel();
		if (_isDelayedOpen)
		{
			_openTween.Chain();
			_openTween.TweenInterval(0.8);
			_openTween.Chain();
		}
		_openTween.TweenProperty(_bgRect, "modulate:a", 0.8f, 0.25);
		_openTween.TweenProperty(_popup, "modulate:a", 1f, 0.5);
		_openTween.TweenProperty(_popup, "position:y", _popup.Position.Y, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
			.From(_popup.Position.Y + 100f);
	}

	private async Task CloseScreen()
	{
		SaveManager.Instance.MarkFtueAsComplete("blitz_disclaimer");
		_openTween?.Kill();
		if (await _fadeOut.Play(delegate(Tween tween)
		{
			tween.TweenProperty(this, "modulate:a", 0f, 0.2);
		}) != TweenOutcome.Replaced)
		{
			this.QueueFreeSafely();
			NHotkeyManager.Instance?.RemoveBlockingScreen(this);
		}
	}

	private void UpdateDisclaimerDescription()
	{
		LocString locString = new LocString("main_menu_ui", "BLITZ_DISCLAIMER.description");
		GetNode<MegaRichTextLabel>("%Description").SetTextAutoSize(locString.GetFormattedText());
	}
}
