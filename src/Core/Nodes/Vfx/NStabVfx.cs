using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NStabVfx : Node2D
{
	private const string _scenePath = "res://scenes/vfx/stab_vfx.tscn";

	private Node2D _primaryVfx;

	private Node2D _secondaryVfx;

	private Vector2 _creatureCenter;

	private VfxColor _vfxColor;

	private bool _facingEnemies;

	private Tween? _tween;

	/// <summary>
	/// Stab vfx is a silly line that goes into the creature center.
	/// Pass in the creature vfx center position!
	/// </summary>
	/// <param name="target">The creature to target</param>
	/// <param name="facingEnemies">Whether the VFX is facing enemies</param>
	/// <param name="vfxColor">The color of the VFX</param>
	/// <returns>The created VFX instance or null if in test mode</returns>
	public static NStabVfx? Create(Creature? target, bool facingEnemies = false, VfxColor vfxColor = VfxColor.Red)
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
		Vector2 vfxSpawnPosition = creatureNode.VfxSpawnPosition;
		NStabVfx nStabVfx = PreloadManager.Cache.GetScene("res://scenes/vfx/stab_vfx.tscn").Instantiate<NStabVfx>(PackedScene.GenEditState.Disabled);
		nStabVfx._vfxColor = vfxColor;
		nStabVfx._facingEnemies = facingEnemies;
		Vector2 vector = new Vector2(facingEnemies ? Rng.Chaotic.NextFloat(0f, 48f) : Rng.Chaotic.NextFloat(-48f, 0f), Rng.Chaotic.NextFloat(-50f, 50f));
		nStabVfx._creatureCenter = vfxSpawnPosition + vector;
		return nStabVfx;
	}

	public override void _Ready()
	{
		_primaryVfx = GetNode<Node2D>("Primary");
		_secondaryVfx = GetNode<Node2D>("%Secondary");
		_primaryVfx.GlobalPosition = GenerateSpawnPosition();
		_primaryVfx.Rotation = MathHelper.GetAngle(_primaryVfx.GlobalPosition - _creatureCenter) + (float)Math.PI / 2f;
		SetColor();
		TaskHelper.RunSafely(Animate());
	}

	private void SetColor()
	{
		switch (_vfxColor)
		{
		case VfxColor.Green:
			_primaryVfx.SelfModulate = new Color("00A52F");
			_secondaryVfx.SelfModulate = new Color("FFCB2D");
			break;
		case VfxColor.Blue:
			_primaryVfx.SelfModulate = new Color("007BDD");
			_secondaryVfx.SelfModulate = new Color("00EFF6");
			break;
		case VfxColor.Purple:
			_primaryVfx.SelfModulate = new Color("A803FF");
			_secondaryVfx.SelfModulate = new Color("00EFF3");
			break;
		case VfxColor.White:
			_primaryVfx.SelfModulate = new Color("808080");
			_secondaryVfx.SelfModulate = new Color("FFFFFF");
			break;
		case VfxColor.Cyan:
			_primaryVfx.SelfModulate = new Color("009599");
			_secondaryVfx.SelfModulate = new Color("5CDCFF");
			break;
		case VfxColor.Gold:
			_primaryVfx.SelfModulate = new Color("EBA800");
			_secondaryVfx.SelfModulate = new Color("FFE39C");
			break;
		default:
			_primaryVfx.SelfModulate = new Color("FF0000");
			_secondaryVfx.SelfModulate = new Color("FFCB2D");
			break;
		case VfxColor.Black:
			break;
		}
	}

	/// <summary>
	/// Spawns our stab vfx in a random position to the left or right of the target.
	/// </summary>
	private Vector2 GenerateSpawnPosition()
	{
		Vector2 vector = new Vector2(Rng.Chaotic.NextFloat(-12f, 12f), Rng.Chaotic.NextFloat(-64f, 64f));
		Vector2 vector2 = new Vector2(_facingEnemies ? (-200f) : 200f, 0f);
		return _creatureCenter + vector + vector2;
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}

	private async Task Animate()
	{
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "modulate:a", 1f, 0.25);
		_tween.TweenProperty(_primaryVfx, "position", _creatureCenter, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
		_tween.TweenProperty(this, "modulate:a", 0f, 0.25).SetDelay(0.25);
		await _tween.AwaitFinished(this);
		this.QueueFreeSafely();
	}
}
