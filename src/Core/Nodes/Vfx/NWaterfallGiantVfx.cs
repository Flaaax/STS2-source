using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

[GlobalClass]
public partial class NWaterfallGiantVfx : Node
{
	private GpuParticles2D _steam1Particles;

	private GpuParticles2D _steam2Particles;

	private GpuParticles2D _steam3Particles;

	private GpuParticles2D _steam4Particles;

	private GpuParticles2D _steam5Particles;

	private GpuParticles2D _steam6Particles;

	private GpuParticles2D _steamLeakParticles1;

	private GpuParticles2D _steamLeakParticles2;

	private GpuParticles2D _steamLeakParticles3;

	private GpuParticles2D _mistParticles;

	private GpuParticles2D _mouthParticles;

	private GpuParticles2D _dropletParticles;

	private ParticleProcessMaterial _leakProcMat1;

	private ParticleProcessMaterial _leakProcMat2;

	private ParticleProcessMaterial _leakProcMat3;

	private bool _isDead;

	private Node2D _parent;

	private MegaSprite _animController;

	public override void _Ready()
	{
		_parent = GetParent<Node2D>();
		_animController = new MegaSprite(_parent);
		_animController.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
		_steam1Particles = _parent.GetNode<GpuParticles2D>("SteamSlot1/steamParticles1");
		_steam2Particles = _parent.GetNode<GpuParticles2D>("SteamSlot2/steamParticles2");
		_steam3Particles = _parent.GetNode<GpuParticles2D>("SteamSlot3/steamParticles3");
		_steam4Particles = _parent.GetNode<GpuParticles2D>("SteamSlot4/steamParticles4");
		_steam5Particles = _parent.GetNode<GpuParticles2D>("SteamSlot5/steamParticles5");
		_steam6Particles = _parent.GetNode<GpuParticles2D>("SteamSlot6/steamParticles6");
		_steamLeakParticles1 = _parent.GetNode<GpuParticles2D>("SteamLeakSlot1/steamLeakParticles1");
		_steamLeakParticles2 = _parent.GetNode<GpuParticles2D>("SteamLeakSlot2/steamLeakParticles2");
		_steamLeakParticles3 = _parent.GetNode<GpuParticles2D>("SteamLeakSlot3/steamLeakParticles3");
		_mistParticles = _parent.GetNode<GpuParticles2D>("MistSlot/MistParticles");
		_dropletParticles = _parent.GetNode<GpuParticles2D>("MistSlot/Droplets");
		_mouthParticles = _parent.GetNode<GpuParticles2D>("MouthDropletsSlot/MouthDroplets");
		_leakProcMat1 = (ParticleProcessMaterial)_steamLeakParticles1.ProcessMaterial;
		_leakProcMat2 = (ParticleProcessMaterial)_steamLeakParticles2.ProcessMaterial;
		_leakProcMat3 = (ParticleProcessMaterial)_steamLeakParticles3.ProcessMaterial;
		_steam1Particles.Emitting = false;
		_steam2Particles.Emitting = false;
		_steam3Particles.Emitting = false;
		_steam4Particles.Emitting = false;
		_steam5Particles.Emitting = false;
		_steam6Particles.Emitting = false;
		_steamLeakParticles1.Emitting = false;
		_steamLeakParticles2.Emitting = false;
		_steamLeakParticles3.Emitting = false;
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
		case 13:
			switch (eventName[6])
			{
			case '1':
				if (eventName == "steam_1_start")
				{
					StartSteam1();
				}
				break;
			case '2':
				if (eventName == "steam_2_start")
				{
					StartSteam2();
				}
				break;
			case '3':
				if (eventName == "steam_3_start")
				{
					StartSteam3();
				}
				break;
			case '5':
				if (eventName == "steam_5_start")
				{
					StartSteam5();
				}
				break;
			case 'a':
				if (eventName == "waterfall_end")
				{
					EndWaterfall();
				}
				break;
			}
			break;
		case 11:
			switch (eventName[6])
			{
			case '1':
				if (eventName == "steam_1_end")
				{
					EndSteam1();
				}
				break;
			case '2':
				if (eventName == "steam_2_end")
				{
					EndSteam2();
				}
				break;
			case '3':
				if (eventName == "steam_3_end")
				{
					EndSteam3();
				}
				break;
			case '5':
				if (eventName == "steam_5_end")
				{
					EndSteam5();
				}
				break;
			case '4':
				break;
			}
			break;
		case 8:
			switch (eventName[7])
			{
			case '1':
				if (eventName == "buildup1")
				{
					Buildup1();
				}
				break;
			case '2':
				if (eventName == "buildup2")
				{
					Buildup2();
				}
				break;
			case '3':
				if (eventName == "buildup3")
				{
					Buildup3();
				}
				break;
			}
			break;
		case 15:
			if (eventName == "waterfall_start")
			{
				StartWaterfall();
			}
			break;
		case 7:
			if (eventName == "explode")
			{
				Explode();
			}
			break;
		case 17:
			if (eventName == "clear_death_steam")
			{
				ClearDeathSteam();
			}
			break;
		case 9:
		case 10:
		case 12:
		case 14:
		case 16:
			break;
		}
	}

	private void StartSteam1()
	{
		EmitGracefully(_steam1Particles);
	}

	private void EndSteam1()
	{
		_steam1Particles.Emitting = false;
	}

	private void StartSteam2()
	{
		EmitGracefully(_steam2Particles);
	}

	private void EndSteam2()
	{
		_steam2Particles.Emitting = false;
	}

	private void StartSteam3()
	{
		EmitGracefully(_steam3Particles);
		EmitGracefully(_steam4Particles);
	}

	private void EndSteam3()
	{
		_steam3Particles.Emitting = false;
		_steam4Particles.Emitting = false;
	}

	private void StartSteam5()
	{
		EmitGracefully(_steam5Particles);
		EmitGracefully(_steam6Particles);
	}

	private void EndSteam5()
	{
		_steam5Particles.Emitting = false;
		_steam6Particles.Emitting = false;
	}

	private void StartWaterfall()
	{
		_mouthParticles.Emitting = true;
		_dropletParticles.Emitting = true;
		_mistParticles.Emitting = true;
		_isDead = false;
	}

	private void EndWaterfall()
	{
		_mouthParticles.Emitting = false;
		_dropletParticles.Emitting = false;
		_mistParticles.Emitting = false;
	}

	private void Explode()
	{
		_steam1Particles.Visible = false;
		_steam2Particles.Visible = false;
		_steam3Particles.Visible = false;
		_steam4Particles.Visible = false;
		_steam5Particles.Visible = false;
		_steam6Particles.Visible = false;
		_steamLeakParticles1.Visible = false;
		_steamLeakParticles2.Visible = false;
		_steamLeakParticles3.Visible = false;
		_isDead = true;
	}

	private void Buildup1()
	{
		if (!_isDead)
		{
			EmitGracefully(_steamLeakParticles1);
			EmitGracefully(_steamLeakParticles2);
			EmitGracefully(_steamLeakParticles3);
			ParticleProcessMaterial leakProcMat = _leakProcMat1;
			ParticleProcessMaterial leakProcMat2 = _leakProcMat2;
			float num = (_leakProcMat3.ScaleMin = 0.2f);
			float scaleMin = (leakProcMat2.ScaleMin = num);
			leakProcMat.ScaleMin = scaleMin;
			ParticleProcessMaterial leakProcMat3 = _leakProcMat1;
			ParticleProcessMaterial leakProcMat4 = _leakProcMat2;
			num = (_leakProcMat3.ScaleMax = 0.4f);
			scaleMin = (leakProcMat4.ScaleMax = num);
			leakProcMat3.ScaleMax = scaleMin;
			GpuParticles2D steamLeakParticles = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles2 = _steamLeakParticles2;
			int num6 = (_steamLeakParticles3.Amount = 10);
			int amount = (steamLeakParticles2.Amount = num6);
			steamLeakParticles.Amount = amount;
			GpuParticles2D steamLeakParticles3 = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles4 = _steamLeakParticles2;
			double num9 = (_steamLeakParticles3.Lifetime = 0.3700000047683716);
			double lifetime = (steamLeakParticles4.Lifetime = num9);
			steamLeakParticles3.Lifetime = lifetime;
		}
	}

	private void Buildup2()
	{
		if (!_isDead)
		{
			EmitGracefully(_steamLeakParticles1);
			EmitGracefully(_steamLeakParticles2);
			EmitGracefully(_steamLeakParticles3);
			ParticleProcessMaterial leakProcMat = _leakProcMat1;
			ParticleProcessMaterial leakProcMat2 = _leakProcMat2;
			float num = (_leakProcMat3.ScaleMin = 0.3f);
			float scaleMin = (leakProcMat2.ScaleMin = num);
			leakProcMat.ScaleMin = scaleMin;
			ParticleProcessMaterial leakProcMat3 = _leakProcMat1;
			ParticleProcessMaterial leakProcMat4 = _leakProcMat2;
			num = (_leakProcMat3.ScaleMax = 0.6f);
			scaleMin = (leakProcMat4.ScaleMax = num);
			leakProcMat3.ScaleMax = scaleMin;
			GpuParticles2D steamLeakParticles = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles2 = _steamLeakParticles2;
			int num6 = (_steamLeakParticles3.Amount = 20);
			int amount = (steamLeakParticles2.Amount = num6);
			steamLeakParticles.Amount = amount;
			GpuParticles2D steamLeakParticles3 = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles4 = _steamLeakParticles2;
			double num9 = (_steamLeakParticles3.Lifetime = 0.75);
			double lifetime = (steamLeakParticles4.Lifetime = num9);
			steamLeakParticles3.Lifetime = lifetime;
		}
	}

	private void Buildup3()
	{
		if (!_isDead)
		{
			EmitGracefully(_steamLeakParticles1);
			EmitGracefully(_steamLeakParticles2);
			EmitGracefully(_steamLeakParticles3);
			ParticleProcessMaterial leakProcMat = _leakProcMat1;
			ParticleProcessMaterial leakProcMat2 = _leakProcMat2;
			float num = (_leakProcMat3.ScaleMin = 0.4f);
			float scaleMin = (leakProcMat2.ScaleMin = num);
			leakProcMat.ScaleMin = scaleMin;
			ParticleProcessMaterial leakProcMat3 = _leakProcMat1;
			ParticleProcessMaterial leakProcMat4 = _leakProcMat2;
			num = (_leakProcMat3.ScaleMax = 0.8f);
			scaleMin = (leakProcMat4.ScaleMax = num);
			leakProcMat3.ScaleMax = scaleMin;
			GpuParticles2D steamLeakParticles = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles2 = _steamLeakParticles2;
			int num6 = (_steamLeakParticles3.Amount = 28);
			int amount = (steamLeakParticles2.Amount = num6);
			steamLeakParticles.Amount = amount;
			GpuParticles2D steamLeakParticles3 = _steamLeakParticles1;
			GpuParticles2D steamLeakParticles4 = _steamLeakParticles2;
			double num9 = (_steamLeakParticles3.Lifetime = 1.2);
			double lifetime = (steamLeakParticles4.Lifetime = num9);
			steamLeakParticles3.Lifetime = lifetime;
		}
	}

	private void ClearDeathSteam()
	{
		_steam3Particles.Emitting = false;
		_steam4Particles.Emitting = false;
		_steam5Particles.Emitting = false;
		_steam6Particles.Emitting = false;
		_steam3Particles.Visible = false;
		_steam4Particles.Visible = false;
		_steam5Particles.Visible = false;
		_steam6Particles.Visible = false;
		_isDead = false;
	}

	private void EmitGracefully(GpuParticles2D emitter)
	{
		if (!emitter.Visible)
		{
			emitter.Visible = true;
			emitter.Restart();
		}
		else
		{
			emitter.Emitting = true;
		}
	}
}
