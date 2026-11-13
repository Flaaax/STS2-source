using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

/// <summary>
/// Disclaimer which explains how Dread works.
/// Appears the first time you enter the Dread mode screen but can also be
/// accessed by selecting the "?" button in the top right corner.
/// </summary>
public partial class NDreadInfoDisclaimer : Control, IScreenContext
{
	public const string id = "dread_disclaimer";

	private const string _disclaimerScenePath = "screens/dread/dread_info_disclaimer";

	private Tween? _openTween;

	private readonly OwnedTween _fadeOut;

	private Control _image;

	private Control _bgRect;

	private Control _popup;

	private NButton _confirmButton;

	private bool _isDelayedOpen;

	public Control? DefaultFocusedControl => null;

	public NDreadInfoDisclaimer()
	{
		_fadeOut = new OwnedTween(this);
	}

	/// <summary>
	/// Created as needed (not preloaded) because it shows up rarely.
	/// </summary>
	public static NDreadInfoDisclaimer Create(bool isDelayedOpen)
	{
		NDreadInfoDisclaimer nDreadInfoDisclaimer = SceneHelper.Instantiate<NDreadInfoDisclaimer>("screens/dread/dread_info_disclaimer");
		NHotkeyManager.Instance?.AddBlockingScreen(nDreadInfoDisclaimer);
		nDreadInfoDisclaimer._isDelayedOpen = isDelayedOpen;
		return nDreadInfoDisclaimer;
	}

	public override void _Ready()
	{
		string formattedText = new LocString("main_menu_ui", "DREAD_DISCLAIMER.title").GetFormattedText();
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
		double num = 1.0;
		int num2 = 1;
		foreach (MegaRichTextLabel item in GetNode<Control>("%LeftList").GetChildren().OfType<MegaRichTextLabel>())
		{
			string rawText = new LocString("ascension", $"LEVEL_{num2:D2}.title").GetRawText();
			string formattedText = new LocString("ascension", $"LEVEL_{num2:D2}.description").GetFormattedText();
			item.SetTextAutoSize("[gold]" + rawText + "[/gold]\n" + formattedText);
			modulate = item.Modulate;
			modulate.A = 0f;
			item.Modulate = modulate;
			_openTween.TweenProperty(item, "modulate:a", 1f, 0.5).SetDelay(num);
			num += 0.2;
			num2++;
		}
		num += 0.5;
		foreach (MegaRichTextLabel item2 in GetNode<Control>("%RightList").GetChildren().OfType<MegaRichTextLabel>())
		{
			string rawText2 = new LocString("ascension", $"LEVEL_{num2:D2}.title").GetRawText();
			string formattedText2 = new LocString("ascension", $"LEVEL_{num2:D2}.description").GetFormattedText();
			item2.SetTextAutoSize("[gold]" + rawText2 + "[/gold]\n" + formattedText2);
			modulate = item2.Modulate;
			modulate.A = 0f;
			item2.Modulate = modulate;
			_openTween.TweenProperty(item2, "modulate:a", 1f, 0.5).SetDelay(num);
			num += 0.2;
			num2++;
		}
	}

	private async Task CloseScreen()
	{
		SaveManager.Instance.MarkFtueAsComplete("dread_disclaimer");
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
		LocString locString = new LocString("main_menu_ui", "DREAD_DISCLAIMER.description");
		locString.Add("rule1", new LocString("ascension", "DREAD_RULE_01").GetRawText());
		locString.Add("rule2", new LocString("ascension", "DREAD_RULE_02").GetRawText());
		locString.Add("rule3", new LocString("ascension", "DREAD_RULE_03").GetRawText());
		locString.Add("rule4", new LocString("ascension", "DREAD_RULE_04").GetRawText());
		GetNode<MegaRichTextLabel>("%Description").SetTextAutoSize(locString.GetFormattedText());
	}
}
