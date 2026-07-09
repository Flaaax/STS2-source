using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

/// <summary>
/// Image of a literal hand.
/// This name is kind of wonky because it's definitely not the same as NPlayerHand.
/// </summary>
public partial class NHandImage : Control
{
	private enum State
	{
		None,
		Frozen,
		GrabbingRelic
	}

	private static readonly string _scenePath = SceneHelper.GetScenePath("ui/hand_image");

	private static readonly Vector2 _pointingPivot = new Vector2(163f, 10f);

	private static readonly Vector2 _fightingPivot = new Vector2(197f, 600f);

	private CancellationTokenSource _cts = new CancellationTokenSource();

	private Marker2D _grabMarker;

	private TextureRect _textureRect;

	private Vector2 _currentVelocity;

	private Vector2 _desiredPosition;

	private Tween? _downTween;

	private State _state;

	private bool _isInFight;

	private Vector2 _originalPosition;

	private float _handAnimateInProgress;

	public Player Player { get; private set; }

	public int Index { get; private set; }

	public bool IsDown { get; private set; }

	public bool IsShown { get; private set; }

	public static NHandImage Create(Player player, int slotIndex)
	{
		NHandImage nHandImage = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NHandImage>(PackedScene.GenEditState.Disabled);
		nHandImage.Player = player;
		nHandImage.Index = slotIndex;
		return nHandImage;
	}

