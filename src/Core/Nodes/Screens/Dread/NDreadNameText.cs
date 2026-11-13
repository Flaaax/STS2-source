using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.RichTextTags;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

public partial class NDreadNameText : Control
{
	private MegaRichTextLabel _nameLabel;

	private MegaRichTextLabel _shadowLabel;

	private RichTextAncientBanner _nameEffect;

	private Tween? _tween;

	public override void _Ready()
	{
		_nameLabel = GetNode<MegaRichTextLabel>("%Name");
		_shadowLabel = GetNode<MegaRichTextLabel>("%Shadow");
		_nameEffect = new RichTextAncientBanner();
		_nameLabel.InstallEffect(_nameEffect);
		_shadowLabel.InstallEffect(_nameEffect);
	}

	public void SetNameText(CharacterModel characterModel)
	{
		SetNameText(characterModel.ShortTitle, characterModel.DreadNameColor);
	}

	public void SetNameText(LocString locString, Color color)
	{
		string formattedText = locString.GetFormattedText();
		string text = formattedText.ToUpper();
		_nameEffect.CenterCharacter = GetTextCenterGlyphIndex(text, _nameLabel.GetThemeFont(ThemeConstants.RichTextLabel.NormalFont, "RichTextLabel"), _nameLabel.GetThemeFontSize(ThemeConstants.RichTextLabel.NormalFontSize, "RichTextLabel"));
		_nameLabel.SelfModulate = color;
		_nameLabel.Text = "[ancient_banner]" + text + "[/ancient_banner]";
		_shadowLabel.Text = _nameLabel.Text;
		TaskHelper.RunSafely(AnimateVfx());
	}

	/// <summary>
	/// Copy of what's seen in NAncientNameBanner.cs
	/// </summary>
	private float GetTextCenterGlyphIndex(string text, Font font, int fontSize)
	{
		using TextParagraph textParagraph = new TextParagraph();
		textParagraph.AddString(text, font, fontSize);
		TextServer primaryInterface = TextServerManager.Singleton.GetPrimaryInterface();
		Array<Dictionary> array = primaryInterface.ShapedTextGetGlyphs(textParagraph.GetLineRid(0));
		float num = array.Sum((Dictionary glyph) => glyph.GetValueOrDefault("advance").AsSingle());
		float num2 = 0f;
		int num3 = 0;
		foreach (Dictionary item in array)
		{
			float num4 = item.GetValueOrDefault("advance").AsSingle();
			num2 += num4;
			if (num2 > num * 0.5f)
			{
				return (float)num3 + (num * 0.5f - (num2 - num4)) / num4;
			}
			num3++;
		}
		return 0f;
	}

	private async Task AnimateVfx()
	{
		UpdateGlyphSpace(0f);
		UpdateTransform(1f);
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenMethod(Callable.From<float>(UpdateGlyphSpace), 0f, 1f, 1.0).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		if (await _tween.AwaitFinished(this) == TweenOutcome.Finished)
		{
			this.IsValid();
		}
	}

	private void UpdateTransform(float obj)
	{
		_nameEffect.Rotation = obj;
	}

	private void UpdateGlyphSpace(float spacing)
	{
		_nameEffect.Spacing = 40f + spacing * 5f;
	}
}
