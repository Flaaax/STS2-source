using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

public partial class NKeyboardOnlyModeTickbox : NSettingsTickbox, IResettableSettingNode
{
	private NSettingsScreen _settingsScreen;

	public override void _Ready()
	{
		ConnectSignals();
		_settingsScreen = this.GetAncestorOfType<NSettingsScreen>();
		SetFromSettings();
	}

	public void SetFromSettings()
	{
		base.IsTicked = SaveManager.Instance.PrefsSave.KeyboardMode;
	}

	protected override void OnTick()
	{
		_settingsScreen.ShowToast(new LocString("settings_ui", "KEYBOARD_ONLY_MODE_ENABLED"));
		SaveManager.Instance.PrefsSave.KeyboardMode = true;
	}

	protected override void OnUntick()
	{
		_settingsScreen.ShowToast(new LocString("settings_ui", "KEYBOARD_ONLY_MODE_DISABLED"));
		SaveManager.Instance.PrefsSave.KeyboardMode = false;
		if (NControllerManager.Instance != null && NControllerManager.Instance.InputType == InputType.KeyboardOnlyMode)
		{
			NControllerManager.Instance.ForceMouseMode();
		}
	}
}
