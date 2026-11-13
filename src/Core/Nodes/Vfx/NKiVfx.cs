using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NKiVfx : Node2D
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/fool/ki_vfx");

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public static NKiVfx? Create(Creature creature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature creatureNode = creature.GetCreatureNode();
		if (creatureNode != null)
		{
			return Create(creatureNode.GetTopOfHitbox());
		}
		return null;
	}

	private static NKiVfx? Create(Vector2 topOfCreature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NKiVfx nKiVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NKiVfx>(PackedScene.GenEditState.Disabled);
		nKiVfx.GlobalPosition = topOfCreature + new Vector2(0f, -50f);
		return nKiVfx;
	}

	public override void _Ready()
	{
		foreach (GpuParticles2D item in GetChildren().OfType<GpuParticles2D>())
		{
			item.Emitting = true;
		}
		SfxCmd.Play("event:/sfx/characters/fugitive/fugitive_ki");
		TaskHelper.RunSafely(PlaySequence());
	}

	private async Task PlaySequence()
	{
		_cts = new CancellationTokenSource();
		await Cmd.Wait(2f, _cts.Token);
		this.QueueFreeSafely();
	}

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}
}
