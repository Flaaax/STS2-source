using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

/// <summary>
/// This is pretty close to NPaginator, but NPaginator only supports a static list of options whereas this needs to
/// - Set its text based on a DateTimeOffset that is (effectively) infinite
/// - Dynamically enable/disable arrows based on whether the query says there is a leaderboard
/// </summary>
public partial class NLeaderboardDayPaginator : Control
{
	protected MegaLabel _label;

	private MegaLabel _vfxLabel;

	private NLeaderboardDayArrow _leftArrow;

	private NLeaderboardDayArrow _rightArrow;

	private NSelectionReticle _selectionReticle;

	private Tween? _tween;

	private const double _animDuration = 0.25;

	private const float _animDistance = 90f;

	private DateTimeOffset _currentDay;

	private NDailyRunLeaderboard? _leaderboard;

	private bool _canPageLeft;

	private bool _canPageRight;

	private bool _paginationAllowed = true;

	public override void _Ready()
	{
		_label = GetNode<MegaLabel>("LabelContainer/Mask/Label");
		_vfxLabel = GetNode<MegaLabel>("LabelContainer/Mask/VfxLabel");
		_selectionReticle = GetNode<NSelectionReticle>("SelectionReticle");
		_leftArrow = GetNode<NLeaderboardDayArrow>("LeftArrow");
		_rightArrow = GetNode<NLeaderboardDayArrow>("RightArrow");
		Connect(Control.SignalName.FocusEntered, Callable.From(OnFocus));
		Connect(Control.SignalName.FocusExited, Callable.From(OnUnfocus));
		_leftArrow.Connect(PageLeft);
		_rightArrow.Connect(PageRight);
		SetEnabled(leftArrowEnabled: false, rightArrowEnabled: false);
	}

	public void Initialize(NDailyRunLeaderboard leaderboard, DateTimeOffset dateTime, bool allowPagination)
	{
		_currentDay = dateTime;
		_leaderboard = leaderboard;
		_paginationAllowed = allowPagination;
		OnDayChanged(changeLeaderboardDay: false);
		_leftArrow.Visible = allowPagination;
		_rightArrow.Visible = allowPagination;
		base.FocusMode = (FocusModeEnum)(allowPagination ? 2 : 0);
	}

	private void PageLeft()
	{
		if (_paginationAllowed && _canPageLeft && _leaderboard != null)
		{
			DateTimeOffset? dateTimeOffset = DailyRunUtility.AddLeaderboardDays(_currentDay, -1);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				_currentDay = valueOrDefault;
				DayChangeHelper(pagedLeft: true);
			}
		}
	}

	private void PageRight()
	{
		if (_paginationAllowed && _canPageRight && _leaderboard != null)
		{
			DateTimeOffset? dateTimeOffset = DailyRunUtility.AddLeaderboardDays(_currentDay, 1);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				_currentDay = valueOrDefault;
				DayChangeHelper(pagedLeft: false);
			}
		}
	}

	private void DayChangeHelper(bool pagedLeft)
	{
		_vfxLabel.SetTextAutoSize(_label.Text);
		_vfxLabel.Modulate = _label.Modulate;
		OnDayChanged(changeLeaderboardDay: true);
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_label, "position:x", 0f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic)
			.From(pagedLeft ? (-90f) : 90f);
		_tween.TweenProperty(_label, "modulate:a", 1f, 0.25).From(0.75f);
		_tween.TweenProperty(_vfxLabel, "position:x", pagedLeft ? 90f : (-90f), 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic)
			.From(0f);
		_tween.TweenProperty(_vfxLabel, "modulate", StsColors.transparentBlack, 0.25);
	}

	private void OnDayChanged(bool changeLeaderboardDay)
	{
		_label.SetTextAutoSize(_currentDay.ToString(NDailyRunScreen.dateFormat));
		if (changeLeaderboardDay)
		{
			_leaderboard.SetDay(_currentDay);
		}
	}

	public void SetEnabled(bool leftArrowEnabled, bool rightArrowEnabled)
	{
		_canPageLeft = leftArrowEnabled && _paginationAllowed;
		_canPageRight = rightArrowEnabled && _paginationAllowed;
		_leftArrow.SetEnabled(_canPageLeft);
		_rightArrow.SetEnabled(_canPageRight);
	}

	private void OnFocus()
	{
		if (NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_selectionReticle.OnSelect();
		}
	}

	private void OnUnfocus()
	{
		_selectionReticle.OnDeselect();
	}
}
