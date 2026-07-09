using System;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace MegaCrit.Sts2.addons.mega_text;

[Tool]
public partial class MegaLabel : Label
{
	private static TextParagraph? _cachedParagraph = new TextParagraph();

	private const float _sizeComparisonEpsilon = 0.01f;

	private bool _autoSizeEnabled = true;

	private int _minFontSize = 8;

	private int _maxFontSize = 100;

	private int _lastSetSize;

	private Vector2 _lastAdjustedSize;

	[Export(PropertyHint.None, "")]
	public bool AutoSizeEnabled
	{
		get
		{
			return _autoSizeEnabled;
		}
		set
		{
			if (_autoSizeEnabled != value)
			{
				_autoSizeEnabled = value;
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
		MegaLabelHelper.AssertThemeFontOverride(this, ThemeConstants.Label.Font);
		RefreshFont();
		AdjustFontSize();
	}

	public void RefreshFont()
	{
		this.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.Label.Font);
	}

	public override void _Notification(int what)
	{
		if ((long)what == 40 && !(_lastAdjustedSize.DistanceSquaredTo(base.Size) < 0.0001f))
		{
			AdjustFontSize();
		}
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
			AdjustFontSize();
		}
	}

	private void SetFontSize(int size)
	{
		if (_lastSetSize != size)
		{
			_lastSetSize = size;
			if (HasThemeFont(ThemeConstants.Label.Font))
			{
				AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, size);
			}
		}
	}

	private void AdjustFontSize()
	{
		TextParagraph cachedParagraph = _cachedParagraph;
		if (!AutoSizeEnabled || cachedParagraph == null)
		{
			return;
		}
		_lastAdjustedSize = base.Size;
		Font themeFont = GetThemeFont(ThemeConstants.Label.Font, "Label");
		float lineSpacing = GetThemeConstant(ThemeConstants.Label.LineSpacing, "Label");
		Vector2 size = GetRect().Size;
		bool wrap = base.AutowrapMode != TextServer.AutowrapMode.Off;
		if (!MegaLabelHelper.IsTooBig(cachedParagraph, base.Text, themeFont, MaxFontSize, lineSpacing, wrap, size))
		{
			SetFontSize(MaxFontSize);
			return;
		}
		if (_lastSetSize >= MinFontSize && _lastSetSize < MaxFontSize && !MegaLabelHelper.IsTooBig(cachedParagraph, base.Text, themeFont, _lastSetSize, lineSpacing, wrap, size) && MegaLabelHelper.IsTooBig(cachedParagraph, base.Text, themeFont, _lastSetSize + 1, lineSpacing, wrap, size))
		{
			SetFontSize(_lastSetSize);
			return;
		}
		int num = MinFontSize;
		int num2 = MaxFontSize;
		while (num2 >= num)
		{
			int num3 = num + (num2 - num) / 2;
			if (num3 == MaxFontSize || MegaLabelHelper.IsTooBig(cachedParagraph, base.Text, themeFont, num3, lineSpacing, wrap, size))
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
}
