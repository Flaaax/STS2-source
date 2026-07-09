using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

public partial class NGlobalRankTickbox : NTickbox
{
	private MegaLabel _label;

	private Tween? _labelTween;

	protected override string[] Hotkeys => new string[1] { MegaInput.peek };

	public override void _Ready()
	{
		ConnectSignals();
		_label = GetNode<MegaLabel>("Label");
		base.IsTicked = false;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		_labelTween?.Kill();
		_labelTween = CreateTween().SetParallel();
		_labelTween.TweenProperty(_label, "scale", Vector2.One * 1.1f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
		_labelTween.TweenProperty(_label, "self_modulate", Colors.White, 0.05);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_labelTween?.Kill();
		_labelTween = CreateTween().SetParallel();
		_labelTween.TweenProperty(_label, "scale", Vector2.One * 1.1f, 0.05);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_labelTween?.Kill();
		_labelTween = CreateTween().SetParallel();
		_labelTween.TweenProperty(_label, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}

	protected override void OnPress()
	{
		base.OnPress();
		_labelTween?.Kill();
		_labelTween = CreateTween().SetParallel();
		_labelTween.TweenProperty(_label, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_labelTween.TweenProperty(_label, "self_modulate", StsColors.lightGray, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}

	public void SetLabel(string text)
	{
		_label.SetTextAutoSize(text);
	}
}
