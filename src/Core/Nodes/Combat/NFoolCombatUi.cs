using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.FoolArrows;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;

namespace MegaCrit.Sts2.Core.Nodes.Combat;

/// <summary>
/// Special UI specifically for the Fool that tracks Arrows and Symbols for the Fool.
/// </summary>
public partial class NFoolCombatUi : Control
{
	private Player? _player;

	private Control _symbolContainer;

	private Node2D _symbolPos0;

	private Node2D _symbolPos1;

	private Node2D _symbolPos2;

	private Node2D _symbolPos3;

	private Node2D _symbolPos4;

	private Control _arrowContainer;

	private static readonly Vector2 _symbolOffset = new Vector2(-42f, -42f);

	private bool _isListeningToCombatState;

	private List<CardTag> MelodyTags => new List<CardTag>
	{
		CardTag.Wah,
		CardTag.Cho,
		CardTag.Ki
	};

	public override void _Ready()
	{
		_symbolContainer = GetNode<Control>("%SymbolContainer");
		_symbolPos0 = GetNode<Node2D>("%Pos0");
		_symbolPos1 = GetNode<Node2D>("%Pos1");
		_symbolPos2 = GetNode<Node2D>("%Pos2");
		_symbolPos3 = GetNode<Node2D>("%Pos3");
		_symbolPos4 = GetNode<Node2D>("%Pos4");
		_arrowContainer = GetNode<Control>("%ArrowContainer");
		base.Visible = false;
	}

	public void Initialize(Player player)
	{
		_player = player;
		ConnectArrowsAndSymbolsChangedSignal();
		RefreshVisibility();
		UpdateArrows();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		ConnectArrowsAndSymbolsChangedSignal();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_player != null && _isListeningToCombatState)
		{
			_player.PlayerCombatState.ArrowsChanged -= UpdateArrows;
			_player.PlayerCombatState.ArrowsChanged -= RefreshVisibility;
			CombatManager.Instance.StateTracker.CombatStateChanged -= OnSymbolsChanged;
			CombatManager.Instance.StateTracker.CombatStateChanged -= RefreshVisibility;
			_isListeningToCombatState = false;
		}
	}

	private void ConnectArrowsAndSymbolsChangedSignal()
	{
		if (_player != null && !_isListeningToCombatState)
		{
			_player.PlayerCombatState.ArrowsChanged += UpdateArrows;
			_player.PlayerCombatState.ArrowsChanged += RefreshVisibility;
			CombatManager.Instance.StateTracker.CombatStateChanged += OnSymbolsChanged;
			CombatManager.Instance.StateTracker.CombatStateChanged += RefreshVisibility;
			_isListeningToCombatState = true;
		}
	}

	private void RefreshVisibility(CombatState? _)
	{
		RefreshVisibility();
	}

	private void RefreshVisibility()
	{
		if (!base.Visible)
		{
			if (_player == null)
			{
				base.Visible = false;
			}
			else if (_player.Character is Fool || _player.PlayerCombatState.Arrows.Count > 0 || GetSymbols().Count > 0)
			{
				base.Visible = true;
			}
		}
	}

	private void UpdateArrows()
	{
		IReadOnlyList<ArrowSlot> arrows = _player.PlayerCombatState.Arrows;
		HashSet<ArrowSlot> hashSet = new HashSet<ArrowSlot>();
		foreach (NFoolArrowUi item in _arrowContainer.GetChildren().OfType<NFoolArrowUi>())
		{
			if (arrows.Contains(item.ArrowSlot))
			{
				hashSet.Add(item.ArrowSlot);
				continue;
			}
			_arrowContainer.RemoveChildSafely(item);
			item.QueueFreeSafely();
		}
		for (int i = 0; i < arrows.Count; i++)
		{
			ArrowSlot arrowSlot = arrows[i];
			if (!hashSet.Contains(arrowSlot))
			{
				NFoolArrowUi nFoolArrowUi = NFoolArrowUi.Create(arrowSlot, i);
				_arrowContainer.AddChildSafely(nFoolArrowUi);
				nFoolArrowUi.SetVisible(arrowSlot.Status != ArrowStatus.Destroyed);
			}
		}
	}

	private void OnSymbolsChanged(CombatState? _)
	{
		if (_player != null)
		{
			SetMelodyAmounts();
		}
	}

	private void SetMelodyAmounts()
	{
		foreach (CardTag melodyTag in MelodyTags)
		{
			bool flag = MelodyCmd.PlayedThisTurn(_player, melodyTag);
			NFoolCombatSymbol nFoolCombatSymbol = HasMelody(melodyTag);
			if (nFoolCombatSymbol == null && flag)
			{
				nFoolCombatSymbol = NFoolCombatSymbol.Create(melodyTag);
				_symbolContainer.AddChildSafely(nFoolCombatSymbol);
				nFoolCombatSymbol.SetGlobalPosition(_symbolPos2.GlobalPosition + _symbolOffset);
			}
			else if (nFoolCombatSymbol != null && !flag)
			{
				nFoolCombatSymbol.SetVisible(visible: false);
				nFoolCombatSymbol.QueueFreeSafely();
			}
		}
		List<NFoolCombatSymbol> symbols = GetSymbols();
		switch (symbols.Count)
		{
		case 1:
			symbols[0].SetGlobalPosition(_symbolPos2.GlobalPosition + _symbolOffset);
			break;
		case 2:
			Place(symbols[0], _symbolPos1);
			Place(symbols[1], _symbolPos3);
			break;
		case 3:
			Place(symbols[0], _symbolPos0);
			Place(symbols[1], _symbolPos2);
			Place(symbols[2], _symbolPos4);
			break;
		}
	}

	/// <summary>
	/// Helper function for weirdo symbol placement UX.
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="slot"></param>
	private static void Place(NFoolCombatSymbol symbol, Node2D slot)
	{
		Vector2 targetPosition = slot.GlobalPosition + _symbolOffset;
		symbol.MoveTo(targetPosition);
	}

	private List<NFoolCombatSymbol> GetSymbols()
	{
		List<NFoolCombatSymbol> list = new List<NFoolCombatSymbol>();
		foreach (NFoolCombatSymbol item in from c in _symbolContainer.GetChildren().OfType<NFoolCombatSymbol>()
			where c.Visible
			select c)
		{
			list.Add(item);
		}
		list.Sort((NFoolCombatSymbol a, NFoolCombatSymbol b) => a.symbolType.CompareTo(b.symbolType));
		return list;
	}

	private NFoolCombatSymbol? HasMelody(CardTag symbolType)
	{
		foreach (NFoolCombatSymbol item in _symbolContainer.GetChildren().OfType<NFoolCombatSymbol>())
		{
			if (item.symbolType == symbolType)
			{
				return item;
			}
		}
		return null;
	}
}
