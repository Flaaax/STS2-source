using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;

namespace MegaCrit.Sts2.Core.Debug;

/// <summary>
/// A helper script to spawn vfx in whole_screen_vfx_playground
/// </summary>
public partial class NDebugVfxSpawner : Control
{
	private int _links = 10;

	private NChainVisualization _chainVisualization;

	public override void _Ready()
	{
		_chainVisualization.Initialize(_links);
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventKey { Pressed: not false } inputEventKey)
		{
			if (inputEventKey.Keycode == Key.B)
			{
				TaskHelper.RunSafely(_chainVisualization.Break(0f));
			}
			else if (inputEventKey.Keycode == Key.R)
			{
				_chainVisualization.Reset();
			}
			else if (inputEventKey.Keycode == Key.A)
			{
				_links++;
				_chainVisualization.Initialize(_links);
				TaskHelper.RunSafely(_chainVisualization.AnimateInLastLink(0f, null));
			}
			else if (inputEventKey.Keycode == Key.Minus)
			{
				_links--;
				_chainVisualization.Initialize(_links);
			}
			else if (inputEventKey.Keycode == Key.Equal)
			{
				_links++;
				_chainVisualization.Initialize(_links);
			}
		}
	}
}
