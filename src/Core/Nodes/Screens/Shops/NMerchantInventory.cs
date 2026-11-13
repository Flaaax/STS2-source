using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Shops;

/// <summary>
/// Manages the shop items for a shop.
/// </summary>
public partial class NMerchantInventory : Control, IScreenContext
{
	[Signal]
	public delegate void InventoryClosedEventHandler();

	private const float _openPosition = 80f;

	private const float _closedPosition = -1000f;

	public const int blitzColumns = 3;

	private Control? _characterCardContainer;

	private Control? _colorlessCardContainer;

	protected Control? _relicContainer;

	private Control? _potionContainer;

	private Control? _blitzContainer;

	private MegaRichTextLabel? _blitzSelectionText;

	private NMerchantCardRemoval? _cardRemovalNode;

	private NBackButton _backButton;

	private NMerchantDialogue _merchantDialogue;

	private Tween? _inventoryTween;

	private Control _slotsContainer;

	private ColorRect _backstop;

	private Control _inputBlocker;

	private bool _isInputBlocked;

	private NMerchantSlot? _lastSlot;

	public MerchantInventory? Inventory { get; private set; }

	public bool IsOpen { get; private set; }

	public NMerchantHand MerchantHand { get; private set; }

	public Control? DefaultFocusedControl
	{
		get
		{
			if (_isInputBlocked)
			{
				return _inputBlocker;
			}
			NMerchantSlot lastSlot = _lastSlot;
			if (lastSlot != null)
			{
				MerchantEntry entry = lastSlot.Entry;
				if (entry != null && entry.IsStocked)
				{
					return _lastSlot;
				}
			}
			return GetAllSlots().FirstOrDefault((NMerchantSlot s) => s.Entry.IsStocked);
		}
	}

