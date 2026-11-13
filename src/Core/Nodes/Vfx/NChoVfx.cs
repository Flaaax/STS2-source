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

public partial class NChoVfx : Node2D
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/fool/cho_vfx");

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public static NChoVfx? Create(Creature creature)
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

	private static NChoVfx? Create(Vector2 topOfCreature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NChoVfx nChoVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NChoVfx>(PackedScene.GenEditState.Disabled);
		nChoVfx.GlobalPosition = topOfCreature + new Vector2(0f, -50f);
		return nChoVfx;
	}

	public override void _Ready()
	{
		foreach (GpuParticles2D item in GetChildren().OfType<GpuParticles2D>())
		{
			item.Emitting = true;
		}
		SfxCmd.Play("event:/sfx/characters/fugitive/fugitive_cho");
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
