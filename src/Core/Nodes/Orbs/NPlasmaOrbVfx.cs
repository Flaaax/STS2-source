using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NPlasmaOrbVfx : NOrbVfx
{
	[Export(PropertyHint.None, "")]
	private Vector2 _projectileOffsetRange;

	[Export(PropertyHint.None, "")]
	private float _projectileSpawnInterval = 0.05f;

	public override void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		base.OnPassiveActivated(passiveVal, evokeVal);
		ShakeOrb(1f, 0.5f);
		TaskHelper.RunSafely(SpawnProjectile(1, GetPlayerVfxPosition()));
	}

	protected override void OnEvokeInternal(Vector2 targetVfxSpawnPosition)
	{
		base.OnEvokeInternal(targetVfxSpawnPosition);
		TaskHelper.RunSafely(SpawnProjectile(2, GetPlayerVfxPosition()));
	}

	private async Task SpawnProjectile(int count, Vector2 targetPosition)
	{
		for (int i = 0; i < count; i++)
		{
			NVfxProjectileHandler child = NVfxProjectileHandler.Create("vfx/orbs/plasma/vfx_plasma_orb_projectile_handler", "vfx/orbs/plasma/vfx_plasma_orb_projectile", base.GlobalPosition + GetRandomOffset(), targetPosition, (i == count - 1) ? Callable.From(delegate
			{
			}) : default(Callable));
			base.VfxContainer.AddChildSafely(child);
			if (i != count - 1)
			{
				await Cmd.Wait(_projectileSpawnInterval);
			}
		}
	}

	private Vector2 GetRandomOffset()
	{
		float s = Mathf.DegToRad(GD.Randf() * 360f);
		float num = Mathf.Lerp(_projectileOffsetRange.X, _projectileOffsetRange.Y, GD.Randf());
		return new Vector2(Mathf.Cos(s) * num, Mathf.Sin(s) * num);
	}
}
