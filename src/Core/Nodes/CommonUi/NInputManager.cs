using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Listens for keyboard and controller inputs, and map them into input events that the UX
/// can listen for (for navigation or hotkeys)
/// That map can be edited to rebind different keyboard/controller inputs to different UX events
/// </summary>
public partial class NInputManager : Node
{
	[Signal]
	public delegate void InputReboundEventHandler();

	private readonly Dictionary<Key, StringName> _debugInputMap = new Dictionary<Key, StringName>
	{
		{
			Key.Key1,
			DebugHotkey.hideTopBar
		},
		{
			Key.Key2,
			DebugHotkey.hideIntents
		},
		{
			Key.Key3,
			DebugHotkey.hideCombatUi
		},
		{
			Key.Key4,
			DebugHotkey.hidePlayContainer
		},
		{
			Key.Key5,
			DebugHotkey.hideHand
		},
		{
			Key.Key6,
			DebugHotkey.hideHpBars
		},
		{
			Key.Key7,
			DebugHotkey.hideTextVfx
		},
		{
			Key.Key8,
			DebugHotkey.hideTargetingUi
		},
		{
			Key.Key9,
			DebugHotkey.slowRewards
		},
		{
			Key.Key0,
			DebugHotkey.hideVersionInfo
		},
		{
			Key.Minus,
			DebugHotkey.speedDown
		},
		{
			Key.Equal,
			DebugHotkey.speedUp
		},
		{
			Key.F1,
			DebugHotkey.hideRestSite
		},
		{
			Key.F3,
			DebugHotkey.hideEventUi
		},
		{
			Key.F4,
			DebugHotkey.hideProceedButton
		},
		{
			Key.F5,
			DebugHotkey.hideHoverTips
		},
		{
			Key.F6,
			DebugHotkey.hideMpCursors
		},
		{
			Key.F7,
			DebugHotkey.hideMpTargeting
		},
		{
			Key.F9,
			DebugHotkey.hideMpIntents
		},
		{
			Key.F10,
			DebugHotkey.hideMpHealthBars
		},
		{
			Key.U,
			DebugHotkey.unlockCharacters
		}
	};

	public static readonly IReadOnlyList<StringName> remappableMKbInputs = new List<StringName>
	{
		MegaInput.cancel,
		MegaInput.viewMap,
		MegaInput.topPanel,
		MegaInput.viewDeckAndTabLeft,
		MegaInput.viewDrawPile,
		MegaInput.viewDiscardPile,
		MegaInput.viewExhaustPileAndTabRight,
		MegaInput.confirm,
		MegaInput.endTurn,
		MegaInput.peek,
		MegaInput.up,
		MegaInput.down,
		MegaInput.left,
		MegaInput.right,
		MegaInput.selectCard1,
		MegaInput.selectCard2,
		MegaInput.selectCard3,
		MegaInput.selectCard4,
		MegaInput.selectCard5,
		MegaInput.selectCard6,
		MegaInput.selectCard7,
		MegaInput.selectCard8,
		MegaInput.selectCard9,
		MegaInput.selectCard10
	};

	public static readonly IReadOnlyList<StringName> remappableKbOnlyInputs = new List<StringName>
	{
		MegaInput.select,
		MegaInput.cancel,
		MegaInput.viewMap,
		MegaInput.topPanel,
		MegaInput.viewDeckAndTabLeft,
		MegaInput.viewDrawPile,
		MegaInput.viewDiscardPile,
		MegaInput.viewExhaustPileAndTabRight,
		MegaInput.confirm,
		MegaInput.endTurn,
		MegaInput.peek,
		MegaInput.up,
		MegaInput.down,
		MegaInput.left,
		MegaInput.right,
		MegaInput.selectCard1,
		MegaInput.selectCard2,
		MegaInput.selectCard3,
		MegaInput.selectCard4,
		MegaInput.selectCard5,
		MegaInput.selectCard6,
		MegaInput.selectCard7,
		MegaInput.selectCard8,
		MegaInput.selectCard9,
		MegaInput.selectCard10
	};

