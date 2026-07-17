using System;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

public partial class NVSyncPaginator : NPaginator, IResettableSettingNode
{
	public override void _Ready()
	{
		ConnectSignals();
		_options.Add(new LocString("settings_ui", "VSYNC_OFF").GetFormattedText());
		_options.Add(new LocString("settings_ui", "VSYNC_ON").GetFormattedText());
		_options.Add(new LocString("settings_ui", "VSYNC_ADAPTIVE").GetFormattedText());
		SetFromSettings();
	}

	public void SetFromSettings()
	{
		int num = _options.IndexOf(GetVSyncString(SaveManager.Instance.SettingsSave.VSync));
		if (num != -1)
		{
			_currentIndex = num;
		}
		else
		{
			_currentIndex = 2;
		}
		_label.SetTextAutoSize(_options[_currentIndex]);
	}

	private static string GetVSyncString(VSyncType vsyncType)
	{
		return new LocString("settings_ui", GetVSyncLabelKey(vsyncType)).GetFormattedText();
	}

	/// <summary>
	/// Maps a stored <see cref="T:MegaCrit.Sts2.Core.Settings.VSyncType" /> to the localization key of the label the paginator
	/// shows for it. This is the inverse of the index-to-<see cref="T:MegaCrit.Sts2.Core.Settings.VSyncType" /> mapping in
	/// <see cref="M:MegaCrit.Sts2.Core.Nodes.Screens.Settings.NVSyncPaginator.OnIndexChanged(System.Int32)" />, so the two must stay in sync: the label shown on load has to
	/// match the value that toggling to that label writes back. Pure and Godot-free so the mapping
	/// can be unit tested without a scene.
	/// </summary>
	public static string GetVSyncLabelKey(VSyncType vsyncType)
	{
		switch (vsyncType)
		{
		case VSyncType.Off:
			return "VSYNC_OFF";
		case VSyncType.On:
			return "VSYNC_ON";
		case VSyncType.Adaptive:
			return "VSYNC_ADAPTIVE";
		default:
			Log.Error("Invalid VSync type: " + vsyncType);
			throw new ArgumentOutOfRangeException("vsyncType", vsyncType, null);
		}
	}

	protected override void OnIndexChanged(int index)
	{
		_currentIndex = index;
		_label.SetTextAutoSize(_options[index]);
		switch (index)
		{
		case 0:
			SaveManager.Instance.SettingsSave.VSync = VSyncType.Off;
			break;
		case 1:
			SaveManager.Instance.SettingsSave.VSync = VSyncType.On;
			break;
		case 2:
			SaveManager.Instance.SettingsSave.VSync = VSyncType.Adaptive;
			break;
		default:
			Log.Error($"Invalid VSync index: {index}");
			break;
		}
		NGame.ApplySyncSetting();
	}
}
