using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NFogmogVfx : Node
{
	private GpuParticles2D _thrustParicles;

	private GpuParticles2D _dustLeftParticles;

	private GpuParticles2D _dustRightParticles;

	private MegaSprite _megaSprite;

	public override void _Ready()
	{
		_thrustParicles = GetNode<GpuParticles2D>("../ThrustSlotNode/ThrustParticles");
		_dustLeftParticles = GetNode<GpuParticles2D>("../DustSlotNode/DustLeftParticles");
		_dustRightParticles = GetNode<GpuParticles2D>("../DustSlotNode/DustRightParticles");
		_thrustParicles.Emitting = false;
		_dustLeftParticles.Emitting = false;
		_dustRightParticles.Emitting = false;
		_megaSprite = new MegaSprite(GetParent<Node2D>());
		_megaSprite.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		string eventName = new MegaEvent(spineEvent).GetData().GetEventName();
		if (!(eventName == "thrust_start"))
		{
			if (eventName == "thrust_end")
			{
				EndThrust();
			}
		}
		else
		{
			StartThrust();
		}
	}

	private void StartThrust()
	{
		_thrustParicles.Restart();
		_dustRightParticles.Restart();
		_dustLeftParticles.Restart();
	}

	private void EndThrust()
	{
		_thrustParicles.Emitting = false;
		_dustLeftParticles.Emitting = false;
		_dustRightParticles.Emitting = false;
	}
}
