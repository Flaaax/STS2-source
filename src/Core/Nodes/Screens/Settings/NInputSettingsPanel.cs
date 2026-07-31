using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

public partial class NInputSettingsPanel : NSettingsPanel
{
	private float _minPadding = 50f;

	private NInputSettingsEntry? _listeningEntry;

	private MegaRichTextLabel _kbModeHeader;

	private NTickbox _kbModeTickbox;

	private MegaRichTextLabel _steamInputPrompt;

	private NButton _resetToDefaultButton;

	private MegaLabel _resetLabel;

	private MegaLabel _commandHeader;

	private MegaLabel _mkbHeader;

	private MegaLabel _keyboardHeader;

	private MegaLabel _controllerHeader;

	private Control _listeningPrompt;

	private MegaRichTextLabel _listeningLabel;

	/// <summary>
	/// Nodes are initialized top down based on what you see on the screen.
	/// </summary>
	public override void _Ready()
	{
		base._Ready();
		GetViewport().Connect(Viewport.SignalName.SizeChanged, Callable.From(OnViewportSizeChange));
		_kbModeHeader = GetNode<MegaRichTextLabel>("%KeyboardOnlyModeHeader");
		_kbModeHeader.SetTextAutoSize(new LocString("settings_ui", "KEYBOARD_ONLY_MODE_HEADER").GetFormattedText());
		_kbModeTickbox = GetNode<NTickbox>("%KeyboardOnlyModeTickbox");
		_steamInputPrompt = GetNode<MegaRichTextLabel>("%SteamInputPrompt");
		_steamInputPrompt.SetTextAutoSize((!NControllerManager.Instance.ShouldAllowControllerRebinding) ? new LocString("settings_ui", "INPUT_SETTINGS.STEAM_INPUT_DETECTED").GetFormattedText() : new LocString("settings_ui", "INPUT_SETTINGS.STEAM_INPUT_NOT_DETECTED").GetFormattedText());
		_resetToDefaultButton = GetNode<NButton>("%ResetToDefaultButton");
		_resetToDefaultButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(delegate
		{
			NInputManager.Instance.ResetToDefaults();
		}));
		_resetLabel = GetNode<MegaLabel>("%ResetLabel");
		_resetLabel.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.RESET_TO_DEFAULT").GetRawText());
		_commandHeader = GetNode<MegaLabel>("%CommandHeader");
		_mkbHeader = GetNode<MegaLabel>("%MKbHeader");
		_keyboardHeader = GetNode<MegaLabel>("%KbModeHeader");
		_controllerHeader = GetNode<MegaLabel>("%ControllerHeader");
		_commandHeader.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.COMMAND_HEADER").GetFormattedText());
		_keyboardHeader.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.KEYBOARD_ONLY_MODE_HEADER").GetFormattedText());
		_mkbHeader.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.MOUSE_KEYBOARD_HEADER").GetFormattedText());
		_controllerHeader.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.CONTROLLER_HEADER").GetFormattedText());
		_listeningPrompt = GetNode<Control>("%ListeningPrompt");
		_listeningLabel = GetNode<MegaRichTextLabel>("%ListeningLabel");
		_listeningLabel.SetTextAutoSize("[sine]" + new LocString("settings_ui", "LISTENING_INPUT").GetRawText() + "[/sine]");
		IReadOnlyList<StringName> readOnlyList = NInputManager.remappableControllerInputs.Concat(NInputManager.remappableMKbInputs).Distinct().ToList();
		foreach (StringName item in readOnlyList)
		{
			NInputSettingsEntry entry = NInputSettingsEntry.Create(item);
			entry.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(delegate
			{
				SetAsListeningEntry(entry);
			}));
			base.Content.AddChildSafely(entry);
		}
		UpdateNavigation();
	}

	private async Task RefreshSize()
	{
		await this.AwaitProcessFrame();
		await this.AwaitProcessFrame();
		Vector2 size = GetParent<Control>().Size;
		Vector2 minimumSize = base.Content.GetMinimumSize();
		if (minimumSize.Y + _minPadding >= size.Y)
		{
			base.Size = new Vector2(base.Content.Size.X, minimumSize.Y + size.Y * 0.4f);
		}
	}

	private void OnViewportSizeChange()
	{
		TaskHelper.RunSafely(RefreshSize());
	}

	protected override void OnVisibilityChange()
	{
		base.OnVisibilityChange();
		_listeningEntry = null;
		_listeningPrompt.Visible = false;
		NControllerManager.Instance.StopListeningForRebind();
		TaskHelper.RunSafely(RefreshSize());
	}

	public override void _ExitTree()
	{
		NControllerManager.Instance.StopListeningForRebind();
	}

	private void SetAsListeningEntry(NInputSettingsEntry entry)
	{
		_listeningEntry = entry;
		_listeningPrompt.Visible = true;
		_listeningPrompt.SetGlobalPosition(new Vector2(960f, 540f) - _listeningPrompt.Size * 0.5f);
		NControllerManager.Instance.StartListeningForRebind();
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (_listeningEntry == null || !(inputEvent is InputEventKey inputEventKey))
		{
			return;
		}
		if (NControllerManager.Instance.InputType == InputType.Controller)
		{
			GetViewport()?.SetInputAsHandled();
			return;
		}
		if (NControllerManager.Instance.InputType == InputType.KeyboardOnlyMode && NInputManager.remappableKbOnlyInputs.Contains(_listeningEntry.InputName))
		{
			NInputManager.Instance.ModifyKbOnlyKey(_listeningEntry.InputName, inputEventKey.Keycode);
		}
		else if (NInputManager.remappableMKbInputs.Contains(_listeningEntry.InputName))
		{
			NInputManager.Instance.ModifyMKbKey(_listeningEntry.InputName, inputEventKey.Keycode);
		}
		GetViewport()?.SetInputAsHandled();
		_listeningPrompt.Visible = false;
		NControllerManager.Instance.StopListeningForRebind();
		_listeningEntry = null;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (_listeningEntry == null)
		{
			return;
		}
		StringName[] allControllerInputs = Controller.AllControllerInputs;
		foreach (StringName stringName in allControllerInputs)
		{
			if (inputEvent.IsActionReleased(stringName))
			{
				if (NInputManager.remappableControllerInputs.Contains(_listeningEntry.InputName) && NControllerManager.Instance.ShouldAllowControllerRebinding)
				{
					NInputManager.Instance.ModifyControllerButton(_listeningEntry.InputName, stringName);
				}
				GetViewport()?.SetInputAsHandled();
				_listeningPrompt.Visible = false;
				NControllerManager.Instance.StopListeningForRebind();
				_listeningEntry = null;
				break;
			}
		}
	}
}
