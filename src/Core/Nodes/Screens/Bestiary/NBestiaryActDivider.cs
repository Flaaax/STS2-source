using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryActDivider : NButton
{
	private MegaRichTextLabel _nameLabel;

	private static string ScenePath => SceneHelper.GetScenePath("screens/bestiary/bestiary_act_divider");

	private ActModel Act { get; set; }

	public static NBestiaryActDivider Create(ActModel act)
	{
		NBestiaryActDivider nBestiaryActDivider = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NBestiaryActDivider>(PackedScene.GenEditState.Disabled);
		nBestiaryActDivider.Act = act;
		return nBestiaryActDivider;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_nameLabel = GetNode<MegaRichTextLabel>("Label");
		_nameLabel.Text = Act.Title.GetFormattedText();
		_nameLabel.Modulate = StsColors.blue;
	}

	protected override void OnFocus()
	{
	}

	protected override void OnUnfocus()
	{
	}
}
