using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryLayoutDecimillipede : NBestiaryLayout
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary_layout_decimillipede");

	private Control? _encounterVisuals;

	private Control _creatureContainer;

	private Control _encounterSlots;

	private List<NCreature> _creatures = new List<NCreature>();

	private NBestiary _bestiary;

	public static NBestiaryLayoutDecimillipede? Create(NBestiary bestiary)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NBestiaryLayoutDecimillipede nBestiaryLayoutDecimillipede = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBestiaryLayoutDecimillipede>(PackedScene.GenEditState.Disabled);
		nBestiaryLayoutDecimillipede._bestiary = bestiary;
		return nBestiaryLayoutDecimillipede;
	}

	public override void _Ready()
	{
		_creatureContainer = GetNode<Control>("%CreatureContainer");
		_encounterSlots = GetNode<Control>("%EncounterSlots");
	}

	public override void Cleanup()
	{
		_encounterVisuals?.QueueFreeSafely();
		_encounterVisuals = null;
	}

	public override List<BestiaryMonsterMove> Setup(BestiaryEntry entry, Tween tween)
	{
		EncounterModel encounterModel = entry.encounterModel.ToMutable();
		_encounterVisuals?.QueueFreeSafely();
		encounterModel.GenerateMonstersWithSlots(NullRunState.Instance);
		foreach (var monstersWithSlot in encounterModel.MonstersWithSlots)
		{
			MonsterModel item = monstersWithSlot.Item1;
			string item2 = monstersWithSlot.Item2;
			item.Rng = Rng.Chaotic;
			item.SetUpForCombat();
			Creature creature = new Creature(item, CombatSide.Enemy, item2)
			{
				CombatState = new NullCombatState()
			};
			NCreature nCreature = NCreature.Create(creature);
			_creatureContainer.AddChildSafely(nCreature);
			_creatures.Add(nCreature);
			nCreature.SetupForBestiary();
			nCreature.GlobalPosition = _encounterSlots.GetNode<Marker2D>(creature.SlotName).GlobalPosition;
		}
		int num = 3;
		List<BestiaryMonsterMove> list = new List<BestiaryMonsterMove>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<BestiaryMonsterMove> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = BestiaryMonsterMove.FromAction(GetBestiaryMoveName("WRITHE"), AnimAttack);
		num2++;
		span[num2] = BestiaryMonsterMove.FromAction(GetBestiaryMoveName("REATTACH"), AnimReattach);
		num2++;
		span[num2] = BestiaryMonsterMove.FromAction(GetBestiaryMoveName("DEAD"), AnimDie);
		return list;
	}

	private LocString GetBestiaryMoveName(string moveId)
	{
		return new LocString("monsters", ModelDb.GetId<DecimillipedeSegment>().Entry + ".moves." + moveId + ".title");
	}

	private async Task AnimAttack()
	{
		foreach (NCreature creature in _creatures)
		{
			((DecimillipedeSegment)creature.Entity.Monster).SegmentAttack();
		}
		Node2D node2D = PreloadManager.Cache.GetScene(DecimillipedeSegment.rocksVfxPath).Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		_bestiary.VfxContainer.AddChildSafely(node2D);
		node2D.GlobalPosition = NGame.Instance.GetViewportRect().Size * 0.5f;
		await Cmd.Wait(0.5f);
	}

	private async Task AnimReattach()
	{
		foreach (NCreature creature in _creatures)
		{
			creature.GetSpecialNode<NDecimillipedeSegmentVfx>("%NDecimillipedeSegmentVfx")?.Regenerate();
			await CreatureCmd.TriggerAnim(creature.Entity, "Revive", 0.15f);
		}
	}

	private async Task AnimDie()
	{
		foreach (NCreature creature in _creatures)
		{
			await CreatureCmd.TriggerAnim(creature.Entity, "Dead", 0.15f);
		}
	}

	public override IEnumerable<NCreature> GetCreatures()
	{
		return _creatures;
	}
}
