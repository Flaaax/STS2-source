using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.Debug;

/// <summary>
/// Helper class to quickly load screens of the game without having to go through the main menu.
/// Can specify character, room type, and room details in the parameters you want to load into.
/// And then start the scene scene_bootstrapper.tscn
/// </summary>
public partial class NSceneBootstrapper : Node
{
	private bool _openConsole;

	private NGame _game;

	public override void _Ready()
	{
		if (GetParent() is NGame game)
		{
			_game = game;
		}
		else
		{
			_game = SceneHelper.Instantiate<NGame>("game");
			_game.StartOnMainMenu = false;
			this.AddChildSafely(_game);
		}
		TaskHelper.RunSafely(StartNewRun());
	}

	private async Task StartNewRun()
	{
		await _game.GameStartupComplete;
		Bootstrapper bootstrapper = new Bootstrapper();
		await bootstrapper.SetupRun();
		await bootstrapper.EnterFirstRoom();
		if (_openConsole)
		{
			NDevConsole.Instance.ShowConsole();
			NDevConsole.Instance.MakeFullScreen();
			NDevConsole.Instance.SetBackgroundColor(Colors.White);
		}
	}
}
