using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NByrdineAttackVfx : Node2D
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/monsters/byrdine_overlay_vfx");

	private Creature _creature;

	public static NByrdineAttackVfx? Create(Creature creature)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NByrdineAttackVfx nByrdineAttackVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NByrdineAttackVfx>(PackedScene.GenEditState.Disabled);
		nByrdineAttackVfx._creature = creature;
		nByrdineAttackVfx._creature.GetVfxContainer().AddChildSafely(nByrdineAttackVfx);
		nByrdineAttackVfx.GlobalPosition = NGame.Instance.GetViewportRect().Size * 0.5f;
		return nByrdineAttackVfx;
	}

	public override void _Ready()
	{
		MegaSprite megaSprite = new MegaSprite(GetNode("SpineSprite"));
		_creature.Monster?.SetupSkins(megaSprite, megaSprite.GetSkeleton());
		megaSprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>(AnimationEnded));
		this.RunWhenSpineReady(megaSprite, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("attack_overlay", loop: false);
		});
	}

	private void AnimationEnded(GodotObject _, GodotObject __, GodotObject ___)
	{
		this.QueueFreeSafely();
	}
}
