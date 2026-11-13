using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NInfestedPrismVfx : Node
{
	private static readonly StringName _outerStep = new StringName("OuterStep");

	private MegaSprite _megaSprite;

	private Node _parent;

	private Tween? _innerTween;

	private MegaSlotNode? _soulSlot;

	private ShaderMaterial? _soulMaterial;

	private Vector2 _dissolveTargStep = new Vector2(0.36f, 0.46f);

	private Vector2 _dissolveBaseStep;

	private GpuParticles2D _deathPArticles;

	private GpuParticles2D _deathSprinkles;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_megaSprite = new MegaSprite(_parent);
		_megaSprite.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
		_megaSprite.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStart));
		_soulSlot = new MegaSlotNode(GetNode<Node2D>("../SoulSlot"));
		_soulMaterial = _soulSlot.GetNormalMaterial() as ShaderMaterial;
		_dissolveBaseStep = _soulMaterial.GetShaderParameter(_outerStep).AsVector2();
		_deathPArticles = _parent.GetNode<GpuParticles2D>("DeathParticles");
		_deathSprinkles = _parent.GetNode<GpuParticles2D>("DeathSprinkles");
		_deathPArticles.Emitting = false;
		_deathSprinkles.Emitting = false;
		_deathPArticles.OneShot = true;
		_deathSprinkles.OneShot = true;
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		string eventName = new MegaEvent(spineEvent).GetData().GetEventName();
		if (!(eventName == "dissolve_soul"))
		{
			if (eventName == "explode")
			{
				Explode();
			}
		}
		else
		{
			DissolveSoul();
		}
	}

	private void DissolveSoul()
	{
		_innerTween = CreateTween();
		_innerTween.TweenProperty(_soulMaterial, "shader_parameter/OuterStep", _dissolveTargStep, 0.75);
		_deathSprinkles.Restart();
	}

	private void Explode()
	{
		_deathPArticles.Restart();
	}

	private void OnAnimationStart(GodotObject spineSprite, GodotObject animationState, GodotObject trackEntry)
	{
		_soulMaterial.SetShaderParameter(_outerStep, _dissolveBaseStep);
	}

	public override void _ExitTree()
	{
		_innerTween?.Kill();
	}
}
