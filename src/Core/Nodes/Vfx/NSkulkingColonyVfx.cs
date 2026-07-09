using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NSkulkingColonyVfx : Node2D
{
	private MegaSprite _megaSprite;

	private GpuParticles2D _particles1;

	private GpuParticles2D _wideParticles;

	private GpuParticles2D _poofParticles;

	public override void _Ready()
	{
		_particles1 = GetNode<GpuParticles2D>("../ParticleSlot1/Particles");
		_wideParticles = GetNode<GpuParticles2D>("../ParticleSlot2/ParticlesWide");
		_poofParticles = GetNode<GpuParticles2D>("../ParticleSlot3/Particles");
		_particles1.Emitting = false;
		_poofParticles.Emitting = false;
		_wideParticles.Emitting = false;
		_particles1.OneShot = true;
		_poofParticles.OneShot = true;
		_wideParticles.OneShot = true;
		_megaSprite = new MegaSprite(GetParent<Node2D>());
		_megaSprite.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		switch (new MegaEvent(spineEvent).GetData().GetEventName())
		{
		case "take_damage":
			DamageHandler();
			break;
		case "take_fatal_damage":
			DeathHandler();
			break;
		case "final_poof":
			PoofHandler();
			break;
		}
	}

	private void DamageHandler()
	{
		_particles1.Restart();
	}

	private void DeathHandler()
	{
		_wideParticles.Restart();
	}

	private void PoofHandler()
	{
		_poofParticles.Restart();
	}
}
