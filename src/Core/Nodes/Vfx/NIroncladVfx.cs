using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NIroncladVfx : Node
{
	private static readonly StringName _step = new StringName("step");

	private Vector2 _slashStepBase;

	private ShaderMaterial? _slashShaderMat;

	private Tween? _tween;

	private Node2D _parent;

	private MegaSprite _megaSprite;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_slashShaderMat = new MegaSlotNode(_parent.GetNode("SlashVfxSlot")).GetNormalMaterial() as ShaderMaterial;
		_slashStepBase = (Vector2)_slashShaderMat.GetShaderParameter(_step);
		_megaSprite = new MegaSprite(_parent);
		_megaSprite.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		string eventName = new MegaEvent(spineEvent).GetData().GetEventName();
		if (!(eventName == "heavy_slash_start"))
		{
			if (eventName == "attack_slash_start")
			{
				OnAttackSlash();
			}
		}
		else
		{
			OnHeavySlash();
		}
	}

	private void OnHeavySlash()
	{
		_slashShaderMat?.SetShaderParameter(_step, _slashStepBase);
		_tween?.Kill();
		_tween = CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
		Vector2 vector = new Vector2(1f, 1.02f);
		_tween.TweenProperty(_slashShaderMat, "shader_parameter/step", vector, 0.3499999940395355);
	}

	private void OnAttackSlash()
	{
		_slashShaderMat?.SetShaderParameter(_step, _slashStepBase);
		_tween?.Kill();
		_tween = CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		Vector2 vector = new Vector2(1f, 1.02f);
		_tween.TweenInterval(0.15000000596046448);
		_tween.TweenProperty(_slashShaderMat, "shader_parameter/step", vector, 0.20000000298023224);
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}
}
