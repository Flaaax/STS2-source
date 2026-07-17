using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NVfxProjectileHandler : Node2D
{
	[Export(PropertyHint.None, "")]
	private Curve[] _pathHeightOffsets = Array.Empty<Curve>();

	[Export(PropertyHint.None, "")]
	private Vector2 _heightOffsetRange;

	[Export(PropertyHint.None, "")]
	private Curve[] _movementCurves = Array.Empty<Curve>();

	[Export(PropertyHint.None, "")]
	private Vector2 _travelTimeRange;

	[Export(PropertyHint.None, "")]
	private string _impactParticlesScenePath = "";

	private Vector2 _sourceGlobalPosition;

	private Vector2 _destinationGlobalPosition;

	private string _projectileScenePath;

	private Callable _endAction;

	private NVfxProjectile? _loadedProjectile;

	public static NVfxProjectileHandler? Create(string handlerScenePath, string projectileScenePath, Vector2 sourceGlobalPosition, Vector2 destinationGlobalPosition, Callable endAction)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NVfxProjectileHandler nVfxProjectileHandler = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(handlerScenePath)).Instantiate<NVfxProjectileHandler>(PackedScene.GenEditState.Disabled);
		nVfxProjectileHandler._sourceGlobalPosition = sourceGlobalPosition;
		nVfxProjectileHandler._destinationGlobalPosition = destinationGlobalPosition;
		nVfxProjectileHandler._projectileScenePath = projectileScenePath;
		nVfxProjectileHandler._endAction = endAction;
		return nVfxProjectileHandler;
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlaySequence());
	}

	public override void _ExitTree()
	{
		if (_loadedProjectile != null)
		{
			_loadedProjectile.QueueFreeSafely();
		}
	}

	private async Task PlaySequence()
	{
		_loadedProjectile = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(_projectileScenePath)).Instantiate<NVfxProjectile>(PackedScene.GenEditState.Disabled);
		if (_loadedProjectile == null)
		{
			return;
		}
		this.AddChildSafely(_loadedProjectile);
		_loadedProjectile.GlobalPosition = _sourceGlobalPosition;
		_loadedProjectile.SetEmitting(emitting: true);
		Vector2 vector = (_destinationGlobalPosition - _sourceGlobalPosition).Normalized();
		Vector2 normal = new Vector2(vector.Y, 0f - vector.X);
		if ((_sourceGlobalPosition + normal).Y < _sourceGlobalPosition.Y)
		{
			normal *= -1f;
		}
		float num = 0f;
		float projectileDuration = (float)GD.RandRange(_travelTimeRange.X, _travelTimeRange.Y);
		float projectileHeightOffset = (float)GD.RandRange(_heightOffsetRange.X, _heightOffsetRange.Y);
		Curve chosenMovementCurve = _movementCurves[Mathf.RoundToInt(GD.RandRange(0.0, _movementCurves.Length - 1))];
		Curve chosenHeightCurve = _pathHeightOffsets[Mathf.RoundToInt(GD.RandRange(0.0, _pathHeightOffsets.Length - 1))];
		while (num < projectileDuration)
		{
			float offset = num / projectileDuration;
			float weight = chosenMovementCurve.Sample(offset);
			float num2 = chosenHeightCurve.Sample(offset);
			Vector2 vector2 = _sourceGlobalPosition.Lerp(_destinationGlobalPosition, weight);
			Vector2 vector3 = normal * projectileHeightOffset * num2;
			Vector2 vector4 = vector2 + vector3;
			if (_loadedProjectile.AlignToVelocity)
			{
				Vector2 vector5 = vector4 - _loadedProjectile.GlobalPosition;
				float globalRotation = Mathf.Atan2(vector5.Y, vector5.X);
				_loadedProjectile.GlobalRotation = globalRotation;
			}
			_loadedProjectile.GlobalPosition = vector4;
			float num3 = num;
			num = num3 + await this.AwaitProcessFrame();
		}
		_loadedProjectile.GlobalPosition = _destinationGlobalPosition;
		_loadedProjectile.SetEmitting(emitting: false);
		SpawnImpactVfx(_destinationGlobalPosition);
		if ((object)_endAction.Delegate != null)
		{
			_endAction.Call();
		}
		TaskHelper.RunSafely(DelayedFree());
	}

	private void SpawnImpactVfx(Vector2 spawnPosition)
	{
		if (!string.IsNullOrEmpty(_impactParticlesScenePath))
		{
			Control vfxContainer = GetParent<Control>();
			if (NCombatRoom.Instance != null)
			{
				vfxContainer = NCombatRoom.Instance.CombatVfxContainer;
			}
			VfxCmd.PlayVfx(spawnPosition, _impactParticlesScenePath, vfxContainer);
		}
	}

	private async Task DelayedFree()
	{
		await Cmd.Wait(2f);
		this.QueueFreeSafely();
	}
}
