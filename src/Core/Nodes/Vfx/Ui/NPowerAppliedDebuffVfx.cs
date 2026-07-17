using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Ui;

public partial class NPowerAppliedDebuffVfx : Node2D
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/ui/vfx_debuff_applied");

	[Export(PropertyHint.None, "")]
	private Array<GpuParticles2D> _particles = new Array<GpuParticles2D>();

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(scenePath);

	public static NPowerAppliedDebuffVfx? Create(Vector2 globalPosition)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NPowerAppliedDebuffVfx nPowerAppliedDebuffVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NPowerAppliedDebuffVfx>(PackedScene.GenEditState.Disabled);
		nPowerAppliedDebuffVfx.GlobalPosition = globalPosition;
		return nPowerAppliedDebuffVfx;
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlaySequence());
	}

	private async Task PlaySequence()
	{
		_cts = new CancellationTokenSource();
		foreach (GpuParticles2D particle in _particles)
		{
			particle.Restart();
		}
		await Cmd.Wait(2f, _cts.Token);
		this.QueueFreeSafely();
	}

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}
}
