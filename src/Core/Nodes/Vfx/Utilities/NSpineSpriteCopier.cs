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
			_targetSpineSprite.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStart));
		}
	}

	private void OnAnimationStart(GodotObject spineSprite, GodotObject animationState, GodotObject trackEntry)
	{
		if (_selfSpineSprite == null || _targetSpineSprite == null)
		{
			return;
		}
		string currentAnimationName = _targetSpineSprite.GetAnimationState().GetCurrentAnimationName();
		if (currentAnimationName == null)
		{
			return;
		}
		using MegaTrackEntry megaTrackEntry = _targetSpineSprite.GetAnimationState().GetCurrent(0);
		if (megaTrackEntry != null)
		{
			bool loop = megaTrackEntry.IsLoop();
			_selfSpineSprite.GetAnimationState().SetAnimation(currentAnimationName, loop);
		}
	}
}
