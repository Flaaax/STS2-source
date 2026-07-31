using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

/// <summary>
/// Represents a single input action and its corresponding hotkey to activate it.
/// Can be clicked on to rebind the key.
/// </summary>
public partial class NInputSettingsEntry : NButton
{
	private static readonly Dictionary<StringName, string> _commandToLocTitle = new Dictionary<StringName, string>
	{
		{
			MegaInput.confirm,
			"confirm"
		},
		{
			MegaInput.endTurn,
			"endTurn"
		},
		{
			MegaInput.select,
			"select"
		},
		{
			MegaInput.viewDiscardPile,
			"viewDiscard"
		},
		{
			MegaInput.viewDrawPile,
			"viewDraw"
		},
		{
			MegaInput.viewDeckAndTabLeft,
			"viewDeck"
		},
		{
			MegaInput.viewExhaustPileAndTabRight,
			"viewExhaust"
		},
		{
			MegaInput.viewMap,
			"viewMap"
		},
		{
			MegaInput.cancel,
			"cancel"
		},
		{
			MegaInput.peek,
			"peek"
		},
		{
			MegaInput.up,
			"up"
		},
		{
			MegaInput.topPanel,
			"topPanel"
		},
		{
			MegaInput.down,
			"down"
		},
		{
			MegaInput.left,
			"left"
		},
		{
			MegaInput.right,
			"right"
		},
		{
			MegaInput.selectCard1,
			"selectCard1"
		},
		{
			MegaInput.selectCard2,
			"selectCard2"
		},
		{
			MegaInput.selectCard3,
			"selectCard3"
		},
		{
			MegaInput.selectCard4,
			"selectCard4"
		},
		{
			MegaInput.selectCard5,
			"selectCard5"
		},
		{
			MegaInput.selectCard6,
			"selectCard6"
		},
		{
			MegaInput.selectCard7,
			"selectCard7"
		},
		{
			MegaInput.selectCard8,
			"selectCard8"
		},
		{
			MegaInput.selectCard9,
			"selectCard9"
		},
		{
			MegaInput.selectCard10,
			"selectCard10"
		}
	};

	private const string _scenePath = "res://scenes/screens/settings_screen/input_settings_entry.tscn";

	private Control _bg;

	private MegaLabel _inputLabel;

	private MegaLabel _mKbBindingLabel;

	private MegaLabel _keyboardOnlyModeBindingLabel;

	private Control _missingControllerBindingLabel;

	private TextureRect _controllerBindingIcon;

	private Tween? _tween;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>("res://scenes/screens/settings_screen/input_settings_entry.tscn");

	public StringName InputName { get; private set; }

	public static NInputSettingsEntry Create(string commandName)
	{
		NInputSettingsEntry nInputSettingsEntry = ResourceLoader.Load<PackedScene>("res://scenes/screens/settings_screen/input_settings_entry.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<NInputSettingsEntry>(PackedScene.GenEditState.Disabled);
		nInputSettingsEntry.InputName = commandName;
		return nInputSettingsEntry;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_inputLabel = GetNode<MegaLabel>("%InputLabel");
		_mKbBindingLabel = GetNode<MegaLabel>("%MKbBindingInputLabel");
		_keyboardOnlyModeBindingLabel = GetNode<MegaLabel>("%KbModeBindingInputLabel");
		_controllerBindingIcon = GetNode<TextureRect>("%ControllerBindingIcon");
		_missingControllerBindingLabel = GetNode<Control>("%MissingControllerBindingLabel");
		_bg = GetNode<Control>("%Bg");
		string text = _commandToLocTitle[InputName];
		_inputLabel.SetTextAutoSize(new LocString("settings_ui", "INPUT_SETTINGS.INPUT_TITLE." + text).GetFormattedText());
		NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdateInput));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateInput));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateInput));
		Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(UpdateInput));
	}

	private void UpdateInput()
	{
		if (IsVisibleInTree())
		{
			if (NInputManager.remappableMKbInputs.Contains(InputName))
			{
				Key mKbHotkey = NInputManager.Instance.GetMKbHotkey(InputName);
				_mKbBindingLabel.Text = ((mKbHotkey != Key.None) ? mKbHotkey.ToString() : "-");
			}
			else
			{
				_mKbBindingLabel.Text = "-";
			}
			if (NInputManager.remappableKbOnlyInputs.Contains(InputName))
			{
				Key kbOnlyHotkey = NInputManager.Instance.GetKbOnlyHotkey(InputName);
				_keyboardOnlyModeBindingLabel.Text = ((kbOnlyHotkey != Key.None) ? kbOnlyHotkey.ToString() : "-");
			}
			else
			{
				_keyboardOnlyModeBindingLabel.Text = "-";
			}
			_mKbBindingLabel.SelfModulate = ((_mKbBindingLabel.Text == "-") ? StsColors.gray : Colors.White);
			_keyboardOnlyModeBindingLabel.SelfModulate = ((_keyboardOnlyModeBindingLabel.Text == "-") ? StsColors.gray : Colors.White);
			if (NInputManager.remappableControllerInputs.Contains(InputName))
			{
				_controllerBindingIcon.Texture = NInputManager.Instance.GetHotkeyIcon(InputName);
				_missingControllerBindingLabel.Visible = false;
			}
			else
			{
				_missingControllerBindingLabel.Visible = true;
			}
			if (!NControllerManager.Instance.ShouldAllowControllerRebinding)
			{
				_controllerBindingIcon.Modulate = StsColors.disabledRed;
			}
			else if (InputName == MegaInput.endTurn)
			{
				_controllerBindingIcon.Modulate = new Color(0.2f, 0.2f, 0.2f);
			}
			else
			{
				_controllerBindingIcon.Modulate = Colors.White;
			}
			_mKbBindingLabel.Modulate = ((NControllerManager.Instance.InputType == InputType.MouseAndKeyboard) ? Colors.White : StsColors.disabledRed);
			_keyboardOnlyModeBindingLabel.Modulate = ((NControllerManager.Instance.InputType == InputType.KeyboardOnlyMode) ? Colors.White : StsColors.disabledRed);
		}
	}

	protected override void OnFocus()
	{
		_tween?.Kill();
		Control bg = _bg;
		Color modulate = _bg.Modulate;
		modulate.A = 0.2f;
		bg.Modulate = modulate;
	}

	protected override void OnUnfocus()
	{
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_bg, "modulate:a", 0f, 0.1);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		NInputManager.Instance.Disconnect(NInputManager.SignalName.InputRebound, Callable.From(UpdateInput));
		NControllerManager.Instance.Disconnect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateInput));
		NControllerManager.Instance.Disconnect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateInput));
		Disconnect(CanvasItem.SignalName.VisibilityChanged, Callable.From(UpdateInput));
	}
}