	public static readonly IReadOnlyList<StringName> remappableControllerInputs = new List<StringName>
	{
		MegaInput.select,
		MegaInput.cancel,
		MegaInput.viewMap,
		MegaInput.topPanel,
		MegaInput.viewDeckAndTabLeft,
		MegaInput.viewDrawPile,
		MegaInput.viewDiscardPile,
		MegaInput.viewExhaustPileAndTabRight,
		MegaInput.confirm,
		MegaInput.endTurn,
		MegaInput.peek,
		MegaInput.up,
		MegaInput.down,
		MegaInput.left,
		MegaInput.right
	};

	private Dictionary<StringName, Key> _mKbInputMap = new Dictionary<StringName, Key>();

	private Dictionary<StringName, StringName> _controllerInputMap = new Dictionary<StringName, StringName>();

	private Dictionary<StringName, Key> _fKbInputMap = new Dictionary<StringName, Key>();

	public static NInputManager? Instance
	{
		get
		{
			if (NGame.Instance == null)
			{
				return null;
			}
			return NGame.Instance.InputManager;
		}
	}

	private static Dictionary<StringName, Key> DefaultHotkeyInputMap => new Dictionary<StringName, Key>
	{
		{
			MegaInput.endTurn,
			Key.E
		},
		{
			MegaInput.confirm,
			Key.Enter
		},
		{
			MegaInput.viewDiscardPile,
			Key.S
		},
		{
			MegaInput.viewDeckAndTabLeft,
			Key.D
		},
		{
			MegaInput.viewExhaustPileAndTabRight,
			Key.X
		},
		{
			MegaInput.viewDrawPile,
			Key.A
		},
		{
			MegaInput.viewMap,
			Key.M
		},
		{
			MegaInput.cancel,
			Key.Escape
		},
		{
			MegaInput.peek,
			Key.Space
		},
		{
			MegaInput.up,
			Key.Up
		},
		{
			MegaInput.down,
			Key.Down
		},
		{
			MegaInput.left,
			Key.Left
		},
		{
			MegaInput.right,
			Key.Right
		},
		{
			MegaInput.pauseAndBack,
			Key.Escape
		},
		{
			MegaInput.selectCard1,
			Key.Key1
		},
		{
			MegaInput.selectCard2,
			Key.Key2
		},
		{
			MegaInput.selectCard3,
			Key.Key3
		},
		{
			MegaInput.selectCard4,
			Key.Key4
		},
		{
			MegaInput.selectCard5,
			Key.Key5
		},
		{
			MegaInput.selectCard6,
			Key.Key6
		},
		{
			MegaInput.selectCard7,
			Key.Key7
		},
		{
			MegaInput.selectCard8,
			Key.Key8
		},
		{
			MegaInput.selectCard9,
			Key.Key9
		},
		{
			MegaInput.selectCard10,
			Key.Key0
		}
	};

	private static Dictionary<StringName, Key> DefaultKbOnlyInputMap => new Dictionary<StringName, Key>
	{
		{
			MegaInput.confirm,
			Key.Enter
		},
		{
			MegaInput.endTurn,
			Key.E
		},
		{
			MegaInput.select,
			Key.Space
		},
		{
			MegaInput.viewDiscardPile,
			Key.D
		},
		{
			MegaInput.viewDeckAndTabLeft,
			Key.Q
		},
		{
			MegaInput.viewExhaustPileAndTabRight,
			Key.F
		},
		{
			MegaInput.viewDrawPile,
			Key.A
		},
		{
			MegaInput.viewMap,
			Key.Tab
		},
		{
			MegaInput.cancel,
			Key.Escape
		},
		{
			MegaInput.peek,
			Key.S
		},
		{
			MegaInput.up,
			Key.Up
		},
		{
			MegaInput.down,
			Key.Down
		},
		{
			MegaInput.left,
			Key.Left
		},
		{
			MegaInput.right,
			Key.Right
		},
		{
			MegaInput.pauseAndBack,
			Key.Escape
		},
		{
			MegaInput.selectCard1,
			Key.Key1
		},
		{
			MegaInput.selectCard2,
			Key.Key2
		},
		{
			MegaInput.selectCard3,
			Key.Key3
		},
		{
			MegaInput.selectCard4,
			Key.Key4
		},
		{
			MegaInput.selectCard5,
			Key.Key5
		},
		{
			MegaInput.selectCard6,
			Key.Key6
		},
		{
			MegaInput.selectCard7,
			Key.Key7
		},
		{
			MegaInput.selectCard8,
			Key.Key8
		},
		{
			MegaInput.selectCard9,
			Key.Key9
		},
		{
			MegaInput.selectCard10,
			Key.Key0
		},
		{
			MegaInput.topPanel,
			Key.W
		}
	};

