using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

public abstract partial class NSubmenu : Control, IScreenContext
{
	private NBackButton _backButton;

	protected NSubmenuStack _stack;

	/// <summary>
	/// The last control node we were focusing on when this submenu was hidden (usually when another submenu is opened above it)
	/// </summary>
	protected Control? _lastFocusedControl;

	public Control? DefaultFocusedControl => _lastFocusedControl ?? InitialFocusedControl;

	/// <summary>
	/// The initial control the submenu is focused on when it is first opened
	/// </summary>
	protected abstract Control? InitialFocusedControl { get; }

	public override void _Ready()
	{
		if (GetType() != typeof(NSubmenu))
		{
			Log.Error($"{GetType()}");
			throw new InvalidOperationException("Don't call base._Ready()! Call ConnectSignals() instead.");
		}
		ConnectSignals();
	}

	protected virtual void ConnectSignals()
	{
		_backButton = GetNode<NBackButton>("BackButton");
		_backButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			_stack.Pop();
		}));
		_backButton.Disable();
		Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnScreenVisibilityChange));
	}

	/// <summary>
	/// Used to override submenu back button behaviors (see: Timeline)
	/// </summary>
	public void HideBackButtonImmediately()
	{
		_backButton.Disable();
		_backButton.MoveToHidePosition();
	}

	public void SetStack(NSubmenuStack stack)
	{
		_stack = stack;
	}

	private void OnScreenVisibilityChange()
	{
		if (base.Visible)
		{
			_backButton.MoveToHidePosition();
			_backButton.Enable();
			OnSubmenuShown();
		}
		else
		{
			_lastFocusedControl = GetViewport()?.GuiGetFocusOwner();
			_backButton.Disable();
			OnSubmenuHidden();
		}
	}

	/// <summary>
	/// Called when this submenu is newly pushed onto the stack OR when it is re-shown because the submenu above it has
	/// been popped from the stack.
	/// </summary>
	protected virtual void OnSubmenuShown()
	{
	}

	/// <summary>
	/// Called when this submenu has just been popped from the stack OR when it is hidden because a new submenu has just
	/// been pushed onto the stack.
	/// </summary>
	protected virtual void OnSubmenuHidden()
	{
	}

	/// <summary>
	/// Called only when this submenu is newly pushed on to a stack.
	/// </summary>
	public virtual void OnSubmenuOpened()
	{
	}

	/// <summary>
	/// Called only when this submenu is popped from the stack it was on.
	/// </summary>
	public virtual void OnSubmenuClosed()
	{
		_lastFocusedControl = null;
	}
}
