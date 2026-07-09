using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NOrbVfx : Node2D
{
	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _focusedParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _passiveActivatedParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _passiveActivatedFocusedParticles;

	[Export(PropertyHint.None, "")]
	private NShaker? _spineShaker;

	[Export(PropertyHint.None, "")]
	private string _evokeVfxSceneName = "";

	private static string _evokeVfxScenePath = "vfx/orbs/";

	protected bool _forcedFocusPower;

	private Control? _overrideCombatVfxContainer;

	protected OrbModel? _orbModel;

	protected Player? _owner;

	protected Node2D? _overridePlayerNode;

	private Tween? _shakeTween;

	protected Control VfxContainer
	{
		get
		{
			Control control = _overrideCombatVfxContainer;
			if (control == null)
			{
				NCombatRoom? instance = NCombatRoom.Instance;
				if (instance == null)
				{
					return null;
				}
				control = instance.CombatVfxContainer;
			}
			return control;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (_spineShaker != null)
		{
			_spineShaker.Strength = 0f;
		}
	}

	public void Initialize(OrbModel orbModel)
	{
		_orbModel = orbModel;
		_owner = _orbModel.Owner;
		if (_owner != null)
		{
			_owner.Creature.PowerApplied += OnPowerApplied;
			_owner.Creature.PowerIncreased += OnPowerIncreased;
			_owner.Creature.PowerDecreased += OnPowerDecreased;
		}
		if (_orbModel != null)
		{
			_orbModel.PassiveActivated += OnPassiveActivated;
			_orbModel.EvokeActivated += OnEvoke;
		}
		UpdateFocusPowerState();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_shakeTween?.Kill();
		if (_owner != null)
		{
			_owner.Creature.PowerApplied -= OnPowerApplied;
			_owner.Creature.PowerIncreased -= OnPowerIncreased;
			_owner.Creature.PowerDecreased -= OnPowerDecreased;
		}
		if (_orbModel != null)
		{
			_orbModel.PassiveActivated -= OnPassiveActivated;
			_orbModel.EvokeActivated -= OnEvoke;
		}
	}

	protected void OnPowerApplied(PowerModel powerModel)
	{
		UpdateFocusPowerState();
	}

	protected void OnPowerIncreased(PowerModel powerModel, int amount, bool silent)
	{
		UpdateFocusPowerState();
	}

	protected void OnPowerDecreased(PowerModel powerModel, bool silent)
	{
		UpdateFocusPowerState();
	}

	protected virtual void UpdateFocusPowerState()
	{
		if (_focusedParticles != null)
		{
			_focusedParticles.SetEmitting(HasFocusPower());
		}
	}

	public virtual void SetForcedFocusPower(bool forcedFocusPower)
	{
		_forcedFocusPower = forcedFocusPower;
		UpdateFocusPowerState();
	}

	protected virtual bool HasFocusPower()
	{
		bool flag = _forcedFocusPower;
		if (_owner != null)
		{
			flag |= _owner.Creature.GetPowerAmount<FocusPower>() > 0;
		}
		return flag;
	}

	public void OnPassiveActivated()
	{
		if (_orbModel != null)
		{
			OnPassiveActivated(_orbModel.PassiveVal, _orbModel.EvokeVal);
		}
	}

	public virtual void OnPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
		if (_passiveActivatedParticles != null)
		{
			_passiveActivatedParticles.Restart();
		}
		if (_passiveActivatedFocusedParticles != null && HasFocusPower())
		{
			_passiveActivatedFocusedParticles.Restart();
		}
	}

	public virtual void AfterPassiveActivated(decimal passiveVal, decimal evokeVal)
	{
	}

	private void OnEvoke(Creature[] targets)
	{
		List<NCreature> list = new List<NCreature>();
		foreach (Creature creature in targets)
		{
			NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
			if (nCreature != null)
			{
				list.Add(nCreature);
			}
		}
		OnEvoke(list.ToArray());
	}

	public void OnEvoke(NCreature[] targets)
	{
		Vector2[] array = new Vector2[targets.Length];
		for (int i = 0; i < targets.Length; i++)
		{
			array[i] = targets[i].VfxSpawnPosition;
		}
		OnEvoke(array);
	}

	public void OnEvoke(Vector2[] targetVfxSpawnPositions)
	{
		SpawnEvokeVfx();
		for (int i = 0; i < targetVfxSpawnPositions.Length; i++)
		{
			OnEvokeInternal(targetVfxSpawnPositions[i]);
		}
	}

	private void SpawnEvokeVfx()
	{
		if (!string.IsNullOrEmpty(_evokeVfxSceneName))
		{
			VfxCmd.PlayVfx(base.GlobalPosition, _evokeVfxScenePath + _evokeVfxSceneName, VfxContainer);
		}
	}

	protected virtual void OnEvokeInternal(Vector2 targetVfxSpawnPosition)
	{
	}

	protected Vector2 GetPlayerVfxPosition()
	{
		if (_overridePlayerNode != null)
		{
			return _overridePlayerNode.Position;
		}
		if (_owner == null)
		{
			return base.Position;
		}
		return (NCombatRoom.Instance?.GetCreatureNode(_owner.Creature))?.VfxSpawnPosition ?? base.Position;
	}

	public void SetOverrideCombatVfxContainer(Control overrideCombatVfxContainer)
	{
		_overrideCombatVfxContainer = overrideCombatVfxContainer;
	}

	public void SetOverridePlayerNode(Node2D overridePlayerNode)
	{
		_overridePlayerNode = overridePlayerNode;
	}

	protected void ShakeOrb(float initialStrength, float duration)
	{
		if (_spineShaker != null)
		{
			if (_shakeTween != null && _shakeTween.IsValid())
			{
				_shakeTween.Kill();
			}
			_spineShaker.Strength = initialStrength;
			_shakeTween = GetTree().CreateTween();
			_shakeTween.TweenProperty(_spineShaker, "_strength", 0f, duration).SetEase(Tween.EaseType.OutIn).SetTrans(Tween.TransitionType.Sine);
		}
	}
}
