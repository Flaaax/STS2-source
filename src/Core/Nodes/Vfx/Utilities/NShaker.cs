using Godot;

public partial class NShaker : Node
{
	[Export(PropertyHint.None, "")]
	private Node2D? _target;

	[Export(PropertyHint.None, "")]
	private float _maxPosOffset;

	[Export(PropertyHint.None, "")]
	private float _maxRotOffset;

	[Export(PropertyHint.None, "")]
	private float _frequency;

	[Export(PropertyHint.None, "")]
	private float _strength;

	private float _timer;

	private Vector2 _previousShakePos;

	private float _previousShakeRot;

	private Vector2 _shakePos;

	private float _shakeRot;

	public float Strength
	{
		get
		{
			return _strength;
		}
		set
		{
			_strength = value;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_timer = 0f;
		_previousShakePos = Vector2.Zero;
		_previousShakeRot = 0f;
		_shakePos = Vector2.Zero;
		_shakeRot = 0f;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_target != null)
		{
			if (_strength == 0f)
			{
				_target.Position = Vector2.Zero;
				_target.RotationDegrees = 0f;
			}
			else if (_timer > 0f)
			{
				_timer = Mathf.Clamp(_timer - (float)delta, 0f, _frequency);
				float weight = 1f - _timer / _frequency;
				Vector2 position = _previousShakePos.Lerp(_shakePos, weight);
				float rotation = Mathf.Lerp(_previousShakeRot, _shakeRot, weight);
				SetTargetTransform(position, rotation);
			}
			else
			{
				_timer = _frequency;
				_previousShakePos = _shakePos;
				_previousShakeRot = _shakeRot;
				float s = Mathf.DegToRad(GD.Randf() * 360f);
				float num = GD.Randf() * _maxPosOffset;
				Vector2 vector = new Vector2(Mathf.Cos(s), Mathf.Sin(s)).Normalized();
				_shakePos = vector * num * _strength;
				_shakeRot = (float)GD.RandRange(0f - _maxRotOffset, _maxRotOffset);
				SetTargetTransform(_shakePos, _shakeRot);
			}
		}
	}

	private void SetTargetTransform(Vector2 position, float rotation)
	{
		if (_target != null)
		{
			_target.Position = position;
			_target.RotationDegrees = rotation;
		}
	}
}
