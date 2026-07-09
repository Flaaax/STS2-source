using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

public abstract partial class NBestiaryLayout : Control
{
	public abstract void Cleanup();

	public abstract List<BestiaryMonsterMove> Setup(BestiaryEntry entry, Tween tween);

	public abstract IEnumerable<NCreature> GetCreatures();
}
