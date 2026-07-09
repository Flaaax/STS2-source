using Godot;
using MegaCrit.Sts2.Core.Commands;

public partial class NFrostOrbVfx : NOrbVfx
{
	public override void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		base.OnPassiveActivated(passiveVal, evokeVal);
		ShakeOrb(1f, 0.5f);
		VfxCmd.PlayVfx(GetPlayerVfxPosition(), "vfx/orbs/frost/vfx_frost_orb_passive_shield", base.VfxContainer);
	}

	protected override void OnEvokeInternal(Vector2 targetVfxSpawnPosition)
	{
		base.OnEvokeInternal(targetVfxSpawnPosition);
		VfxCmd.PlayVfx(GetPlayerVfxPosition(), "vfx/orbs/frost/vfx_frost_orb_evoke_shield", base.VfxContainer);
	}
}
