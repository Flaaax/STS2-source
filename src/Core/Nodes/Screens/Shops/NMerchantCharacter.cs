using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Shops;

public partial class NMerchantCharacter : Node2D
{
	public override void _Ready()
	{
		this.RunWhenSpineReady(new MegaSprite(GetChild(0)), delegate
		{
			PlayAnimation("relaxed_loop", loop: true);
		});
	}

	public void PlayAnimation(string anim, bool loop = false)
	{
		MegaAnimationState animationState = new MegaSprite(GetChild(0)).GetAnimationState();
		animationState.SetAnimation(anim, loop);
		if (loop)
		{
			using (MegaTrackEntry megaTrackEntry = animationState.GetCurrent(0))
			{
				megaTrackEntry?.SetTrackTime(megaTrackEntry.GetAnimationEnd() * Rng.Chaotic.NextFloat());
			}
		}
	}
}
