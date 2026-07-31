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

public partial class NVoidFormVfx : NFormVfx
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/forms/void/vfx_void_form_idle_vfx");

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
	private Node2D[] _swords;

	[Export(PropertyHint.None, "")]
	private Vector2 _swordsScaleRange = new Vector2(0.7f, 1f);

	[Export(PropertyHint.None, "")]
	private NSpineSpriteBoneFollower? _boneFollower;

	[Export(PropertyHint.None, "")]
	private NValueRamp _valueRamp;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer? _powerActiveParticles;

	[Export(PropertyHint.None, "")]
	private Gradient _glowSelfModulateGradient;

	[Export(PropertyHint.None, "")]
	private Node2D _glow;

	public static NVoidFormVfx? Create(Creature target)
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
		NVoidFormVfx nVoidFormVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NVoidFormVfx>(PackedScene.GenEditState.Disabled);
		nVoidFormVfx.Initialize(target.Player);
		creatureNode.Visuals.AddFormVfx(nVoidFormVfx);
		return nVoidFormVfx;
	}

	public override void Initialize(Player owner)
	{
		base.Initialize(owner);
		_valueRamp.SetIncreasing(isIncreasing: true);
		_valueRamp.ForceValue(1f);
		UpdateVfx(1f);
	}

	public override void _Process(double delta)
	{
		if (_valueRamp.TryProcess(delta, out var returnValue))
		{
			UpdateVfx(returnValue);
		}
	}

	private void UpdateVfx(float progress)
	{
		_glow.SelfModulate = _glowSelfModulateGradient.Sample(progress);
		for (int i = 0; i < _swords.Length; i++)
		{
			_swords[i].Scale = Vector2.One * Mathf.Lerp(_swordsScaleRange.X, _swordsScaleRange.Y, progress);
		}
	}

	public override void SetActive(bool isActive)
	{
		base.SetActive(isActive);
		_powerActiveParticles?.SetEmitting(isActive);
		_valueRamp.SetIncreasing(isActive);
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
