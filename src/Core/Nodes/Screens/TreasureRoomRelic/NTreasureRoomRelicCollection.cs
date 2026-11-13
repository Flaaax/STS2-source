using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

/// <summary>
/// Selection screen for relics at the treasure room screen.
/// In singleplayer, this just shows one relic. In multiplayer, this shows multiple relics and handles animation that
/// occurs after all players select a relic.
/// </summary>
public partial class NTreasureRoomRelicCollection : Control, IScreenContext
{
	private const int _maxRelics = 6;

	private const ulong _noSelectionTimeMsec = 200uL;

	private Control _relicContainer;

	private Control _fightBackstop;

	private MegaLabel _fightLabel;

	private Tween? _emptyVfxTween;

	private NHandImageCollection _hands;

	private CancellationTokenSource _cts = new CancellationTokenSource();

	private readonly List<Control> _holderLayouts = new List<Control>();

	private List<NTreasureRoomRelicHolder> _holdersInUse = new List<NTreasureRoomRelicHolder>();

	private readonly TaskCompletionSource _relicPickingCompleteTaskCompletionSource = new TaskCompletionSource();

	private ulong _openedTicks;

	private IRunState _runState;

	private bool _isEmptyChest;

	public IReadOnlyList<NTreasureRoomRelicHolder> HoldersInUse => _holdersInUse;

	public Control? DefaultFocusedControl
	{
		get
		{
			if (_holdersInUse.Count <= 0)
			{
				return null;
			}
			return _holdersInUse[_runState.GetPlayerSlotIndex(LocalContext.GetMe(_runState.Players))];
		}
	}

	public override void _Ready()
	{
		_fightBackstop = GetNode<Control>("%FightBackstop");
		_hands = GetNode<NHandImageCollection>("%HandsContainer");
		_relicContainer = GetNode<Control>("Container");
		_holderLayouts.AddRange(_relicContainer.GetChildren().OfType<Control>().ToList());
		Control fightBackstop = _fightBackstop;
		Color modulate = _fightBackstop.Modulate;
		modulate.A = 0f;
		fightBackstop.Modulate = modulate;
		_fightBackstop.Visible = false;
		_fightLabel = GetNode<MegaLabel>("%FightLabel");
		_fightLabel.Text = new LocString("gameplay_ui", "TREASURE_FIGHT_TEXT").GetFormattedText();
		RunManager.Instance.TreasureRoomRelicSynchronizer.VotesChanged += RefreshVotes;
		RunManager.Instance.TreasureRoomRelicSynchronizer.RelicsAwarded += OnRelicsAwarded;
	}

	public override void _EnterTree()
	{
		_cts = new CancellationTokenSource();
	}

	public override void _ExitTree()
	{
		_cts.Cancel();
		RunManager.Instance.TreasureRoomRelicSynchronizer.VotesChanged -= RefreshVotes;
		RunManager.Instance.TreasureRoomRelicSynchronizer.RelicsAwarded -= OnRelicsAwarded;
	}

	public void Initialize(IRunState runState)
	{
		_runState = runState;
		_hands.Initialize(runState);
	}

	/// <summary>
	/// Initialize the relic display.
	/// This can't get called in _Ready because RelicPickingSynchronizer.BeginRelicPicking has not been called by then.
	/// </summary>
	public void InitializeRelics()
	{
		IReadOnlyList<RelicModel> currentRelics = RunManager.Instance.TreasureRoomRelicSynchronizer.CurrentRelics;
		if (currentRelics == null || currentRelics.Count == 0)
		{
			_isEmptyChest = true;
			foreach (Control holderLayout in _holderLayouts)
			{
				holderLayout.Visible = false;
			}
			SpawnEmptyChestVfx();
			return;
		}
		for (int i = 0; i < _holderLayouts.Count; i++)
		{
			if (i == currentRelics.Count - 1)
			{
				_holderLayouts[i].Visible = true;
				_holdersInUse = _holderLayouts[i].GetChildren().OfType<NTreasureRoomRelicHolder>().ToList();
			}
			else
			{
				_holderLayouts[i].Visible = false;
			}
		}
		for (int j = 0; j < _holdersInUse.Count; j++)
		{
			NTreasureRoomRelicHolder nTreasureRoomRelicHolder = _holdersInUse[j];
			nTreasureRoomRelicHolder.Initialize(currentRelics[j], _runState);
			nTreasureRoomRelicHolder.Index = j;
			nTreasureRoomRelicHolder.Connect(NClickableControl.SignalName.Released, Callable.From<NTreasureRoomRelicHolder>(PickRelic));
			nTreasureRoomRelicHolder.VoteContainer.RefreshPlayerVotes();
		}
		RefreshNavigation();
	}

