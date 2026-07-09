using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;

/// <summary>
/// Abstract class of the Unlock Screens. Used for general animation and some logistics required for the Timeline Screen.
/// All Unlock Screens in the Timeline must use this!
/// </summary>
public abstract partial class NUnlockScreen : Control, IScreenContext
{
	private NUnlockConfirmButton? _unlockConfirmButton;

	private Tween? _tween;

	public virtual Control? DefaultFocusedControl => null;

	public override void _Ready()
	{
		if (GetType() != typeof(NUnlockScreen))
		{
			Log.Error($"{GetType()}");
			throw new InvalidOperationException("Don't call base._Ready()! Call ConnectSignals() instead.");
		}
		ConnectSignals();
	}

	protected void ConnectSignals()
	{
		_unlockConfirmButton = GetNode<NUnlockConfirmButton>("ConfirmButton");
		_unlockConfirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(delegate
		{
			TaskHelper.RunSafely(Close());
		}));
	}

	/// <summary>
	/// Useful for UI stuff because this Node is in the Scene so the positions are accurate.
	/// See: NUnlockEpochScreen where we animate the Epochs on screen open!
	/// </summary>
	public virtual void Open()
	{
		NTimelineScreen.Instance.DisableInput();
		NTimelineScreen.Instance.CurrentUnlockScreen = this;
		_unlockConfirmButton?.Disable();
		_tween?.FastForwardToCompletion();
		_tween = CreateTween();
		_tween.TweenProperty(this, "modulate:a", 1f, 0.5);
		_tween.Chain().TweenCallback(Callable.From(delegate
		{
			_unlockConfirmButton?.Enable();
		}));
	}

	protected async Task Close()
	{
		Log.Info($"Closing: {base.Name}");
		if (NTimelineScreen.Instance.CurrentUnlockScreen == this)
		{
			NTimelineScreen.Instance.CurrentUnlockScreen = null;
		}
		_tween?.FastForwardToCompletion();
		OnScreenPreClose();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "modulate", StsColors.transparentBlack, 1.0);
		if (await _tween.AwaitFinished(this))
		{
			OnScreenClose();
			if (!NTimelineScreen.Instance.IsScreenQueued())
			{
				await NTimelineScreen.Instance.HideBackstopAndShowUi(showBackButton: true);
			}
			else
			{
				NTimelineScreen.Instance.OpenQueuedScreen();
			}
			this.QueueFreeSafely();
		}
	}

	protected virtual void OnScreenPreClose()
	{
	}

	protected virtual void OnScreenClose()
	{
	}
}
