using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

/// <summary>
/// Button in the top right corner in the Dread character select screen.
/// NOTE: Is also used by the Blitz screen (for now).
/// </summary>
public partial class NDreadInfoButton : NButton
{
	private Control _icon;

	private Tween? _tween;

	protected override string[] Hotkeys => new string[1] { MegaInput.confirm };

	public override void _Ready()
	{
		ConnectSignals();
		_icon = GetNode<Control>("%Icon");
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One * 1.1f, 0.05);
		_tween.TweenProperty(_icon, "self_modulate:a", 1f, 0.05);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_tween.TweenProperty(_icon, "self_modulate:a", 0.5f, 0.3);
	}

	protected override void OnPress()
	{
		base.OnPress();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One * 0.9f, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_tween.TweenProperty(_icon, "self_modulate:a", 0.5f, 0.3);
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_tween.TweenProperty(_icon, "self_modulate:a", 0.5f, 0.3);
	}
}
