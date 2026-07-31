using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

/// <summary>
/// Screen that has a list of monsters that you can click on to view their name, description, hp, some stats, and
/// a list of their moves which you can click on to play the associated animation and sfx.
/// </summary>
public partial class NBestiary : NSubmenu
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary");

	private SerializableProgress _progress;

	private MegaRichTextLabel _monsterNameLabel;

	private MegaLabel _epithet;

	private NScrollableContainer _sidebar;

	private VBoxContainer _bestiaryList;

	private static readonly LocString _locked = new LocString("bestiary", "LOCKED.monsterTitle");

	private Control _selectionArrow;

	private Tween? _arrowTween;

	private static readonly Vector2 _arrowOffset = new Vector2(-42f, 119f);

	private bool _initSelectionArrow = true;

	private Control _layoutContainer;

	private NBestiaryLayout? _currentLayout;

	private Control _characterIcon;

	private TextureRect _iconTexture;

	private TextureRect _iconOutlineTexture;

	private Control _dialogueLine;

	private MegaRichTextLabel _dialogueLabel;

	private Control _dialogueBubble;

	private TextureRect _dialogueTail;

	private TextureRect _dialogueTailShadow;

	private const string _dialogueTailPath = "res://images/ui/dialogue_tail.png";

	private const string _thoughtTailPath = "res://images/ui/thought_tail.png";

	private NBestiaryModeButton _modeButton;

	private bool _isStatsMode;

	private NHotkeyIcon _pageLeftIcon;

	private NHotkeyIcon _pageRightIcon;

	private static readonly StringName _filterLeftHotkey = MegaInput.viewDeckAndTabLeft;

	private static readonly StringName _filterRightHotkey = MegaInput.viewExhaustPileAndTabRight;

	private Control _moveList;

	private Control _moveContainer;

	private Control _statsContainer;

	private Control _filterContainer;

	private MegaRichTextLabel _statsLabel;

	private NBestiaryCharacterFilter _currentFilter;

	private HashSet<ModelId> _discoveredMonsterIds;

	private HashSet<ModelId> _discoveredEncounterIds;

	private NBestiaryEntry? _selectedEntry;

	private Control? _previousScreenshakeTarget;

	private Tween? _tween;

	private Tween? _dialogueTween;

	public static NBestiary? Instance { get; private set; }

	public static string[] AssetPaths
	{
		get
		{
			List<string> list = new List<string>();
			list.Add(_scenePath);
			list.AddRange(NBestiaryEntry.AssetPaths);
			return list.ToArray();
		}
	}

	protected override Control? InitialFocusedControl => _bestiaryList.GetChildren().OfType<NBestiaryEntry>().FirstOrDefault();

	public Control BackVfxContainer { get; private set; }

	public Control VfxContainer { get; private set; }

	public Control? Layout => _currentLayout;

	public static NBestiary? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBestiary>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		ConnectSignals();
		GetNode<MegaLabel>("%MoveHeader").SetTextAutoSize(new LocString("bestiary", "ACTIONS.header").GetFormattedText());
		GetNode<MegaRichTextLabel>("%ConstructionLabel").SetTextAutoSize(new LocString("bestiary", "UNDER_CONSTRUCTION").GetRawText());
		_sidebar = GetNode<NScrollableContainer>("%Sidebar");
		_bestiaryList = GetNode<VBoxContainer>("%BestiaryList");
		_monsterNameLabel = GetNode<MegaRichTextLabel>("%MonsterName");
		_layoutContainer = GetNode<Control>("%LayoutContainer");
		_epithet = GetNode<MegaLabel>("%Epithet");
		_characterIcon = GetNode<Control>("%CharacterIcon");
		_iconTexture = GetNode<TextureRect>("%Icon");
		_iconOutlineTexture = GetNode<TextureRect>("%Outline");
		_dialogueLine = GetNode<Control>("%DialogueLine");
		_dialogueLabel = GetNode<MegaRichTextLabel>("%DialogueText");
		_dialogueBubble = GetNode<Control>("%Bubble");
		_dialogueTail = GetNode<TextureRect>("%DialogueTail");
		_dialogueTailShadow = GetNode<TextureRect>("%DialogueTailShadow");
		_modeButton = GetNode<NBestiaryModeButton>("%ModeButton");
		_modeButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(ToggleMode));
		_pageLeftIcon = GetNode<NHotkeyIcon>("%PageLeftIcon");
		_pageRightIcon = GetNode<NHotkeyIcon>("%PageRightIcon");
		NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdatePageIcons));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdatePageIcons));
		NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdatePageIcons));
		_moveContainer = GetNode<Control>("%MoveContainer");
		_statsContainer = GetNode<Control>("%StatsContainer");
		_selectionArrow = GetNode<Control>("%SelectionArrow");
		_moveList = GetNode<Control>("%MoveList");
		_filterContainer = GetNode<Control>("%PoolFilters");
		_statsLabel = GetNode<MegaRichTextLabel>("%StatisticsFull");
		VfxContainer = GetNode<Control>("%VfxContainer");
		BackVfxContainer = GetNode<Control>("%BackVfxContainer");
	}

	private void ToggleMode(NButton _)
	{
		_isStatsMode = !_isStatsMode;
		if (_isStatsMode)
		{
			_modeButton.SetLabel(new LocString("bestiary", "MODE.viewActions").GetRawText());
			ShowStatsPanel();
			DisplayCharacterData();
			EnableStatsModeHotkeys();
			DisableMoveButtonHotkeys();
		}
		else
		{
			_modeButton.SetLabel(new LocString("bestiary", "MODE.viewStats").GetRawText());
			ShowMovesPanel();
			HideDialogue();
			DisableStatsModeHotkeys();
			EnableMoveButtonHotkeys();
		}
		UpdatePageIcons();
	}

	private void EnableMoveButtonHotkeys()
	{
		foreach (Node child in _moveList.GetChildren())
		{
			((NBestiaryMoveButton)child).Enable();
		}
	}

	/// <summary>
	/// We need to disable these hotkeys when we go to Stats mode because otherwise
	/// paginating through the characters will make the monster perform actions.
	/// </summary>
	private void DisableMoveButtonHotkeys()
	{
		foreach (Node child in _moveList.GetChildren())
		{
			((NBestiaryMoveButton)child).Disable();
		}
	}

	private void ShowMovesPanel()
	{
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_statsContainer.Visible = false;
		_moveContainer.Visible = true;
		Control moveContainer = _moveContainer;
		Color modulate = base.Modulate;
		modulate.A = 0f;
		moveContainer.Modulate = modulate;
		_tween.TweenProperty(_moveContainer, "position:x", 242f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
			.From(210f);
		_tween.TweenProperty(_moveContainer, "modulate:a", 1f, 0.5);
	}

	private void ShowStatsPanel()
	{
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_moveContainer.Visible = false;
		_statsContainer.Visible = true;
		Control statsContainer = _statsContainer;
		Color modulate = base.Modulate;
		modulate.A = 0f;
		statsContainer.Modulate = modulate;
		_tween.TweenProperty(_statsContainer, "position:x", 242f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
			.From(210f);
		_tween.TweenProperty(_statsContainer, "modulate:a", 1f, 0.5);
	}

	private void RefreshStatisticsText()
	{
		if (_selectedEntry == null)
		{
			Log.Error("How did this happen?");
			return;
		}
		BestiaryEntry entry = _selectedEntry.Entry;
		EnemyStats enemyStats = null;
		EncounterStats encounterStats = null;
		if (entry.monsterModel != null)
		{
			enemyStats = _progress.EnemyStats.FirstOrDefault((EnemyStats e) => e.Id == entry.monsterModel.Id);
		}
		else
		{
			encounterStats = _progress.EncounterStats.FirstOrDefault((EncounterStats e) => e.Id == entry.encounterModel.Id);
		}
		foreach (Node child in _filterContainer.GetChildren())
		{
			NBestiaryCharacterFilter filter = child as NBestiaryCharacterFilter;
			if (filter == null)
			{
				continue;
			}
			if (enemyStats != null)
			{
				if (filter.character != null)
				{
					FightStats fightStats = enemyStats.FightStats.FirstOrDefault((FightStats f) => f.Character == filter.character.Id);
					filter.kills = fightStats?.Wins ?? 0;
					filter.deaths = fightStats?.Losses ?? 0;
				}
				else
				{
					filter.kills = enemyStats.TotalWins;
					filter.deaths = enemyStats.TotalLosses;
				}
			}
			else if (encounterStats != null)
			{
				if (filter.character != null)
				{
					FightStats fightStats2 = encounterStats.FightStats.FirstOrDefault((FightStats f) => f.Character == filter.character.Id);
					filter.kills = fightStats2?.Wins ?? 0;
					filter.deaths = fightStats2?.Losses ?? 0;
				}
				else
				{
					filter.kills = encounterStats.TotalWins;
					filter.deaths = encounterStats.TotalLosses;
				}
			}
			filter.IsLocked = filter.kills + filter.deaths <= 0;
		}
	}

	private void DisplayCharacterData()
	{
		if (_selectedEntry == null)
		{
			Log.Error("How did this happen?");
			return;
		}
		BestiaryEntry entry = _selectedEntry.Entry;
		LocString locString = new LocString("bestiary", "STATS.layout");
		if (_currentFilter.Total == 0)
		{
			locString.Add("total", 0m);
			locString.Add("kills", 0m);
			locString.Add("deaths", 0m);
			locString.Add("winrate", "--");
		}
		else
		{
			locString.Add("total", _currentFilter.Total);
			locString.Add("kills", _currentFilter.kills);
			locString.Add("deaths", _currentFilter.deaths);
			locString.Add("winrate", _currentFilter.WinRate);
		}
		_statsLabel.SetTextAutoSize(locString.GetFormattedText());
		if (_currentFilter.kills <= 0)
		{
			_dialogueLabel.SetTextAutoSize(_currentFilter.BestiarySeenQuote);
		}
		else
		{
			LocString bestiaryKillQuote = _currentFilter.BestiaryKillQuote;
			if (bestiaryKillQuote == null)
			{
				_dialogueLabel.SetTextAutoSize(new LocString("bestiary", "QUOTE_PLACEHOLDER").GetFormattedText());
			}
			else
			{
				_dialogueLabel.SetTextAutoSize(bestiaryKillQuote.GetFormattedText());
			}
		}
		if (_currentFilter.character == null)
		{
			HideDialogue();
		}
		else
		{
			ShowDialogue();
		}
		UpdateDialogueBubbleStyle();
	}

	private void UpdateDialogueBubbleStyle()
	{
		CharacterModel character = _currentFilter.character;
		Color selfModulate = character?.DialogueColor ?? StsColors.transparentWhite;
		_characterIcon.Modulate = ((character == null) ? StsColors.transparentWhite : Colors.White);
		if (character != null)
		{
			_iconTexture.Texture = character.IconTexture;
			_iconOutlineTexture.Texture = character.IconOutlineTexture;
		}
		_dialogueBubble.SelfModulate = selfModulate;
		_dialogueTail.SelfModulate = selfModulate;
		string path = ((character is Silent) ? "res://images/ui/thought_tail.png" : "res://images/ui/dialogue_tail.png");
		_dialogueTail.Texture = PreloadManager.Cache.GetCompressedTexture2D(path);
	}

	private void ShowDialogue()
	{
		_dialogueTween?.Kill();
		_dialogueTween = CreateTween().SetParallel();
		_dialogueLine.Modulate = StsColors.transparentWhite;
		_dialogueTween.TweenProperty(_dialogueLine, "modulate", Colors.White, 0.1).SetDelay(0.1);
		_dialogueTween.TweenProperty(_dialogueLine, "position:x", 560f, 0.4).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
			.From(528f)
			.SetDelay(0.1);
	}

	private void HideDialogue()
	{
		_dialogueTween?.Kill();
		_dialogueTween = CreateTween();
		_dialogueTween.TweenProperty(_dialogueLine, "modulate", StsColors.transparentWhite, 0.1);
	}

	/// <summary>
	/// On screen open. When the player opens the Bestiary.
	/// </summary>
	public override void OnSubmenuOpened()
	{
		Instance = this;
		_progress = SaveManager.Instance.Progress.ToSerializable();
		_previousScreenshakeTarget = NGame.Instance?.ScreenshakeTarget;
		_isStatsMode = !SaveManager.Instance.PrefsSave.IsBestiaryActionsPreferred;
		if (_isStatsMode)
		{
			_modeButton.SetLabel(new LocString("bestiary", "MODE.viewActions").GetRawText());
		}
		else
		{
			_modeButton.SetLabel(new LocString("bestiary", "MODE.viewStats").GetRawText());
		}
		CreateFilters();
		CreateEntries();
		if (_isStatsMode)
		{
			DisplayCharacterData();
			EnableStatsModeHotkeys();
			DisableMoveButtonHotkeys();
		}
		UpdatePageIcons();
	}

	/// <summary>
	/// Called when the Bestiary is closed (Back button)
	/// </summary>
	public override void OnSubmenuClosed()
	{
		DisableStatsModeHotkeys();
		DisableMoveButtonHotkeys();
		_initSelectionArrow = true;
		_selectedEntry = null;
		Instance = null;
		SaveManager.Instance.PrefsSave.IsBestiaryActionsPreferred = !_isStatsMode;
		SaveManager.Instance.SavePrefsFile();
		_currentLayout?.Cleanup();
		if (_previousScreenshakeTarget != null)
		{
			if (_previousScreenshakeTarget.IsValid())
			{
				NGame.Instance?.SetScreenShakeTarget(_previousScreenshakeTarget);
			}
			else
			{
				Log.Warn("The screenshake target is no longer valid. This should never happen.");
				_previousScreenshakeTarget = null;
			}
		}
		else
		{
			NGame.Instance?.ClearScreenShakeTarget();
		}
		_bestiaryList.FreeChildren();
		_lastFocusedControl = null;
	}

	/// <summary>
	/// Initializes the list of monsters based on your save file.
	/// </summary>
	private void CreateEntries()
	{
		_discoveredMonsterIds = (from e in SaveManager.Instance.Progress.EnemyStats.Values
			where e.TotalWins > 0
			select e.Id).ToHashSet();
		_discoveredEncounterIds = (from e in SaveManager.Instance.Progress.EncounterStats.Values
			where e.TotalWins > 0
			select e.Id).ToHashSet();
		foreach (ActModel act in ModelDb.Acts)
		{
			AddAct(act);
		}
		AddEvents();
		Control node = _sidebar.GetNode<Control>("Content");
		Vector2 position = node.Position;
		position.Y = 0f;
		node.Position = position;
		_sidebar.InstantlyScrollToTop();
		NBestiaryEntry nBestiaryEntry = _bestiaryList.GetChildren().OfType<NBestiaryEntry>().FirstOrDefault((NBestiaryEntry e) => e.IsDiscovered && e.IsEnabled);
		if (nBestiaryEntry == null)
		{
			Log.Error("Should not be possible as the Compendium + Bestiary isn't unlocked by default!");
		}
		else
		{
			SelectMonster(nBestiaryEntry);
		}
	}

	private void CreateFilters()
	{
		_filterContainer.FreeChildren();
		AddFilter(null);
		AddFilter(ModelDb.Character<Ironclad>());
		AddFilter(ModelDb.Character<Silent>());
		AddFilter(ModelDb.Character<Regent>());
		AddFilter(ModelDb.Character<Necrobinder>());
		AddFilter(ModelDb.Character<Defect>());
	}

	private void AddFilter(CharacterModel? character)
	{
		NBestiaryCharacterFilter nBestiaryCharacterFilter = NBestiaryCharacterFilter.Create(character);
		nBestiaryCharacterFilter.Connect(NBestiaryCharacterFilter.SignalName.Toggled, Callable.From<NBestiaryCharacterFilter>(OnCharacterFilterSelected));
		_filterContainer.AddChildSafely(nBestiaryCharacterFilter);
		if (character == null)
		{
			_currentFilter = nBestiaryCharacterFilter;
			_currentFilter.IsSelected = true;
		}
	}

	private void OnCharacterFilterSelected(NBestiaryCharacterFilter selectedFilter)
	{
		_currentFilter = selectedFilter;
		foreach (Node child in _filterContainer.GetChildren())
		{
			if (!child.Equals(selectedFilter))
			{
				((NBestiaryCharacterFilter)child).Deselect();
			}
		}
		if (_isStatsMode)
		{
			DisplayCharacterData();
		}
	}

	private void AddAct(ActModel act)
	{
		if (!SaveManager.Instance.Progress.DiscoveredActs.Contains(act.Id))
		{
			return;
		}
		_bestiaryList.AddChildSafely(NBestiaryLabelDivider.Create(act));
		HashSet<ModelId> hashSet = new HashSet<ModelId>();
		List<BestiaryEntry> list = new List<BestiaryEntry>();
		foreach (EncounterModel allEncounter in act.AllEncounters)
		{
			foreach (MonsterModel allPossibleMonster in allEncounter.AllPossibleMonsters)
			{
				if (hashSet.Add(allPossibleMonster.Id) && allPossibleMonster.ShouldShowInCompendium)
				{
					list.Add(BestiaryEntry.FromMonster(allPossibleMonster, allEncounter, allEncounter.RoomType));
				}
			}
		}
		if (act is Hive)
		{
			list.Add(BestiaryEntry.FromEncounter(ModelDb.Encounter<DecimillipedeElite>(), RoomType.Elite));
		}
		AddEntries(list);
	}

	private void AddEvents()
	{
		_bestiaryList.AddChildSafely(NBestiaryLabelDivider.Create(new LocString("bestiary", "EVENTS.title")));
		HashSet<ModelId> hashSet = new HashSet<ModelId>();
		List<BestiaryEntry> list = new List<BestiaryEntry>();
		foreach (EncounterModel eventEncounter in ModelDb.EventEncounters)
		{
			foreach (MonsterModel allPossibleMonster in eventEncounter.AllPossibleMonsters)
			{
				if (hashSet.Add(allPossibleMonster.Id) && allPossibleMonster.ShouldShowInCompendium)
				{
					list.Add(BestiaryEntry.FromMonster(allPossibleMonster, eventEncounter, eventEncounter.RoomType));
				}
			}
		}
		AddEntries(list);
	}

	private void AddEntries(List<BestiaryEntry> entries)
	{
		entries.Sort(delegate(BestiaryEntry e1, BestiaryEntry e2)
		{
			if (e1.roomType != e2.roomType)
			{
				return e1.roomType.CompareTo(e2.roomType);
			}
			if (e1.roomType == RoomType.Boss)
			{
				int num = string.Compare(e1.GetEncounterTitle(), e2.GetEncounterTitle(), StringComparison.CurrentCulture);
				if (num != 0)
				{
					return num;
				}
			}
			return string.Compare(e1.GetEntryTitle(), e2.GetEntryTitle(), StringComparison.CurrentCulture);
		});
		foreach (BestiaryEntry entry in entries)
		{
			NBestiaryEntry nBestiaryEntry = NBestiaryEntry.Create(entry, entry.IsDiscovered(_discoveredMonsterIds, _discoveredEncounterIds));
			_bestiaryList.AddChildSafely(nBestiaryEntry);
			nBestiaryEntry.Connect(NClickableControl.SignalName.Released, Callable.From<NBestiaryEntry>(OnMonsterClicked));
		}
	}

	/// <summary>
	/// A player clicked on a monster in the list on the right.
	/// </summary>
	private void OnMonsterClicked(NBestiaryEntry entry)
	{
		SelectMonster(entry);
	}

	/// <summary>
	/// Loads a specific monster's bestiary entry.
	/// </summary>
	private void SelectMonster(NBestiaryEntry entry)
	{
		if (entry == _selectedEntry)
		{
			return;
		}
		_moveList.FreeChildren();
		_selectedEntry = entry;
		if (entry.IsUnderConstruction)
		{
			_monsterNameLabel.Text = entry.Entry.GetEntryTitle();
			_currentLayout?.Cleanup();
			_currentLayout?.QueueFreeSafely();
		}
		else if (!entry.IsDiscovered)
		{
			_monsterNameLabel.Text = _locked.GetFormattedText();
			_currentLayout?.Cleanup();
			_currentLayout?.QueueFreeSafely();
		}
		else
		{
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_monsterNameLabel.Text = entry.Entry.GetEntryTitle();
			_monsterNameLabel.SelfModulate = StsColors.transparentWhite;
			_epithet.Modulate = StsColors.transparentWhite;
			_tween.TweenProperty(_monsterNameLabel, "position:y", 88f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
				.From(24f);
			_tween.TweenProperty(_monsterNameLabel, "self_modulate:a", 1f, 0.5);
			_tween.TweenProperty(_epithet, "modulate:a", 1f, 0.5).SetDelay(0.2);
			_tween.TweenProperty(_dialogueLabel, "position:y", 894f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
				.From(958f);
			_tween.TweenProperty(_dialogueLabel, "modulate:a", 1f, 0.5);
			if (_isStatsMode)
			{
				ShowStatsPanel();
			}
			else
			{
				ShowMovesPanel();
			}
			RefreshStatisticsText();
			_currentLayout?.Cleanup();
			if (!entry.Entry.CanReuseLayout(_currentLayout))
			{
				_currentLayout?.QueueFreeSafely();
				_currentLayout = entry.Entry.CreateLayoutNode(this);
				_layoutContainer.AddChildSafely(_currentLayout);
				NGame.Instance?.SetScreenShakeTarget(_currentLayout);
			}
			List<BestiaryMonsterMove> list = _currentLayout.Setup(entry.Entry, _tween);
			for (int i = 0; i < list.Count; i++)
			{
				if (i >= 9)
				{
					Log.Error("Hotkeys for monster Actions beyond 9 are not supported!");
				}
				NBestiaryMoveButton nBestiaryMoveButton = CreateBestiaryMoveButton(list[i], i + 1);
				_moveList.AddChildSafely(nBestiaryMoveButton);
				nBestiaryMoveButton.Connect(NClickableControl.SignalName.Released, Callable.From<NBestiaryMoveButton>(OnMoveButtonClicked));
			}
			if (_isStatsMode)
			{
				DisableMoveButtonHotkeys();
				if (_currentFilter.IsLocked)
				{
					NBestiaryCharacterFilter child = _filterContainer.GetChild<NBestiaryCharacterFilter>(0);
					child.IsSelected = true;
					OnCharacterFilterSelected(child);
				}
				DisplayCharacterData();
			}
		}
		if (_initSelectionArrow)
		{
			Control selectionArrow = _selectionArrow;
			Color modulate = _selectionArrow.Modulate;
			modulate.A = 0f;
			selectionArrow.Modulate = modulate;
			_initSelectionArrow = false;
			TaskHelper.RunSafely(InitializeSelectorArrow(entry));
		}
		else
		{
			Control selectionArrow2 = _selectionArrow;
			Color modulate = _selectionArrow.Modulate;
			modulate.A = 1f;
			selectionArrow2.Modulate = modulate;
			_arrowTween?.Kill();
			_arrowTween = CreateTween().SetParallel();
			_arrowTween.TweenProperty(_selectionArrow, "position", entry.Position + _arrowOffset, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		}
	}

	private NBestiaryMoveButton CreateBestiaryMoveButton(BestiaryMonsterMove move, int moveIndex)
	{
		if (NControllerManager.Instance?.IsUsingDirectionalNavigation ?? false)
		{
			return moveIndex switch
			{
				1 => NBestiaryMoveButton.Create(move, MegaInput.viewDeckAndTabLeft), 
				2 => NBestiaryMoveButton.Create(move, MegaInput.viewExhaustPileAndTabRight), 
				3 => NBestiaryMoveButton.Create(move, MegaInput.viewDiscardPile), 
				4 => NBestiaryMoveButton.Create(move, MegaInput.viewDrawPile), 
				5 => NBestiaryMoveButton.Create(move, MegaInput.topPanel), 
				6 => NBestiaryMoveButton.Create(move, MegaInput.altUp), 
				7 => NBestiaryMoveButton.Create(move, MegaInput.altDown), 
				8 => NBestiaryMoveButton.Create(move, MegaInput.altLeft), 
				9 => NBestiaryMoveButton.Create(move, MegaInput.altRight), 
				_ => NBestiaryMoveButton.Create(move, $"{moveIndex}"), 
			};
		}
		return NBestiaryMoveButton.Create(move, $"mega_select_card_{moveIndex}");
	}

	private async Task InitializeSelectorArrow(NBestiaryEntry entry)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		_selectionArrow.Position = entry.Position + _arrowOffset;
		_arrowTween?.Kill();
		_arrowTween = CreateTween().SetParallel();
		_arrowTween.TweenProperty(_selectionArrow, "modulate:a", 1f, 0.2);
	}

	private void OnMoveButtonClicked(NButton button)
	{
		NBestiaryMoveButton nBestiaryMoveButton = (NBestiaryMoveButton)button;
		PlayMoveAnim(_currentLayout?.GetCreatures() ?? Array.Empty<NCreature>(), nBestiaryMoveButton.Move);
	}

	private static void PlayMoveAnim(IEnumerable<NCreature> creatures, BestiaryMonsterMove move)
	{
		foreach (NCreature creature in creatures)
		{
			if (move.stateId != null)
			{
				MonsterModel monster = creature.Entity.Monster;
				if (monster == null)
				{
					throw new InvalidOperationException($"Non-monster creature {creature} is in the bestiary!");
				}
				monster.SetMoveImmediate((MoveState)monster.MoveStateMachine.States[move.stateId], forceTransition: true);
				TaskHelper.RunSafely(monster.PerformMove());
			}
			else if (move.nonStateMove != null)
			{
				TaskHelper.RunSafely(move.nonStateMove(Array.Empty<Creature>()));
			}
			else if (move.action != null)
			{
				TaskHelper.RunSafely(move.action());
			}
			else if (move.animId != null)
			{
				creature.Visuals.SpineBody?.GetAnimationState().SetAnimation(move.animId, loop: false);
				if (move.animId != "die")
				{
					creature.Visuals.SpineBody?.GetAnimationState().AddAnimation("idle_loop");
				}
				if (move.sfx != null)
				{
					NAudioManager.Instance.PlayOneShot(move.sfx);
				}
			}
			if (move.stopSfxLoops)
			{
				creature.StopAllSfxLoops();
			}
		}
	}

	public NCreature? GetCreatureNode(Creature? creature)
	{
		foreach (NCreature item in _currentLayout?.GetCreatures() ?? Array.Empty<NCreature>())
		{
			if (item.Entity == creature)
			{
				return item;
			}
		}
		return null;
	}

	public Vector2 GetSideCenter()
	{
		if (_currentLayout == null)
		{
			Log.Error("Tried to get current side center, but we're not showing anything!");
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		int num = 0;
		foreach (NCreature creature in _currentLayout.GetCreatures())
		{
			zero += creature.VfxSpawnPosition;
			num++;
		}
		return zero / num;
	}

	public Vector2 GetSideFloor()
	{
		if (_currentLayout == null)
		{
			Log.Error("Tried to get current side floor, but we're not showing anything!");
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		int num = 0;
		foreach (NCreature creature in _currentLayout.GetCreatures())
		{
			zero += creature.GetBottomOfHitbox();
			num++;
		}
		return zero / num;
	}

	private void EnableStatsModeHotkeys()
	{
		NHotkeyManager.Instance.PushHotkeyPressedBinding(_filterLeftHotkey, FilterLeft);
		NHotkeyManager.Instance.PushHotkeyPressedBinding(_filterRightHotkey, FilterRight);
	}

	private void DisableStatsModeHotkeys()
	{
		NHotkeyManager.Instance.RemoveHotkeyPressedBinding(_filterLeftHotkey, FilterLeft);
		NHotkeyManager.Instance.RemoveHotkeyPressedBinding(_filterRightHotkey, FilterRight);
	}

	private void FilterLeft()
	{
		List<NBestiaryCharacterFilter> list = _filterContainer.GetChildren().OfType<NBestiaryCharacterFilter>().ToList();
		int num = list.IndexOf(_currentFilter);
		for (int i = 1; i <= list.Count; i++)
		{
			int index = (num - i + list.Count) % list.Count;
			if (!list[index].IsLocked)
			{
				SelectFilter(list[index]);
				break;
			}
		}
	}

	private void FilterRight()
	{
		List<NBestiaryCharacterFilter> list = _filterContainer.GetChildren().OfType<NBestiaryCharacterFilter>().ToList();
		int num = list.IndexOf(_currentFilter);
		for (int i = 1; i <= list.Count; i++)
		{
			int index = (num + i) % list.Count;
			if (!list[index].IsLocked)
			{
				SelectFilter(list[index]);
				break;
			}
		}
	}

	private void SelectFilter(NBestiaryCharacterFilter filter)
	{
		filter.IsSelected = true;
		OnCharacterFilterSelected(filter);
	}

	private void UpdatePageIcons()
	{
		bool flag = _isStatsMode && NControllerManager.Instance.IsUsingDirectionalNavigation;
		_pageLeftIcon.Visible = flag;
		_pageRightIcon.Visible = flag;
		if (flag)
		{
			_pageLeftIcon.UpdateInput(MegaInput.viewDeckAndTabLeft);
			_pageRightIcon.UpdateInput(MegaInput.viewExhaustPileAndTabRight);
		}
	}

	public static bool CanBeShown()
	{
		if (SaveManager.Instance.Progress.DiscoveredActs.Count == 0)
		{
			return false;
		}
		return SaveManager.Instance.Progress.EnemyStats.Values.Any((EnemyStats e) => e.TotalWins > 0);
	}
}
