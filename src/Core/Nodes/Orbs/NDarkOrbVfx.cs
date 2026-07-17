using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NDarkOrbVfx : NOrbVfx
{
	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _superchargedParticles;

	[Export(PropertyHint.None, "")]
	private Node2D _darkBg;

	/// <summary>
	/// Show the supercharged particles when the evoke value is greater than or equal to this value.
	/// </summary>
	[Export(PropertyHint.None, "")]
	private float _superchargeThreshold = 24f;

	private float _darkBgNormalScale = 1f;

	private float _darkBgSuperchargedScale = 1.2f;

	public override void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		base.OnPassiveActivated(passiveVal, evokeVal);
		bool flag = (float)evokeVal >= _superchargeThreshold;
		if (_superchargedParticles != null)
		{
			_superchargedParticles.SetEmitting(flag);
		}
		UpdateDarkBgSize(flag);
		ShakeOrb(HasFocusPower() ? 1f : 0.65f, 0.55f);
	}

	private void UpdateDarkBgSize(bool isSupercharged)
	{
		float num = (isSupercharged ? _darkBgSuperchargedScale : _darkBgNormalScale);
		if (!Mathf.IsEqualApprox(_darkBg.Scale.X, num))
		{
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(_darkBg, "scale", Vector2.One * num, 0.25);
		}
	}

	protected override void OnEvokeInternal(Vector2 targetVfxSpawnPosition)
	{
		base.OnEvokeInternal(targetVfxSpawnPosition);
		NVfxProjectileHandler child = NVfxProjectileHandler.Create("vfx/orbs/dark/vfx_dark_orb_evoke_projectile_handler", "vfx/orbs/dark/vfx_dark_orb_evoke_projectile", base.GlobalPosition, targetVfxSpawnPosition, default(Callable));
		base.VfxContainer.AddChildSafely(child);
	}
}
