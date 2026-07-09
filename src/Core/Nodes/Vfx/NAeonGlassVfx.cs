using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NAeonGlassVfx : Node
{
	private bool _ringsSpinningNormal;

	private string? _curAnimName;

	private static readonly StringName _progressString = new StringName("Progress");

	private ShaderMaterial? _rayShaderMat;

	private static readonly StringName _scrollSpeedString = new StringName("ScrollSpeed");

	private ShaderMaterial? _liquidShaderMat;

	private float _baseScrollSpeed;

	private Node2D _parent;

	private MegaSprite _animController;

	private GpuParticles2D _witherParticles;

	private GpuParticles2D _leakParticles;

	private GpuParticles2D _shardParticles;

	private GpuParticles2D _dumpParticles;

	private GpuParticles2D _topSparkParticles;

	private GpuParticles2D _bottomSparkParticles;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_animController = new MegaSprite(_parent);
		_rayShaderMat = new MegaSlotNode(_parent.GetNode("RaySlot")).GetNormalMaterial() as ShaderMaterial;
		_liquidShaderMat = new MegaSlotNode(_parent.GetNode("LiquidSlot")).GetNormalMaterial() as ShaderMaterial;
		if (_liquidShaderMat != null)
		{
			_baseScrollSpeed = (float)_liquidShaderMat.GetShaderParameter(_scrollSpeedString);
		}
		_witherParticles = _parent.GetNode<GpuParticles2D>("WitherSlot/WitherParticles");
		_leakParticles = _parent.GetNode<GpuParticles2D>("LiquidSlot/LeakParticles");
		_shardParticles = _parent.GetNode<GpuParticles2D>("GlassCenterSlot/ShardParticles");
		_dumpParticles = _parent.GetNode<GpuParticles2D>("GlassCenterSlot/DumpParticles");
		_topSparkParticles = _parent.GetNode<GpuParticles2D>("TopSparksSlot/TopSparkParticles");
		_bottomSparkParticles = _parent.GetNode<GpuParticles2D>("BottomSparksSlot/BottomSparkParticles");
		_bottomSparkParticles.OneShot = true;
		_topSparkParticles.OneShot = true;
		_witherParticles.OneShot = true;
		_shardParticles.OneShot = true;
		_dumpParticles.OneShot = true;
		ResetVfx();
		this.RunWhenSpineReady(_animController, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("_track1/rings_normal", loop: true, 1);
		});
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
		_animController.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStart));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		switch (new MegaEvent(spineEvent).GetData().GetEventName())
		{
		case "fire_ray":
			FireRay();
			break;
		case "start_wither":
			StartWither();
			break;
		case "end_wither":
			EndWither();
			break;
		case "start_die":
			StartDie();
			break;
		case "end_die":
			EndDie();
			break;
		case "start_sparks":
			StartSparks();
			break;
		}
	}

	private void OnAnimationStart(GodotObject spineSprite, GodotObject animationState, GodotObject trackEntry)
	{
		ResetVfx();
		MegaAnimationState animationState2 = _animController.GetAnimationState();
		string currentAnimationName = animationState2.GetCurrentAnimationName();
		if (currentAnimationName == _curAnimName)
		{
			return;
		}
		_curAnimName = currentAnimationName;
		switch (currentAnimationName)
		{
		case "idle_loop":
		case "hurt":
		case "wither":
			if (!_ringsSpinningNormal)
			{
				animationState2.SetAnimation("_track1/rings_normal", loop: true, 1);
			}
			_ringsSpinningNormal = true;
			break;
		}
		switch (currentAnimationName)
		{
		case "attack_heavy":
			animationState2.SetAnimation("_track1/rings_attack_heavy", loop: false, 1);
			animationState2.AddAnimation("_track1/rings_normal", 0f, loop: true, 1);
			_ringsSpinningNormal = false;
			break;
		case "attack_double":
			animationState2.SetAnimation("_track1/rings_attack_double", loop: false, 1);
			animationState2.AddAnimation("_track1/rings_normal", 0f, loop: true, 1);
			_ringsSpinningNormal = false;
			break;
		case "die":
			animationState2.SetAnimation("_track1/rings_die", loop: false, 1);
			_ringsSpinningNormal = false;
			break;
		}
	}

	private void FireRay()
	{
		_rayShaderMat?.SetShaderParameter(_progressString, 0.4f);
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.InOut);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(_rayShaderMat, "shader_parameter/Progress", -0.8f, 0.6000000238418579);
	}

	private void StartWither()
	{
		_witherParticles.Restart();
		_liquidShaderMat?.SetShaderParameter(_scrollSpeedString, -2f);
	}

	private void EndWither()
	{
		ResetVfx();
	}

	private void StartDie()
	{
		_dumpParticles.Restart();
		_leakParticles.Restart();
		_shardParticles.Restart();
	}

	private void EndDie()
	{
		_leakParticles.Emitting = false;
	}

	private void StartSparks()
	{
		_topSparkParticles.Restart();
		_bottomSparkParticles.Restart();
	}

	private void ResetVfx()
	{
		_rayShaderMat?.SetShaderParameter(_progressString, 1f);
		_liquidShaderMat?.SetShaderParameter(_scrollSpeedString, _baseScrollSpeed);
		_witherParticles.Restart();
		_witherParticles.Emitting = false;
		_leakParticles.Restart();
		_leakParticles.Emitting = false;
		_shardParticles.Emitting = false;
		_dumpParticles.Restart();
		_dumpParticles.Emitting = false;
		_topSparkParticles.Emitting = false;
		_bottomSparkParticles.Emitting = false;
	}
}
