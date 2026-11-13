using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Nodes.GodotExtensions;

/// <summary>
/// A base TextEdit class that is used to access the virtual keyboard
/// for various devices/apis (ie Steam Deck)
/// </summary>
public partial class NMegaTextEdit : Control
{
	private NSelectionReticle? _selectionReticle;

	private bool _isEditing;

	public TextEdit TextEdit { get; set; }

	/// <summary>
	/// This mirrors LineEdit.IsEditing()
	/// </summary>
	public bool IsEditing()
	{
		return _isEditing;
	}

	public override void _Ready()
	{
		TextEdit = GetNode<TextEdit>("TextEdit");
		if (HasNode("SelectionReticle"))
		{
			_selectionReticle = GetNode<NSelectionReticle>("SelectionReticle");
		}
		RefreshFont();
		Connect(Control.SignalName.FocusEntered, Callable.From(OnFocus));
		Connect(Control.SignalName.FocusExited, Callable.From(OnUnfocus));
		Connect(Control.SignalName.MouseEntered, Callable.From(OnFocus));
		Connect(Control.SignalName.MouseExited, Callable.From(OnUnfocus));
		TextEdit.Connect(Control.SignalName.FocusExited, Callable.From(StopEditing));
	}

	public void RefreshFont()
	{
		TextEdit.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.TextEdit.Font);
	}

	private void OnFocus()
	{
		if (NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_selectionReticle?.OnSelect();
		}
	}

	private void OnUnfocus()
	{
		_selectionReticle?.OnDeselect();
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		base._GuiInput(inputEvent);
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.IsPressed())
		{
			OpenKeyboard();
		}
		if (inputEvent.IsActionPressed(MegaInput.select))
		{
			OpenKeyboard();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionReleased(MegaInput.cancel) && IsEditing())
		{
			StopEditing();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void OpenKeyboard()
	{
		if (!_isEditing)
		{
			_isEditing = true;
			TextEdit.TryGrabFocus();
			PlatformUtil.OpenVirtualKeyboard();
			NHotkeyManager.Instance?.AddBlockingScreen(this);
			if (NControllerManager.Instance.IsUsingDirectionalNavigation)
			{
				_selectionReticle?.OnSelect();
			}
		}
	}

	private void StopEditing()
	{
		if (_isEditing)
		{
			_isEditing = false;
			_selectionReticle?.OnDeselect();
			PlatformUtil.CloseVirtualKeyboard();
			NHotkeyManager.Instance?.RemoveBlockingScreen(this);
			this.TryGrabFocus();
		}
	}
}
