using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NThinSliceVfx : Node2D
{
	private const string _scenePath = "res://scenes/vfx/thin_slice_vfx.tscn";

	private CancellationTokenSource? _cts;

	private GpuParticles2D _slash;

	private GpuParticles2D _sparkle;

	private Vector2 _creatureCenter;

	private VfxColor _vfxColor;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>("res://scenes/vfx/thin_slice_vfx.tscn");

	/// <summary>
	/// Thin Slice vfx will intersect the given position. Pass in the creature vfx center position!
	/// </summary>
	/// <param name="target"></param>
	/// <param name="vfxColor"></param>
	/// <returns></returns>
	public static NThinSliceVfx? Create(Creature? target, VfxColor vfxColor = VfxColor.Cyan)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(target);
		if (nCreature == null)
		{
			return null;
		}
		Vector2 vfxSpawnPosition = nCreature.VfxSpawnPosition;
		NThinSliceVfx nThinSliceVfx = PreloadManager.Cache.GetScene("res://scenes/vfx/thin_slice_vfx.tscn").Instantiate<NThinSliceVfx>(PackedScene.GenEditState.Disabled);
		nThinSliceVfx._vfxColor = vfxColor;
		Vector2 vector = new Vector2(Rng.Chaotic.NextFloat(-50f, 50f), Rng.Chaotic.NextFloat(-50f, 50f));
		nThinSliceVfx._creatureCenter = vfxSpawnPosition + vector;
		return nThinSliceVfx;
	}

	public override void _Ready()
	{
		_slash = GetNode<GpuParticles2D>("Slash");
		_slash.GlobalPosition = GenerateSpawnPosition();
		_slash.Rotation = GetAngle();
		_slash.Emitting = true;
		_sparkle = _slash.GetNode<GpuParticles2D>("Sparkle");
		_sparkle.GlobalPosition = _creatureCenter;
		_sparkle.Emitting = true;
		SetColor();
		TaskHelper.RunSafely(SelfDestruct());
	}

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}

	private void SetColor()
	{
		ParticleProcessMaterial particleProcessMaterial = (ParticleProcessMaterial)_slash.GetProcessMaterial();
		switch (_vfxColor)
		{
		case VfxColor.Red:
			particleProcessMaterial.Color = new Color("FF9900");
			break;
		case VfxColor.White:
			particleProcessMaterial.Color = Colors.White;
			break;
		case VfxColor.Cyan:
			particleProcessMaterial.Color = new Color("C4FFE6");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case VfxColor.Green:
		case VfxColor.Blue:
		case VfxColor.Purple:
		case VfxColor.Black:
			break;
		}
	}

	/// <summary>
	/// Spawns our slash particle in a random position around the given centerPoint
	/// </summary>
	private Vector2 GenerateSpawnPosition()
	{
		float s = Rng.Chaotic.NextFloat(0f, (float)Math.PI * 2f);
		float num = Rng.Chaotic.NextFloat(400f, 500f);
		return new Vector2(_creatureCenter.X + num * Mathf.Cos(s), _creatureCenter.Y + num * Mathf.Sin(s));
	}

	/// <summary>
	/// Returns the angle from one vector to another in radians.
	/// Written because Vector2.AngleTo() was confusing
	/// </summary>
	/// <returns></returns>
	private float GetAngle()
	{
		Vector2 vector = _creatureCenter - _slash.GlobalPosition;
		return Mathf.Atan2(vector.Y, vector.X);
	}

	private async Task SelfDestruct()
	{
		_cts?.Cancel();
		_cts = new CancellationTokenSource();
		await Task.Delay(1000, _cts.Token);
		if (!_cts.IsCancellationRequested)
		{
			this.QueueFreeSafely();
		}
	}
}
