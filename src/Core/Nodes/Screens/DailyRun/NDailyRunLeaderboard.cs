using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Leaderboard;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

public partial class NDailyRunLeaderboard : Control
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/daily_run/daily_run_leaderboard");

	private const int _maxEntries = 10;

	private NLeaderboardDayPaginator _paginator;

	private VBoxContainer _scoreContainer;

	private NLeaderboardPageArrow _leftArrow;

	private NLeaderboardPageArrow _rightArrow;

	private MegaRichTextLabel _loadingIndicator;

	private MegaLabel _noScoresIndicator;

	private MegaLabel _noFriendsIndicator;

	private Control? _noScoreUploadIndicator;

	private Control _separators;

	private NGlobalRankTickbox _globalTickbox;

	private const int _defaultGlobalIndex = -4;

	private int _currentPage;

	private int _currentIndex = -4;

	private DateTimeOffset _todaysDailyTime;

	private DateTimeOffset? _leaderboardTime;

	private readonly List<ulong> _playersInRun = new List<ulong>();

	private bool _hasNegativeScore;

	private CancellationTokenSource? _loadCts;

	private static readonly LocString _titleLoc = new LocString("main_menu_ui", "DAILY_RUN_MENU.LEADERBOARDS.title");

	private static readonly LocString _scoreLoc = new LocString("main_menu_ui", "DAILY_RUN_MENU.LEADERBOARDS.noScore");

	private static readonly LocString _fetchingScoreLoc = new LocString("main_menu_ui", "DAILY_RUN_MENU.LEADERBOARDS.fetchingScores");

	private static readonly LocString _friendsLoc = new LocString("main_menu_ui", "DAILY_RUN_MENU.LEADERBOARDS.noFriends");

	public static string[] AssetPaths => new string[1] { _scenePath };

	public override void _Ready()
	{
		_paginator = GetNode<NLeaderboardDayPaginator>("Paginator");
		_scoreContainer = GetNodeOrNull<VBoxContainer>("%ScoreContainer") ?? GetNodeOrNull<VBoxContainer>("%LeaderboardScoreContainer") ?? throw new InvalidOperationException("Couldn't find score container");
		_leftArrow = GetNode<NLeaderboardPageArrow>("%LeftArrow");
		_rightArrow = GetNode<NLeaderboardPageArrow>("%RightArrow");
		_loadingIndicator = GetNode<MegaRichTextLabel>("%LoadingText");
		_noScoresIndicator = GetNode<MegaLabel>("%NoScoresIndicator");
		_noFriendsIndicator = GetNode<MegaLabel>("%NoFriendsIndicator");
		_noScoreUploadIndicator = GetNodeOrNull<Control>("%ScoreWarning");
		_separators = GetNode<Control>("%Separators");
		_globalTickbox = GetNode<NGlobalRankTickbox>("%GlobalTickbox");
		_globalTickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(ToggleGlobalRank));
		_globalTickbox.SetLabel(new LocString("main_menu_ui", "DAILY_RUN_MENU.LEADERBOARDS.globalTickbox").GetRawText());
		CallDeferred(MethodName.SetLocalizedText);
		_loadingIndicator.SetTextAutoSize(_fetchingScoreLoc.GetFormattedText());
		_rightArrow.Connect(delegate
		{
			ChangePage(_currentIndex += 10);
		});
		_leftArrow.Connect(delegate
		{
			ChangePage(_currentIndex -= 10);
		});
	}

	private void ToggleGlobalRank(NTickbox tickbox)
	{
		if (tickbox.IsTicked)
		{
			DateTimeOffset? leaderboardTime = _leaderboardTime;
			if (leaderboardTime.HasValue)
			{
				DateTimeOffset valueOrDefault = leaderboardTime.GetValueOrDefault();
				_currentIndex = -4;
				TaskHelper.RunSafely(LoadLeaderboard(valueOrDefault, _currentPage, LeaderboardQueryType.AroundUser));
			}
		}
		else
		{
			SetPage(0);
		}
	}

	private void SetLocalizedText()
	{
		GetNodeOrNull<MegaLabel>("%Title")?.SetTextAutoSize(_titleLoc.GetRawText());
		_noScoresIndicator.SetTextAutoSize(_scoreLoc.GetRawText());
		_noFriendsIndicator.SetTextAutoSize(_friendsLoc.GetRawText());
	}

	public void Cleanup()
	{
		_loadCts?.Cancel();
		_loadCts = null;
		_leftArrow.Visible = false;
		_rightArrow.Visible = false;
		_loadingIndicator.Visible = false;
		_noScoresIndicator.Visible = false;
		_noFriendsIndicator.Visible = false;
		_paginator.Visible = false;
		_globalTickbox.Visible = false;
		_currentIndex = -4;
		_leaderboardTime = null;
		if (_noScoreUploadIndicator != null)
		{
			_noScoreUploadIndicator.Visible = false;
		}
		ClearEntries();
	}

	/// <summary>
	/// Does first-time initialization with the given day. Should only be called when the page loads and when a new
	/// player joins the run.
	/// </summary>
	public void Initialize(DateTimeOffset dateTime, IEnumerable<ulong> playersInRun, bool allowPagination)
	{
		_playersInRun.Clear();
		_playersInRun.AddRange(playersInRun);
		_paginator.Initialize(this, dateTime, allowPagination);
		_paginator.Visible = true;
		_todaysDailyTime = dateTime;
		SetDay(dateTime);
	}

	/// <summary>
	/// Called by the paginator when the day in the paginator changes.
	/// </summary>
	public void SetDay(DateTimeOffset dateTime)
	{
		_leaderboardTime = dateTime;
		if (!_globalTickbox.IsTicked)
		{
			SetPage(0);
		}
		else
		{
			TaskHelper.RunSafely(LoadLeaderboard(dateTime, _currentPage, LeaderboardQueryType.AroundUser));
		}
	}

	private void ChangePage(int _ = 0)
	{
		NavigateGlobalRank();
	}

	private void SetPage(int page)
	{
		DateTimeOffset? leaderboardTime = _leaderboardTime;
		if (leaderboardTime.HasValue)
		{
			DateTimeOffset valueOrDefault = leaderboardTime.GetValueOrDefault();
			_currentPage = page;
			TaskHelper.RunSafely(LoadLeaderboard(valueOrDefault, _currentPage, LeaderboardQueryType.FriendsOnly));
		}
	}

	/// <summary>
	/// Called when we press left/right arrows when viewing Global Rank.
	/// Lets other players view the scores and names of other players ranked above/below them.
	/// </summary>
	private void NavigateGlobalRank()
	{
		DateTimeOffset? leaderboardTime = _leaderboardTime;
		if (leaderboardTime.HasValue)
		{
			DateTimeOffset valueOrDefault = leaderboardTime.GetValueOrDefault();
			TaskHelper.RunSafely(LoadLeaderboard(valueOrDefault, _currentPage, LeaderboardQueryType.AroundUser));
		}
	}

	private async Task LoadLeaderboard(DateTimeOffset dateTime, int page, LeaderboardQueryType queryType)
	{
		if (_loadCts != null)
		{
			await _loadCts.CancelAsync();
		}
		_loadCts = new CancellationTokenSource();
		CancellationToken ct = _loadCts.Token;
		ClearEntries();
		_rightArrow.Disable();
		_leftArrow.Disable();
		_paginator.Disable();
		_noFriendsIndicator.Visible = false;
		_noScoresIndicator.Visible = false;
		_loadingIndicator.Visible = true;
		_separators.Visible = false;
		_globalTickbox.Visible = false;
		try
		{
			string leaderboardName = DailyRunUtility.GetLeaderboardName(dateTime, _playersInRun.Count);
			DateTimeOffset? dateTimeOffset = DailyRunUtility.AddLeaderboardDays(dateTime, -1);
			DateTimeOffset? rightLeaderboardTime = DailyRunUtility.AddLeaderboardDays(dateTime, 1);
			Task<ILeaderboardHandle?> mainTask = LeaderboardManager.GetLeaderboard(leaderboardName, ct);
			Task<ILeaderboardHandle> task;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				task = LeaderboardManager.GetLeaderboard(DailyRunUtility.GetLeaderboardName(valueOrDefault, _playersInRun.Count), ct);
			}
			else
			{
				task = Task.FromResult<ILeaderboardHandle>(null);
			}
			Task<ILeaderboardHandle?> leftTask = task;
			Task<ILeaderboardHandle> task2;
			if (rightLeaderboardTime.HasValue)
			{
				DateTimeOffset valueOrDefault2 = rightLeaderboardTime.GetValueOrDefault();
				task2 = LeaderboardManager.GetLeaderboard(DailyRunUtility.GetLeaderboardName(valueOrDefault2, _playersInRun.Count), ct);
			}
			else
			{
				task2 = Task.FromResult<ILeaderboardHandle>(null);
			}
			Task<ILeaderboardHandle?> rightTask = task2;
			global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>> buffer = default(global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>>, Task<ILeaderboardHandle>>(ref buffer, 0) = mainTask;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>>, Task<ILeaderboardHandle>>(ref buffer, 1) = leftTask;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>>, Task<ILeaderboardHandle>>(ref buffer, 2) = rightTask;
			await Task.WhenAll(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<Task<ILeaderboardHandle>>, Task<ILeaderboardHandle>>(in buffer, 3));
			ILeaderboardHandle handle = await mainTask;
			if (ct.IsCancellationRequested)
			{
				return;
			}
			if (handle != null)
			{
				switch (queryType)
				{
				case LeaderboardQueryType.FriendsOnly:
					await QueryFriendScores(handle, page, ct);
					break;
				case LeaderboardQueryType.AroundUser:
					await QueryGlobalScores(handle, ct);
					break;
				}
			}
			else
			{
				_noScoresIndicator.Visible = true;
				_leftArrow.Visible = false;
				_rightArrow.Visible = false;
				_separators.Visible = false;
				_globalTickbox.Visible = false;
			}
			bool hasLeftLeaderboard = await leftTask != null;
			bool rightArrowEnabled = await rightTask != null || rightLeaderboardTime == _todaysDailyTime;
			_currentPage = page;
			_paginator.Enable(hasLeftLeaderboard, rightArrowEnabled);
			_loadingIndicator.Visible = false;
			if (_noScoreUploadIndicator != null && _todaysDailyTime == dateTime)
			{
				bool flag = await DailyRunUtility.ShouldUploadScore(handle, _playersInRun, ct);
				if (!ct.IsCancellationRequested)
				{
					_noScoreUploadIndicator.Visible = !flag;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	private async Task QueryFriendScores(ILeaderboardHandle handle, int page, CancellationToken ct)
	{
		List<LeaderboardEntry> list = await LeaderboardManager.QueryLeaderboard(handle, LeaderboardQueryType.FriendsOnly, page * 10, 10, ct);
		if (!ct.IsCancellationRequested)
		{
			if (list.Count > 0)
			{
				_noScoresIndicator.Visible = false;
				_globalTickbox.Visible = true;
				_separators.Visible = true;
			}
			else
			{
				_noScoresIndicator.Visible = true;
				_globalTickbox.Visible = false;
				_separators.Visible = false;
			}
			FillEntries(list);
			_leftArrow.Visible = false;
			_rightArrow.Visible = false;
		}
	}

	private async Task QueryGlobalScores(ILeaderboardHandle handle, CancellationToken ct)
	{
		List<LeaderboardEntry> list = await LeaderboardManager.QueryLeaderboard(handle, LeaderboardQueryType.AroundUser, _currentIndex, 10, ct);
		if (ct.IsCancellationRequested)
		{
			return;
		}
		if (list.Count > 0)
		{
			_noScoresIndicator.Visible = false;
			_globalTickbox.Visible = true;
			_separators.Visible = true;
		}
		else
		{
			_noScoresIndicator.Visible = true;
			_globalTickbox.Visible = false;
			_separators.Visible = false;
		}
		FillEntries(list);
		if (list.Count > 0)
		{
			int leaderboardEntryCount = LeaderboardManager.GetLeaderboardEntryCount(handle);
			_leftArrow.Visible = true;
			_rightArrow.Visible = true;
			if (list[0].rank > 0)
			{
				_leftArrow.Enable();
			}
			else
			{
				_leftArrow.Disable();
			}
			if (list[list.Count - 1].rank < leaderboardEntryCount - 1 && !_hasNegativeScore)
			{
				_rightArrow.Enable();
			}
			else
			{
				_rightArrow.Disable();
			}
		}
	}

	private void FillEntries(List<LeaderboardEntry> entries)
	{
		if (entries.Count == 0)
		{
			return;
		}
		_hasNegativeScore = false;
		NDailyRunLeaderboardHeader child = NDailyRunLeaderboardHeader.Create();
		_scoreContainer.AddChildSafely(child);
		NDailyRunLeaderboardSeparator child2 = NDailyRunLeaderboardSeparator.Create();
		_scoreContainer.AddChildSafely(child2);
		ulong localPlayerId = PlatformUtil.GetLocalPlayerId(LeaderboardManager.CurrentPlatform);
		foreach (LeaderboardEntry entry in entries)
		{
			if (entry.score < 0)
			{
				_hasNegativeScore = true;
				continue;
			}
			_scoreContainer.AddChildSafely(NDailyRunLeaderboardRow.Create(entry, entry.userIds.Contains(localPlayerId)));
			_scoreContainer.AddChildSafely(NDailyRunLeaderboardSeparator.Create());
		}
	}

	private void ClearEntries()
	{
		_noScoresIndicator.Visible = false;
		_noFriendsIndicator.Visible = false;
		foreach (Node child in _scoreContainer.GetChildren())
		{
			child.QueueFreeSafely();
		}
	}

	public override void _ExitTree()
	{
		_loadCts?.Cancel();
		_loadCts = null;
	}
}
