using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

/// <summary>
/// The slots container intercepts inputs and transforms its child node "WhatsMoved" we do this because if this Node is moved too much we can't intercept GUI inputs and such.
/// We could probably bypass this by using _Input oops. Whatever.
/// </summary>
public partial class NSlotsContainer : Control
{
	private Control _whatsMoved;

	private Vector2 _dragStartPosition;

	private Vector2 _targetPosition;

	private bool _isDragging;

	private const float _scrollSpeed = 50f;

	private const float _trackpadScrollSpeed = 20f;

	private const float _bounceBackStrength = 36f;

	private const float _lerpSmoothness = 20f;

	private Tween? _tween;

	private Control _epochSlots;

	public float GetInitX => _whatsMoved.GlobalPosition.X;

	public override void _Ready()
	{
		_whatsMoved = GetNode<Control>("%WhatsMoved");
		_epochSlots = GetNode<Control>("%EpochSlots");
		_targetPosition = _whatsMoved.Position;
		Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnToggleVisibility));
	}

	public override void _EnterTree()
	{
		GetViewport().Connect(Viewport.SignalName.GuiFocusChanged, Callable.From<Control>(ProcessGuiFocus));
	}

	public override void _ExitTree()
	{
		GetViewport().Disconnect(Viewport.SignalName.GuiFocusChanged, Callable.From<Control>(ProcessGuiFocus));
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		ProcessPanEvent(inputEvent);
		ProcessScrollEvent(inputEvent);
	}

	/// <summary>
	/// Processes clicking and dragging the screen horizontally.
	/// </summary>
	/// <param name="inputEvent"></param>
	private void ProcessPanEvent(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				_isDragging = true;
				_dragStartPosition = inputEventMouseButton.Position;
			}
			else
			{
				_isDragging = false;
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _isDragging)
		{
			_targetPosition += new Vector2((inputEventMouseMotion.Position - _dragStartPosition).X, 0f);
			_dragStartPosition = inputEventMouseMotion.Position;
		}
	}

	private void ProcessGuiFocus(Control focusedControl)
	{
		if (IsVisibleInTree() && NControllerManager.Instance.IsUsingController && IsAncestorOf(focusedControl))
		{
			float x = _whatsMoved.GlobalPosition.X - focusedControl.GetParent<Control>().GlobalPosition.X;
			_targetPosition = new Vector2(x, _targetPosition.Y);
		}
	}

	/// <summary>
	/// Processes mouse wheel to zoom in and out of the Timeline screen.
	/// </summary>
	/// <param name="inputEvent"></param>
	private void ProcessScrollEvent(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				_targetPosition -= new Vector2(50f, 0f);
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				_targetPosition += new Vector2(50f, 0f);
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelRight)
			{
				_targetPosition -= new Vector2(50f, 0f);
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelLeft)
			{
				_targetPosition += new Vector2(50f, 0f);
			}
		}
		else if (inputEvent is InputEventPanGesture inputEventPanGesture)
		{
			_targetPosition += new Vector2((0f - inputEventPanGesture.Delta.X) * 20f, 0f);
		}
	}

	public override void _Process(double delta)
	{
		float a = Mathf.Sign(_whatsMoved.Position.X - _targetPosition.X);
		_whatsMoved.Position = _whatsMoved.Position.Lerp(_targetPosition, (float)delta * 20f);
		float b = Mathf.Sign(_whatsMoved.Position.X - _targetPosition.X);
		if (Math.Abs(_whatsMoved.Position.X - _targetPosition.X) < 0.5f || !Mathf.IsEqualApprox(a, b))
		{
			_whatsMoved.Position = _targetPosition;
		}
		if (!_isDragging)
		{
			float num = _targetPosition.X;
			float num2 = _epochSlots.Position.X - _whatsMoved.Size.X;
			float num3 = _epochSlots.Position.X + _epochSlots.Size.X - _whatsMoved.Size.X;
			if (num < num2)
			{
				num = Mathf.Lerp(num, num2, (float)delta * 36f);
			}
			else if (num > num3)
			{
				num = Mathf.Lerp(num, num3, (float)delta * 36f);
			}
			_targetPosition = new Vector2(num, _targetPosition.Y);
		}
	}

	private void OnToggleVisibility()
	{
		if (base.Visible)
		{
			_targetPosition = _whatsMoved.Position;
			_dragStartPosition = Vector2.Zero;
		}
	}

	public void Reset()
	{
		_whatsMoved.Position = new Vector2(-960f, _whatsMoved.Position.Y);
	}

	/// <summary>
	/// When the Timeline is opened, if there are Epochs the game will automatically center to the closest one.
	/// </summary>
	public async Task LerpToSlot(float slotPositionX)
	{
		float num = _whatsMoved.GlobalPosition.X - slotPositionX + 960f - 96f + (base.Size.X - 1920f) * 0.5f;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_whatsMoved, "global_position:x", num, 2.5).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Cubic);
		if (await _tween.AwaitFinished(this))
		{
			_targetPosition = _whatsMoved.Position;
		}
	}

	public void SetEnabled(bool enabled)
	{
		base.FocusBehaviorRecursive = (FocusBehaviorRecursiveEnum)(enabled ? 2 : 1);
		_isDragging = false;
	}
}
