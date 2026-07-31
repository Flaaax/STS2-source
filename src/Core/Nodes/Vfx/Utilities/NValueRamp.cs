using Godot;

public partial class NValueRamp : Node
{
	[Export(PropertyHint.None, "")]
	private float _rampSpeed = 1f;

	[Export(PropertyHint.None, "")]
	private Curve _rampCurve;

	private float _previousValue = -1f;

	private float _currentValue;

	private bool _isIncreasing;

	private bool _didForceValueThisFrame;

	public bool TryProcess(double delta, out float returnValue)
	{
		_currentValue += (float)delta * _rampSpeed * (_isIncreasing ? 1f : (-1f));
		_currentValue = Mathf.Clamp(_currentValue, 0f, 1f);
		if (_currentValue != _previousValue || _didForceValueThisFrame)
		{
			returnValue = _rampCurve.Sample(_currentValue);
			return true;
		}
		returnValue = 0f;
		return false;
	}

	public void SetIncreasing(bool isIncreasing)
	{
		_isIncreasing = isIncreasing;
	}

	public void ForceValue(float forcedValue)
	{
		_currentValue = forcedValue;
	}
}
