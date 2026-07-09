using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryLabelDivider : NButton
{
	private MegaRichTextLabel _nameLabel;

	private static string ScenePath => SceneHelper.GetScenePath("screens/bestiary/bestiary_label_divider");

	private LocString LocString { get; set; }

	public static NBestiaryLabelDivider Create(ActModel act)
	{
		NBestiaryLabelDivider nBestiaryLabelDivider = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NBestiaryLabelDivider>(PackedScene.GenEditState.Disabled);
		nBestiaryLabelDivider.LocString = act.Title;
		return nBestiaryLabelDivider;
	}

	public static NBestiaryLabelDivider Create(LocString locString)
	{
		NBestiaryLabelDivider nBestiaryLabelDivider = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NBestiaryLabelDivider>(PackedScene.GenEditState.Disabled);
		nBestiaryLabelDivider.LocString = locString;
		return nBestiaryLabelDivider;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_nameLabel = GetNode<MegaRichTextLabel>("Label");
		_nameLabel.Text = LocString.GetFormattedText();
		_nameLabel.Modulate = StsColors.blue;
	}

	protected override void OnFocus()
	{
	}

	protected override void OnUnfocus()
	{
	}
}
