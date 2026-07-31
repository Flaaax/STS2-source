using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Forms;

public partial class NEchoFormVfx : NFormVfx
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/forms/echo/vfx_echo_form_idle_vfx");

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
	private NValueRamp _valueRamp;

	[Export(PropertyHint.None, "")]
	private Node2D _glow;

	[Export(PropertyHint.None, "")]
	private Node2D _echoLines;

	[Export(PropertyHint.None, "")]
	private Gradient _glowSelfModulateGradient;

	[Export(PropertyHint.None, "")]
	private Gradient _echoFormLinesSelfModulateGradient;

	[Export(PropertyHint.None, "")]
	private GpuParticles2D _speckParticles;

	[Export(PropertyHint.None, "")]
	private NSpineSpriteCopier? _spineCopier;

	public static NEchoFormVfx? Create(Creature target)
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
		NEchoFormVfx nEchoFormVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NEchoFormVfx>(PackedScene.GenEditState.Disabled);
		nEchoFormVfx.Initialize(target.Player);
		nEchoFormVfx.SetActive(isActive: false);
		creatureNode.Visuals.AddFormVfx(nEchoFormVfx);
		return nEchoFormVfx;
	}

	public override void _Process(double delta)
	{
		if (_valueRamp.TryProcess(delta, out var returnValue))
		{
			UpdateModulates(returnValue);
		}
	}

	private void UpdateModulates(float progress)
	{
		_glow.SelfModulate = _glowSelfModulateGradient.Sample(progress);
		_echoLines.SelfModulate = _echoFormLinesSelfModulateGradient.Sample(progress);
	}

	public override void SetActive(bool isActive)
	{
		base.SetActive(isActive);
		_speckParticles.Emitting = isActive;
		_valueRamp.SetIncreasing(isActive);
	}

	protected override void SetSpineSprite(MegaSprite spineSprite, Node2D sourceNode)
	{
		base.SetSpineSprite(spineSprite, sourceNode);
		if (_spineCopier != null)
		{
			_spineCopier.Initialize(spineSprite, sourceNode);
		}
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
