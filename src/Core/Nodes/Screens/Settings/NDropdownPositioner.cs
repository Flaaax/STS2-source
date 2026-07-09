using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

/// <summary>
/// Helper script to allow a Node to become anchored to this Node.
/// Used specifically for positioning the DropdownContainers (they're tricky due to how they're layered)
/// </summary>
public partial class NDropdownPositioner : Control
{
	[Export(PropertyHint.None, "")]
	private Control _dropdownNode;

	public override void _Ready()
	{
		Connect(Control.SignalName.FocusEntered, Callable.From(delegate
		{
			_dropdownNode.TryGrabFocus();
		}));
		Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnVisibilityChange));
		OnVisibilityChange();
	}

	private void OnVisibilityChange()
	{
		if (base.Visible)
		{
			_dropdownNode.FocusNeighborBottom = base.FocusNeighborBottom;
			_dropdownNode.FocusNeighborTop = base.FocusNeighborTop;
			_dropdownNode.FocusNeighborLeft = base.FocusNeighborLeft;
			_dropdownNode.FocusNeighborRight = base.FocusNeighborRight;
		}
	}

	public override void _Process(double delta)
	{
		_dropdownNode.GlobalPosition = base.GlobalPosition;
	}
}
