using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

public partial class NTreasureRoomRelicHolder : NButton
{
	private GpuParticles2D _glowVfx;

	private bool _animatedIn;

	private Tween? _tween;

	private Tween? _initTween;

	public int Index { get; set; }

	public NMultiplayerVoteContainer VoteContainer { get; private set; }

	public NRelic Relic { get; private set; }

	public override void _Ready()
	{
		ConnectSignals();
		VoteContainer = GetNode<NMultiplayerVoteContainer>("%MultiplayerVoteContainer");
	}

	public void Initialize(RelicModel relic, IRunState runState)
	{
		Relic = NRelic.CreateNonNull(relic, NRelic.IconSize.Large);
		this.AddChildSafely(Relic);
		this.MoveChildSafely(Relic, 0);
		Relic.Modulate = StsColors.transparentBlack;
		Relic.Scale = Vector2.One * 2f;
		Relic.Position = new Vector2(68f, 68f);
		VoteContainer.Initialize(PlayerVotedForRelic, runState.Players);
		_initTween?.Kill();
		_initTween = CreateTween().SetParallel();
		_initTween.TweenProperty(Relic, "modulate", Colors.White, 0.25);
		CreateGlowVfx(relic);
	}

	private void CreateGlowVfx(RelicModel relic)
	{
		if (!TestMode.IsOn)
		{
			if (relic.Rarity == RelicRarity.Uncommon)
			{
				Tween tween = CreateTween().SetParallel();
				_glowVfx = NCardUncommonGlow.Create();
				Relic.AddChildSafely(_glowVfx);
				_glowVfx.Scale = Vector2.One * 0.3f;
				_glowVfx.ShowBehindParent = true;
				_glowVfx.Modulate = StsColors.transparentWhite;
				_glowVfx.Position = Relic.Size * 0.5f;
				tween.TweenProperty(_glowVfx, "modulate", Colors.White, 0.25);
			}
			else if (relic.Rarity == RelicRarity.Rare)
			{
				Tween tween2 = CreateTween().SetParallel();
				_glowVfx = NRelicRareGlow.Create();
				Relic.AddChildSafely(_glowVfx);
				_glowVfx.Scale = Vector2.One * 0.3f;
				_glowVfx.ShowBehindParent = true;
				_glowVfx.Modulate = StsColors.transparentWhite;
				_glowVfx.Position = Relic.Size * 0.5f;
				tween2.TweenProperty(_glowVfx, "modulate", Colors.White, 0.25);
			}
		}
	}

	private bool PlayerVotedForRelic(Player player)
	{
		TreasureRoomRelicSynchronizer.PlayerVote playerVote = RunManager.Instance.TreasureRoomRelicSynchronizer.GetPlayerVote(player);
		if (playerVote.voteReceived)
		{
			return playerVote.index == Index;
		}
		return false;
	}

	public void AnimateAwayVotes()
	{
		CreateTween().TweenProperty(VoteContainer, "modulate:a", 0f, 0.25);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(Relic, "scale", Vector2.One * 2.1f, 0.05);
		NHoverTipSet.CreateAndShow(this, Relic.Model.HoverTips)?.SetAlignmentForRelic(Relic);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(Relic, "scale", Vector2.One * 2f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		NHoverTipSet.Remove(this);
	}

	protected override void OnPress()
	{
		base.OnPress();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(Relic, "scale", Vector2.One * 1.9f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(Relic, "scale", Vector2.One * 2f, 0.05);
	}
}