	public override void _Ready()
	{
		_merchantDialogue = GetNode<NMerchantDialogue>("%Dialogue");
		_merchantDialogue.Modulate = Colors.Transparent;
		_slotsContainer = GetNode<Control>("%SlotsContainer");
		_slotsContainer.Position = new Vector2(_slotsContainer.Position.X, -1000f);
		_backstop = GetNode<ColorRect>("Backstop");
		MerchantHand = GetNode<NMerchantHand>("%MerchantHand");
		_characterCardContainer = GetNodeOrNull<Control>("%CharacterCards");
		_colorlessCardContainer = GetNodeOrNull<Control>("%ColorlessCards");
		_relicContainer = GetNodeOrNull<Control>("%Relics");
		_potionContainer = GetNodeOrNull<Control>("%Potions");
		_blitzContainer = GetNodeOrNull<Control>("%BlitzSlots");
		_blitzSelectionText = GetNodeOrNull<MegaRichTextLabel>("%BlitzSelectionText");
		_cardRemovalNode = GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
		_backButton = GetNode<NBackButton>("%BackButton");
		_backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			Close();
		}));
		_backButton.Disable();
		_inputBlocker = GetNode<Control>("%InputBlocker");
		NGame.Instance.SetScreenShakeTarget(this);
	}

	public override void _EnterTree()
	{
		ActiveScreenContext.Instance.Updated += OnActiveScreenUpdated;
		SubscribeToEntries();
	}

	public override void _ExitTree()
	{
		ActiveScreenContext.Instance.Updated -= OnActiveScreenUpdated;
		if (Inventory == null)
		{
			return;
		}
		foreach (MerchantEntry allEntry in Inventory.AllEntries)
		{
			allEntry.PurchaseCompleted -= OnPurchaseCompleted;
			allEntry.PurchaseFailed -= _merchantDialogue.ShowForPurchaseAttempt;
		}
	}

	public void Initialize(MerchantInventory inventory, MerchantDialogueSet dialogue)
	{
		if (Inventory != null)
		{
			throw new InvalidOperationException("Merchant inventory already populated.");
		}
		Inventory = inventory;
		_characterCardContainer?.SetVisible(Inventory.CharacterCardEntries.Any());
		_colorlessCardContainer?.SetVisible(Inventory.ColorlessCardEntries.Any());
		_potionContainer?.SetVisible(Inventory.PotionEntries.Any());
		_relicContainer?.SetVisible(Inventory.RelicEntries.Any());
		_cardRemovalNode?.SetVisible(Inventory.CardRemovalEntry != null);
		_blitzContainer?.SetVisible(Inventory.BlitzEntries.Any());
		_blitzSelectionText?.SetVisible(Inventory.BlitzEntries.Any());
		for (int i = 0; i < Inventory.CharacterCardEntries.Count; i++)
		{
			NMerchantCard child = _characterCardContainer.GetChild<NMerchantCard>(i);
			child.Initialize(this);
			child.FillSlot(Inventory.CharacterCardEntries[i]);
		}
		for (int j = 0; j < Inventory.ColorlessCardEntries.Count; j++)
		{
			NMerchantCard child2 = _colorlessCardContainer.GetChild<NMerchantCard>(j);
			child2.Initialize(this);
			child2.FillSlot(Inventory.ColorlessCardEntries[j]);
		}
		for (int k = 0; k < Inventory.RelicEntries.Count; k++)
		{
			NMerchantRelic child3 = _relicContainer.GetChild<NMerchantRelic>(k);
			child3.Initialize(this);
			child3.FillSlot(Inventory.RelicEntries[k]);
		}
		for (int l = 0; l < Inventory.PotionEntries.Count; l++)
		{
			NMerchantPotion child4 = _potionContainer.GetChild<NMerchantPotion>(l);
			child4.Initialize(this);
			child4.FillSlot(Inventory.PotionEntries[l]);
		}
		if (Inventory.CardRemovalEntry != null)
		{
			_cardRemovalNode.Initialize(this);
			_cardRemovalNode.FillSlot(Inventory.CardRemovalEntry);
		}
		for (int m = 0; m < Inventory.BlitzEntries.Count; m++)
		{
			NBlitzMerchantSlot child5 = _blitzContainer.GetChild<NBlitzMerchantSlot>(m);
			child5.Initialize(this);
			child5.FillSlot(Inventory.BlitzEntries[m]);
		}
		UpdateBlitzSelectionText();
		SubscribeToEntries();
		UpdateNavigation();
		foreach (NMerchantSlot slot in GetAllSlots())
		{
			slot.Connect(Control.SignalName.FocusEntered, Callable.From(delegate
			{
				_lastSlot = slot;
			}));
		}
		_merchantDialogue.Initialize(dialogue);
	}

	public void Open()
	{
		if (!SaveManager.Instance.SeenFtue("merchant_ftue"))
		{
			SaveManager.Instance.MarkFtueAsComplete("merchant_ftue");
		}
		TaskHelper.RunSafely(DoOpenAnimation());
		base.MouseFilter = MouseFilterEnum.Stop;
		if (!_isInputBlocked)
		{
			_backButton.Enable();
		}
		foreach (NMerchantCard cardSlot in GetCardSlots())
		{
			cardSlot.OnInventoryOpened();
		}
		IsOpen = true;
		ActiveScreenContext.Instance.Update();
		TaskHelper.RunSafely(ShowOpenInventory());
	}

	public async Task ShowOpenInventory()
	{
		await Cmd.Wait(0.75f);
		SfxCmd.Play("event:/sfx/npcs/merchant/merchant_welcome");
		MerchantInventory inventory = Inventory;
		if (inventory != null)
		{
			IReadOnlyList<BlitzMerchantEntry> blitzEntries = inventory.BlitzEntries;
			if (blitzEntries != null && blitzEntries.Count > 0)
			{
				if (Inventory.BlitzChoicesRemaining <= 0)
				{
					_merchantDialogue.ShowForBlitzFinished();
				}
				return;
			}
		}
		_merchantDialogue.ShowOnInventoryOpen();
	}

	private void SubscribeToEntries()
	{
		if (!IsNodeReady() || Inventory == null)
		{
			return;
		}
		foreach (MerchantEntry allEntry in Inventory.AllEntries)
		{
			allEntry.PurchaseCompleted += OnPurchaseCompleted;
			allEntry.PurchaseFailed += _merchantDialogue.ShowForPurchaseAttempt;
		}
	}

	private async Task DoOpenAnimation()
	{
		_inventoryTween?.Kill();
		_inventoryTween = CreateTween().SetParallel();
		_inventoryTween.TweenProperty(_backstop, "modulate:a", 0.8f, 1.0).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine)
			.FromCurrent();
		_inventoryTween.TweenProperty(_slotsContainer, "position:y", 80f, 0.699999988079071).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint)
			.FromCurrent();
		await _inventoryTween.AwaitFinished(this);
	}

	public void Close()
	{
		MerchantHand.StopPointing(0f);
		_inventoryTween?.Kill();
		_inventoryTween = CreateTween().SetParallel();
		_inventoryTween.TweenProperty(_backstop, "modulate:a", 0f, 0.800000011920929).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine)
			.FromCurrent();
		_inventoryTween.TweenProperty(_slotsContainer, "position:y", -1000f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic)
			.FromCurrent();
		base.MouseFilter = MouseFilterEnum.Ignore;
		_backButton.Disable();
		_lastSlot = null;
		IsOpen = false;
		ActiveScreenContext.Instance.Update();
		EmitSignalInventoryClosed();
	}

	private void OnPurchaseCompleted(PurchaseStatus status, MerchantEntry entry)
	{
		UpdateNavigation();
		NMerchantSlot lastSlot = GetAllSlots().FirstOrDefault((NMerchantSlot s) => s.Entry == entry);
		if (lastSlot != null && !_isInputBlocked)
		{
			NMerchantSlot nMerchantSlot = (from s in GetAllSlots()
				where s.Visible && s.Entry.IsStocked && s != lastSlot
				select s).MinBy((NMerchantSlot s) => (s.GlobalPosition - lastSlot.GlobalPosition).Length());
			if (!ActiveScreenContext.Instance.IsCurrent(this))
			{
				_lastSlot = nMerchantSlot;
			}
			else if (nMerchantSlot != null)
			{
				nMerchantSlot.TryGrabFocus();
			}
			else
			{
				ActiveScreenContext.Instance.FocusOnDefaultControl();
			}
		}
		SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
		MerchantInventory inventory = Inventory;
		if (inventory != null)
		{
			IReadOnlyList<BlitzMerchantEntry> blitzEntries = inventory.BlitzEntries;
			if (blitzEntries != null && blitzEntries.Count > 0)
			{
				if (Inventory.BlitzChoicesRemaining <= 0)
				{
					_merchantDialogue.ShowForBlitzFinished();
				}
				UpdateBlitzSelectionText();
				return;
			}
		}
		_merchantDialogue.ShowForPurchaseAttempt(status);
	}

	private void UpdateBlitzSelectionText()
	{
		if (_blitzSelectionText == null)
		{
			return;
		}
		MerchantInventory inventory = Inventory;
		if (inventory == null)
		{
			return;
		}
		IReadOnlyList<BlitzMerchantEntry> blitzEntries = inventory.BlitzEntries;
		if (blitzEntries != null && blitzEntries.Count > 0)
		{
			LocString locString;
			if (Inventory.BlitzChoicesRemaining > 0)
			{
				locString = new LocString("merchant_room", "MERCHANT.blitzSelection.remaining");
				locString.Add("Amount", Inventory.BlitzChoicesRemaining);
			}
			else
			{
				locString = new LocString("merchant_room", "MERCHANT.blitzSelection.none");
			}
			_blitzSelectionText.SetTextAutoSize(locString.GetFormattedText());
		}
	}

	public void OnCardRemovalUsed()
	{
		_cardRemovalNode.OnCardRemovalUsed();
	}

	/// <summary>
	/// Returns the slots that hold an entry. Containers with no entries for this inventory are hidden in
	/// <see cref="M:MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory.Initialize(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory,MegaCrit.Sts2.Core.Entities.Merchant.MerchantDialogueSet)" /> and their slots are never filled, so they are skipped here.
	/// </summary>
	public IEnumerable<NMerchantSlot> GetAllSlots()
	{
		List<NMerchantSlot> list = new List<NMerchantSlot>();
		list.AddRange(GetCardSlots());
		Control relicContainer = _relicContainer;
		if (relicContainer != null && relicContainer.Visible)
		{
			list.AddRange(_relicContainer.GetChildren().OfType<NMerchantRelic>());
		}
		relicContainer = _potionContainer;
		if (relicContainer != null && relicContainer.Visible)
		{
			list.AddRange(_potionContainer.GetChildren().OfType<NMerchantPotion>());
		}
		relicContainer = _blitzContainer;
		if (relicContainer != null && relicContainer.Visible)
		{
			list.AddRange(_blitzContainer.GetChildren().OfType<NBlitzMerchantSlot>());
		}
		NMerchantCardRemoval cardRemovalNode = _cardRemovalNode;
		if (cardRemovalNode != null && cardRemovalNode.Visible)
		{
			list.Add(_cardRemovalNode);
		}
		return list;
	}

	private IEnumerable<NMerchantCard> GetCardSlots()
	{
		IEnumerable<Node>[] array = new IEnumerable<Node>[2];
		Control characterCardContainer = _characterCardContainer;
		array[0] = ((characterCardContainer != null && characterCardContainer.Visible) ? _characterCardContainer.GetChildren() : new Array<Node>());
		characterCardContainer = _colorlessCardContainer;
		array[1] = ((characterCardContainer != null && characterCardContainer.Visible) ? _colorlessCardContainer.GetChildren() : new Array<Node>());
		return array.SelectMany((IEnumerable<Node> n) => n).OfType<NMerchantCard>();
	}

	protected virtual void UpdateNavigation()
	{
		Control blitzContainer = _blitzContainer;
		if (blitzContainer != null && blitzContainer.Visible)
		{
			UpdateBlitzNavigation();
			return;
		}
		UpdateHorizontalNavigation();
		UpdateVerticalNavigation();
	}

	private void UpdateBlitzNavigation()
	{
		List<NMerchantSlot> list = (from c in _blitzContainer?.GetChildren().OfType<NMerchantSlot>()
			where c.Visible
			select c).ToList() ?? new List<NMerchantSlot>();
		if (list.Count == 0)
		{
			return;
		}
		List<List<NMerchantSlot>> list2 = (from r in list.Chunk(3)
			select r.ToList()).ToList();
		for (int num = 0; num < list2.Count; num++)
		{
			List<NMerchantSlot> list3 = list2[num];
			SetWrappedHorizontalNeighbors(list3);
			for (int num2 = 0; num2 < list3.Count; num2++)
			{
				NMerchantSlot nMerchantSlot = list3[num2];
				nMerchantSlot.FocusNeighborTop = GetStockedSlotInColumn(list2, num, num2, -1).GetPath();
				nMerchantSlot.FocusNeighborBottom = GetStockedSlotInColumn(list2, num, num2, 1).GetPath();
			}
		}
	}

	private static void SetWrappedHorizontalNeighbors(List<NMerchantSlot> row)
	{
		for (int i = 0; i < row.Count; i++)
		{
			row[i].FocusNeighborLeft = GetWrappedStockedSlotInRow(row, i, -1).GetPath();
			row[i].FocusNeighborRight = GetWrappedStockedSlotInRow(row, i, 1).GetPath();
		}
	}

	private static NMerchantSlot GetWrappedStockedSlotInRow(List<NMerchantSlot> row, int idx, int dir)
	{
		for (int i = 1; i < row.Count; i++)
		{
			NMerchantSlot nMerchantSlot = row[Mathf.PosMod(idx + dir * i, row.Count)];
			if (nMerchantSlot.Entry.IsStocked)
			{
				return nMerchantSlot;
			}
		}
		return row[idx];
	}

	private NMerchantSlot GetStockedSlotInColumn(List<List<NMerchantSlot>> rows, int rowIdx, int col, int dir)
	{
		for (int i = rowIdx + dir; i >= 0 && i < rows.Count; i += dir)
		{
			List<NMerchantSlot> row = rows[i];
			if (GetClosestStockedSlot(col, row) is NMerchantSlot result)
			{
				return result;
			}
		}
		return rows[rowIdx][col];
	}

	private void UpdateHorizontalNavigation()
	{
		List<NMerchantSlot> source = (from c in _characterCardContainer?.GetChildren().OfType<NMerchantSlot>()
			where c.Visible
			select c).ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> list = (from c in _colorlessCardContainer?.GetChildren().OfType<NMerchantSlot>()
			where c.Visible
			select c).ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> list2 = (from c in _relicContainer?.GetChildren().OfType<NMerchantSlot>()
			where c.Visible
			select c).ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> list3 = (from c in _potionContainer?.GetChildren().OfType<NMerchantSlot>()
			where c.Visible
			select c).ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> wrappedHorizontalNeighbors = source.ToList();
		IEnumerable<NMerchantSlot> first = list.Concat(list2);
		IEnumerable<NMerchantSlot> second;
		if (_cardRemovalNode == null)
		{
			IEnumerable<NMerchantSlot> enumerable = System.Array.Empty<NMerchantSlot>();
			second = enumerable;
		}
		else
		{
			IEnumerable<NMerchantSlot> enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(_cardRemovalNode);
			second = enumerable;
		}
		List<NMerchantSlot> wrappedHorizontalNeighbors2 = first.Concat(second).ToList();
		IEnumerable<NMerchantSlot> first2 = list.Concat(list3);
		IEnumerable<NMerchantSlot> second2;
		if (_cardRemovalNode == null)
		{
			IEnumerable<NMerchantSlot> enumerable = System.Array.Empty<NMerchantSlot>();
			second2 = enumerable;
		}
		else
		{
			IEnumerable<NMerchantSlot> enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(_cardRemovalNode);
			second2 = enumerable;
		}
		List<NMerchantSlot> wrappedHorizontalNeighbors3 = first2.Concat(second2).ToList();
		SetWrappedHorizontalNeighbors(wrappedHorizontalNeighbors);
		SetWrappedHorizontalNeighbors(wrappedHorizontalNeighbors3);
		SetWrappedHorizontalNeighbors(wrappedHorizontalNeighbors2);
		NMerchantSlot firstStockedSlot = GetFirstStockedSlot(GetRelicPotionColumnOrder(list2, list3, fromRight: false));
		NMerchantSlot firstStockedSlot2 = GetFirstStockedSlot(GetRelicPotionColumnOrder(list2, list3, fromRight: true));
		if (firstStockedSlot == null || firstStockedSlot2 == null)
		{
			return;
		}
		_cardRemovalNode?.SetFocusNeighbor(Side.Left, firstStockedSlot2.GetPath());
		list.LastOrDefault()?.SetFocusNeighbor(Side.Right, firstStockedSlot.GetPath());
		if (_cardRemovalNode == null)
		{
			list.FirstOrDefault((NMerchantSlot c) => c.Entry.IsStocked)?.SetFocusNeighbor(Side.Left, firstStockedSlot2.GetPath());
		}
	}

	/// <summary>
	/// Orders relics and potions column by column (relic above potion) starting from the left or right edge.
	/// </summary>
	private static IEnumerable<NMerchantSlot?> GetRelicPotionColumnOrder(List<NMerchantSlot> relics, List<NMerchantSlot> potions, bool fromRight)
	{
		int columns = Math.Max(relics.Count, potions.Count);
		for (int i = 0; i < columns; i++)
		{
			int col = (fromRight ? (columns - 1 - i) : i);
			yield return relics.ElementAtOrDefault(col);
			yield return potions.ElementAtOrDefault(col);
		}
	}

	/// <summary>
	/// Returns the first candidate that can take focus, skipping missing, hidden, unfilled, and sold out slots.
	/// </summary>
	private static NMerchantSlot? GetFirstStockedSlot(IEnumerable<NMerchantSlot?> candidates)
	{
		return candidates.FirstOrDefault((NMerchantSlot s) => s != null && s.Visible && (s.Entry?.IsStocked ?? false));
	}

	private void UpdateVerticalNavigation()
	{
		List<NMerchantSlot> source = _characterCardContainer?.GetChildren().OfType<NMerchantSlot>().ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> colorlessCards = _colorlessCardContainer?.GetChildren().OfType<NMerchantSlot>().ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> relics = _relicContainer?.GetChildren().OfType<NMerchantSlot>().ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> potions = _potionContainer?.GetChildren().OfType<NMerchantSlot>().ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> list = source.ToList();
		IEnumerable<NMerchantSlot> first = colorlessCards.Concat(relics);
		IEnumerable<NMerchantSlot> second;
		if (_cardRemovalNode == null)
		{
			IEnumerable<NMerchantSlot> enumerable = System.Array.Empty<NMerchantSlot>();
			second = enumerable;
		}
		else
		{
			IEnumerable<NMerchantSlot> enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(_cardRemovalNode);
			second = enumerable;
		}
		List<NMerchantSlot> list2 = first.Concat(second).ToList();
		IEnumerable<NMerchantSlot> first2 = colorlessCards.Concat(potions);
		IEnumerable<NMerchantSlot> second2;
		if (_cardRemovalNode == null)
		{
			IEnumerable<NMerchantSlot> enumerable = System.Array.Empty<NMerchantSlot>();
			second2 = enumerable;
		}
		else
		{
			IEnumerable<NMerchantSlot> enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(_cardRemovalNode);
			second2 = enumerable;
		}
		List<NMerchantSlot> list3 = first2.Concat(second2).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].FocusNeighborTop = list[i].GetPath();
			if (list2.Count > 0)
			{
				Control closestStockedSlot = GetClosestStockedSlot(i, list2);
				if (closestStockedSlot != null)
				{
					list[i].FocusNeighborBottom = closestStockedSlot.GetPath();
					continue;
				}
			}
			Control closestStockedSlot2 = GetClosestStockedSlot(i, list3);
			if (closestStockedSlot2 != null)
			{
				list[i].FocusNeighborBottom = closestStockedSlot2.GetPath();
			}
			else
			{
				list[i].FocusNeighborBottom = list[i].GetPath();
			}
		}
		NMerchantSlot nMerchantSlot = list.ElementAtOrDefault(2);
		if (nMerchantSlot != null)
		{
			NMerchantSlot firstStockedSlot = GetFirstStockedSlot(new global::_003C_003Ez__ReadOnlyArray<NMerchantSlot>(new NMerchantSlot[5]
			{
				Relic(0),
				Relic(1),
				Potion(0),
				Potion(1),
				Colorless(1)
			}));
			if (firstStockedSlot != null)
			{
				nMerchantSlot.FocusNeighborBottom = firstStockedSlot.GetPath();
			}
		}
		NMerchantSlot nMerchantSlot2 = list.ElementAtOrDefault(3);
		if (nMerchantSlot2 != null)
		{
			NMerchantSlot firstStockedSlot2 = GetFirstStockedSlot(new global::_003C_003Ez__ReadOnlyArray<NMerchantSlot>(new NMerchantSlot[9]
			{
				Relic(2),
				Relic(1),
				Potion(2),
				Potion(1),
				_cardRemovalNode,
				Relic(0),
				Potion(0),
				Colorless(1),
				Colorless(0)
			}));
			if (firstStockedSlot2 != null)
			{
				nMerchantSlot2.FocusNeighborBottom = firstStockedSlot2.GetPath();
			}
		}
		NMerchantSlot nMerchantSlot3 = list.ElementAtOrDefault(4);
		if (nMerchantSlot3 != null)
		{
			NMerchantSlot firstStockedSlot3 = GetFirstStockedSlot(new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(_cardRemovalNode));
			if (firstStockedSlot3 != null)
			{
				nMerchantSlot3.FocusNeighborBottom = firstStockedSlot3.GetPath();
			}
		}
		for (int j = 0; j < list3.Count; list3[j].FocusNeighborBottom = list3[j].GetPath(), j++)
		{
			if (list2.Count > 0)
			{
				Control closestStockedSlot3 = GetClosestStockedSlot(j, list2);
				if (closestStockedSlot3 != null)
				{
					list3[j].FocusNeighborTop = closestStockedSlot3.GetPath();
					continue;
				}
			}
			Control closestStockedSlot4 = GetClosestStockedSlot(j, list);
			if (closestStockedSlot4 != null)
			{
				list3[j].FocusNeighborTop = closestStockedSlot4.GetPath();
			}
			else
			{
				list3[j].FocusNeighborTop = list3[j].GetPath();
			}
		}
		for (int k = 0; k < list2.Count; k++)
		{
			if (list.Count > 0)
			{
				Control closestStockedSlot5 = GetClosestStockedSlot(k, list);
				if (closestStockedSlot5 != null)
				{
					list2[k].FocusNeighborTop = closestStockedSlot5.GetPath();
					goto IL_03fd;
				}
			}
			list2[k].FocusNeighborTop = list2[k].GetPath();
			goto IL_03fd;
			IL_03fd:
			if (list3.Count > 0)
			{
				Control closestStockedSlot6 = GetClosestStockedSlot(k, list3);
				if (closestStockedSlot6 != null)
				{
					list2[k].FocusNeighborBottom = closestStockedSlot6.GetPath();
					continue;
				}
			}
			list2[k].FocusNeighborBottom = list2[k].GetPath();
		}
		NMerchantSlot? Colorless(int index)
		{
			return colorlessCards.ElementAtOrDefault(index);
		}
		NMerchantSlot? Potion(int index)
		{
			return potions.ElementAtOrDefault(index);
		}
		NMerchantSlot? Relic(int index)
		{
			return relics.ElementAtOrDefault(index);
		}
	}

	public void BlockInput()
	{
		_isInputBlocked = true;
		_inputBlocker.MouseFilter = MouseFilterEnum.Stop;
		NHotkeyManager.Instance.AddBlockingScreen(_inputBlocker);
		if (_backButton.IsEnabled)
		{
			_backButton.Disable();
		}
	}

	public void UnblockInput()
	{
		_isInputBlocked = false;
		_inputBlocker.MouseFilter = MouseFilterEnum.Ignore;
		NHotkeyManager.Instance.RemoveBlockingScreen(_inputBlocker);
		if (!_backButton.IsEnabled)
		{
			_backButton.Enable();
		}
	}

	private Control? GetClosestStockedSlot(int idx, List<NMerchantSlot> row)
	{
		NMerchantSlot nMerchantSlot = row[Math.Min(idx, row.Count - 1)];
		if (nMerchantSlot.Entry.IsStocked)
		{
			return nMerchantSlot;
		}
		int num = row.IndexOf(nMerchantSlot);
		int num2 = num - 1;
		int num3 = num + 1;
		while (num2 >= 0 || num3 < row.Count)
		{
			if (num2 >= 0)
			{
				if (row[num2].Entry.IsStocked)
				{
					return row[num2];
				}
				num2--;
			}
			if (num3 < row.Count)
			{
				if (row[num3].Entry.IsStocked)
				{
					return row[num3];
				}
				num3++;
			}
		}
		return null;
	}

	private void OnActiveScreenUpdated()
	{
		this.UpdateControllerNavEnabled();
		if (ActiveScreenContext.Instance.IsCurrent(this))
		{
			if (_characterCardContainer != null && NControllerManager.Instance.IsUsingDirectionalNavigation && _inventoryTween != null && _inventoryTween.IsRunning())
			{
				float num = 80f - _slotsContainer.Position.Y;
				NMerchantCard child = _characterCardContainer.GetChild<NMerchantCard>(0);
				MerchantHand.PointAtTarget(child, child.MerchantHandOffset + Vector2.Down * num);
			}
			if (!_isInputBlocked)
			{
				_backButton.Enable();
			}
		}
		else
		{
			_backButton.Disable();
		}
	}
}
