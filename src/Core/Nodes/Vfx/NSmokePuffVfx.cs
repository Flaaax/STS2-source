using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

/// <summary>
/// A nice puff of smoke used by cards and potions.
/// </summary>
public partial class NSmokePuffVfx : Node2D
{
	/// <summary>
	/// Special vfx-only enum as passing in any color can be problematic.
	/// Color treatments require bespoke tuning (i.e. the color of the associated embers)
	/// </summary>
	public enum SmokePuffColor
	{
		Green,
		Purple
	}

	private GpuParticles2D _clouds;

	private GpuParticles2D _ember;

	private SmokePuffColor _color;

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	private static string ScenePath => SceneHelper.GetScenePath("vfx/vfx_smoke_puff");

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}

	public static NSmokePuffVfx? Create(Creature target, SmokePuffColor puffColor)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(target);
		if (creatureNode == null)
		{
			Log.Warn($"Tried to spawn {"NSmokePuffVfx"} on creature {target} without node!");
			return null;
		}
		NSmokePuffVfx nSmokePuffVfx = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NSmokePuffVfx>(PackedScene.GenEditState.Disabled);
		nSmokePuffVfx._color = puffColor;
		nSmokePuffVfx.GlobalPosition = creatureNode.VfxSpawnPosition;
		return nSmokePuffVfx;
	}

	public override void _Ready()
	{
		_ember = GetNode<GpuParticles2D>("Ember");
		_clouds = GetNode<GpuParticles2D>("Clouds");
		_ember.Emitting = true;
		_clouds.Emitting = true;
		if (_color == SmokePuffColor.Purple)
		{
			ParticleProcessMaterial particleProcessMaterial = (ParticleProcessMaterial)_clouds.ProcessMaterial;
			particleProcessMaterial.HueVariationMin = -0.02f;
			particleProcessMaterial.HueVariationMax = 0.02f;
			particleProcessMaterial.Color = new Color("F6B1FF");
		}
		TaskHelper.RunSafely(DeleteAfterComplete());
	}

	private async Task DeleteAfterComplete()
	{
		_cts = new CancellationTokenSource();
		await Task.Delay(2500, _cts.Token);
		this.QueueFreeSafely();
	}
}
