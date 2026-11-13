using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

/// <summary>
/// Confirm button seen in the Dread info disclaimer.
/// Is also used by the Blitz info disclaimer as it's similar enough.
/// </summary>
public partial class NDreadDisclaimerConfirmButton : NButton
{
	private Tween? _tween;

	private Control _bgRect;

	private Control _popup;

	protected override string[] Hotkeys => new string[1] { MegaInput.confirm };

	public override void _Ready()
	{
		ConnectSignals();
		LocString locString = new LocString("main_menu_ui", "DREAD_DISCLAIMER.confirmButton");
		GetNode<MegaLabel>("%Label").SetTextAutoSize(locString.GetFormattedText());
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One * 1.05f, 0.05);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One * 1.05f, 0.05);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}

	protected override void OnPress()
	{
		base.OnPress();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One * 0.95f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}
}
