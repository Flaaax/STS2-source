using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

[GlobalClass]
public partial class NFoolVfx : Node2D
{
	private GpuParticles2D _leafCannonParticles;

	private GpuParticles2D _leafWaterfallParticles;

	private GpuParticles2D _magicLeafParticles;

	private GpuParticles2D _musicParticles;

	private GpuParticles2D _starParticles;

	private GpuParticles2D _impactParticle;

	private Node2D _parent;

	private MegaSprite _animController;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_animController = new MegaSprite(_parent);
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
		_animController.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStart));
		_leafCannonParticles = _parent.GetNode<GpuParticles2D>("ParticleCannonSlot/LeafCannonParticles");
		_leafWaterfallParticles = _parent.GetNode<GpuParticles2D>("LeafWaterfallSlot/LeafWaterfallParticles");
		_musicParticles = _parent.GetNode<GpuParticles2D>("ParticleCannonSlot/MusicParticles");
		_magicLeafParticles = _parent.GetNode<GpuParticles2D>("ParticleCannonSlot/MagicLeafParticles");
		_starParticles = _parent.GetNode<GpuParticles2D>("ParticleCannonSlot/StarParticles");
		_impactParticle = _parent.GetNode<GpuParticles2D>("ParticleCannonSlot/ImpactParticle");
		_magicLeafParticles.OneShot = true;
		_musicParticles.OneShot = true;
		_starParticles.OneShot = true;
		_impactParticle.OneShot = true;
		ResetVfx();
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
		case 17:
			if (eventName == "leaf_cannon_start")
			{
				StartLeafCannon();
			}
			break;
		case 15:
			if (eventName == "leaf_cannon_end")
			{
				EndLeafCannon();
			}
			break;
		case 20:
			if (eventName == "leaf_waterfall_start")
			{
				StartLeafWaterfall();
			}
			break;
		case 18:
			if (eventName == "leaf_waterfall_end")
			{
				EndLeafWaterfall();
			}
			break;
		case 16:
			if (eventName == "magic_leaf_shoot")
			{
				ShootMagicLeaves();
			}
			break;
		case 21:
			if (eventName == "music_particles_shoot")
			{
				ShootMusic();
			}
			break;
		case 11:
			if (eventName == "stars_shoot")
			{
				ShootStars();
			}
			break;
		case 12:
		case 13:
		case 14:
		case 19:
			break;
		}
	}

	private void OnAnimationStart(GodotObject spineSprite, GodotObject animationState, GodotObject trackEntry)
	{
		ResetVfx();
	}

	private void ResetVfx()
	{
		_leafCannonParticles.Restart();
		_leafCannonParticles.Emitting = false;
		_magicLeafParticles.Emitting = false;
		_musicParticles.Emitting = false;
		_leafWaterfallParticles.Restart();
		_leafWaterfallParticles.Emitting = false;
		_starParticles.Emitting = false;
		_impactParticle.Emitting = false;
	}

	private void StartLeafCannon()
	{
		_leafCannonParticles.Restart();
	}

	private void EndLeafCannon()
	{
		_leafCannonParticles.Emitting = false;
	}

	private void StartLeafWaterfall()
	{
		_leafWaterfallParticles.Restart();
	}

	private void EndLeafWaterfall()
	{
		_leafWaterfallParticles.Restart();
		_leafWaterfallParticles.Emitting = false;
	}

	private void ShootMagicLeaves()
	{
		_magicLeafParticles.Restart();
	}

	private void ShootMusic()
	{
		_musicParticles.Restart();
	}

	private void ShootStars()
	{
		_starParticles.Restart();
		_impactParticle.Restart();
	}
}
