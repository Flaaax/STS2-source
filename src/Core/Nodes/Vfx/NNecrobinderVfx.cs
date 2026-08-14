using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

[GlobalClass]
public partial class NNecrobinderVfx : Node
{
	private Node2D _parent;

	private MegaSprite _animController;

	private Node2D _headRef;

	private ShaderMaterial? _slashShaderMat;

	private float _slashStepBase = 0.04f;

	private float _slashOpacityBase = 0.4f;

	private static readonly StringName _masterStepString = new StringName("master_step");

	private static readonly StringName _opactyString = new StringName("opacity");

	private Tween _tween;

	private Tween _tween2;

	private GpuParticles2D? _scytheFireParticles1;

	private GpuParticles2D? _scytheFireParticles2;

	private GpuParticles2D? _hurtParticles;

	private GpuParticles2D? _lowHealthParticles;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_headRef = _parent.GetNode<Node2D>("HeadBoneNode");
		Node nodeOrNull = _parent.GetNodeOrNull("SlashVfxSlot");
		_slashShaderMat = ((nodeOrNull != null) ? (new MegaSlotNode(nodeOrNull).GetNormalMaterial() as ShaderMaterial) : null);
		_scytheFireParticles1 = _parent.GetNodeOrNull<GpuParticles2D>("ScytheVfxSlot1/ScytheParticles");
		_scytheFireParticles2 = _parent.GetNodeOrNull<GpuParticles2D>("ScytheVfxSlot2/ScytheParticles");
		_hurtParticles = _parent.GetNodeOrNull<GpuParticles2D>("HeadBoneNode/HurtParticles");
		_lowHealthParticles = _parent.GetNodeOrNull<GpuParticles2D>("HeadBoneNode/LowHealthParticles");
		_scytheFireParticles1?.SetEmitting(emitting: false);
		_scytheFireParticles2?.SetEmitting(emitting: false);
		_scytheFireParticles1?.SetOneShot(secs: true);
		_scytheFireParticles2?.SetOneShot(secs: true);
		_hurtParticles?.SetEmitting(emitting: false);
		_hurtParticles?.SetOneShot(secs: true);
		_lowHealthParticles?.SetEmitting(emitting: false);
		_animController = new MegaSprite(_parent);
		_animController.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(UpdateFlameVisibility));
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		switch (new MegaEvent(spineEvent).GetData().GetEventName())
		{
		case "scythe_fx1":
			OnScytheFlame1();
			break;
		case "scythe_fx2":
			OnScytheFlame2();
			break;
		case "attack_slash_start":
			OnAttackSlashStart();
			break;
		case "low_health_start":
			OnLowHealthStart();
			break;
		case "low_health_end":
			OnLowHealthEnd();
			break;
		case "hurt_particles_start":
			OnHurtParticlesStart();
			break;
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

	private void OnAttackSlashStart()
	{
		_slashShaderMat?.SetShaderParameter(_masterStepString, _slashStepBase);
		_tween?.Kill();
		_tween = CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		float num = 0.235f;
		_tween.TweenProperty(_slashShaderMat, "shader_parameter/master_step", num, 0.4000000059604645);
		_slashShaderMat?.SetShaderParameter(_opactyString, _slashOpacityBase);
		_tween2?.Kill();
		_tween2 = CreateTween().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		float num2 = 0f;
		_tween2.TweenProperty(_slashShaderMat, "shader_parameter/opacity", num2, 0.44999998807907104);
	}

	private void OnHurtParticlesStart()
	{
		_hurtParticles?.Restart();
	}

	private void OnLowHealthStart()
	{
		_lowHealthParticles?.SetEmitting(emitting: true);
	}

	private void OnLowHealthEnd()
	{
		_lowHealthParticles?.SetEmitting(emitting: false);
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
		_tween2?.Kill();
	}
}
