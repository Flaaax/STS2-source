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

public partial class NPowerAppliedBuffVfx : Node2D
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/ui/vfx_buff_applied");

	[Export(PropertyHint.None, "")]
	private Array<GpuParticles2D> _particles = new Array<GpuParticles2D>();

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}

	public static NPowerAppliedBuffVfx? Create(Vector2 globalPosition)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NPowerAppliedBuffVfx nPowerAppliedBuffVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NPowerAppliedBuffVfx>(PackedScene.GenEditState.Disabled);
		nPowerAppliedBuffVfx.GlobalPosition = globalPosition;
		return nPowerAppliedBuffVfx;
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
}
