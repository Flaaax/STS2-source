using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

/// <summary>
/// The huge submenu buttons you see in the MainMenu.
/// </summary>
public partial class NSubmenuButton : NButton
{
	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	private ShaderMaterial _hsv;

	private Control _bgPanel;

	private NSubmenuPortrait _portrait;

	private MegaLabel _title;

	private MegaRichTextLabel _description;

	private string? _locKeyPrefix;

	private float _defaultV;

	private const float _hoverV = 1f;

	private Tween? _scaleTween;

	private static readonly Vector2 _hoverScale = Vector2.One * 1.025f;

	public override void _Ready()
	{
		if (GetType() != typeof(NSubmenuButton))
		{
			throw new InvalidOperationException("Don't call base._Ready(). Use ConnectSignals() instead.");
		}
		ConnectSignals();
	}

	protected override void ConnectSignals()
	{
		base.ConnectSignals();
		_bgPanel = GetNode<Control>("BgPanel");
		_hsv = (ShaderMaterial)_bgPanel.Material;
		_portrait = GetNode<NSubmenuPortrait>("%SubmenuPortrait");
		_title = GetNode<MegaLabel>("%Title");
		_description = GetNode<MegaRichTextLabel>("%Description");
		_defaultV = (float)_hsv.GetShaderParameter(_v);
	}

	public void SetLocalization(string locKeyPrefix)
	{
		_locKeyPrefix = locKeyPrefix;
		RefreshLabels();
	}

	public void SetIcon(string fileName)
	{
		_portrait.SetTexture(PreloadManager.Cache.GetCompressedTexture2D(GetImagePath(fileName)));
	}

	private string GetImagePath(string fileName)
	{
		string iconPath = NSubmenuPortrait.GetIconPath(fileName);
		if (ResourceLoader.Exists(iconPath))
		{
			return iconPath;
		}
		Log.Warn("MISSING ICON: " + fileName);
		return NSubmenuPortrait.GetIconPath("unknown");
	}

	public override void _Notification(int what)
	{
		if ((long)what == 2010 && _locKeyPrefix != null && IsNodeReady())
		{
			RefreshLabels();
		}
	}

	public void RefreshLabels(string? locKeyOverride = null)
	{
		string text = locKeyOverride ?? _locKeyPrefix;
		LocString locString = new LocString("main_menu_ui", text + ".title");
		_title.SetTextAutoSize(locString.GetFormattedText());
		_title.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.Label.Font);
		LocString locString2;
		if (base.IsEnabled)
		{
			locString2 = new LocString("main_menu_ui", text + ".description");
		}
		else
		{
			locString2 = new LocString("main_menu_ui", text + ".LOCKED.description");
			if (!locString2.Exists())
			{
				Log.Warn($"Submenu button {base.Name} tried to find locked description for {text} but couldn't");
				locString2 = new LocString("main_menu_ui", text + ".description");
			}
		}
		_description.Text = locString2.GetFormattedText();
		_description.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.RichTextLabel.NormalFont);
		_description.ApplyLocaleFontSubstitution(FontType.Bold, ThemeConstants.RichTextLabel.BoldFont);
		_description.ApplyLocaleFontSubstitution(FontType.Italic, ThemeConstants.RichTextLabel.ItalicsFont);
	}

	protected override void OnEnable()
	{
		base.Modulate = Colors.White;
		_hsv.SetShaderParameter(_s, 1f);
		_portrait.SetUnlocked();
	}

	/// <summary>
	/// Sets this button to be locked.
	/// </summary>
	protected override void OnDisable()
	{
		base.Modulate = Colors.DarkGray;
		_portrait.SetLocked();
		_hsv.SetShaderParameter(_s, 0f);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_scaleTween?.Kill();
		base.Scale = _hoverScale;
		_hsv.SetShaderParameter(_v, 1f);
	}

	protected override void OnUnfocus()
	{
		_scaleTween?.Kill();
		_scaleTween = CreateTween().SetParallel();
		_scaleTween.TweenProperty(this, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_scaleTween.TweenMethod(Callable.From<float>(UpdateShaderParam), 1f, _defaultV, 1.0).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}

	private void UpdateShaderParam(float newV)
	{
		_hsv.SetShaderParameter(_v, newV);
	}
}
