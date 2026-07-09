using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.sts2.Core.Nodes.TopBar;

public partial class NTopBarFloorIcon : NClickableControl
{
	private MegaLabel _floorNumLabel;

	private IRunState _runState;

	public override void _Ready()
	{
		_floorNumLabel = GetNode<MegaLabel>("FloorNumLabel");
		ConnectSignals();
	}

	public void Initialize(IRunState runState)
	{
		_runState = runState;
		UpdateIcon();
	}

	public override void _EnterTree()
	{
		RunManager.Instance.RoomEntered += UpdateIcon;
	}

	public override void _ExitTree()
	{
		RunManager.Instance.RoomEntered -= UpdateIcon;
	}

	private void UpdateIcon()
	{
		if (_runState.CurrentRoom != null)
		{
			_floorNumLabel.SetTextAutoSize(_runState.TotalFloor.ToString());
		}
	}

	protected override void OnFocus()
	{
		HoverTip hoverTip = new HoverTip(new LocString("static_hover_tips", "FLOOR.title"), new LocString("static_hover_tips", "FLOOR.description"));
		NHoverTipSet.CreateAndShow(this, hoverTip)?.SetGlobalPosition(base.GlobalPosition + new Vector2(0f, base.Size.Y + 20f));
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
	}
}
