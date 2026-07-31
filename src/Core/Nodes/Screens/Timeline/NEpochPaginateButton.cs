using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

public partial class NEpochPaginateButton : NGoldArrowButton
{
	private bool _isLeft;

	/// <summary>
	/// Whether this arrow is facing left or right
	/// </summary>
	public bool IsLeft
	{
		get
		{
			return _isLeft;
		}
		set
		{
			if (_isLeft != value)
			{
				UnregisterHotkeys();
				_isLeft = value;
				RegisterHotkeys();
				UpdateControllerButton();
			}
		}
	}

	protected override string[] Hotkeys => new string[1] { _isLeft ? MegaInput.left : MegaInput.right };

	protected override string ClickedSfx => "event:/sfx/ui/timeline/ui_timeline_click";

	protected override void OnDisable()
	{
		base.OnDisable();
		base.Visible = false;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		base.Visible = true;
	}
}