	private void RefreshNavigation()
	{
		for (int i = 0; i < _holdersInUse.Count; i++)
		{
			_holdersInUse[i].SetFocusMode(FocusModeEnum.All);
			_holdersInUse[i].FocusNeighborTop = _holdersInUse[i].GetPath();
			_holdersInUse[i].FocusNeighborBottom = _holdersInUse[i].GetPath();
			NTreasureRoomRelicHolder nTreasureRoomRelicHolder = _holdersInUse[i];
			NodePath path;
			if (i <= 0)
			{
				List<NTreasureRoomRelicHolder> holdersInUse = _holdersInUse;
				path = holdersInUse[holdersInUse.Count - 1].GetPath();
			}
			else
			{
				path = _holdersInUse[i - 1].GetPath();
			}
			nTreasureRoomRelicHolder.FocusNeighborLeft = path;
			_holdersInUse[i].FocusNeighborRight = ((i < _holdersInUse.Count - 1) ? _holdersInUse[i + 1].GetPath() : _holdersInUse[0].GetPath());
		}
	}

	private void SpawnEmptyChestVfx()
	{
		MegaLabel node = GetNode<MegaLabel>("%EmptyLabel");
		node.Text = new LocString("gameplay_ui", "TREASURE_EMPTY").GetFormattedText();
		node.Visible = true;
		_emptyVfxTween = CreateTween().SetParallel();
		_emptyVfxTween.TweenProperty(node, "self_modulate:a", 1f, 0.25);
		_emptyVfxTween.TweenProperty(node, "position:y", node.Position.Y, 1.0).From(node.Position.Y + 64f).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Spring);
		_emptyVfxTween.Chain().TweenInterval(1.0);
		_emptyVfxTween.Chain();
		_emptyVfxTween.TweenProperty(node, "self_modulate:a", 0f, 1.0);
		GpuParticles2D node2 = GetNode<GpuParticles2D>("%SmokePuffVfx");
		node2.Emitting = true;
	}

	public void SetSelectionEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			foreach (NTreasureRoomRelicHolder item in _holdersInUse)
			{
				item.Enable();
			}
			return;
		}
		foreach (NTreasureRoomRelicHolder item2 in _holdersInUse)
		{
			item2.Disable();
		}
	}

	/// <summary>
	/// Await this to know when relic picking finishes.
	/// </summary>
	public Task RelicPickingFinished()
	{
		return _relicPickingCompleteTaskCompletionSource.Task;
	}

	/// <summary>
	/// Animates in the relic collection.
	/// </summary>
	public void AnimIn()
	{
		base.Visible = true;
		base.Modulate = Colors.Transparent;
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(this, "modulate", Colors.White, 0.4);
		if (_isEmptyChest)
		{
			LocalContext.GetMe(_runState)?.Relics.OfType<SilverCrucible>().FirstOrDefault()?.Flash();
			tween.TweenCallback(Callable.From(delegate
			{
				RunManager.Instance.TreasureRoomRelicSynchronizer.CompleteWithNoRelics();
			})).SetDelay(1.0);
			return;
		}
		foreach (NTreasureRoomRelicHolder holder in _holdersInUse)
		{
			holder.MouseFilter = MouseFilterEnum.Ignore;
			float num = ((_holdersInUse.Count == 1) ? 150f : 50f);
			float num2 = 0.2f + 0.2f * Rng.Chaotic.NextFloat();
			holder.Modulate = Colors.Black;
			NTreasureRoomRelicHolder nTreasureRoomRelicHolder = holder;
			Vector2 position = holder.Position;
			position.Y = holder.Position.Y + num;
			nTreasureRoomRelicHolder.Position = position;
			Tween tween2 = CreateTween().SetParallel();
			tween2.TweenProperty(holder, "modulate", Colors.White, 0.2).SetDelay(num2);
			tween2.TweenProperty(holder, "position:y", holder.Position.Y - num, 0.6).SetDelay(num2).SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
			tween2.TweenCallback(Callable.From(() => holder.MouseFilter = MouseFilterEnum.Stop)).SetDelay((double)num2 + 0.6);
		}
		NRun.Instance.ScreenStateTracker.SetIsInSharedRelicPickingScreen(isInSharedRelicPicking: true);
		_hands.AnimateHandsIn();
	}

	/// <summary>
	/// Animates out the relic collection, after relic picking is done.
	/// </summary>
	public void AnimOut()
	{
		base.Modulate = Colors.White;
		Tween tween = CreateTween().Parallel();
		tween.TweenProperty(this, "modulate", StsColors.transparentWhite, 0.3);
		tween.TweenCallback(Callable.From(() => base.Visible = false));
		NRun.Instance.ScreenStateTracker.SetIsInSharedRelicPickingScreen(isInSharedRelicPicking: false);
	}

	private void PickRelic(NTreasureRoomRelicHolder holder)
	{
		if (Time.GetTicksMsec() - _openedTicks > 200)
		{
			RunManager.Instance.TreasureRoomRelicSynchronizer.PickRelicLocally(holder.Index);
		}
	}

	private void OnRelicsAwarded(List<RelicPickingResult> results, bool isFinished)
	{
		TaskHelper.RunSafely(AnimateRelicAwards(results, isFinished));
	}

	private async Task AnimateRelicAwards(List<RelicPickingResult> results, bool isFinished)
	{
		CancellationToken ct = _cts.Token;
		SetSelectionEnabled(isEnabled: false);
		foreach (Player player in _runState.Players)
		{
			TreasureRoomRelicSynchronizer.PlayerVote playerVote = RunManager.Instance.TreasureRoomRelicSynchronizer.GetPlayerVote(player);
			if (playerVote.voteReceived && !playerVote.index.HasValue)
			{
				_hands.GetHand(player.NetId)?.SetSkipped();
			}
		}
		_hands.BeforeRelicsAwarded();
		List<Task> tasksToWait = new List<Task>();
		RelicPickingResultType? relicPickingResultType = null;
		results.Sort((RelicPickingResult r1, RelicPickingResult r2) => r1.type.CompareTo(r2.type));
		foreach (RelicPickingResult result in results)
		{
			NTreasureRoomRelicHolder holder = _holdersInUse[result.relicIndex];
			holder.AnimateAwayVotes();
			if (relicPickingResultType.HasValue && result.type != relicPickingResultType && result.type != RelicPickingResultType.Skipped)
			{
				await Cmd.Wait(0.5f, ct);
			}
			if (result.type == RelicPickingResultType.FoughtOver)
			{
				Node parent = _fightBackstop.GetParent();
				holder.Reparent(parent);
				parent.MoveChildSafely(holder, _fightBackstop.GetIndex() + 1);
				_fightBackstop.Visible = true;
				Tween tween = CreateTween();
				tween.TweenProperty(holder, "global_position", (_fightBackstop.Size - holder.Size) * 0.5f, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
				tween.TweenProperty(_fightBackstop, "modulate:a", 1f, 0.25);
				_hands.BeforeFightStarted(result.fight.playersInvolved);
				if (await tween.AwaitFinished(this, ct) != TweenOutcome.Finished)
				{
					return;
				}
				await Cmd.Wait(1f, ct);
				await _hands.DoFight(result, holder);
				if (HasBeenRemoved())
				{
					return;
				}
				tween = CreateTween();
				tween.TweenProperty(_fightBackstop, "modulate:a", 0f, 0.25);
				if (await tween.AwaitFinished(this, ct) != TweenOutcome.Finished)
				{
					return;
				}
				_fightBackstop.Visible = false;
			}
			else if (result.type == RelicPickingResultType.Skipped)
			{
				if (_runState.Players.Count > 1)
				{
					Tween tween2 = CreateTween().Parallel();
					tween2.TweenProperty(holder, "modulate", StsColors.transparentWhite, 0.3);
					tasksToWait.Add(tween2.AwaitFinished(this));
				}
			}
			else if (result.type != RelicPickingResultType.Skipped)
			{
				NHandImage hand = _hands.GetHand(result.player.NetId);
				if (hand != null)
				{
					tasksToWait.Add(TaskHelper.RunSafely(hand.GrabRelic(holder)));
					await Cmd.Wait(0.25f, ct);
				}
			}
			relicPickingResultType = result.type;
		}
		await Task.WhenAll(tasksToWait);
		if (tasksToWait.Count == 0 && _runState.Players.Count > 1)
		{
			await Cmd.Wait(0.7f, ct);
		}
		if (HasBeenRemoved())
		{
			return;
		}
		foreach (RelicPickingResult result2 in results)
		{
			NTreasureRoomRelicHolder nTreasureRoomRelicHolder = _holdersInUse[result2.relicIndex];
			RelicModel relic = result2.relic.ToMutable();
			if (result2.type != RelicPickingResultType.Skipped)
			{
				TaskHelper.RunSafely(RelicCmd.Obtain(relic, result2.player));
				if (LocalContext.IsMe(result2.player))
				{
					NRun.Instance.GlobalUi.RelicInventory.AnimateRelic(relic, nTreasureRoomRelicHolder.GlobalPosition, nTreasureRoomRelicHolder.Scale);
				}
				if (_runState.Players.Count == 1)
				{
					nTreasureRoomRelicHolder.Visible = false;
				}
			}
			foreach (Player player2 in _runState.Players)
			{
				if (player2 != result2.player)
				{
					player2.RelicGrabBag.MoveToFallback(result2.relic);
				}
			}
		}
		if (isFinished)
		{
			_relicPickingCompleteTaskCompletionSource.SetResult();
			return;
		}
		foreach (RelicPickingResult result3 in results)
		{
			_holdersInUse.RemoveAt(result3.relicIndex);
		}
		for (int num = 0; num < _holdersInUse.Count; num++)
		{
			_holdersInUse[num].Index = num;
		}
		RefreshNavigation();
		DefaultFocusedControl?.TryGrabFocus();
		SetSelectionEnabled(isEnabled: true);
		bool HasBeenRemoved()
		{
			if (this.IsValid())
			{
				return !IsInsideTree();
			}
			return true;
		}
	}

	private void RefreshVotes()
	{
		foreach (NTreasureRoomRelicHolder item in _holdersInUse)
		{
			item.VoteContainer.RefreshPlayerVotes();
		}
	}
}
