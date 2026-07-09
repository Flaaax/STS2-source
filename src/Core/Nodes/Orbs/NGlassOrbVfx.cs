using Godot;
using MegaCrit.Sts2.Core.Commands;

public partial class NGlassOrbVfx : NOrbVfx
{
	[Export(PropertyHint.None, "")]
	private GpuParticles2D _passiveChromaticAberration;

	[Export(PropertyHint.None, "")]
	private float _basePassiveChromaticAberrationStength = 0.01f;

	private decimal _basePassiveVal = 4m;

	private static readonly StringName _aberrationStrengthString = new StringName("instance_shader_parameters/base_intensity");

	public override void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		if (!(passiveVal <= 0m))
		{
			base.OnPassiveActivated(passiveVal, evokeVal);
			float num = (float)(passiveVal / _basePassiveVal);
			_passiveChromaticAberration.Set(_aberrationStrengthString, Mathf.Lerp(0f, _basePassiveChromaticAberrationStength, num));
			ShakeOrb(num, 0.5f);
		}
	}

	public override void AfterPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		base.AfterPassiveActivated(passiveVal, evokeVal);
		UpdateFocusPowerState();
	}

	protected override bool HasFocusPower()
	{
		if (_orbModel != null && _orbModel.PassiveVal <= 0m)
		{
			return false;
		}
		return base.HasFocusPower();
	}

	public void ShowPassiveImpact(Vector2[] targetVfxSpawnPositions)
	{
		for (int i = 0; i < targetVfxSpawnPositions.Length; i++)
		{
			ShowPassiveImpact(targetVfxSpawnPositions[i]);
		}
	}

	private void ShowPassiveImpact(Vector2 targetVfxSpawnPosition)
	{
		VfxCmd.PlayVfx(targetVfxSpawnPosition, "vfx/orbs/glass/vfx_glass_orb_passive_impact", base.VfxContainer);
	}

	protected override void OnEvokeInternal(Vector2 targetVfxSpawnPosition)
	{
		base.OnEvokeInternal(targetVfxSpawnPosition);
		VfxCmd.PlayVfx(targetVfxSpawnPosition, "vfx/orbs/glass/vfx_glass_orb_evoke_impact", base.VfxContainer);
	}
}
