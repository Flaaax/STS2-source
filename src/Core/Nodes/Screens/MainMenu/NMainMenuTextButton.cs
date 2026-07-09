using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

/// <summary>
/// The text buttons seen as a vertical list in the main menu.
/// Buttons like Singleplayer, Multiplayer, Statistics, Options, Quit, etc
/// Note that we tween the scale/modulate of the label instead of the entire button because stuff is parented to the
/// continue button
/// </summary>
public partial class NMainMenuTextButton : NButton
{
	/// <summary>
	/// This is null at certain points when the scene is being loaded.
	/// </summary>
	public MegaLabel? label;

	private Color _defaultColor = StsColors.cream;

	private Color _hoveredColor = StsColors.gold;

	private Color _downColor = StsColors.halfTransparentWhite;

	private static readonly Vector2 _hoverScale = new Vector2(1.05f, 1.05f);

	private static readonly Vector2 _downScale = new Vector2(0.95f, 0.95f);

	private static readonly StyleBoxEmpty _emptyStyleBox = new StyleBoxEmpty();

	private const double _pressDownDur = 0.2;

	private const double _unhoverAnimDur = 0.5;

	private Tween? _tween;

	private LocString? _locString;

	public override void _Ready()
	{
		if (GetType() != typeof(NMainMenuTextButton))
		{
			throw new InvalidOperationException("Don't call base._Ready()!");
		}
		ConnectSignals();
	}

	protected override void ConnectSignals()
	{
		base.ConnectSignals();
		label = GetChild<MegaLabel>(0);
		label.AddThemeStyleboxOverride(ThemeConstants.Control.Focus, _emptyStyleBox);
		label.FocusMode = FocusModeEnum.None;
	}

	public void SetLocalization(string locKey)
	{
		_locString = new LocString("main_menu_ui", locKey);
		RefreshLabel();
	}

	public override void _Notification(int what)
	{
		if ((long)what == 2010)
		{
			RefreshLabel();
		}
	}

	private void RefreshLabel()
	{
		if (label != null && _locString != null)
		{
			label.Text = _locString.GetFormattedText();
			label.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.Label.Font);
			TaskHelper.RunSafely(UpdatePivotOffset());
		}
	}

	/// <summary>
	/// Wait one frame before the text change affects the Control node's size so we can set the pivot accurately.
	/// </summary>
	private async Task UpdatePivotOffset()
	{
		await this.AwaitProcessFrame();
		if (label != null)
		{
			label.PivotOffset = label.Size * 0.5f;
		}
	}

	protected override void OnPress()
	{
		base.OnPress();
		AnimPressDown();
	}

	protected override void OnRelease()
	{
		AnimRelease();
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(label, "scale", _hoverScale, 0.05);
		_tween.TweenProperty(label, "self_modulate", _hoveredColor, 0.05);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		AnimUnhover();
	}

	private void AnimUnhover()
	{
		_tween?.Kill();
		if (label != null)
		{
			label.SelfModulate = _defaultColor;
			_tween = CreateTween();
			_tween.TweenProperty(label, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		}
	}

	private void AnimPressDown()
	{
		_tween?.Kill();
		if (label != null)
		{
			label.SelfModulate = _hoveredColor;
			label.Scale = _hoverScale;
			_tween = CreateTween();
			_tween.TweenProperty(label, "scale", _downScale, 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			_tween.TweenProperty(label, "self_modulate", _downColor, 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		}
	}

	private void AnimRelease()
	{
		_tween?.Kill();
		if (label != null)
		{
			_tween = CreateTween();
			_tween.TweenProperty(label, "scale", base.IsFocused ? _hoverScale : Vector2.One, 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			_tween.TweenProperty(label, "self_modulate", _defaultColor, 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		}
	}

	protected override void OnDisable()
	{
		if (label != null)
		{
			label.Modulate = StsColors.quarterTransparentWhite;
		}
	}

	protected override void OnEnable()
	{
		if (label != null)
		{
			label.Modulate = Colors.White;
		}
	}
}
