using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Entities.Text;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.RichTextTags;

namespace MegaCrit.Sts2.addons.mega_text;

[Tool]
public partial class MegaRichTextLabel : RichTextLabel
{
	private static TextParagraph? _cachedParagraph = new TextParagraph();

	private const float _sizeComparisonEpsilon = 0.01f;

	private bool _isAutoSizeEnabled = true;

	private int _minFontSize = 8;

	private int _maxFontSize = 100;

	private int _lastSetSize;

	private bool _isVerticallyBound = true;

	private bool _isHorizontallyBound;

	private bool _needsResize = true;

	private bool _effectsInstalled;

	private Vector2 _lastAdjustedSize;

	private static readonly AbstractMegaRichTextEffect[] _textEffects = new AbstractMegaRichTextEffect[14]
	{
		new RichTextAqua(),
		new RichTextBlue(),
		new RichTextFadeIn(),
		new RichTextFlyIn(),
		new RichTextGold(),
		new RichTextGreen(),
		new RichTextJitter(),
		new RichTextOrange(),
		new RichTextPink(),
		new RichTextPurple(),
		new RichTextRed(),
		new RichTextScramble(),
		new RichTextSine(),
		new RichTextThinkyDots()
	};

	private bool _isAutoSizing;

	[Export(PropertyHint.None, "")]
	public bool AutoSizeEnabled
	{
		get
		{
			return _isAutoSizeEnabled;
		}
		set
		{
			if (value && base.FitContent)
			{
				GD.PushWarning("Auto Size is not compatible with Fit Content, disabling Auto Size...");
				_isAutoSizeEnabled = false;
			}
			else if (AutoSizeEnabled != value)
			{
				_isAutoSizeEnabled = value;
				if (Engine.IsEditorHint())
				{
					AdjustFontSize();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int MinFontSize
	{
		get
		{
			return _minFontSize;
		}
		set
		{
			if (_minFontSize != value)
			{
				_minFontSize = value;
				if (Engine.IsEditorHint())
				{
					AdjustFontSize();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int MaxFontSize
	{
		get
		{
			return _maxFontSize;
		}
		set
		{
			if (_maxFontSize != value)
			{
				_maxFontSize = value;
				if (Engine.IsEditorHint())
				{
					AdjustFontSize();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool IsVerticallyBound
	{
		get
		{
			return _isVerticallyBound;
		}
		set
		{
			_isVerticallyBound = value;
			if (Engine.IsEditorHint())
			{
				AdjustFontSize();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool IsHorizontallyBound
	{
		get
		{
			return _isHorizontallyBound;
		}
		set
		{
			_isHorizontallyBound = value;
			if (Engine.IsEditorHint())
			{
				AdjustFontSize();
			}
		}
	}

	public new string Text
	{
		get
		{
			return base.Text;
		}
		set
		{
			SetTextAutoSize(value);
		}
	}

	/// <summary>
	/// Releases the cached TextParagraph to free text server RIDs at exit.
	/// Uses Dispose() (not Free()) because TextParagraph is RefCounted.
	/// Nulled to guard against AdjustFontSize running during Godot's quit frames.
	/// </summary>
	public static void DisposeCachedParagraph()
	{
		_cachedParagraph?.Dispose();
		_cachedParagraph = null;
	}

	public override void _Ready()
	{
		MegaLabelHelper.AssertThemeFontOverride(this, ThemeConstants.RichTextLabel.NormalFont);
		RefreshFont();
		InstallEffectsIfNeeded();
		AdjustFontSize();
		ParseBbcode(Text);
	}

	public void RefreshFont()
	{
		this.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.RichTextLabel.NormalFont);
		this.ApplyLocaleFontSubstitution(FontType.Bold, ThemeConstants.RichTextLabel.BoldFont);
		this.ApplyLocaleFontSubstitution(FontType.Italic, ThemeConstants.RichTextLabel.ItalicsFont);
	}

	public override void _Notification(int what)
	{
		switch (what)
		{
		case 40:
			if (!(_lastAdjustedSize.DistanceSquaredTo(base.Size) < 0.0001f) && AutoSizeEnabled)
			{
				_needsResize = true;
				AdjustFontSize();
			}
			break;
		case 9001:
			base.CustomEffects.Clear();
			break;
		case 9002:
			InstallEffectsIfNeeded();
			break;
		}
	}

	private void InstallEffectsIfNeeded()
	{
		if ((!_effectsInstalled || base.CustomEffects.Count <= 0) && base.BbcodeEnabled)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			AbstractMegaRichTextEffect[] textEffects = _textEffects;
			foreach (AbstractMegaRichTextEffect abstractMegaRichTextEffect in textEffects)
			{
				array.Add(abstractMegaRichTextEffect);
			}
			base.CustomEffects = array;
			_effectsInstalled = true;
		}
	}

	private bool HasEffect(AbstractMegaRichTextEffect effect)
	{
		return base.CustomEffects.Contains(effect);
	}

	/// <summary>
	/// Unfortunately, there's no way to override the setting of text for a Label. So if you want the text size to
	/// automatically adjust after being updated during gameplay, you must use this method instead of setting the
	/// Text property directly.
	/// </summary>
	/// <param name="text"></param>
	public void SetTextAutoSize(string text)
	{
		if (!(base.Text == text))
		{
			base.Text = text;
			InstallEffectsIfNeeded();
			if (AutoSizeEnabled)
			{
				_needsResize = true;
				CallDeferred("AdjustFontSize");
			}
		}
	}

	private void AdjustFontSize()
	{
		TextParagraph cachedParagraph = _cachedParagraph;
		if (!AutoSizeEnabled || _isAutoSizing || cachedParagraph == null || !_needsResize)
		{
			return;
		}
		_isAutoSizing = true;
		try
		{
			_needsResize = true;
			_lastAdjustedSize = base.Size;
			Font themeFont = GetThemeFont(ThemeConstants.RichTextLabel.NormalFont, "RichTextLabel");
			float lineSpacing = GetThemeConstant(ThemeConstants.RichTextLabel.LineSpacing, "RichTextLabel");
			Vector2 size = GetRect().Size;
			List<BbcodeObject> objs = MegaLabelHelper.ParseBbcode(Text);
			if (!MegaLabelHelper.IsTooBig(cachedParagraph, objs, themeFont, MaxFontSize, lineSpacing, size, _isHorizontallyBound, _isVerticallyBound))
			{
				SetFontSize(MaxFontSize);
				return;
			}
			if (_lastSetSize >= MinFontSize && _lastSetSize < MaxFontSize && !MegaLabelHelper.IsTooBig(cachedParagraph, objs, themeFont, _lastSetSize, lineSpacing, size, _isHorizontallyBound, _isVerticallyBound) && MegaLabelHelper.IsTooBig(cachedParagraph, objs, themeFont, _lastSetSize + 1, lineSpacing, size, _isHorizontallyBound, _isVerticallyBound))
			{
				SetFontSize(_lastSetSize);
				return;
			}
			int num = MinFontSize;
			int num2 = MaxFontSize;
			while (num2 >= num)
			{
				int num3 = num + (num2 - num) / 2;
				if (MegaLabelHelper.IsTooBig(cachedParagraph, objs, themeFont, num3, lineSpacing, size, _isHorizontallyBound, _isVerticallyBound))
				{
					num2 = num3 - 1;
				}
				else
				{
					num = num3 + 1;
				}
			}
			SetFontSize(Math.Min(num, num2));
		}
		finally
		{
			_isAutoSizing = false;
		}
	}

	private void SetFontSize(int size)
	{
		if (_lastSetSize != size)
		{
			_lastSetSize = size;
			AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize, size);
			if (base.BbcodeEnabled)
			{
				AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldFontSize, size);
				AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.BoldItalicsFontSize, size);
				AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.ItalicsFontSize, size);
				AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.MonoFontSize, size);
				ParseBbcode(Text);
			}
		}
	}
}