	public override void _Ready()
	{
		_textureRect = GetNode<TextureRect>("TextureRect");
		_grabMarker = GetNode<Marker2D>("GrabMarker");
		_originalPosition = _textureRect.Position;
		base.Rotation = (Index % 4) switch
		{
			0 => 0f, 
			1 => (float)Math.PI / 2f, 
			2 => -(float)Math.PI / 2f, 
			3 => (float)Math.PI, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		if (!LocalContext.IsMe(Player))
		{
			base.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
		_textureRect.Texture = Player.Character.ArmPointingTexture;
	}

	public override void _EnterTree()
	{
		_cts = new CancellationTokenSource();
	}

	public override void _ExitTree()
	{
		_cts.Cancel();
	}

	/// <summary>
	/// If true: Modulates them to white regardless of whether they belong to the local player and adjusts their pivot
	/// in preparation for throwing RPS moves.
	/// If false: undoes all the above.
	/// </summary>
	/// <param name="inFight"></param>
	public void SetIsInFight(bool inFight)
	{
		_isInFight = inFight;
		if (_isInFight)
		{
			_textureRect.PivotOffset = _fightingPivot;
			base.Modulate = Colors.White;
			return;
		}
		_textureRect.PivotOffset = _pointingPivot;
		if (!LocalContext.IsMe(Player))
		{
			base.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
		else
		{
			base.Modulate = Colors.White;
		}
	}

	/// <summary>
	/// If true is passed, stops the hand from following the mouse cursor (local or remote), and tweens the hand towards
	/// the edge of the screen.
	/// </summary>
	public void SetFrozenForRelicAwards(bool frozenForRelicAwards)
	{
		if (frozenForRelicAwards)
		{
			_state = State.Frozen;
			_desiredPosition = GetFrozenPosition();
		}
		else
		{
			_state = State.None;
		}
	}

	private Vector2 GetFrozenPosition()
	{
		Rect2 viewportRect = GetViewportRect();
		Vector2 vector = Vector2.Down.Rotated(base.Rotation);
		return viewportRect.Size / 2f + viewportRect.Size * vector * 0.1667f;
	}

	/// <summary>
	/// Emulates a person throwing a rock-paper-scissors move; that is, the hand shakes three times, then shows an image
	/// that looks like a fist, a peace sign, or a hand with all fingers spread.
	/// </summary>
	/// <param name="move">The RPS move to show.</param>
	/// <param name="duration">How long, in total, the three shakes should take to animate.</param>
	public Tween DoFightMove(RelicPickingFightMove move, float duration)
	{
		float num = 0.666f * duration / 3f;
		float num2 = 0.333f * duration / 3f;
		int num3 = 6;
		List<float> list = new List<float>(num3);
		CollectionsMarshal.SetCount(list, num3);
		Span<float> span = CollectionsMarshal.AsSpan(list);
		int num4 = 0;
		span[num4] = num;
		num4++;
		span[num4] = num2;
		num4++;
		span[num4] = num;
		num4++;
		span[num4] = num2;
		num4++;
		span[num4] = num;
		num4++;
		span[num4] = num2;
		List<float> list2 = list;
		for (int i = 0; i < list2.Count - 1; i++)
		{
			float num5 = Rng.Chaotic.NextFloat((0f - duration) / 25f, duration / 25f);
			list2[i] += num5;
			list2[i + 1] -= num5;
		}
		SetTextureToFightMove(RelicPickingFightMove.Rock);
		Tween tween = CreateTween();
		tween.Chain().TweenProperty(_textureRect, "rotation", -(float)Math.PI / 10f + Rng.Chaotic.NextFloat(-0.05f, 0.05f), list2[0]).SetTrans(Tween.TransitionType.Linear)
			.SetEase(Tween.EaseType.In);
		tween.Chain().TweenProperty(_textureRect, "rotation", Rng.Chaotic.NextFloat(-0.02f, 0.02f), list2[1]).SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		tween.Chain().TweenProperty(_textureRect, "rotation", -(float)Math.PI / 10f + Rng.Chaotic.NextFloat(-0.05f, 0.05f), list2[2]).SetTrans(Tween.TransitionType.Linear)
			.SetEase(Tween.EaseType.In);
		tween.Chain().TweenProperty(_textureRect, "rotation", Rng.Chaotic.NextFloat(-0.02f, 0.02f), list2[3]).SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		tween.Chain().TweenProperty(_textureRect, "rotation", -(float)Math.PI / 10f + Rng.Chaotic.NextFloat(-0.05f, 0.05f), list2[4]).SetTrans(Tween.TransitionType.Linear)
			.SetEase(Tween.EaseType.In);
		tween.Chain().TweenProperty(_textureRect, "rotation", Rng.Chaotic.NextFloat(-0.02f, 0.02f), list2[5]).SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(delegate
		{
			SetTextureToFightMove(move);
		}));
		return tween;
	}

	private void SetTextureToFightMove(RelicPickingFightMove move)
	{
		TextureRect textureRect = _textureRect;
		textureRect.Texture = move switch
		{
			RelicPickingFightMove.Rock => Player.Character.ArmRockTexture, 
			RelicPickingFightMove.Paper => Player.Character.ArmPaperTexture, 
			RelicPickingFightMove.Scissors => Player.Character.ArmScissorsTexture, 
			_ => throw new ArgumentOutOfRangeException("move", move, null), 
		};
	}

	/// <summary>
	/// Sets the position at which the hand will point.
	/// If SetFrozenForRelicAwards has been called with true, then this does nothing.
	/// </summary>
	/// <param name="position"></param>
	public void SetPointingPosition(Vector2 position)
	{
		if (_state == State.None)
		{
			_desiredPosition = position;
		}
	}

	/// <summary>
	/// Called after relic picking is done and the hand should go away.
	/// </summary>
	public void AnimateAway()
	{
		Rect2 viewportRect = GetViewportRect();
		Vector2 vector = Vector2.Down.Rotated(base.Rotation);
		_desiredPosition = viewportRect.Size / 2f + viewportRect.Size * vector * 0.8f;
		IsShown = false;
	}

	/// <summary>
	/// Called when the hand is shown and we should animate it in from off-screen.
	/// </summary>
	public void AnimateIn()
	{
		Tween tween = CreateTween();
		_handAnimateInProgress = 0f;
		tween.TweenMethod(Callable.From(delegate(float v)
		{
			_handAnimateInProgress = v;
		}), 0f, 1f, 0.6000000238418579).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		IsShown = true;
		if (_state == State.Frozen)
		{
			_desiredPosition = GetFrozenPosition();
		}
	}

	/// <summary>
	/// Does a small tween: scale down if true, scale up if false.
	/// Should be called when the left mouse button (local or remote) is pressed down.
	/// </summary>
	/// <param name="isDown"></param>
	public void SetIsDown(bool isDown)
	{
		if (IsDown != isDown)
		{
			IsDown = isDown;
			_downTween?.Kill();
			if (isDown)
			{
				_textureRect.Scale = Vector2.One * 0.98f;
				return;
			}
			_downTween = CreateTween();
			_downTween.TweenProperty(_textureRect, "scale", Vector2.One, 0.20000000298023224).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		}
	}

	/// <summary>
	/// Shakes the hand very dramatically and fades it out.
	/// </summary>
	/// <param name="duration">The amount of time to shake and fade.</param>
	public async Task DoLoseShake(float duration)
	{
		CreateTween().TweenProperty(this, "modulate", new Color(0.5f, 0.5f, 0.5f, 0.5f), duration * 0.333f).SetDelay(duration * 0.667f);
		ScreenRumbleInstance rumble = new ScreenRumbleInstance(100f, duration, 5f, RumbleStyle.Rumble);
		while (!rumble.IsDone)
		{
			Vector2 vector = rumble.Update(GetProcessDeltaTime());
			_textureRect.Position = _originalPosition + vector;
			await this.AwaitProcessFrame(_cts.Token);
		}
		_textureRect.Position = _originalPosition;
	}

	/// <summary>
	/// Does a tween that looks like the hand is grabbing the holder.
	/// After the tween is complete, the holder will be parented to the hand.
	/// </summary>
	/// <param name="holder">The relic holder to grab.</param>
	public async Task GrabRelic(NTreasureRoomRelicHolder holder)
	{
		State oldState = _state;
		_state = State.GrabbingRelic;
		SetTextureToFightMove(RelicPickingFightMove.Paper);
		Tween tween = CreateTween();
		tween.TweenProperty(this, "global_position", holder.GlobalPosition - _grabMarker.Position.Rotated(base.Rotation) + holder.Size * 0.5f, 0.5).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await tween.AwaitFinished(_cts.Token);
		SetTextureToFightMove(RelicPickingFightMove.Rock);
		holder.Reparent(this);
		holder.Rotation = 0f - base.Rotation;
		holder.Position = _grabMarker.Position - holder.Size * 0.5f;
		tween = CreateTween();
		tween.TweenProperty(this, "global_position", _desiredPosition, 0.5).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await tween.AwaitFinished(_cts.Token);
		_state = oldState;
	}

	/// <summary>
	/// Sets hand to fist.
	/// </summary>
	public void SetSkipped()
	{
		SetTextureToFightMove(RelicPickingFightMove.Rock);
	}

	public override void _Process(double delta)
	{
		Vector2 size = GetViewportRect().Size;
		int num = Index % 4;
		if (_state != State.GrabbingRelic)
		{
			Vector2 vector;
			if ((num == 0 || num == 3) ? true : false)
			{
				float num2 = ((num == 0) ? 1 : (-1));
				vector = num2 * Vector2.Down;
			}
			else
			{
				float num3 = ((num == 1) ? 1 : (-1));
				vector = num3 * Vector2.Left;
			}
			Rect2 viewportRect = GetViewportRect();
			Vector2 vector2 = viewportRect.Size / 2f + viewportRect.Size * vector;
			float smoothTime = ((_state == State.Frozen) ? 0.25f : ((!LocalContext.IsMe(Player)) ? 0.07f : 0.01f));
			Vector2 target = vector2.Lerp(_desiredPosition, _handAnimateInProgress);
			base.GlobalPosition = MathHelper.SmoothDamp(base.GlobalPosition, target, ref _currentVelocity, smoothTime, (float)delta);
		}
		if (_state == State.None)
		{
			if ((num == 0 || num == 3) ? true : false)
			{
				float num4 = ((num == 0) ? 1 : (-1));
				_textureRect.Rotation = num4 * (base.GlobalPosition.X - size.X / 2f) / 2000f;
			}
			else
			{
				float num5 = ((num == 1) ? 1 : (-1));
				_textureRect.Rotation = num5 * (base.GlobalPosition.Y - size.Y / 2f) / 1000f;
			}
		}
		else
		{
			_textureRect.Rotation = 0f;
		}
	}
}
