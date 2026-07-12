using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
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

	private MegaRichTextLabel _monsterNameLabel;

	private MegaLabel _epithet;

	private NScrollableContainer _sidebar;

	private VBoxContainer _bestiaryList;

	private static readonly LocString _locked = new LocString("bestiary", "LOCKED.monsterTitle");

	private Control _selectionArrow;

	private Tween? _arrowTween;

	private static readonly Vector2 _arrowOffset = new Vector2(-38f, 117f);

	private bool _initSelectionArrow = true;

	private Control _layoutContainer;

	private NBestiaryLayout? _currentLayout;

	private static readonly LocString _placeholderDesc = new LocString("bestiary", "DESCRIPTION.placeholder");

	private MegaRichTextLabel _descriptionLabel;

	private Control _moveList;

	private Control _moveContainer;

	private HashSet<ModelId> _discoveredMonsterIds;

	private HashSet<ModelId> _discoveredEncounterIds;

	private NBestiaryEntry? _selectedEntry;

	private Control? _previousScreenshakeTarget;

	private Tween? _tween;

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
		_descriptionLabel = GetNode<MegaRichTextLabel>("%Description");
		_moveContainer = GetNode<Control>("%MoveContainer");
		_selectionArrow = GetNode<Control>("%SelectionArrow");
		_moveList = GetNode<Control>("%MoveList");
		VfxContainer = GetNode<Control>("%VfxContainer");
		BackVfxContainer = GetNode<Control>("%BackVfxContainer");
	}

	/// <summary>
	/// On screen open. When the player opens the Bestiary.
	/// </summary>
	public override void OnSubmenuOpened()
	{
		Instance = this;
		_previousScreenshakeTarget = NGame.Instance?.ScreenshakeTarget;
		CreateEntries();
	}

	/// <summary>
	/// Called when the Bestiary is closed (Back button)
	/// </summary>
	public override void OnSubmenuClosed()
	{
		_initSelectionArrow = true;
		_selectedEntry = null;
		Instance = null;
		_currentLayout?.Cleanup();
		if (_previousScreenshakeTarget != null)
		{
			NGame.Instance?.SetScreenShakeTarget(_previousScreenshakeTarget);
		}
		else
		{
			NGame.Instance?.ClearScreenShakeTarget();
		}
		_bestiaryList.FreeChildren();
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

	private void AddAct(ActModel act)
	{
		if (!SaveManager.Instance.Progress.DiscoveredActs.Contains(act.Id))
		{
			return;
		}
		_bestiaryList.AddChildSafely(NBestiaryActDivider.Create(act));
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
		list.Sort(delegate(BestiaryEntry e1, BestiaryEntry e2)
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
				return string.Compare(e1.GetEntryTitle(), e2.GetEntryTitle(), StringComparison.CurrentCulture);
			}
			return string.Compare(e1.GetEntryTitle(), e2.GetEntryTitle(), StringComparison.CurrentCulture);
		});
		foreach (BestiaryEntry item in list)
		{
			NBestiaryEntry nBestiaryEntry = NBestiaryEntry.Create(item, item.IsDiscovered(_discoveredMonsterIds, _discoveredEncounterIds));
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
			_descriptionLabel.Text = _placeholderDesc.GetFormattedText();
			_currentLayout?.Cleanup();
			_currentLayout?.QueueFreeSafely();
			_moveContainer.Visible = false;
		}
		else if (!entry.IsDiscovered)
		{
			_monsterNameLabel.Text = _locked.GetFormattedText();
			_descriptionLabel.Text = _placeholderDesc.GetFormattedText();
			_currentLayout?.Cleanup();
			_currentLayout?.QueueFreeSafely();
			_moveContainer.Visible = false;
		}
		else
		{
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_descriptionLabel.Text = _placeholderDesc.GetFormattedText();
			_descriptionLabel.Modulate = StsColors.transparentWhite;
			_monsterNameLabel.Text = entry.Entry.GetEntryTitle();
			_monsterNameLabel.SelfModulate = StsColors.transparentWhite;
			_epithet.Modulate = StsColors.transparentWhite;
			_moveContainer.Modulate = StsColors.transparentWhite;
			_tween.TweenProperty(_monsterNameLabel, "position:y", 88f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
				.From(24f);
			_tween.TweenProperty(_monsterNameLabel, "self_modulate:a", 1f, 0.5);
			_tween.TweenProperty(_epithet, "modulate:a", 1f, 0.5).SetDelay(0.2);
			_tween.TweenProperty(_descriptionLabel, "position:y", 894f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
				.From(958f);
			_tween.TweenProperty(_descriptionLabel, "modulate:a", 1f, 0.5);
			_tween.TweenProperty(_moveContainer, "position:x", 242f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
				.From(210f)
				.SetDelay(0.2);
			_tween.TweenProperty(_moveContainer, "modulate:a", 1f, 0.5).SetDelay(0.2);
			_currentLayout?.Cleanup();
			if (!entry.Entry.CanReuseLayout(_currentLayout))
			{
				_currentLayout?.QueueFreeSafely();
				_currentLayout = entry.Entry.CreateLayoutNode(this);
				_layoutContainer.AddChildSafely(_currentLayout);
				NGame.Instance?.SetScreenShakeTarget(_currentLayout);
			}
			List<BestiaryMonsterMove> list = _currentLayout.Setup(entry.Entry, _tween);
			_moveContainer.Visible = true;
			for (int i = 0; i < list.Count; i++)
			{
				if (i >= 9)
				{
					Log.Error("Hotkeys for monster Actions beyond 9 are not supported!");
				}
				NBestiaryMoveButton nBestiaryMoveButton = NBestiaryMoveButton.Create(list[i], $"mega_select_card_{i + 1}");
				_moveList.AddChildSafely(nBestiaryMoveButton);
				nBestiaryMoveButton.Connect(NClickableControl.SignalName.Released, Callable.From<NBestiaryMoveButton>(OnMoveButtonClicked));
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

	private void PlayMoveAnim(IEnumerable<NCreature> creatures, BestiaryMonsterMove move)
	{
		foreach (NCreature creature in creatures)
		{
			if (move.stateId != null)
			{
				MonsterModel monsterModel = creature?.Entity.Monster;
				if (monsterModel == null)
				{
					throw new InvalidOperationException($"Non-monster creature {creature} is in the bestiary!");
				}
				monsterModel.SetMoveImmediate((MoveState)monsterModel.MoveStateMachine.States[move.stateId], forceTransition: true);
				TaskHelper.RunSafely(monsterModel.PerformMove());
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
				creature?.Visuals.SpineBody?.GetAnimationState().SetAnimation(move.animId, loop: false);
				if (move.animId != "die")
				{
					creature?.Visuals.SpineBody?.GetAnimationState().AddAnimation("idle_loop");
				}
				if (move.sfx != null)
				{
					NAudioManager.Instance.PlayOneShot(move.sfx);
				}
			}
			if (move.stopSfxLoops)
			{
				creature?.StopAllSfxLoops();
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

	public static bool CanBeShown()
	{
		if (SaveManager.Instance.Progress.DiscoveredActs.Count == 0)
		{
			return false;
		}
		return SaveManager.Instance.Progress.EnemyStats.Values.Any((EnemyStats e) => e.TotalWins > 0);
	}
}
