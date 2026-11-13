using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

public partial class NResolutionDropdownItem : NDropdownItem
{
	public Vector2I resolution;

	public void Init(Vector2I setResolution)
	{
		resolution = setResolution;
		string textAutoSize = NResolutionDropdown.FormatResolution(resolution);
		_label.SetTextAutoSize(textAutoSize);
	}
}
