using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public partial class NBestiaryLayoutKaiserCrab : NBestiaryLayout
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary_layout_kaiser_crab");

	private NCreature? _creature;

	private NKaiserCrabBossBackground _background;

	private Vector2 _backgroundPosition;

	public static NBestiaryLayoutKaiserCrab? Create()
	{
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBestiaryLayoutKaiserCrab>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_background = GetNode<NKaiserCrabBossBackground>("%KaiserCrab");
		_backgroundPosition = _background.Position;
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
		if (monsterModel is Crusher)
		{
			_background.Position = _backgroundPosition + 1020f * Vector2.Right * 0.5f / _background.Scale;
		}
		else if (monsterModel is Rocket)
		{
			_background.Position = _backgroundPosition + 1240f * Vector2.Left * 0.5f / _background.Scale;
		}
		_background.SetVisible(visible: true);
		_creature?.QueueFreeSafely();
		monsterModel.SetUpForCombat();
		Creature entity = new Creature(monsterModel, CombatSide.Enemy, null)
		{
			CombatState = NullCombatState.Instance
		};
		_creature = NCreature.Create(entity);
		this.AddChildSafely(_creature);
		_creature.SetupForBestiary();
		_creature.Position = new Vector2(0f, _creature.Hitbox.Size.Y * 0.5f);
		_creature.Modulate = StsColors.transparentBlack;
		tween.TweenProperty(_creature, "modulate", Colors.White, 0.25);
		List<BestiaryMonsterMove> list = monsterModel.GenerateBestiaryMoveList(_creature.Visuals);
		list.Add(BestiaryMonsterMove.FromAction(new LocString("bestiary", "ACTION_NAME.hurt"), AnimHurt));
		list.Add(BestiaryMonsterMove.FromAction(new LocString("bestiary", "ACTION_NAME.die"), AnimDie));
		return list;
	}

	public Task AnimHurt()
	{
		_background.PlayHurtAnim((!(_creature.Entity.Monster is Crusher)) ? NKaiserCrabBossBackground.ArmSide.Right : NKaiserCrabBossBackground.ArmSide.Left);
		return Task.CompletedTask;
	}

	public Task AnimDie()
	{
		NAudioManager.Instance.PlayOneShot(_creature.Entity.Monster.DeathSfx);
		_background.PlayArmDeathAnim((!(_creature.Entity.Monster is Crusher)) ? NKaiserCrabBossBackground.ArmSide.Right : NKaiserCrabBossBackground.ArmSide.Left);
		return Task.CompletedTask;
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
