using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.RichTextTags;

[GlobalClass]
[Tool]
public partial class RichTextYellow : AbstractMegaRichTextEffect
{
	public new string bbcode = "yellow";

	protected override string Bbcode => bbcode;

	public override bool _ProcessCustomFX(CharFXTransform charFx)
	{
		charFx.Color = StsColors.yellow;
		return true;
	}
}
