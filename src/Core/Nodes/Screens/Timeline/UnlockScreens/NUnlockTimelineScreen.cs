using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;

/// <summary>
/// Unlock screen which isn't really a screen but just an animation.
/// Extends NUnlockScreen so it supports queueing it up like the other unlocks on the TimelineScreen.
/// </summary>
public partial class NUnlockTimelineScreen : NUnlockScreen
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("timeline_screen/unlock_timeline_screen");

	private List<EpochSlotData> _erasToUnlock;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public static NUnlockTimelineScreen Create()
	{
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NUnlockTimelineScreen>(PackedScene.GenEditState.Disabled);
	}

	/// <summary>
	/// Empty _Ready() function so we don't initialize the Confirm button like other unlock screens.
	/// Better ways to handle but w/e.
	/// </summary>
	public override void _Ready()
	{
	}

	/// <summary>
	/// Set which era slots are unlocked when this "screen" is triggered.
	/// </summary>
	/// <param name="eras"></param>
	public void SetUnlocks(List<EpochSlotData> eras)
	{
		_erasToUnlock = eras.OrderBy((EpochSlotData a) => a.EraPosition).ToList();
	}

	public override void Open()
	{
		base.Open();
		TaskHelper.RunSafely(AnimateExpansion());
	}

	private async Task AnimateExpansion()
	{
		await NTimelineScreen.Instance.HideBackstopAndShowUi(showBackButton: false);
		await NTimelineScreen.Instance.AddEpochSlots(_erasToUnlock, isAnimated: true);
		NTimelineScreen.Instance.ShowHeaderAndActionsUi();
		NTimelineScreen.Instance.SetScreenDraggability();
		await Close();
		NTimelineScreen.Instance.EnableInput();
	}
}
