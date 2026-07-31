using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.ControllerInput.ControllerConfigs;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

public partial class NControllerManager : Node
{
	[Signal]
	public delegate void ControllerDetectedEventHandler();

	[Signal]
	public delegate void MouseDetectedEventHandler();

	/// <summary>
	/// Fires when we detect that the controller type has changed (ie xbox to ps4).
	/// </summary>
	[Signal]
	public delegate void ControllerTypeChangedEventHandler();

	private IControllerInputStrategy? _inputStrategy;

	/// <summary>
	/// The position we warp the mouse to when we switch to controller mode. This is so it no
	/// longer hovers over the last control it ws positioned at
	/// </summary>
	private static readonly Vector2 _offscreenPos = Vector2.One * -1000f;

	/// <summary>
	/// Used to reset the mouse position to the last place it was before we swapped to controller mode
	/// </summary>
	private Vector2 _lastMousePosition;

	/// <summary>
	/// Number of frames to ignore mouse motion events after warping the cursor offscreen.
	/// WarpMouse generates a synthetic InputEventMouseMotion (via OS event queue, arriving next
	/// frame) that would otherwise immediately flip us back to mouse mode.
	/// </summary>
	private int _skipMouseCheckFrames;

	/// <summary>
	/// Minimum relative displacement (squared) to consider a mouse motion event as a warp artifact
	/// rather than real user input. No human mouse movement covers 500+ pixels in a single frame.
	/// </summary>
	private const float _warpDisplacementThresholdSq = 250000f;

	private MegaLabel _label;

	private Tween? _notifyTween;

	/// <summary>
	/// Make sure you know what you are doing when using this.
	/// used to disable switching between InputTypes while we are listening for inputs to rebind them
	/// </summary>
	private bool _inputTypeCheckingDisabled;

	public static NControllerManager? Instance
	{
		get
		{
			if (NGame.Instance == null)
			{
				return null;
			}
			return NGame.Instance.InputManager.ControllerManager;
		}
	}

	public bool ShouldAllowControllerRebinding => _inputStrategy?.ShouldAllowControllerRebinding ?? true;

