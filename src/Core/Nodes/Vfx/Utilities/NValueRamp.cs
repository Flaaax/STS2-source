using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NValueRamp : Node
{
	[Export(PropertyHint.None, "")]
	private float _rampSpeed = 1f;

	[Export(PropertyHint.None, "")]
	private Curve _rampCurve;

	private float _currentValue;

	private bool _isIncreasing;

	private bool _didForceValueThisFrame = true;

	public bool TryProcess(double delta, out float returnValue)
	{
		bool didForceValueThisFrame = _didForceValueThisFrame;
		_didForceValueThisFrame = false;
		float num = (float)delta * _rampSpeed * (_isIncreasing ? 1f : (-1f));
		bool flag = ((num > 0f) ? (_currentValue >= 1f) : (!(num < 0f) || _currentValue <= 0f));
		bool flag2 = flag;
		if (!didForceValueThisFrame && flag2)
		{
			returnValue = 0f;
			return false;
		}
		_currentValue = Mathf.Clamp(_currentValue + num, 0f, 1f);
		returnValue = _rampCurve.Sample(_currentValue);
		return true;
	}

	public void SetIncreasing(bool isIncreasing)
	{
		_isIncreasing = isIncreasing;
	}

	public void ForceValue(float forcedValue)
	{
		_currentValue = forcedValue;
		_didForceValueThisFrame = true;
	}
}
