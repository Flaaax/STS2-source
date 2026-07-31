using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NSpineSpriteCopier : Node2D
{
	private MegaSprite? _selfSpineSprite;

	private MegaSprite? _targetSpineSprite;

	public void Initialize(MegaSprite targetSprite, Node2D sourceNode)
	{
		base.Position = sourceNode.Position;
		base.Scale = sourceNode.Scale;
		if (_selfSpineSprite == null)
		{
			_selfSpineSprite = new MegaSprite(this);
		}
		_targetSpineSprite = targetSprite;
		if (_targetSpineSprite != null && _selfSpineSprite != null)
		{
			_selfSpineSprite.SetSkeletonDataRes(_targetSpineSprite.GetSkeleton().GetData());
		}
	}

	public override void _Process(double delta)
	{
		if (_selfSpineSprite != null && _targetSpineSprite != null)
		{
			_targetSpineSprite.GetAnimationState().Apply(_selfSpineSprite.GetSkeleton());
		}
	}
}
