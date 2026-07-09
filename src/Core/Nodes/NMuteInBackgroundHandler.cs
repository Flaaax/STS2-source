using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes;

/// <summary>
/// Responsible for muting audio when the game enters the background, if the player has the setting set.
/// </summary>
public partial class NMuteInBackgroundHandler : Node
{
	private Tween? _tween;

	private ulong _lastFocusOutMsec;

	private bool _loggedEnvironment;

	public override void _Notification(int what)
	{
		if ((long)what == 1005)
		{
			if (!_loggedEnvironment)
			{
				_loggedEnvironment = true;
				Log.Info($"MuteInBackground: environment [displayServer={DisplayServer.GetName()}, os={OS.GetName()}]");
			}
			_lastFocusOutMsec = Time.GetTicksMsec();
			Log.Info($"MuteInBackground: FocusOut received [tween={_tween != null}, windowFocused={GetWindow().HasFocus()}]");
			Mute();
		}
		else if ((long)what == 1004)
		{
			ulong value = Time.GetTicksMsec() - _lastFocusOutMsec;
			Log.Info($"MuteInBackground: FocusIn received [tween={_tween != null}, msSinceFocusOut={value}, windowFocused={GetWindow().HasFocus()}]");
			Unmute();
		}
	}

	private void Mute()
	{
		PrefsSave prefsSave = SaveManager.Instance.PrefsSave;
		SettingsSave settingsSave = SaveManager.Instance.SettingsSave;
		if (prefsSave != null && settingsSave != null && prefsSave.MuteInBackground)
		{
			bool flag = _tween != null;
			_tween?.Kill();
			_tween = CreateTween();
			_tween.TweenMethod(Callable.From<float>(SetMasterVolume), settingsSave.VolumeMaster, 0f, 1.0);
			if (flag)
			{
				Log.Info("MuteInBackground: Muting (replaced existing tween)");
			}
		}
		else
		{
			Log.Info("MuteInBackground: FocusOut ignored (setting off or saves null)");
		}
	}

	private void Unmute()
	{
		if (_tween != null)
		{
			_tween?.Kill();
			_tween = null;
			float volumeMaster = SaveManager.Instance.SettingsSave.VolumeMaster;
			SetMasterVolume(volumeMaster);
			Log.Info($"MuteInBackground: Unmuted, restored volume to {volumeMaster}");
		}
	}

	private static void SetMasterVolume(float volume)
	{
		NGame.Instance.AudioManager.SetMasterVol(volume);
		NGame.Instance.DebugAudio.SetMasterAudioVolume(volume);
	}
}
