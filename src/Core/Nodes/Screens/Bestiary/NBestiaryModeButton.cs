using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryModeButton : NButton
{
	private Tween? _tween;

	private MegaLabel _modeLabel;

	protected override string ClickedSfx => "event:/sfx/ui/timeline/ui_timeline_close_epoch";

	protected override string[] Hotkeys => new string[1] { MegaInput.confirm };

	protected override string ControllerIconHotkey => MegaInput.confirm;

	public override void _Ready()
	{
		ConnectSignals();
		_modeLabel = GetNode<MegaLabel>("%ModeLabel");
	}

	public void SetLabel(string str)
	{
		_modeLabel.SetTextAutoSize(str);
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
