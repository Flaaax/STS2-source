using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace MegaCrit.Sts2.Core.Nodes.Cards;

/// <summary>
/// Shows a card's before and after for when they get enchanted.
/// </summary>
public partial class NEnchantPreview : Control
{
	private Control _before;

	private Control _after;

	private Control _arrows;

	public override void _Ready()
	{
		_before = GetNode<Control>("%Before");
		_after = GetNode<Control>("%After");
		_arrows = GetNode<Control>("Arrows");
	}

	public void Init(CardModel card, EnchantmentModel canonicalEnchantment, int amount)
	{
		canonicalEnchantment.AssertCanonical();
		RemoveExistingCards();
		NPreviewCardHolder nPreviewCardHolder = NPreviewCardHolder.Create(NCard.Create(card), showHoverTips: true, scaleOnHover: false);
		_before.AddChildSafely(nPreviewCardHolder);
		nPreviewCardHolder.CardNode.UpdateVisuals(card.Pile.Type, CardPreviewMode.Normal);
		CardModel cardModel = card.CardScope.CloneCard(card);
		EnchantmentModel enchantmentModel = canonicalEnchantment.ToMutable();
		cardModel.EnchantInternal(enchantmentModel, amount);
		cardModel.IsEnchantmentPreview = true;
		enchantmentModel.ModifyCard();
		NPreviewCardHolder nPreviewCardHolder2 = NPreviewCardHolder.Create(NCard.Create(cardModel), showHoverTips: true, scaleOnHover: false);
		_after.AddChildSafely(nPreviewCardHolder2);
		nPreviewCardHolder2.CardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
	}

	/// <summary>
	/// Helper function to remove any existing preview cards
	/// </summary>
	private void RemoveExistingCards()
	{
		foreach (Node child in _before.GetChildren())
		{
			child.QueueFreeSafely();
		}
		foreach (Node child2 in _after.GetChildren())
		{
			child2.QueueFreeSafely();
		}
	}
}
