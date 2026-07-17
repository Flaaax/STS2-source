using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NVfxSpawner : Control
{
	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed)
		{
			Log.Info("Spawning Vfx!");
			NFailedJoinVfx nFailedJoinVfx = NFailedJoinVfx.Create("<Username> couldn't join. (Reason goes here)");
			if (nFailedJoinVfx != null)
			{
				this.AddChildSafely(nFailedJoinVfx);
			}
		}
	}
}
