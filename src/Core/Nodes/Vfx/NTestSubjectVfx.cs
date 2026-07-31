using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

[GlobalClass]
public partial class NTestSubjectVfx : Node
{
	private GpuParticles2D _neckParticles;

	private GpuParticles2D _dizzyParticles;

	private GpuParticles2D _emberParticles;

	private GpuParticles2D _flameParticles;

	private GpuParticles2D _burnParticles;

	private GpuParticles2D _targetedBurnParticle;

	private GpuParticles2D _burnParticleFountain;

	private Node2D _burnParticleContainer;

	private TextureRect _burnFire1;

	private TextureRect _burnFire2;

	private TextureRect _burnFire3;

	private Tween? _burnTween1;

	private Tween? _burnTween2;

	private Tween? _burnTween3;

	private Vector2 _burnFire1Scale;

	private Vector2 _burnFire2Scale;

	private Vector2 _burnFire3Scale;

	private Vector2 _burnParticleGlobalScale;

	private Node2D _parent;

	private MegaSprite _animController;

	private MegaSprite _frontBurnVfxController;

	private MegaSprite _backBurnVfxController;

	private bool _keyDown;

	private bool _doingThing;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_animController = new MegaSprite(_parent);
		_frontBurnVfxController = new MegaSprite(GetNode("../FrontBurnVfxSlot/FrontBurnVfx"));
		_backBurnVfxController = new MegaSprite(GetNode("../BackBurnVfxSlot/BackBurnVfx"));
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
		_neckParticles = _parent.GetNode<GpuParticles2D>("NeckParticlesSlot/NeckParticles");
		_dizzyParticles = _parent.GetNode<GpuParticles2D>("NeckParticlesSlot/DizzyPaticles");
		_emberParticles = _parent.GetNode<GpuParticles2D>("../../EmberParticles");
		_flameParticles = _parent.GetNode<GpuParticles2D>("../../FlameParticles");
		_burnParticles = _parent.GetNode<GpuParticles2D>("../../BurnParticleContainer/BurnParticles");
		_targetedBurnParticle = _parent.GetNode<GpuParticles2D>("../../BurnParticleContainer/TargetedBurnParticle");
		_burnParticleFountain = _parent.GetNode<GpuParticles2D>("../../BurnParticleContainer/BurnParticleFountain");
		_burnParticleContainer = _parent.GetNode<Node2D>("../../BurnParticleContainer");
		_burnFire1 = _parent.GetNode<TextureRect>("../../BurnFire1");
		_burnFire2 = _parent.GetNode<TextureRect>("../../BurnFire2");
		_burnFire3 = _parent.GetNode<TextureRect>("../../BurnFire3");
		_neckParticles.OneShot = true;
		_neckParticles.Emitting = false;
		_dizzyParticles.Emitting = false;
		_emberParticles.OneShot = true;
		_emberParticles.Emitting = false;
		_flameParticles.Emitting = false;
		_burnParticles.Emitting = false;
		_targetedBurnParticle.Emitting = false;
		_burnParticleFountain.Emitting = false;
		_burnParticleGlobalScale = _burnParticleContainer.GlobalScale;
		_burnFire1.Visible = false;
		_burnFire2.Visible = false;
		_burnFire3.Visible = false;
		_burnFire1Scale = _burnFire1.Scale;
		_burnFire2Scale = _burnFire2.Scale;
		_burnFire3Scale = _burnFire3.Scale;
		this.RunWhenSpineReady(_animController, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("idle_loop3");
		});
		this.RunWhenSpineReady(_frontBurnVfxController, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("empty");
		});
		this.RunWhenSpineReady(_backBurnVfxController, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("empty");
		});
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		string eventName = new MegaEvent(spineEvent).GetData().GetEventName();
		if (eventName == null)
		{
			return;
		}
		switch (eventName.Length)
		{
		case 12:
			switch (eventName[6])
			{
			case 'x':
				if (eventName == "neck_explode")
				{
					OnSquirtNeck();
				}
				break;
			case 'e':
				if (eventName == "start_embers")
				{
					OnStartEmbers();
				}
				break;
			case 'f':
				if (eventName == "start_flames")
				{
					OnStartFlames();
				}
				break;
			case 'r':
				if (eventName == "end_burn_vfx")
				{
					OnEndBurnVfx();
				}
				break;
			}
			break;
		case 13:
			if (eventName == "start_dizzies")
			{
				OnStartDizzies();
			}
			break;
		case 11:
			if (eventName == "end_dizzies")
			{
				OnEndDizzies();
			}
			break;
		case 10:
			if (eventName == "end_flames")
			{
				OnEndFlames();
			}
			break;
		case 14:
			if (eventName == "start_burn_vfx")
			{
				OnStartBurnVfx();
			}
			break;
		}
	}

	private void PlayAnim1()
	{
		_animController.GetAnimationState().SetAnimation("die3", loop: false);
		_animController.GetAnimationState().AddAnimation("idle_loop3");
	}

	private void OnSquirtNeck()
	{
		_neckParticles.Restart();
	}

	private void OnStartDizzies()
	{
		if (!_dizzyParticles.Emitting)
		{
			_dizzyParticles.Emitting = true;
		}
	}

	private void OnEndDizzies()
	{
		_dizzyParticles.Emitting = false;
	}

	private void OnStartEmbers()
	{
		_emberParticles.Restart();
	}

	private void OnStartFlames()
	{
		_flameParticles.Emitting = true;
	}

	private void OnEndFlames()
	{
		_flameParticles.Emitting = false;
	}

	private void OnStartBurnVfx()
	{
		_burnParticleContainer.GlobalScale = _burnParticleGlobalScale;
		_frontBurnVfxController.GetAnimationState().SetAnimation("burn", loop: false);
		_backBurnVfxController.GetAnimationState().SetAnimation("burn", loop: false);
		_burnParticles.Restart();
		_targetedBurnParticle.Emitting = true;
		_burnParticleFountain.Restart();
		TextureRect burnFire = _burnFire1;
		TextureRect burnFire2 = _burnFire2;
		bool flag = (_burnFire3.Visible = true);
		bool visible = (burnFire2.Visible = flag);
		burnFire.Visible = visible;
		TextureRect burnFire3 = _burnFire1;
		TextureRect burnFire4 = _burnFire2;
		Vector2 vector = (_burnFire3.Scale = Vector2.Zero);
		Vector2 scale = (burnFire4.Scale = vector);
		burnFire3.Scale = scale;
		_burnTween1?.Kill();
		_burnTween2?.Kill();
		_burnTween3?.Kill();
		_burnTween1 = CreateTween();
		_burnTween2 = CreateTween();
		_burnTween3 = CreateTween();
		_burnTween1.TweenProperty(_burnFire1, "scale", _burnFire1Scale, 0.10000000149011612).SetDelay(0.20000000298023224);
		_burnTween2.TweenProperty(_burnFire2, "scale", _burnFire2Scale, 0.10000000149011612).SetDelay(0.20000000298023224);
		_burnTween3.TweenProperty(_burnFire3, "scale", _burnFire3Scale, 0.10000000149011612).SetDelay(0.30000001192092896);
		_burnTween3.TweenCallback(Callable.From(TweenOutBurnFire));
	}

	private void OnEndBurnVfx()
	{
		_burnParticles.Emitting = false;
		_targetedBurnParticle.Emitting = false;
		_burnParticleFountain.Emitting = false;
	}

	private void TweenOutBurnFire()
	{
		_burnTween1.Kill();
		_burnTween2.Kill();
		_burnTween3.Kill();
		_burnTween1 = CreateTween();
		_burnTween2 = CreateTween();
		_burnTween3 = CreateTween();
		Vector2 vector = new Vector2(0.2f, 0f);
		_burnTween1.TweenProperty(_burnFire1, "scale", vector, 0.800000011920929).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad)
			.SetDelay(1.2000000476837158);
		_burnTween2.TweenProperty(_burnFire2, "scale", vector, 0.800000011920929).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad)
			.SetDelay(1.100000023841858);
		_burnTween3.TweenProperty(_burnFire3, "scale", vector, 0.800000011920929).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad)
			.SetDelay(1.0);
		_burnTween1.TweenCallback(Callable.From(ClearBurnFire));
	}

	private void ClearBurnFire()
	{
		TextureRect burnFire = _burnFire1;
		TextureRect burnFire2 = _burnFire2;
		bool flag = (_burnFire3.Visible = false);
		bool visible = (burnFire2.Visible = flag);
		burnFire.Visible = visible;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_burnTween1?.Kill();
		_burnTween2?.Kill();
		_burnTween3?.Kill();
	}
}
