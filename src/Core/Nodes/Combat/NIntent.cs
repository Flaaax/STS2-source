using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MegaCrit.Sts2.Core.Nodes.Combat;

public partial class NIntent : Control
{
	private const string _scenePath = "res://scenes/combat/intent.tscn";

	private const float _bobSpeed = (float)Math.PI;

	private const float _bobDistance = 10f;

	private const float _bobOffset = 8f;

	private const int _animationFps = 15;

	private Control _intentHolder;

	private Sprite2D _intentSprite;

	private MegaRichTextLabel _valueLabel;

	private CpuParticles2D _intentParticle;

	private Creature _owner;

	private IEnumerable<Creature> _targets;

	private AbstractIntent _intent;

	private float _timeOffset;

	private float _timeAccumulator;

	private bool _isFrozen;

	private string? _animationName;

	private readonly List<Texture2D> _animationFrames = new List<Texture2D>();

	private int? _animationFrame;

	private NCombatRoom? _combatRoom;

	public static IEnumerable<string> AssetPaths
	{
		get
		{
			List<string> list = new List<string>();
			list.Add("res://scenes/combat/intent.tscn");
			list.AddRange(IntentAnimData.AssetPaths);
			return new _003C_003Ez__ReadOnlyList<string>(list);
		}
	}

	public override void _Ready()
	{
		_intentHolder = GetNode<Control>("%IntentHolder");
		_intentSprite = GetNode<Sprite2D>("%Intent");
		_valueLabel = GetNode<MegaRichTextLabel>("%Value");
		_intentParticle = GetNode<CpuParticles2D>("%IntentParticle");
		Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
		Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));
		_intentHolder.Modulate = (NCombatUi.IsDebugHidingIntent ? Colors.Transparent : Colors.White);
	}

	public override void _EnterTree()
	{
		CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
		if (NCombatRoom.Instance != null)
		{
			_combatRoom = NCombatRoom.Instance;
			_combatRoom.Ui.DebugToggleIntent += DebugToggleVisibility;
		}
	}

	private void DebugToggleVisibility()
	{
		_intentHolder.Modulate = (NCombatUi.IsDebugHidingIntent ? Colors.Transparent : Colors.White);
	}

	public override void _ExitTree()
	{
		CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
		if (_combatRoom != null)
		{
			_combatRoom.Ui.DebugToggleIntent -= DebugToggleVisibility;
		}
	}

	public void UpdateIntent(AbstractIntent intent, IEnumerable<Creature> targets, Creature owner)
	{
		_owner = owner;
		_targets = targets;
		_intent = intent;
		UpdateVisuals();
	}

	private void OnCombatStateChanged(CombatState _)
	{
		if (!_isFrozen)
		{
			UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		string animation = _intent.GetAnimation(_targets, _owner);
		if (_animationName != animation)
		{
			_animationName = animation;
			_animationFrame = null;
			_timeAccumulator = 0f;
			int animationFrameCount = IntentAnimData.GetAnimationFrameCount(animation);
			_animationFrames.Clear();
			for (int i = 0; i < animationFrameCount; i++)
			{
				_animationFrames.Add(PreloadManager.Cache.GetTexture2D(IntentAnimData.GetAnimationFrame(animation, i)));
			}
		}
		_intentParticle.Texture = _intent.GetTexture(_targets, _owner);
		MegaRichTextLabel valueLabel = _valueLabel;
		AbstractIntent intent = _intent;
		string text = ((intent is AttackIntent attackIntent) ? (attackIntent.GetIntentLabel(_targets, _owner).GetFormattedText() ?? "") : ((!(intent is StatusIntent)) ? string.Empty : (_intent.GetIntentLabel(_targets, _owner).GetFormattedText() ?? "")));
		valueLabel.Text = text;
	}

	public override void _Process(double delta)
	{
		_intentHolder.Position = Vector2.Up * (Mathf.Sin((float)Time.GetTicksMsec() * 0.001f * (float)Math.PI + _timeOffset) * 10f + 8f);
		if (_animationFrames.Count > 0)
		{
			int num = (int)(_timeAccumulator * 15f) % _animationFrames.Count;
			if (_animationFrame != num)
			{
				_animationFrame = num;
				_intentSprite.Texture = _animationFrames[num];
			}
			_timeAccumulator += (float)delta;
		}
	}

	public static NIntent Create(float startTime)
	{
		NIntent nIntent = PreloadManager.Cache.GetScene("res://scenes/combat/intent.tscn").Instantiate<NIntent>(PackedScene.GenEditState.Disabled);
		nIntent._timeOffset = startTime;
		return nIntent;
	}

	public void PlayPerform()
	{
		_intentParticle.Emitting = true;
	}

	public void SetFrozen(bool isFrozen)
	{
		_isFrozen = isFrozen;
	}

	private void OnHovered()
	{
		if (_intent.HasIntentTip)
		{
			NCombatRoom.Instance?.GetCreatureNode(_owner)?.ShowHoverTips(new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(_intent.GetHoverTip(_targets, _owner)));
		}
	}

	private void OnUnhovered()
	{
		NCombatRoom.Instance?.GetCreatureNode(_owner)?.HideHoverTips();
	}
}
