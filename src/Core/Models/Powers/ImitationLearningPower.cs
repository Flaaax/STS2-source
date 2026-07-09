using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class ImitationLearningPower : PowerModel
{
	private const string _targetPlayerKey = "TargetPlayer";

	private Player? _playerTarget;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	/// <summary>
	/// The player whose powers we are copying.
	/// This is different than <see cref="P:MegaCrit.Sts2.Core.Models.PowerModel.Target" />, which is used specifically for the case in which an
	/// enemy has multiple of the same power targeting different players. If this kind of concept becomes common, we
	/// can make it first class in PowerModel, or combine it with Target in some way.
	/// </summary>
	public Player PlayerTarget
	{
		get
		{
			return _playerTarget ?? throw new InvalidOperationException();
		}
		set
		{
			AssertMutable();
			_playerTarget = value;
			((StringVar)base.DynamicVars["TargetPlayer"]).StringValue = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, _playerTarget.NetId);
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new StringVar("TargetPlayer"));

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (_playerTarget == null)
		{
			throw new InvalidOperationException("ImitationLearningPower applied without a player target!");
		}
		if (cardPlay.Card.Owner == _playerTarget && cardPlay.Card.Type == CardType.Power)
		{
			Flash();
			CardModel card = cardPlay.Card.CreateCloneForPlayer(base.Owner.Player);
			await PowerCmd.Decrement(this);
			await CardCmd.AutoPlay(choiceContext, card, null);
		}
	}
}
