using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

/// <summary>
/// Holds both the Sprites (the card image + particle effects) and the Trails for the "card trail vfx".
/// We need these detached so that the Trail nodes' parents don't affect the trail's positions.
/// </summary>
public partial class NCardTrailVfx : Node2D
{
	private Control _nodeToFollow;

	private Node2D _sprites;

	private bool _updateSprites = true;

	private Tween? _introTween;

	private readonly OwnerFreeingTween _fadeOut;

	public NCardTrailVfx()
	{
		_fadeOut = new OwnerFreeingTween(this);
	}

	public static NCardTrailVfx? Create(Control card, string characterTrailPath)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardTrailVfx nCardTrailVfx = PreloadManager.Cache.GetScene(characterTrailPath).Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
		nCardTrailVfx._nodeToFollow = card;
		return nCardTrailVfx;
	}

	public override void _Ready()
	{
		if (NCombatUi.IsDebugHidingPlayContainer)
		{
			base.Visible = false;
		}
		_sprites = GetNode<Node2D>("Sprites");
		_sprites.Modulate = StsColors.transparentWhite;
		_introTween = CreateTween().SetParallel();
		_introTween.TweenProperty(_sprites, "scale", Vector2.One * 0.5f, 0.5).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic)
			.SetDelay(0.25);
		_introTween.TweenProperty(_nodeToFollow, "modulate:a", 0.75f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_introTween.TweenProperty(_sprites, "modulate:a", 1f, 1.0).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	public override void _Process(double delta)
	{
		if (_updateSprites)
		{
			base.GlobalPosition = _nodeToFollow.GlobalPosition;
			base.Rotation = _nodeToFollow.Rotation;
		}
	}

	public Task FadeOut()
	{
		_updateSprites = false;
		return _fadeOut.Play(delegate(Tween tween)
		{
			tween.SetParallel();
			tween.TweenProperty(this, "modulate:a", 0f, 0.5);
			StopParticles(tween);
		});
	}

	/// <summary>
	/// Prevents the trail from emitting a ton of sparks upon reaching destination.
	/// </summary>
	private void StopParticles(Tween tween)
	{
		foreach (Node child in _sprites.GetChildren())
		{
			if (child is CpuParticles2D cpuParticles2D)
			{
				tween.TweenProperty(cpuParticles2D, "amount", 1, 0.5);
			}
		}
	}

	public override void _ExitTree()
	{
		_introTween?.Kill();
	}
}
