using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryLayoutDefault : NBestiaryLayout
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary_layout_default");

	private NCreature? _creature;

	private Control _creatureContainer;

	public static NBestiaryLayoutDefault? Create()
	{
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBestiaryLayoutDefault>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_creatureContainer = GetNode<Control>("%MonsterVisualsContainer");
	}

	public override void Cleanup()
	{
		_creature?.QueueFreeSafely();
		_creature = null;
	}

	public override List<BestiaryMonsterMove> Setup(BestiaryEntry entry, Tween tween)
	{
		MonsterModel monsterModel = entry.monsterModel.ToMutable();
		monsterModel.Rng = Rng.Chaotic;
		_creature?.QueueFreeSafely();
		monsterModel.SetUpForCombat();
		Creature entity = new Creature(monsterModel, CombatSide.Enemy, null)
		{
			CombatState = NullCombatState.Instance
		};
		_creature = NCreature.Create(entity);
		_creatureContainer.AddChildSafely(_creature);
		_creature.SetupForBestiary();
		_creature.Position = new Vector2(0f, _creature.Hitbox.Size.Y * 0.5f);
		_creature.Modulate = StsColors.transparentBlack;
		tween.TweenProperty(_creature, "modulate", Colors.White, 0.25);
		return monsterModel.GenerateBestiaryMoveList(_creature.Visuals);
	}

	public override IEnumerable<NCreature> GetCreatures()
	{
		if (_creature == null)
		{
			return Array.Empty<NCreature>();
		}
		return new global::_003C_003Ez__ReadOnlySingleElementList<NCreature>(_creature);
	}
}
