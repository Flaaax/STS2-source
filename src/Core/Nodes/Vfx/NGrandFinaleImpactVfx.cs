using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NGrandFinaleImpactVfx : Node2D
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/vfx_grand_finale_impact");

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _centerParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _groundParticles;

	private CancellationTokenSource? _cts;

	public static NGrandFinaleImpactVfx? Create(Creature creature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
		if (nCreature != null)
		{
			return Create(nCreature.VfxSpawnPosition, nCreature.GetBottomOfHitbox());
		}
		return null;
	}

	public static NGrandFinaleImpactVfx? Create(Vector2 targetCenterPosition, Vector2 targetGroundPosition)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NGrandFinaleImpactVfx nGrandFinaleImpactVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NGrandFinaleImpactVfx>(PackedScene.GenEditState.Disabled);
		nGrandFinaleImpactVfx.InitializePositions(targetCenterPosition, targetGroundPosition);
		return nGrandFinaleImpactVfx;
	}

	private void InitializePositions(Vector2 targetCenterPosition, Vector2 targetGroundPosition)
	{
		_centerParticles.GlobalPosition = targetCenterPosition;
		_groundParticles.GlobalPosition = targetGroundPosition;
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlaySequence());
	}

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}

	private async Task PlaySequence()
	{
		_cts = new CancellationTokenSource();
		_centerParticles.Restart();
		_groundParticles.Restart();
		await Cmd.Wait(2f, _cts.Token);
		this.QueueFreeSafely();
	}
}
