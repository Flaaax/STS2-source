using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.RichTextTags;

/// <summary>
/// Scrambles the text to be one of the replacement characters over time.
/// Must use a monospace font (or have the tag [code]) for the spacing to be correct.
/// </summary>
[GlobalClass]
[Tool]
public partial class RichTextScramble : AbstractMegaRichTextEffect
{
	public new string bbcode = "scramble";

	private const string _replacementCharacters = "<!0{541#00>";

	protected override string Bbcode => bbcode;

	public override bool _ProcessCustomFX(CharFXTransform charFx)
	{
		if (!ShouldTransformText())
		{
			return false;
		}
		Dictionary env = charFx.Env;
		uint num = GlyphConverter.CharToGlyphIdx(charFx.Font, ' ');
		if (charFx.GlyphIndex != num)
		{
			double num2 = charFx.ElapsedTime + (double)charFx.GlyphIndex * 10.2 + (double)(charFx.Range.X * 2);
			num2 *= 4.0;
			if (charFx.Env.TryGetValue("speed", out var value))
			{
				num2 *= (double)value.AsSingle();
			}
			if (Mathf.Sin(num2) <= 0.0 || !charFx.Env.ContainsKey("showBaseChar"))
			{
				char c = "<!0{541#00>"[(int)num2 % "<!0{541#00>".Length];
				charFx.SetGlyphIndex(GlyphConverter.CharToGlyphIdx(charFx.Font, c));
			}
		}
		if (env.TryGetValue(RichTextUtil.colorKey, out var value2))
		{
			charFx.Color = (Color)value2;
		}
		charFx.Visible = !env.ContainsKey(RichTextUtil.visibleKey) || (bool)env[RichTextUtil.visibleKey];
		return true;
	}
}
