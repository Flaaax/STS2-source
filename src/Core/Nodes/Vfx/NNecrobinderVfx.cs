using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

[GlobalClass]
public partial class NNecrobinderVfx : Node
{
	private Node2D _parent;

	private MegaSprite _animController;

	private Node2D _headRef;

	private GpuParticles2D? _scytheFireParticles1;

	private GpuParticles2D? _scytheFireParticles2;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_headRef = _parent.GetNode<Node2D>("HeadBoneNode");
		_scytheFireParticles1 = _parent.GetNodeOrNull<GpuParticles2D>("ScytheVfxSlot1/ScytheParticles");
		_scytheFireParticles2 = _parent.GetNodeOrNull<GpuParticles2D>("ScytheVfxSlot2/ScytheParticles");
		_scytheFireParticles1?.SetEmitting(emitting: false);
		_scytheFireParticles2?.SetEmitting(emitting: false);
		_scytheFireParticles1?.SetOneShot(secs: true);
		_scytheFireParticles2?.SetOneShot(secs: true);
		_animController = new MegaSprite(_parent);
		_animController.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(UpdateFlameVisibility));
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		string eventName = new MegaEvent(spineEvent).GetData().GetEventName();
		if (!(eventName == "scythe_fx1"))
		{
			if (eventName == "scythe_fx2")
			{
				OnScytheFlame2();
			}
		}
		else
		{
			OnScytheFlame1();
		}
	}

	private void UpdateFlameVisibility(GodotObject spineSprite, GodotObject animationState, GodotObject trackEntry)
	{
		_headRef.Visible = new MegaAnimationState(animationState).GetCurrentAnimationName() != "die";
	}

	private void OnScytheFlame1()
	{
		_scytheFireParticles1?.Restart();
	}

	private void OnScytheFlame2()
	{
		_scytheFireParticles2?.Restart();
	}
}
