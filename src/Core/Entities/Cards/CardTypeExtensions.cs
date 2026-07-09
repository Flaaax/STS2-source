using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Entities.Cards;

public static class CardTypeExtensions
{
	public static LocString ToLocString(this CardType cardType)
	{
		switch (cardType)
		{
		case CardType.Attack:
			return new LocString("gameplay_ui", "CARD_TYPE.ATTACK");
		case CardType.Skill:
			return new LocString("gameplay_ui", "CARD_TYPE.SKILL");
		case CardType.Power:
			return new LocString("gameplay_ui", "CARD_TYPE.POWER");
		case CardType.Status:
			return new LocString("gameplay_ui", "CARD_TYPE.STATUS");
		case CardType.Curse:
			return new LocString("gameplay_ui", "CARD_TYPE.CURSE");
		case CardType.Quest:
			return new LocString("gameplay_ui", "CARD_TYPE.QUEST");
		case CardType.None:
			return new LocString("gameplay_ui", "CARD_TYPE.NONE");
		default:
		{
			throw new System.Runtime.CompilerServices.SwitchExpressionException(cardType);
			LocString result = default(LocString);
			return result;
		}
		}
	}
}
