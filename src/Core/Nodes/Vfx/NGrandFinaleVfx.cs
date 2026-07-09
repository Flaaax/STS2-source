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

public partial class NGrandFinaleVfx : Node2D
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/vfx_grand_finale");

	[Export(PropertyHint.None, "")]
	private Node2D? _spotlight;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _spotlightParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _anticipationParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _slashParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _endParticles;

	private CancellationTokenSource? _cts;

	private static readonly float _spotlightDuration = 1.25f;

	private static readonly float _anticipationDuration = 0.25f;

	private static readonly float _slashDuration = 0.125f;

	private static readonly float _hitDuration = 0.0125f;

	public static readonly float totalAnticipationDuration = _spotlightDuration + _anticipationDuration + _slashDuration;

	public static NGrandFinaleVfx? Create(Creature creature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
		if (nCreature != null)
		{
			return Create(nCreature.VfxSpawnPosition);
		}
		return null;
	}

	public static NGrandFinaleVfx? Create(Vector2 playerPosition)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NGrandFinaleVfx nGrandFinaleVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NGrandFinaleVfx>(PackedScene.GenEditState.Disabled);
		nGrandFinaleVfx.Initialize(playerPosition);
		return nGrandFinaleVfx;
	}

	private void Initialize(Vector2 playerPosition)
	{
		_anticipationParticles.GlobalPosition = playerPosition;
		_slashParticles.GlobalPosition = playerPosition;
		_endParticles.GlobalPosition = playerPosition;
		_spotlight.Modulate = new Color(1f, 1f, 1f, 0f);
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
		_spotlightParticles.GlobalPosition = new Vector2(GetViewportRect().Size.X / 2f, 0f);
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(_spotlight, "modulate", new Color(1f, 1f, 1f), 1.0);
		_spotlightParticles.Restart();
		await Cmd.Wait(_spotlightDuration, _cts.Token);
		_anticipationParticles.Restart();
		await Cmd.Wait(_anticipationDuration, _cts.Token);
		_slashParticles.Restart();
		await Cmd.Wait(_slashDuration, _cts.Token);
		NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal);
		await Cmd.Wait(_hitDuration, _cts.Token);
		_endParticles.Restart();
		Tween tween2 = GetTree().CreateTween();
		tween2.TweenProperty(_spotlight, "modulate", new Color(1f, 1f, 1f, 0f), 0.5);
		await Cmd.Wait(2f, _cts.Token);
		this.QueueFreeSafely();
	}
}
