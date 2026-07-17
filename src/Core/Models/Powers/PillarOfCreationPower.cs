using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class PillarOfCreationPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Block));

	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator != null && creator.Creature == base.Owner)
		{
			CardModel cardModel = (from entry in CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>()
				where entry.Creator?.Creature == base.Owner && entry.HappenedThisTurn(base.CombatState)
				select entry.Card).FirstOrDefault();
			if (cardModel == card)
			{
				Flash();
				await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
			}
		}
	}
}