	public NControllerManager ControllerManager { get; private set; }

	public override void _EnterTree()
	{
		ControllerManager = GetNode<NControllerManager>("%ControllerManager");
	}

	public override void _Ready()
	{
		ControllerManager.Connect(NControllerManager.SignalName.ControllerTypeChanged, Callable.From(OnControllerTypeChanged));
		TaskHelper.RunSafely(Init());
	}

	private async Task Init()
	{
		await ControllerManager.Init();
		SettingsSave settingsSave = SaveManager.Instance.SettingsSave;
		if (settingsSave.KeyboardMapping.Count > 0)
		{
			Dictionary<StringName, Key> defaultHotkeyInputMap = DefaultHotkeyInputMap;
			_mKbInputMap = new Dictionary<StringName, Key>(defaultHotkeyInputMap);
			foreach (KeyValuePair<string, string> item in settingsSave.KeyboardMapping)
			{
				if (Enum.TryParse<Key>(item.Value, out var result))
				{
					_mKbInputMap[item.Key] = result;
				}
			}
		}
		else
		{
			_mKbInputMap = DefaultHotkeyInputMap;
			SaveMKbInputMapping();
		}
		if (settingsSave.KbOnlyMapping.Count > 0)
		{
			Dictionary<StringName, Key> defaultKbOnlyInputMap = DefaultKbOnlyInputMap;
			_fKbInputMap = new Dictionary<StringName, Key>(defaultKbOnlyInputMap);
			foreach (KeyValuePair<string, string> item2 in settingsSave.KbOnlyMapping)
			{
				if (Enum.TryParse<Key>(item2.Value, out var result2))
				{
					_fKbInputMap[item2.Key] = result2;
				}
			}
		}
		else
		{
			_fKbInputMap = DefaultKbOnlyInputMap;
			SaveFKbInputMapping();
		}
		if (settingsSave.ControllerMapping.Count > 0 && settingsSave.ControllerMappingType == ControllerManager.ControllerMappingType)
		{
			_controllerInputMap = MergeSavedControllerBindings(ControllerManager.GetDefaultControllerInputMap, settingsSave.ControllerMapping);
			return;
		}
		_controllerInputMap = ControllerManager.GetDefaultControllerInputMap;
		SaveControllerInputMapping();
	}

	/// <summary>
	/// Overlays a saved controller mapping onto the defaults, ignoring any saved binding whose
	/// value is not a registered InputMap action. A binding can point at a missing action when a
	/// save was migrated forward by a newer build and then loaded by an older one, or when a save
	/// was hand-edited. Applying it would make <see cref="M:MegaCrit.Sts2.Core.Nodes.CommonUi.NInputManager._UnhandledInput(Godot.InputEvent)" /> call
	/// <c>IsActionPressed</c> on a nonexistent action and spam errors, so the default is kept.
	/// </summary>
	public static Dictionary<StringName, StringName> MergeSavedControllerBindings(Dictionary<StringName, StringName> defaults, Dictionary<string, string> savedMapping)
	{
		Dictionary<StringName, StringName> dictionary = new Dictionary<StringName, StringName>(defaults);
		foreach (KeyValuePair<string, string> item in savedMapping)
		{
			if (InputMap.HasAction(item.Value))
			{
				dictionary[item.Key] = item.Value;
			}
		}
		return dictionary;
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (ControllerManager.InputType == InputType.KeyboardOnlyMode)
		{
			ProcessFkbInput(inputEvent);
		}
		else
		{
			ProcessHotkeyInput(inputEvent);
		}
		ProcessDebugKeyInput(inputEvent);
	}

