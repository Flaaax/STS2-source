using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.Animation;

/// <summary>
/// A node that automatically plays a single animation on a SpineSprite.
/// There are two conditions that must be true when using this node:
///
/// 1. This node must be the direct child of a SpineSprite node.
/// 2. The parent SpineSprite must have exactly 1 animation.
///
/// The animation is started once the parent SpineSprite's skeleton is ready.
/// If condition 2 isn't met, this node will throw an exception.
/// </summary>
[GlobalClass]
public partial class NSpineAutoPlayer : Node
{
	public override void _Ready()
	{
		MegaSprite sprite = new MegaSprite(GetParent());
		this.RunWhenSpineReady(sprite, delegate(MegaAnimationState animState)
		{
			IReadOnlyList<string> animationNames = sprite.GetSkeleton().GetData().GetAnimationNames();
			if (animationNames.Count != 1)
			{
				throw new InvalidOperationException($"{"NSpineAutoPlayer"}'s parent's skeleton data must have exactly 1 animation. This has {animationNames.Count}.");
			}
			animState.SetAnimation(animationNames[0]);
		});
	}
}
