using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Confirm Button used in various places to confirm choices, embark on a run, etc.
/// Very useful but not as cool as Back Button but that's life.
/// Use the ButtonReleased Action to handle events.
/// </summary>
public partial class NConfirmButton : NButton
{
	private Control _outline;

	private Control _buttonImage;

	private Color _defaultOutlineColor = StsColors.cream;

	private Color _hoveredOutlineColor = StsColors.gold;

	private Color _downColor = Colors.Gray;

	private Color _outlineColor = new Color("F0B400");

	private Color _outlineTransparentColor = new Color("00FFFF00");

	private Viewport _viewport;

	private string[] _hotkeys = new string[1] { MegaInput.accept };

	private static readonly Vector2 _hoverScale = new Vector2(1.05f, 1.05f);

	private static readonly Vector2 _downScale = new Vector2(0.95f, 0.95f);

	private const float _pressDownDur = 0.25f;

	private const float _unhoverAnimDur = 0.5f;

	private const float _animInOutDur = 0.35f;

	private Vector2 _posOffset;

	private Vector2 _showPos;

	private Vector2 _hidePos;

	private static readonly Vector2 _hideOffset = new Vector2(180f, 0f);

	private Tween? _moveTween;

	private CancellationTokenSource? _pressDownCancelToken;

	private CancellationTokenSource? _unhoverAnimCancelToken;

	protected override string[] Hotkeys => _hotkeys;

	public override void _Ready()
	{
		ConnectSignals();
		_isEnabled = false;
		_outline = GetNode<Control>("Outline");
		_buttonImage = GetNode<Control>("Image");
		_viewport = GetViewport();
		_posOffset = new Vector2(base.OffsetRight + 120f, 0f - base.OffsetBottom + 110f);
		GetTree().Root.Connect(Viewport.SignalName.SizeChanged, Callable.From(OnWindowChange));
		OnWindowChange();
		OnDisable();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_pressDownCancelToken?.Cancel();
		_unhoverAnimCancelToken?.Cancel();
	}

	private void OnWindowChange()
	{
		_showPos = NGame.Instance.Size - _posOffset;
		_hidePos = _showPos + _hideOffset;
		base.Position = (_isEnabled ? _showPos : _hidePos);
	}

	/// <summary>
	/// Call when we want this button to animate in.
	/// </summary>
	protected override void OnEnable()
	{
		base.OnEnable();
		_isEnabled = true;
		_outline.Modulate = Colors.Transparent;
		_buttonImage.Modulate = Colors.White;
		_moveTween?.Kill();
		_moveTween = CreateTween();
		_moveTween.TweenProperty(this, "position", _showPos, 0.3499999940395355).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
			.FromCurrent();
	}

	/// <summary>
	/// Call when we want this button to hide this button (Disables clickability/hotkeys)
	/// </summary>
	protected override void OnDisable()
	{
		base.OnDisable();
		_isEnabled = false;
		_moveTween?.Kill();
		_moveTween = CreateTween();
		_moveTween.TweenProperty(this, "position", _hidePos, 0.3499999940395355).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
			.FromCurrent();
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_unhoverAnimCancelToken?.Cancel();
		base.Scale = _hoverScale;
		_outline.Modulate = _outlineColor;
		_buttonImage.Modulate = Colors.White;
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_pressDownCancelToken?.Cancel();
		_unhoverAnimCancelToken = new CancellationTokenSource();
		TaskHelper.RunSafely(AnimUnhover(_unhoverAnimCancelToken));
	}

	private async Task AnimUnhover(CancellationTokenSource cancelToken)
	{
		float num = 0f;
		Vector2 startScale = base.Scale;
		Color startButtonColor = _buttonImage.Modulate;
		Color startColor = _outline.Modulate;
		while (num < 0.5f)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			base.Scale = startScale.Lerp(Vector2.One, Ease.ExpoOut(num / 0.5f));
			_outline.Modulate = startColor.Lerp(_outlineTransparentColor, Ease.ExpoOut(num / 0.5f));
			_buttonImage.Modulate = startButtonColor.Lerp(Colors.White, Ease.ExpoOut(num / 0.5f));
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		base.Scale = Vector2.One;
		_outline.Modulate = _outlineTransparentColor;
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
		_outline.Modulate = _outlineColor;
		base.Scale = _hoverScale;
		while (num < 0.25f)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			base.Scale = _hoverScale.Lerp(_downScale, Ease.CubicOut(num / 0.25f));
			_buttonImage.Modulate = Colors.White.Lerp(_downColor, Ease.CubicOut(num / 0.25f));
			_outline.Modulate = _outlineColor.Lerp(_outlineTransparentColor, Ease.CubicOut(num / 0.25f));
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		base.Scale = _downScale;
		_buttonImage.Modulate = _downColor;
		_outline.Modulate = _outlineTransparentColor;
	}

	public void OverrideHotkeys(string[] hotkeys)
	{
		_hotkeys = hotkeys;
	}
}
