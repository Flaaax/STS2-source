using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;

/// <summary>
/// Opens the feedback screen from anywhere when a hotkey is pressed.
/// </summary>
public partial class NFeedbackScreenOpener : Node
{
	public static NFeedbackScreenOpener Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		Instance = null;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey { Pressed: not false } inputEventKey && inputEventKey.Keycode == Key.F2 && !NGame.Instance.GetOrCreateFeedbackScreen().Visible && !(NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCapstoneSubmenuStack { ScreenType: NetScreenType.Feedback }))
		{
			TaskHelper.RunSafely(OpenFeedbackScreen());
		}
	}

	public async Task OpenFeedbackScreen()
	{
		Image screenshot = GetViewport().GetTexture().GetImage();
		await this.AwaitProcessFrame();
		NGame.Instance.GetInspectCardScreen().Close();
		NGame.Instance.GetInspectRelicScreen().Close();
		NSendFeedbackScreen orCreateFeedbackScreen = NGame.Instance.GetOrCreateFeedbackScreen();
		orCreateFeedbackScreen.SetScreenshot(screenshot);
		orCreateFeedbackScreen.Open();
	}
}
