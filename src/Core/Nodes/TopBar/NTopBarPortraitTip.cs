using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.sts2.Core.Nodes.TopBar;

/// <summary>
/// Allows this UI element to be hovered and display character/ascension/achievement info.
/// </summary>
public partial class NTopBarPortraitTip : NClickableControl
{
	private IHoverTip _hoverTip;

	public override void _Ready()
	{
		ConnectSignals();
	}

	public void Initialize(IRunState runState)
	{
		_hoverTip = AscensionHelper.GetPortraitHoverTip(runState);
		base.FocusMode = FocusModeEnum.All;
	}

	protected override void OnFocus()
	{
		NHoverTipSet.CreateAndShow(this, _hoverTip)?.SetGlobalPosition(base.GlobalPosition + new Vector2(0f, base.Size.Y + 20f));
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
	}
}
