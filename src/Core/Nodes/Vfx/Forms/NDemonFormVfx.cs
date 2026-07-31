using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Forms;

public partial class NDemonFormVfx : NFormVfx
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/forms/demon/vfx_demon_form_idle_vfx");

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _effectTriggeredParticles;

	public static NDemonFormVfx? Create(Creature target)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(target);
		if (creatureNode == null)
		{
			return null;
		}
		NDemonFormVfx nDemonFormVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NDemonFormVfx>(PackedScene.GenEditState.Disabled);
		nDemonFormVfx.Initialize(target.Player);
		creatureNode.Visuals.AddFormVfx(nDemonFormVfx);
		return nDemonFormVfx;
	}

	public override void OnEffectTriggered()
	{
		base.OnEffectTriggered();
		_effectTriggeredParticles?.Restart();
	}
}
