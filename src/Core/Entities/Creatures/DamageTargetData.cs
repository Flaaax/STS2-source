namespace MegaCrit.Sts2.Core.Entities.Creatures;

public record struct DamageTargetData
{
	public Creature target;

	public string? hitVfx;

	public bool playVfxOnCreatureCenter;

	public DamageTargetData(Creature target)
	{
		hitVfx = null;
		playVfxOnCreatureCenter = false;
		this.target = target;
	}
}
