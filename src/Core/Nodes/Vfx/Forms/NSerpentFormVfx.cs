using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Forms;

public partial class NSerpentFormVfx : NFormVfx
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/forms/serpent/vfx_serpent_form_idle_vfx");

	[Export(PropertyHint.None, "")]
	private string _ironcladBoneName = "";

	[Export(PropertyHint.None, "")]
	private string _silentBoneName = "";

	[Export(PropertyHint.None, "")]
	private string _regentBoneName = "";

	[Export(PropertyHint.None, "")]
	private string _necrobinderBoneName = "";

	[Export(PropertyHint.None, "")]
	private string _defectBoneName = "";

	[Export(PropertyHint.None, "")]
	private NSpineSpriteBoneFollower? _boneFollower;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _effectTriggeredParticles;

	[Export(PropertyHint.None, "")]
	private Node2D _snakesContainer;

	[Export(PropertyHint.None, "")]
	private NValueRamp _valueRamp;

	[Export(PropertyHint.None, "")]
	private Gradient _snakesModulateGradient;

	public static NSerpentFormVfx? Create(Creature target)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(target);
		if (creatureNode == null)
		{
			return null;
		}
		NSerpentFormVfx nSerpentFormVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NSerpentFormVfx>(PackedScene.GenEditState.Disabled);
		nSerpentFormVfx.Initialize(target.Player);
		creatureNode.Visuals.AddFormVfx(nSerpentFormVfx);
		return nSerpentFormVfx;
	}

	public override void Initialize(Player owner)
	{
		base.Initialize(owner);
		_valueRamp.SetIncreasing(isIncreasing: false);
		_valueRamp.ForceValue(0f);
		UpdateSnakesContainerModulate(0f);
	}

	public override void _Process(double delta)
	{
		if (_valueRamp.TryProcess(delta, out var returnValue))
		{
			UpdateSnakesContainerModulate(returnValue);
		}
	}

	private void UpdateSnakesContainerModulate(float progress)
	{
		_snakesContainer.Modulate = _snakesModulateGradient.Sample(progress);
	}

	public override void OnEffectTriggered()
	{
		base.OnEffectTriggered();
		_effectTriggeredParticles?.Restart();
		_valueRamp.ForceValue(1f);
	}

	protected override void SetSpineSprite(MegaSprite spineSprite, Node2D sourceNode)
	{
		base.SetSpineSprite(spineSprite, sourceNode);
		if (_boneFollower == null)
		{
			return;
		}
		string boneName = "";
		if (_owner != null)
		{
			CharacterModel character = _owner.Character;
			if (character is Ironclad)
			{
				boneName = _ironcladBoneName;
			}
			else if (character is Silent)
			{
				boneName = _silentBoneName;
			}
			else if (character is Regent)
			{
				boneName = _regentBoneName;
			}
			else if (character is Necrobinder)
			{
				boneName = _necrobinderBoneName;
			}
			else if (character is Defect)
			{
				boneName = _defectBoneName;
			}
		}
		else
		{
			boneName = _testBoneName;
		}
		_boneFollower.SetSpineSprite(spineSprite, boneName);
	}
}