	private void ProcessDebugKeyInput(InputEvent inputEvent)
	{
		if (!(inputEvent is InputEventKey inputEventKey) || PlatformUtil.IsPlatformOverlayOpen() || !DisplayServer.WindowIsFocused() || NDevConsole.IsConsoleVisible || !NGame.IsTrailerMode)
		{
			return;
		}
		foreach (KeyValuePair<Key, StringName> item in _debugInputMap)
		{
			if (inputEventKey.Keycode == item.Key)
			{
				InputEventAction inputEventAction = new InputEventAction
				{
					Action = item.Value,
					Pressed = inputEvent.IsPressed()
				};
				Input.ParseInputEvent(inputEventAction);
			}
		}
	}

	private void ProcessHotkeyInput(InputEvent inputEvent)
	{
		if (NGame.Instance.Transition.InTransition || !NGame.IsGameFocusedWindow() || !(inputEvent is InputEventKey inputEventKey))
		{
			return;
		}
		foreach (KeyValuePair<StringName, Key> item in _mKbInputMap)
		{
			if (inputEventKey.Keycode == item.Value && !inputEvent.IsEcho())
			{
				InputEventAction inputEventAction = new InputEventAction
				{
					Action = item.Key,
					Pressed = inputEvent.IsPressed()
				};
				Input.ParseInputEvent(inputEventAction);
			}
		}
	}

