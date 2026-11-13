using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

public partial class NCustomGameModeDropdownItem : NDropdownItem
{
	public GameMode GameMode { get; set; }

	public void Init(GameMode gameMode)
	{
		GameMode = gameMode;
		base.Text = NCustomGameModeDropdown.GetGameModeLoc(gameMode).GetFormattedText();
	}
}
