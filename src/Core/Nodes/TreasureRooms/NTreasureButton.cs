using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.TreasureRooms;

public partial class NTreasureButton : NButton
{
	private Node2D _chestNode;

	private MegaSprite _chestAnimController;

	private MegaSkin? _regularChestSkin;

	private MegaSkin? _outlineChestSkin;

	protected override string[] Hotkeys => new string[1] { MegaInput.select };

	public override void _Ready()
	{
		ConnectSignals();
		_chestNode = GetNode<Node2D>("%ChestVisual");
		_chestAnimController = new MegaSprite(_chestNode);
	}

	public void Setup(ActModel act)
	{
		_chestAnimController.SetSkeletonDataRes(act.ChestSpineResource);
		MegaSkeleton skeleton = _chestAnimController.GetSkeleton();
		if (skeleton != null)
		{
			MegaSkeletonDataResource data = skeleton.GetData();
			_regularChestSkin = data.FindSkin(act.ChestSpineSkinNameNormal);
			_outlineChestSkin = data.FindSkin(act.ChestSpineSkinNameStroke);
			skeleton.SetSlotsToSetupPose();
			_chestAnimController.GetAnimationState().Apply(skeleton);
			MegaAnimationState animationState = _chestAnimController.GetAnimationState();
			animationState.SetAnimation("animation", loop: false);
			_chestAnimController.GetAnimationState().AddAnimation("shine_fade", 0f, loop: false);
			animationState.SetTimeScale(0f);
			UpdateChestSkin(showOutline: false);
		}
	}

	public void AnimOut()
	{
		_chestAnimController.GetAnimationState().SetTimeScale(1f);
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(_chestNode, "modulate", StsColors.halfTransparentWhite, 0.4);
	}

	public void AnimIn()
	{
		_chestAnimController.GetAnimationState().SetTimeScale(1f);
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(_chestNode, "modulate", Colors.White, 0.3);
	}

	public void UpdateChestSkin(bool showOutline)
	{
		MegaSkeleton skeleton = _chestAnimController.GetSkeleton();
		if (skeleton != null)
		{
			skeleton.SetSkin(showOutline ? _outlineChestSkin : _regularChestSkin);
			skeleton.SetSlotsToSetupPose();
			_chestAnimController.GetAnimationState().Apply(skeleton);
		}
	}
}