	public bool ShouldShowInputGlyphs
	{
		get
		{
			InputType inputType = InputType;
			if ((uint)(inputType - 1) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public InputType InputType { get; private set; }

	public bool IsUsingDirectionalNavigation
	{
		get
		{
			InputType inputType = InputType;
			if ((uint)(inputType - 1) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public Dictionary<StringName, StringName> GetDefaultControllerInputMap
	{
		get
		{
			if (_inputStrategy == null)
			{
				return new SteamControllerConfig().DefaultControllerInputMap;
			}
			return _inputStrategy.GetDefaultControllerInputMap;
		}
	}

	public ControllerMappingType ControllerMappingType
	{
		get
		{
			if (_inputStrategy == null)
			{
				return ControllerMappingType.Default;
			}
			return _inputStrategy.ControllerConfig.ControllerMappingType;
		}
	}

	public async Task Init()
	{
		ActiveScreenContext.Instance.Updated += OnScreenContextChanged;
		_label = GetNode<MegaLabel>("Label");
		_label.Modulate = Colors.Transparent;
		_inputStrategy = new SteamControllerInputStrategy();
		await _inputStrategy.Init();
	}

	public override void _ExitTree()
	{
		ActiveScreenContext.Instance.Updated -= OnScreenContextChanged;
	}

	public override void _Process(double delta)
	{
		if (NGame.IsGameFocusedWindow())
		{
			_inputStrategy?.ProcessInput();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!_inputTypeCheckingDisabled)
		{
			if (InputType != InputType.Controller)
			{
				CheckForControllerInput(inputEvent);
			}
			if (InputType != InputType.MouseAndKeyboard)
			{
				CheckForMouseInput(inputEvent);
			}
			if (InputType != InputType.KeyboardOnlyMode)
			{
				CheckForArrowKeyInput(inputEvent);
			}
		}
	}

	public void OnControllerTypeChanged()
	{
		EmitSignalControllerTypeChanged();
	}

	/// <summary>
	/// Checks if the input event is from a mouse and notifies the ui that we are now using mouse input
	/// </summary>
	/// <param name="inputEvent"></param>
	private void CheckForMouseInput(InputEvent inputEvent)
	{
		bool flag = inputEvent is InputEventMouseButton;
		bool flag2 = inputEvent is InputEventMouseMotion { Velocity: var velocity } inputEventMouseMotion && velocity.LengthSquared() > 100f && _skipMouseCheckFrames <= 0 && inputEventMouseMotion.Relative.LengthSquared() <= 250000f;
		if (flag || flag2)
		{
			SwitchToMouseMode();
		}
	}

	/// <summary>
	/// Checks if the input event is from a controller and notifies the ui that we are now using controller input
	/// </summary>
	/// <param name="inputEvent"></param>
	private void CheckForControllerInput(InputEvent inputEvent)
	{
		if (NGame.IsGameFocusedWindow() && Controller.AllControllerInputs.Any((StringName i) => inputEvent.IsActionPressed(i)))
		{
			InputType = InputType.Controller;
			Viewport viewport = GetViewport();
			NGame.Instance?.SetMouseBehaviorRecursive(Control.MouseBehaviorRecursiveEnum.Disabled);
			ActiveScreenContext.Instance.FocusOnDefaultControl();
			EmitSignal(SignalName.ControllerDetected);
			ControlModeChanged();
			viewport?.SetInputAsHandled();
		}
	}

	private void CheckForArrowKeyInput(InputEvent inputEvent)
	{
		if (NGame.IsGameFocusedWindow() && inputEvent is InputEventKey inputEventKey && inputEventKey.IsPressed() && (inputEventKey.Keycode == Key.Up || inputEventKey.Keycode == Key.Down || inputEventKey.Keycode == Key.Left || inputEventKey.Keycode == Key.Right))
		{
			Viewport viewport = GetViewport();
			if (SaveManager.Instance.PrefsSave.KeyboardMode)
			{
				InputType = InputType.KeyboardOnlyMode;
				NGame.Instance?.SetMouseBehaviorRecursive(Control.MouseBehaviorRecursiveEnum.Disabled);
				ActiveScreenContext.Instance.FocusOnDefaultControl();
				EmitSignal(SignalName.ControllerDetected);
				ControlModeChanged();
				viewport?.SetInputAsHandled();
			}
			else if (InputType == InputType.Controller)
			{
				SwitchToMouseMode();
			}
		}
	}

	/// <summary>
	/// WARNING: Normally this should be handled by CheckForMouseInput.
	/// Make sure you know what you are doing if you use this.
	/// </summary>
	public void ForceMouseMode()
	{
		SwitchToMouseMode();
	}

	private void SwitchToMouseMode()
	{
		Viewport viewport = GetViewport();
		InputType = InputType.MouseAndKeyboard;
		viewport?.GuiReleaseFocus();
		NGame.Instance?.SetMouseBehaviorRecursive(Control.MouseBehaviorRecursiveEnum.Inherited);
		EmitSignal(SignalName.MouseDetected);
		ControlModeChanged();
	}

	private void ControlModeChanged()
	{
		_notifyTween?.Kill();
		_notifyTween = CreateTween();
		_notifyTween.TweenProperty(_label, "modulate", Colors.White, 0.25);
		_notifyTween.TweenInterval(0.5);
		_notifyTween.TweenProperty(_label, "modulate", Colors.Transparent, 0.75);
		switch (InputType)
		{
		case InputType.Controller:
			_label.SetTextAutoSize(new LocString("main_menu_ui", "CONTROLLER_DETECTED").GetFormattedText());
			Log.Info("CONTROLLER DETECTED: " + ((_inputStrategy != null) ? _inputStrategy.GetControllerName() : "NONE"));
			break;
		case InputType.MouseAndKeyboard:
			_label.SetTextAutoSize(new LocString("main_menu_ui", "MOUSE_DETECTED").GetFormattedText());
			Log.Info("MOUSE DETECTED");
			break;
		case InputType.KeyboardOnlyMode:
			_label.SetTextAutoSize(new LocString("main_menu_ui", "KEYBOARD_ONLY_DETECTED").GetFormattedText());
			Log.Info("KEYBOARD-MODE DETECTED");
			break;
		}
	}

	private void OnScreenContextChanged()
	{
		if (IsUsingDirectionalNavigation)
		{
			Callable.From(delegate
			{
				ActiveScreenContext.Instance.FocusOnDefaultControl();
			}).CallDeferred();
			return;
		}
		Vector2 mousePosition = GetViewport().GetMousePosition();
		using InputEventMouseMotion inputEventMouseMotion = new InputEventMouseMotion();
		inputEventMouseMotion.Position = mousePosition;
		inputEventMouseMotion.GlobalPosition = mousePosition;
		Input.ParseInputEvent(inputEventMouseMotion);
	}

	public void StartListeningForRebind()
	{
		_inputTypeCheckingDisabled = true;
	}

	public void StopListeningForRebind()
	{
		_inputTypeCheckingDisabled = false;
	}

	public Texture2D? GetHotkeyIcon(string hotkey)
	{
		return _inputStrategy?.GetHotkeyIcon(hotkey);
	}

	public Vector2 GetLeftAnalogStickDirection()
	{
		return _inputStrategy?.GetLeftAnalogStickDirection() ?? Vector2.Zero;
	}
}
