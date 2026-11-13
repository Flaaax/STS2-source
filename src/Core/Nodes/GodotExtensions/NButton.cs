using System;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Nodes.GodotExtensions;

/// <summary>
/// A Base Button class which doesn't rely on Godot's Button class.
/// </summary>
public partial class NButton : NClickableControl
{
	protected NHotkeyIcon? _hotkeyIcon;

	private string[]? _removedHotkeys;

	private long[]? _removedPressedSequences;

	private long[]? _removedReleasedSequences;

	protected virtual string? ClickedSfx => "event:/sfx/ui/clicks/ui_click";

	protected virtual string? HoveredSfx => "event:/sfx/ui/clicks/ui_hover";

	protected virtual string[] Hotkeys => Array.Empty<string>();

	protected virtual string? ControllerIconHotkey
	{
		get
		{
			if (Hotkeys.Length == 0)
			{
				return null;
			}
			return Hotkeys[0];
		}
	}

	private bool HasControllerHotkey => Hotkeys.Length != 0;

	public override void _Ready()
	{
		if (GetType() != typeof(NButton))
		{
			Log.Error($"{GetType()}");
			throw new InvalidOperationException("Don't call base._Ready()! Call ConnectSignals() instead.");
		}
		ConnectSignals();
	}

	protected override void ConnectSignals()
	{
		base.ConnectSignals();
		if (HasControllerHotkey)
		{
			RegisterHotkeys();
		}
		GetControllerIconNode();
		UpdateControllerButton();
	}

	protected virtual void GetControllerIconNode()
	{
		_hotkeyIcon = GetNodeOrNull<NHotkeyIcon>("%HotkeyIcon");
	}

	/// <summary>
	/// Detects input actions
	/// </summary>
	/// <param name="inputEvent"></param>
	public override void _Input(InputEvent inputEvent)
	{
		CheckMouseDragThreshold(inputEvent);
	}

	protected override void OnPress()
	{
		if (ClickedSfx != null)
		{
			SfxCmd.Play(ClickedSfx);
		}
	}

	protected override void OnFocus()
	{
		if (HoveredSfx != null)
		{
			SfxCmd.Play(HoveredSfx);
		}
	}

	protected override void OnEnable()
	{
		RegisterHotkeys();
		UpdateControllerButton();
	}

	protected override void OnDisable()
	{
		UnregisterHotkeys();
		UpdateControllerButton();
	}

	protected void UpdateControllerButton()
	{
		NControllerManager instance = NControllerManager.Instance;
		if (instance != null && _hotkeyIcon != null)
		{
			_hotkeyIcon.Visible = instance.IsUsingDirectionalNavigation && _isEnabled;
			if (ControllerIconHotkey != null)
			{
				_hotkeyIcon.UpdateInput(ControllerIconHotkey);
			}
		}
	}

	protected void RegisterHotkeys()
	{
		RegisterHotkeys(restorePriority: false);
	}

	private void RegisterHotkeys(bool restorePriority)
	{
		if (HasControllerHotkey && _isEnabled)
		{
			string[] hotkeys = Hotkeys;
			string[] array = (restorePriority ? _removedHotkeys : null);
			for (int i = 0; i < hotkeys.Length; i++)
			{
				int num = ((array != null) ? Array.IndexOf(array, hotkeys[i]) : (-1));
				long sequence = ((num >= 0) ? _removedPressedSequences[num] : 0);
				long sequence2 = ((num >= 0) ? _removedReleasedSequences[num] : 0);
				NHotkeyManager.Instance.PushHotkeyPressedBinding(hotkeys[i], base.OnPressHandler, sequence);
				NHotkeyManager.Instance.PushHotkeyReleasedBinding(hotkeys[i], base.OnReleaseHandler, sequence2);
			}
		}
		ForgetRemovedPriorities();
	}

	protected void UnregisterHotkeys()
	{
		ForgetRemovedPriorities();
		if (HasControllerHotkey)
		{
			string[] hotkeys = Hotkeys;
			long[] array = new long[hotkeys.Length];
			long[] array2 = new long[hotkeys.Length];
			for (int i = 0; i < hotkeys.Length; i++)
			{
				array[i] = NHotkeyManager.Instance.RemoveHotkeyPressedBinding(hotkeys[i], base.OnPressHandler);
				array2[i] = NHotkeyManager.Instance.RemoveHotkeyReleasedBinding(hotkeys[i], base.OnReleaseHandler);
			}
			_removedHotkeys = hotkeys;
			_removedPressedSequences = array;
			_removedReleasedSequences = array2;
		}
	}

	private void ForgetRemovedPriorities()
	{
		_removedHotkeys = null;
		_removedPressedSequences = null;
		_removedReleasedSequences = null;
	}

	public override void _EnterTree()
	{
		if (NControllerManager.Instance != null)
		{
			NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateControllerButton));
			NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateControllerButton));
		}
		if (NInputManager.Instance != null)
		{
			NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdateControllerButton));
		}
		RegisterHotkeys(restorePriority: true);
	}

	public override void _ExitTree()
	{
		if (NControllerManager.Instance != null)
		{
			NControllerManager.Instance.Disconnect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateControllerButton));
			NControllerManager.Instance.Disconnect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateControllerButton));
		}
		if (NInputManager.Instance != null)
		{
			NInputManager.Instance.Disconnect(NInputManager.SignalName.InputRebound, Callable.From(UpdateControllerButton));
		}
		UnregisterHotkeys();
	}
}
