using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.FoolArrows;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NImpaledArrowVfx : Node2D
{
	public const string frontArrowSlot = "ARROW_HIT_FRONT";

	public const string backArrowSlot = "ARROW_HIT_BACK";

	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/impaled_arrow_vfx");

	private float[] _arrowRotationOffsets = new float[3] { 0f, -20f, 20f };

	private Vector2[] _arrowPositionOffsets = new Vector2[3]
	{
		new Vector2(0f, 0f),
		new Vector2(15f, 5f),
		new Vector2(-15f, 5f)
	};

	private static readonly StringName _fadingName = "ImpaledArrowVfxFading";

	private Node2D _frontArrowNode;

	private Node2D _backArrowNode;

	private Node2D _frontArrowVisual;

	private Node2D _backArrowVisual;

	private Node2D? _frontSlotNode;

	private Node2D? _backSlotNode;

	private ArrowSlot? _arrowSlot;

	private Creature _creature;

	private Tween? _retrieveTween;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public static NImpaledArrowVfx? Create(Creature creature, ArrowSlot arrowSlot)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NImpaledArrowVfx nImpaledArrowVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NImpaledArrowVfx>(PackedScene.GenEditState.Disabled);
		nImpaledArrowVfx._creature = creature;
		nImpaledArrowVfx._arrowSlot = arrowSlot;
		return nImpaledArrowVfx;
	}

	public override void _Ready()
	{
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(_creature);
		_frontArrowNode = GetNode<Node2D>("FrontArrow");
		_backArrowNode = GetNode<Node2D>("BackArrow");
		_frontArrowVisual = _frontArrowNode.GetNode<Node2D>("ArrowFg");
		_backArrowVisual = _backArrowNode.GetNode<Node2D>("ArrowBg");
		if (nCreature != null)
		{
			if (nCreature.HasSpineArrowSlots)
			{
				ImpaleArrowIntoSpineSlots(nCreature);
			}
			else
			{
				ImpaleArrowIntoCreature(nCreature.Visuals);
			}
		}
	}

	private void ImpaleArrowIntoSpineSlots(NCreature creature)
	{
		_frontSlotNode = MegaSlotNode.Create(creature.Visuals.Body, "ARROW_HIT_FRONT");
		_backSlotNode = MegaSlotNode.Create(creature.Visuals.Body, "ARROW_HIT_BACK");
		_backSlotNode.ShowBehindParent = true;
		_frontArrowVisual.Reparent(_frontSlotNode, keepGlobalTransform: false);
		_backArrowVisual.Reparent(_backSlotNode, keepGlobalTransform: false);
		float num = Rng.Chaotic.NextFloat(0.9f, 1.1f);
		_frontArrowVisual.Scale *= 1f / creature.Visuals.Body.Scale.X * num;
		_backArrowVisual.Scale *= 1f / creature.Visuals.Body.Scale.X * num;
		ImpaledPower power = creature.Entity.GetPower<ImpaledPower>();
		if (power != null)
		{
			int count = power.Arrows.Count;
			float num2 = _arrowRotationOffsets[count % _arrowRotationOffsets.Length];
			Vector2 vector = _arrowPositionOffsets[count % _arrowRotationOffsets.Length];
			_frontArrowVisual.RotationDegrees += num2;
			_backArrowVisual.RotationDegrees += num2;
			_frontArrowVisual.Position += vector;
			_backArrowVisual.Position += vector;
		}
	}

	private void ImpaleArrowIntoCreature(NCreatureVisuals visuals)
	{
		Marker2D nodeOrNull = visuals.GetNodeOrNull<Marker2D>("ArrowPosition");
		if (nodeOrNull != null)
		{
			base.Position = nodeOrNull.Position;
		}
		else
		{
			base.Position = visuals.VfxSpawnPosition.Position;
		}
		base.RotationDegrees += Rng.Chaotic.NextFloat(-15f, 15f);
		_backArrowNode.Reparent(visuals);
	}

	public override void _EnterTree()
	{
		if (_arrowSlot != null)
		{
			_arrowSlot.ArrowStatusUpdated += OnArrowStatusUpdated;
		}
	}

	public override void _ExitTree()
	{
		if (_retrieveTween != null)
		{
			_retrieveTween.Finished -= OnRetrieveFinished;
			_retrieveTween.Kill();
		}
		if (_arrowSlot != null)
		{
			_arrowSlot.ArrowStatusUpdated -= OnArrowStatusUpdated;
		}
		_backArrowNode.QueueFreeSafely();
		_frontSlotNode?.QueueFreeSafely();
		_backSlotNode?.QueueFreeSafely();
	}

	private void OnArrowStatusUpdated()
	{
		if (_arrowSlot.Status == ArrowStatus.Destroyed)
		{
			this.QueueFreeSafely();
		}
		else
		{
			RetrieveAndFree();
		}
	}

	/// <summary>
	/// Animates the arrow back to its owner, for when it has been pulled back out of the creature.
	/// </summary>
	private void RetrieveAndFree()
	{
		if (_retrieveTween == null)
		{
			SfxCmd.Play("event:/sfx/characters/fool/fool_dislodge");
			if (_frontSlotNode != null)
			{
				base.GlobalPosition = _frontSlotNode.GlobalPosition;
			}
			_frontArrowVisual.Reparent(_frontArrowNode);
			_backArrowVisual.Reparent(_backArrowNode);
			_backArrowNode.Reparent(this);
			NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(_arrowSlot.owner.Creature);
			base.Name = _fadingName;
			if (nCreature == null)
			{
				_retrieveTween = CreateTween();
				_retrieveTween.TweenProperty(this, "modulate:a", 0f, 0.20000000298023224);
			}
			else
			{
				_retrieveTween = CreateTween();
				_retrieveTween.TweenProperty(this, "global_position:x", nCreature.VfxSpawnPosition.X, 0.5);
				_retrieveTween.Parallel().TweenProperty(this, "rotation_degrees", -540f, 0.5);
				_retrieveTween.Parallel().TweenProperty(this, "global_position:y", nCreature.VfxSpawnPosition.Y - 500f, 0.25).SetEase(Tween.EaseType.Out)
					.SetTrans(Tween.TransitionType.Sine);
				_retrieveTween.Parallel().TweenProperty(this, "global_position:y", nCreature.VfxSpawnPosition.Y, 0.17499999701976776).SetDelay(0.25)
					.SetEase(Tween.EaseType.In)
					.SetTrans(Tween.TransitionType.Sine);
				_retrieveTween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.10000000149011612).SetDelay(0.4000000059604645);
			}
			_retrieveTween.Finished += OnRetrieveFinished;
		}
	}

	private void OnRetrieveFinished()
	{
		this.QueueFreeSafely();
	}
}
