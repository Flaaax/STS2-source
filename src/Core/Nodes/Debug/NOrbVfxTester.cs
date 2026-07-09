using System;
using Godot;

public partial class NOrbVfxTester : Control
{
	private enum OrbVfxTestModelType
	{
		Lightning,
		Dark,
		Frost,
		Glass,
		Plasma
	}

	[Export(PropertyHint.None, "")]
	private OrbVfxTestModelType _testModelType;

	[Export(PropertyHint.None, "")]
	private NOrbVfx _orbVfx;

	[Export(PropertyHint.None, "")]
	private float _basePassiveValue = 4f;

	[Export(PropertyHint.None, "")]
	private float _baseEvokeValue = 4f;

	[Export(PropertyHint.None, "")]
	private float _passiveIncrements;

	[Export(PropertyHint.None, "")]
	private Node2D _playerCenter;

	[Export(PropertyHint.None, "")]
	private Node2D _target;

	[Export(PropertyHint.None, "")]
	private Control _combatVfxContainer;

	private bool _isFocused;

	private decimal _passiveVal;

	private decimal _evokeVal;

	public override void _Ready()
	{
		_passiveVal = (decimal)_basePassiveValue;
		_evokeVal = (decimal)_baseEvokeValue;
		_orbVfx.SetOverrideCombatVfxContainer(_combatVfxContainer);
		_orbVfx.SetOverridePlayerNode(_playerCenter);
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible)
		{
			return;
		}
		base._Input(inputEvent);
		if (inputEvent is InputEventKey inputEventKey && inputEventKey.Keycode == Key.A && inputEventKey.Pressed)
		{
			if (_testModelType == OrbVfxTestModelType.Dark)
			{
				_evokeVal += _passiveVal;
			}
			_orbVfx.OnPassiveActivated(_passiveVal, _evokeVal);
			if (_orbVfx is NGlassOrbVfx && _passiveVal > 0m)
			{
				(_orbVfx as NGlassOrbVfx).ShowPassiveImpact(new Vector2[1] { _target.GlobalPosition });
			}
			if (_testModelType == OrbVfxTestModelType.Glass)
			{
				_passiveVal = Math.Clamp(_passiveVal - 1m, 0m, (decimal)_basePassiveValue);
			}
			_orbVfx.AfterPassiveActivated(_passiveVal, _evokeVal);
		}
		if (inputEvent is InputEventKey inputEventKey2 && inputEventKey2.Keycode == Key.Z && inputEventKey2.Pressed)
		{
			_passiveVal = (decimal)_basePassiveValue;
			_evokeVal = (decimal)_baseEvokeValue;
			_orbVfx.OnPassiveActivated(0m, _evokeVal);
			_orbVfx.Modulate = new Color(1f, 1f, 1f);
		}
		if (inputEvent is InputEventKey inputEventKey3 && inputEventKey3.Keycode == Key.S && inputEventKey3.Pressed)
		{
			_isFocused = !_isFocused;
			_orbVfx.SetForcedFocusPower(_isFocused);
		}
		if (inputEvent is InputEventKey inputEventKey4 && inputEventKey4.Keycode == Key.D && inputEventKey4.Pressed)
		{
			_orbVfx.OnEvoke(new Vector2[1] { (_testModelType == OrbVfxTestModelType.Frost || _testModelType == OrbVfxTestModelType.Plasma) ? _playerCenter.Position : _target.Position });
			_orbVfx.Modulate = new Color(1f, 1f, 1f, 0f);
		}
	}
}
