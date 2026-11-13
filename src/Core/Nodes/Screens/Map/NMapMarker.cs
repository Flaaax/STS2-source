using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Map;

/// <summary>
/// The little arrow which indicates where you are on the map.
/// </summary>
public partial class NMapMarker : TextureRect
{
	private Tween? _tween;

	private Vector2 _posOffset;

	private bool _isEnabled;

	private bool _isInverted;

	public override void _Ready()
	{
		_posOffset = new Vector2((0f - base.Size.X) * 0.5f, -35f);
	}

	public void Invert()
	{
		_isInverted = !_isInverted;
		_posOffset.Y = 0f - _posOffset.Y;
	}

	public void Initialize(Player player)
	{
		IRunState runState = player.RunState;
		_isEnabled = runState.Players.Count == 1 || runState.GameMode == GameMode.Blitz;
		base.Texture = player.Character.MapMarker;
		base.Visible = false;
	}

	public void ResetMapPoint()
	{
		base.Visible = false;
	}

	public void HideMapPoint()
	{
		if (_isEnabled)
		{
			_tween?.FastForwardToCompletion();
			_tween = CreateTween();
			_tween.TweenProperty(this, "scale", Vector2.Zero, 0.20000000298023224).From(Vector2.One);
			_tween.TweenCallback(Callable.From(delegate
			{
				base.Visible = false;
			}));
		}
	}

	public void SetMapPoint(Control node)
	{
		if (_isEnabled)
		{
			_tween?.FastForwardToCompletion();
			if (!base.Visible)
			{
				float num = ((!_isInverted) ? 1 : (-1));
				base.Visible = true;
				Vector2 vector = new Vector2(node.Size.X * 0.5f, 0f);
				base.Position = node.Position + vector + _posOffset;
				base.Scale = new Vector2(0f, num);
				Vector2 vector2 = new Vector2(1f, num);
				_tween = CreateTween();
				_tween.TweenProperty(this, "scale", vector2, 0.20000000298023224);
				_tween.Parallel().TweenProperty(this, "position", base.Position + Vector2.Up * 25f * num, 0.20000000298023224).SetEase(Tween.EaseType.In)
					.SetTrans(Tween.TransitionType.Sine)
					.FromCurrent();
				_tween.TweenProperty(this, "position", base.Position, 0.75).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
			}
		}
	}
}
