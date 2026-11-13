using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NBlitzMerchantVfx : Node
{
	private Node2D _spineNode;

	private MegaSprite _megaSprite;

	private GpuParticles2D _greenBlast;

	private GpuParticles2D _greenEmbers;

	private GpuParticles2D _redEmbers;

	private GpuParticles2D _greenGlow;

	private GpuParticles2D _redGlow;

	private Sprite2D _greenFire;

	private Sprite2D _redFire;

	private Vector2 _redFireScale;

	private Vector2 _greenFireScale;

	private Tween _redTween;

	private Tween _greenTween;

	public override void _Ready()
	{
		_spineNode = GetNode<Node2D>("MerchantVisual");
		_megaSprite = new MegaSprite(_spineNode);
		_greenBlast = GetNode<GpuParticles2D>("BackVfx/GreenBlast");
		_greenEmbers = GetNode<GpuParticles2D>("BackVfx/GreenEmbers");
		_redEmbers = GetNode<GpuParticles2D>("BackVfx/RedEmbers");
		_greenGlow = GetNode<GpuParticles2D>("FrontVfx/GreenGlow");
		_redGlow = GetNode<GpuParticles2D>("FrontVfx/RedGlow");
		_greenFire = GetNode<Sprite2D>("BackVfx/GreenFire");
		_redFire = GetNode<Sprite2D>("BackVfx/RedFire");
		_redFireScale = _redFire.Scale;
		_greenFireScale = _greenFire.Scale;
		_greenBlast.OneShot = true;
		ShowRed();
	}

	public void ShowGreen()
	{
		_greenGlow.Restart();
		_greenFire.Visible = true;
		_greenTween = CreateTween();
		_greenFire.Scale = Vector2.Zero;
		Vector2 vector = new Vector2(1.1f, 1.3f);
		_greenTween.TweenProperty(_greenFire, "scale", _greenFireScale * vector, 0.10000000149011612).SetDelay(0.15000000596046448).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		_greenTween.TweenProperty(_greenFire, "scale", _greenFireScale, 0.10000000149011612).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		_redGlow.Restart();
		_redGlow.Emitting = false;
		_redTween = CreateTween();
		_redTween.TweenProperty(_redFire, "scale", Vector2.Zero, 0.15000000596046448).SetEase(Tween.EaseType.In);
		_redTween.TweenCallback(Callable.From(StartGreenSparks));
		MegaAnimationState animationState = _megaSprite.GetAnimationState();
		animationState.SetAnimation("done");
	}

	private void StartGreenSparks()
	{
		_redEmbers.Restart();
		_redEmbers.Emitting = false;
		_greenBlast.Restart();
		_greenEmbers.Restart();
	}

	public void ShowRed()
	{
		_redEmbers.Restart();
		_redGlow.Restart();
		_redFire.Visible = true;
		_redFire.Scale = _redFireScale;
		_greenBlast.Restart();
		_greenEmbers.Restart();
		_greenGlow.Restart();
		_greenBlast.Emitting = false;
		_greenEmbers.Emitting = false;
		_greenGlow.Emitting = false;
		_greenFire.Visible = false;
		_greenFire.Scale = _greenFireScale;
		MegaAnimationState animationState = _megaSprite.GetAnimationState();
		animationState.SetAnimation("animation");
	}

	public override void _ExitTree()
	{
		_redTween.Kill();
		_greenTween.Kill();
	}
}
