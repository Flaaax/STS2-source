using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Forms;

public partial class NFormVfx : Node2D
{
	protected Player? _owner;

	protected bool _isActive;

	protected string _testBoneName = "";

	public virtual void Initialize(Player owner)
	{
		_owner = owner;
		if (_owner != null)
		{
			NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(_owner.Creature);
			if (nCreature != null && nCreature.Visuals.HasSpineAnimation)
			{
				SetSpineSprite(nCreature.Visuals.SpineBody, nCreature.Visuals.GetCurrentBody());
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public virtual void OnEffectTriggered()
	{
	}

	public virtual void SetActive(bool isActive)
	{
		_isActive = isActive;
	}

	protected virtual void SetSpineSprite(MegaSprite spineSprite, Node2D sourceNode)
	{
	}

	public void ForceSetSpineSprite(Node2D sourceNode)
	{
		SetSpineSprite(new MegaSprite(sourceNode), sourceNode);
	}

	public void ForceTestBoneName(string testBoneName)
	{
		_testBoneName = testBoneName;
	}
}
