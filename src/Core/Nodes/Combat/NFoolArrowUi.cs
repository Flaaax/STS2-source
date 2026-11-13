using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.FoolArrows;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Combat;

/// <summary>
/// Represents a single arrow in the fool's bow and arrow UI.
/// </summary>
public partial class NFoolArrowUi : NClickableControl
{
	private ArrowSlot _arrowSlot;

	private TextureRect _visual;

	private TextureRect _outline;

	private bool _hasArrow;

	private static readonly string _path = SceneHelper.GetScenePath("combat/fool_arrow");

	private Tween? _createTween;

	private Tween? _shootTween;

	private int _arrowIndex;

	private const double _fadeDuration = 0.25;

	private static readonly Vector2 _shotArrowScale = Vector2.One * 1.4f;

	public ArrowSlot ArrowSlot => _arrowSlot;

	public static NFoolArrowUi Create(ArrowSlot arrowSlot, int index)
	{
		NFoolArrowUi nFoolArrowUi = PreloadManager.Cache.GetScene(_path).Instantiate<NFoolArrowUi>(PackedScene.GenEditState.Disabled);
		nFoolArrowUi._arrowSlot = arrowSlot;
		nFoolArrowUi._arrowIndex = index;
		return nFoolArrowUi;
	}

	public override void _Ready()
	{
		_visual = GetNode<TextureRect>("%Visual");
		_outline = GetNode<TextureRect>("%Stroke");
		bool flag = _arrowSlot.Status == ArrowStatus.Retrieved;
		base.Visible = _arrowSlot.Status != ArrowStatus.Destroyed;
		_visual.Modulate = (flag ? Colors.White : StsColors.transparentBlack);
		_visual.Scale = (flag ? Vector2.One : _shotArrowScale);
		ConnectSignals();
		_createTween = CreateTween().SetParallel();
		_createTween.TweenProperty(this, "modulate:a", 1f, 0.25);
		if (_arrowIndex % 2 == 0)
		{
			_visual.FlipV = false;
			_outline.FlipV = false;
			_createTween.TweenProperty(_visual, "position:y", 0f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
				.From(16f);
			_createTween.TweenProperty(_outline, "position:y", 0f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
				.From(16f);
		}
		else
		{
			_visual.FlipV = true;
			_outline.FlipV = true;
			_createTween.TweenProperty(_visual, "position:y", -8f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
				.From(-24f);
			_createTween.TweenProperty(_outline, "position:y", -8f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
				.From(-24f);
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		_arrowSlot.ArrowStatusUpdated += UpdateVisuals;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_arrowSlot.ArrowStatusUpdated -= UpdateVisuals;
		_createTween?.Kill();
		_shootTween?.Kill();
	}

	private void UpdateVisuals()
	{
		_shootTween?.Kill();
		base.Visible = _arrowSlot.Status != ArrowStatus.Destroyed;
		if (base.Visible)
		{
			_shootTween = CreateTween().SetParallel();
			_shootTween.TweenProperty(_visual, "modulate", (_arrowSlot.Status == ArrowStatus.Retrieved) ? Colors.White : StsColors.transparentBlack, 0.25);
			if (_arrowSlot.Status != ArrowStatus.Retrieved)
			{
				_shootTween.TweenProperty(_visual, "scale", _shotArrowScale, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
			}
			else
			{
				_shootTween.TweenProperty(_visual, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			}
		}
	}

	protected override void OnFocus()
	{
		NHoverTipSet.CreateAndShow(this, HoverTipFactory.ForArrow(_arrowSlot.owner, _arrowSlot.Status == ArrowStatus.Away))?.SetGlobalPosition(base.GlobalPosition + new Vector2(50f, 0f));
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
	}
}
