using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Confirm Button used in various places to confirm choices, embark on a run, etc.
/// Very useful but not as cool as Back Button but that's life.
/// Use the ButtonReleased signal to handle events.
/// </summary>
public partial class NMiscConfirmButton : NButton
{
	private Control _buttonImage;

	private Color _downColor = Colors.Gray;

	private static readonly Vector2 _hoverScale = new Vector2(1.05f, 1.05f);

	private static readonly Vector2 _downScale = new Vector2(0.95f, 0.95f);

	private const float _pressDownDur = 0.25f;

	private const float _unhoverAnimDur = 0.5f;

	private const float _animInOutDur = 0.35f;

	private Vector2 _showPos;

	private Vector2 _hidePos;

	private Tween? _moveTween;

	private CancellationTokenSource? _pressDownCancelToken;

	private CancellationTokenSource? _unhoverAnimCancelToken;

	public override void _Ready()
	{
		ConnectSignals();
		_isEnabled = false;
		_buttonImage = GetNode<Control>("Image");
		GetTree().Root.Connect(Viewport.SignalName.SizeChanged, Callable.From(OnWindowChange));
		OnWindowChange();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_pressDownCancelToken?.Cancel();
		_unhoverAnimCancelToken?.Cancel();
	}

	private void OnWindowChange()
	{
		_showPos = base.Position;
		_hidePos = base.Position + new Vector2(0f, 64f);
	}

	/// <summary>
	/// Call when we want this button to animate in.
	/// </summary>
	protected override void OnEnable()
	{
		_buttonImage.Modulate = Colors.White;
		_moveTween?.Kill();
		_moveTween = CreateTween();
		_moveTween.TweenProperty(this, "position", _showPos, 0.3499999940395355).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
			.From(_hidePos);
	}

	/// <summary>
	/// Call when we want this button to hide this button (Disables clickability/hotkeys)
	/// </summary>
	protected override void OnDisable()
	{
		_moveTween?.Kill();
		_moveTween = CreateTween();
		_moveTween.TweenProperty(this, "position", _hidePos, 0.3499999940395355).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
			.From(_showPos);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_unhoverAnimCancelToken?.Cancel();
		base.Scale = _hoverScale;
		_buttonImage.Modulate = Colors.White;
	}

	protected override void OnUnfocus()
	{
		_pressDownCancelToken?.Cancel();
		_unhoverAnimCancelToken = new CancellationTokenSource();
		TaskHelper.RunSafely(AnimUnhover(_unhoverAnimCancelToken));
	}

	private async Task AnimUnhover(CancellationTokenSource cancelToken)
	{
		float num = 0f;
		Vector2 startScale = base.Scale;
		Color startButtonColor = _buttonImage.Modulate;
		while (num < 0.5f)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			base.Scale = startScale.Lerp(Vector2.One, Ease.ExpoOut(num / 0.5f));
			_buttonImage.Modulate = startButtonColor.Lerp(Colors.White, Ease.ExpoOut(num / 0.5f));
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		base.Scale = Vector2.One;
		_buttonImage.Modulate = Colors.White;
	}

	protected override void OnPress()
	{
		base.OnPress();
		_pressDownCancelToken = new CancellationTokenSource();
		TaskHelper.RunSafely(AnimPressDown(_pressDownCancelToken));
	}

	private async Task AnimPressDown(CancellationTokenSource cancelToken)
	{
		float num = 0f;
		_buttonImage.Modulate = Colors.White;
		base.Scale = _hoverScale;
		while (num < 0.25f)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			base.Scale = _hoverScale.Lerp(_downScale, Ease.CubicOut(num / 0.25f));
			_buttonImage.Modulate = Colors.White.Lerp(_downColor, Ease.CubicOut(num / 0.25f));
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		base.Scale = _downScale;
		_buttonImage.Modulate = _downColor;
	}
}
