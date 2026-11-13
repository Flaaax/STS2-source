using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Nodes.Screens;

/// <summary>
/// Sets the loc, that's it.
/// Yes, I know the text is in bestiary.json, it's placeholder and I don't want it retranslated.
/// </summary>
public partial class NUnderConstruction : Control
{
	public override void _Ready()
	{
		GetNode<MegaRichTextLabel>("%Label").SetTextAutoSize(new LocString("bestiary", "UNDER_CONSTRUCTION").GetRawText());
	}
}
