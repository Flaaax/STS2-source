using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Orbs;

public partial class NLightningOrbVfx : NOrbVfx
{
	public override void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		base.OnPassiveActivated(passiveVal, evokeVal);
		ShakeOrb(1f, 0.4f);
	}
}
