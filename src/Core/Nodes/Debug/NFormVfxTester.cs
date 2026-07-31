using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx.Forms;

public partial class NFormVfxTester : Node2D
{
	[Export(PropertyHint.None, "")]
	private NFormVfx _formVfx;

	[Export(PropertyHint.None, "")]
	private Node2D _testSpine;

	[Export(PropertyHint.None, "")]
	private string _testBoneName = "";

	private bool _testActiveState = true;

	public override void _Ready()
	{
		_formVfx.ForceTestBoneName(_testBoneName);
		_formVfx.ForceSetSpineSprite(_testSpine);
		_formVfx.SetActive(_testActiveState);
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (base.Visible)
		{
			base._Input(inputEvent);
			if (inputEvent is InputEventKey inputEventKey && inputEventKey.Keycode == Key.A && inputEventKey.Pressed)
			{
				_testActiveState = !_testActiveState;
				_formVfx.SetActive(_testActiveState);
			}
			if (inputEvent is InputEventKey inputEventKey2 && inputEventKey2.Keycode == Key.S && inputEventKey2.Pressed)
			{
				_formVfx?.OnEffectTriggered();
			}
		}
	}
}
