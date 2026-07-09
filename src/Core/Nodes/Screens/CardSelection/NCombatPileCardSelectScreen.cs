using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

public sealed partial class NCombatPileCardSelectScreen : NCardGridSelectionScreen
{
	private Control _bottomTextContainer;

	private MegaRichTextLabel _infoLabel;

	private NConfirmButton _confirmButton;

	private NCombatPilesContainer _combatPiles;

	private CancellationTokenSource _cts = new CancellationTokenSource();

	private HashSet<CardModel> _selectedCards = new HashSet<CardModel>();

	private CardSelectorPrefs _prefs;

	private CardPile _pile;

	private Func<CardModel, bool> _filter;

	private List<CardCreationResult>? _cardResults;

	private bool _isSubscribedToPile;

	private static string ScenePath => SceneHelper.GetScenePath("screens/card_selection/combat_pile_card_select_screen");

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	protected override IEnumerable<Control> PeekButtonTargets => new global::_003C_003Ez__ReadOnlySingleElementList<Control>(_bottomTextContainer);

	public static NCombatPileCardSelectScreen Create(CardPile pile, CardSelectorPrefs prefs, Func<CardModel, bool> filter)
	{
		NCombatPileCardSelectScreen nCombatPileCardSelectScreen = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NCombatPileCardSelectScreen>(PackedScene.GenEditState.Disabled);
		nCombatPileCardSelectScreen.Name = "NCombatPileCardSelectScreen";
		nCombatPileCardSelectScreen._cardResults = null;
		nCombatPileCardSelectScreen._prefs = prefs;
		nCombatPileCardSelectScreen._cards = Array.Empty<CardModel>();
		nCombatPileCardSelectScreen._pile = pile;
		nCombatPileCardSelectScreen._filter = filter;
		return nCombatPileCardSelectScreen;
	}

	public override void _Ready()
	{
		ConnectSignalsAndInitGrid();
		_confirmButton = GetNode<NConfirmButton>("%Confirm");
		_bottomTextContainer = GetNode<Control>("%BottomText");
		_infoLabel = _bottomTextContainer.GetNode<MegaRichTextLabel>("%BottomLabel");
		_infoLabel.Text = _prefs.Prompt.GetFormattedText();
		UpdatePileContents();
		if (_prefs.MinSelect == 0)
		{
			_confirmButton.Enable();
		}
		else
		{
			_confirmButton.Disable();
		}
		_confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			CompleteSelection();
		}));
	}

	public override void _EnterTree()
	{
		_cts = new CancellationTokenSource();
		_pile.ContentsChanged += UpdatePileContents;
		_isSubscribedToPile = true;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_cts.Cancel();
		UnsubscribeFromPile();
	}

	protected override void ConnectSignalsAndInitGrid()
	{
		base.ConnectSignalsAndInitGrid();
		_combatPiles = GetNode<NCombatPilesContainer>("%CombatPiles");
		if (CombatManager.Instance.IsInProgress)
		{
			_combatPiles.Initialize(_pile.Cards.First().Owner);
		}
		_combatPiles.Disable();
		_combatPiles.SetVisible(visible: false);
		_peekButton.Connect(NPeekButton.SignalName.Toggled, Callable.From<NPeekButton>(delegate
		{
			if (_peekButton.IsPeeking)
			{
				_combatPiles.Enable();
				_combatPiles.SetVisible(visible: true);
			}
			else
			{
				_combatPiles.Disable();
				_combatPiles.SetVisible(visible: false);
			}
		}));
	}

	public override void AfterOverlayOpened()
	{
		base.AfterOverlayOpened();
		TaskHelper.RunSafely(FlashRelicsOnModifiedCards());
	}

	private async Task FlashRelicsOnModifiedCards()
	{
		if (_cardResults == null)
		{
			return;
		}
		await this.AwaitProcessFrame(_cts.Token);
		foreach (CardCreationResult result in _cardResults)
		{
			NGridCardHolder nGridCardHolder = _grid.CurrentlyDisplayedCardHolders.FirstOrDefault((NGridCardHolder h) => h.CardModel == result.Card);
			if (nGridCardHolder == null || !result.HasBeenModified)
			{
				continue;
			}
			foreach (RelicModel modifyingRelic in result.ModifyingRelics)
			{
				modifyingRelic.Flash();
				nGridCardHolder.CardNode?.FlashRelicOnCard(modifyingRelic);
			}
		}
	}

	protected override void OnCardClicked(CardModel card)
	{
		if (_selectedCards.Contains(card))
		{
			_grid.UnhighlightCard(card);
			_selectedCards.Remove(card);
		}
		else
		{
			if (_selectedCards.Count < _prefs.MaxSelect)
			{
				_grid.HighlightCard(card);
				_selectedCards.Add(card);
			}
			if (!_prefs.RequireManualConfirmation)
			{
				CheckIfSelectionComplete();
			}
		}
		UpdateConfirmButton();
	}

	private void UpdateConfirmButton()
	{
		int num = Mathf.Min(_prefs.MinSelect, _grid.CurrentlyDisplayedCards.Count());
		if (_selectedCards.Count >= num && _prefs.RequireManualConfirmation)
		{
			_confirmButton.Enable();
		}
		else
		{
			_confirmButton.Disable();
		}
	}

	private void CheckIfSelectionComplete()
	{
		int num = Mathf.Min(_prefs.MaxSelect, _grid.CurrentlyDisplayedCards.Count());
		if (_selectedCards.Count >= num)
		{
			CompleteSelection();
		}
	}

	private void CompleteSelection()
	{
		UnsubscribeFromPile();
		_completionSource.SetResult(_selectedCards);
		NOverlayStack.Instance.Remove(this);
	}

	private void UnsubscribeFromPile()
	{
		if (_isSubscribedToPile)
		{
			_pile.ContentsChanged -= UpdatePileContents;
			_isSubscribedToPile = false;
		}
	}

	private void UpdatePileContents()
	{
		List<CardModel> validPileCards = _pile.Cards.Where(_filter).ToList();
		_selectedCards = _selectedCards.Where((CardModel c) => validPileCards.Contains(c)).ToHashSet();
		if (validPileCards.Count == 0)
		{
			_selectedCards.Clear();
			CompleteSelection();
			return;
		}
		if (validPileCards.Count == _selectedCards.Count)
		{
			CompleteSelection();
			return;
		}
		List<SortingOrders> list;
		if (_pile.Type != PileType.Draw)
		{
			int num = 1;
			list = new List<SortingOrders>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<SortingOrders> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = SortingOrders.Ascending;
		}
		else
		{
			int index = 2;
			list = new List<SortingOrders>(index);
			CollectionsMarshal.SetCount(list, index);
			Span<SortingOrders> span = CollectionsMarshal.AsSpan(list);
			int num = 0;
			span[num] = SortingOrders.RarityAscending;
			num++;
			span[num] = SortingOrders.AlphabetAscending;
		}
		List<SortingOrders> sortingPriority = list;
		_grid.SetCards(validPileCards, _pile.Type, sortingPriority);
		UpdateConfirmButton();
		foreach (CardModel selectedCard in _selectedCards)
		{
			_grid.HighlightCard(selectedCard);
		}
	}
}
