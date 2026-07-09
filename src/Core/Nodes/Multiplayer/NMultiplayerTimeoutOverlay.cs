using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Quality;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Multiplayer;

/// <summary>
/// This overlay is displayed over everything on the client when we lose connection with the host for a long period of
/// time.
/// It is in both Run and Game. The Game one is only used when we are in the middle of a transition, or if the run isn't
/// started yet. Usually, we want to use the Run one because it allows usage of menus (it's under the top bar), but
/// sometimes we get locked while we're loading and we still want to display this over everything.
/// </summary>
public partial class NMultiplayerTimeoutOverlay : Control
{
	private const int _noResponseMsec = 3000;

	private const int _loadingNoResponseMsec = 8000;

	private bool _gameLevel;

	private TextureRect _icon;

	private NetClientGameService? _netService;

	public bool IsShown { get; private set; }

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("%Icon");
	}

	public void Relocalize()
	{
		MegaLabel node = GetNode<MegaLabel>("%Title");
		MegaRichTextLabel node2 = GetNode<MegaRichTextLabel>("%Description");
		node.SetTextAutoSize(new LocString("main_menu_ui", "TIMEOUT_OVERLAY.title").GetFormattedText());
		node2.SetTextAutoSize(new LocString("main_menu_ui", "TIMEOUT_OVERLAY.description").GetFormattedText());
		node.RefreshFont();
		node2.RefreshFont();
	}

	public void Initialize(INetGameService netService, bool isGameLevel)
	{
		if (netService is NetClientGameService netService2)
		{
			_netService = netService2;
			_gameLevel = isGameLevel;
			TaskHelper.RunSafely(UpdateLoop());
		}
	}

	private async Task UpdateLoop()
	{
		while ((_netService?.IsConnected ?? false) && this.IsValid())
		{
			ConnectionStats statsForPeer = _netService.GetStatsForPeer(_netService.HostNetId);
			if (statsForPeer == null)
			{
				return;
			}
			int num = (int)(statsForPeer.LastReceivedTime.HasValue ? (Time.GetTicksMsec() - statsForPeer.LastReceivedTime.Value) : 0);
			bool flag = _gameLevel == (_netService.IsGameLoading || !RunManager.Instance.IsInProgress);
			int num2 = (statsForPeer.RemoteIsLoading ? 8000 : 3000);
			bool flag2 = flag && num >= num2;
			if (!IsShown && flag2)
			{
				base.Visible = true;
			}
			else if (IsShown && !flag2)
			{
				base.Visible = false;
			}
			IsShown = flag2;
			await Task.Delay(200);
		}
		if (this.IsValid())
		{
			base.Visible = false;
		}
		_netService = null;
	}
}