	private void ProcessFkbInput(InputEvent inputEvent)
	{
		if (NGame.Instance.Transition.InTransition || !NGame.IsGameFocusedWindow() || !(inputEvent is InputEventKey inputEventKey))
		{
			return;
		}
		foreach (KeyValuePair<StringName, Key> item in _fKbInputMap)
		{
			if (inputEventKey.Keycode == item.Value && !inputEvent.IsEcho())
			{
				InputEventAction inputEventAction = new InputEventAction
				{
					Action = item.Key,
					Pressed = inputEvent.IsPressed()
				};
				Input.ParseInputEvent(inputEventAction);
			}
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (NGame.Instance.Transition.InTransition || !NGame.IsGameFocusedWindow())
		{
			return;
		}
		foreach (KeyValuePair<StringName, StringName> item in _controllerInputMap)
		{
			if (inputEvent.IsActionPressed(item.Value))
			{
				InputEventAction inputEventAction = new InputEventAction
				{
					Action = item.Key,
					Pressed = true
				};
				Input.ParseInputEvent(inputEventAction);
			}
			else if (inputEvent.IsActionReleased(item.Value))
			{
				InputEventAction inputEventAction2 = new InputEventAction
				{
					Action = item.Key,
					Pressed = false
				};
				Input.ParseInputEvent(inputEventAction2);
			}
		}
	}

	public Key GetCurrentHotkey(StringName input)
	{
		if (ControllerManager.InputType != InputType.KeyboardOnlyMode)
		{
			return GetMKbHotkey(input);
		}
		return GetKbOnlyHotkey(input);
	}

	public Key GetMKbHotkey(StringName input)
	{
		if (!_mKbInputMap.TryGetValue(input, out var value))
		{
			return Key.None;
		}
		return value;
	}

	public Key GetKbOnlyHotkey(StringName input)
	{
		if (!_fKbInputMap.TryGetValue(input, out var value))
		{
			return Key.None;
		}
		return value;
	}

	public Texture2D? GetHotkeyIcon(string hotkey)
	{
		if (_controllerInputMap.TryGetValue(hotkey, out StringName value))
		{
			return ControllerManager.GetHotkeyIcon(value);
		}
		return null;
	}

	public void ModifyMKbKey(StringName input, Key shortcutKey)
	{
		KeyValuePair<StringName, Key> keyValuePair = _mKbInputMap.FirstOrDefault<KeyValuePair<StringName, Key>>((KeyValuePair<StringName, Key> kvp) => kvp.Value == shortcutKey && remappableMKbInputs.Contains(kvp.Key));
		if (keyValuePair.Key != null)
		{
			Key value = _mKbInputMap[input];
			_mKbInputMap[keyValuePair.Key] = value;
		}
		_mKbInputMap[input] = shortcutKey;
		SaveMKbInputMapping();
		EmitSignalInputRebound();
	}

	public void ModifyKbOnlyKey(StringName input, Key shortcutKey)
	{
		KeyValuePair<StringName, Key> keyValuePair = _fKbInputMap.FirstOrDefault<KeyValuePair<StringName, Key>>((KeyValuePair<StringName, Key> kvp) => kvp.Value == shortcutKey && remappableKbOnlyInputs.Contains(kvp.Key));
		if (keyValuePair.Key != null)
		{
			Key value = _fKbInputMap[input];
			_fKbInputMap[keyValuePair.Key] = value;
		}
		_fKbInputMap[input] = shortcutKey;
		SaveFKbInputMapping();
		EmitSignalInputRebound();
	}

	public void ModifyControllerButton(StringName input, StringName controllerInput)
	{
		KeyValuePair<StringName, StringName> keyValuePair = _controllerInputMap.FirstOrDefault<KeyValuePair<StringName, StringName>>((KeyValuePair<StringName, StringName> kvp) => kvp.Value == controllerInput && remappableControllerInputs.Contains(kvp.Key));
		if (keyValuePair.Key != null)
		{
			StringName value = _controllerInputMap[input];
			_controllerInputMap[keyValuePair.Key] = value;
		}
		_controllerInputMap[input] = controllerInput;
		if (input == MegaInput.confirm)
		{
			_controllerInputMap[MegaInput.endTurn] = controllerInput;
		}
		else if (input == MegaInput.endTurn)
		{
			_controllerInputMap[MegaInput.confirm] = controllerInput;
		}
		SaveControllerInputMapping();
		EmitSignalInputRebound();
	}

	public void ResetToDefaults()
	{
		_mKbInputMap = DefaultHotkeyInputMap;
		_controllerInputMap = ControllerManager.GetDefaultControllerInputMap;
		_fKbInputMap = DefaultKbOnlyInputMap;
		SaveControllerInputMapping();
		SaveMKbInputMapping();
		SaveFKbInputMapping();
		EmitSignalInputRebound();
	}

	private void OnControllerTypeChanged()
	{
		if (ControllerManager.ControllerMappingType != SaveManager.Instance.SettingsSave.ControllerMappingType)
		{
			_controllerInputMap = ControllerManager.GetDefaultControllerInputMap;
			SaveControllerInputMapping();
			EmitSignalInputRebound();
		}
	}

	private void SaveControllerInputMapping()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<StringName, StringName> item in _controllerInputMap)
		{
			dictionary.Add(item.Key.ToString(), item.Value.ToString());
		}
		SaveManager.Instance.SettingsSave.ControllerMappingType = ControllerManager.ControllerMappingType;
		SaveManager.Instance.SettingsSave.ControllerMapping = dictionary;
		SaveManager.Instance.SaveSettings();
	}

	private void SaveMKbInputMapping()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<StringName, Key> item in _mKbInputMap)
		{
			dictionary.Add(item.Key.ToString(), item.Value.ToString());
		}
		SaveManager.Instance.SettingsSave.KeyboardMapping = dictionary;
		SaveManager.Instance.SaveSettings();
	}

	private void SaveFKbInputMapping()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<StringName, Key> item in _fKbInputMap)
		{
			dictionary.Add(item.Key.ToString(), item.Value.ToString());
		}
		SaveManager.Instance.SettingsSave.KbOnlyMapping = dictionary;
		SaveManager.Instance.SaveSettings();
	}
}
