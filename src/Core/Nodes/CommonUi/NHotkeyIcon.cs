using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// The input glyph we render next to buttons when using a gamepad or we're in keyboard-only mode.
/// Handles logic based on our current control scheme.
/// </summary>
public partial class NHotkeyIcon : Control
{
	private TextureRect _controllerIcon;

	private Control _keyboardIcon;

	private MegaLabel _keyboardHotkeyLabel;

	public override void _Ready()
	{
		_controllerIcon = GetNode<TextureRect>("%ControllerIcon");
		_keyboardIcon = GetNode<Control>("%KeyboardIcon");
		_keyboardHotkeyLabel = _keyboardIcon.GetNode<MegaLabel>("%KeyboardLabel");
	}

	public void UpdateInput(string input)
	{
		NControllerManager instance = NControllerManager.Instance;
		if (instance != null)
		{
			Texture2D hotkeyIcon = NInputManager.Instance.GetHotkeyIcon(input);
			if (hotkeyIcon != null)
			{
				_controllerIcon.Texture = hotkeyIcon;
			}
			Key currentHotkey = NInputManager.Instance.GetCurrentHotkey(input);
			switch (currentHotkey)
			{
			case Key.Escape:
				_keyboardHotkeyLabel.Text = "Esc";
				break;
			default:
				_keyboardHotkeyLabel.Text = currentHotkey.ToString();
				break;
			case Key.None:
				break;
			}
			_controllerIcon.Visible = instance.InputType == InputType.Controller;
			_keyboardIcon.Visible = instance.InputType == InputType.KeyboardOnlyMode;
		}
	}
}
