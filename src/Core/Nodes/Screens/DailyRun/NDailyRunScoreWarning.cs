using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

/// <summary>
/// Displays when the player's already uploaded a score for today's daily.
/// When hovered, displays info about why the score won't be uploaded.
/// </summary>
public partial class NDailyRunScoreWarning : NClickableControl
{
	public override void _Ready()
	{
		ConnectSignals();
	}

	protected override void OnFocus()
	{
		HoverTip hoverTip = new HoverTip(new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.title"), new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.description"));
		NHoverTipSet.CreateAndShow(this, hoverTip, HoverTipAlignment.Left);
		base.Scale = Vector2.One * 1.1f;
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
		base.Scale = Vector2.One;
	}
}
