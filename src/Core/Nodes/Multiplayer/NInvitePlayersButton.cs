using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Nodes.Multiplayer;

public partial class NInvitePlayersButton : NButton
{
	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	private ShaderMaterial _shaderMaterial;

	private Control _container;

	private StartRunLobby? _startRunLobby;

	protected override string[] Hotkeys => new string[1] { MegaInput.viewMap };

	public override void _Ready()
	{
		ConnectSignals();
		_container = GetParent<Control>();
		Control node = GetNode<Control>("Background");
		MegaRichTextLabel node2 = GetNode<MegaRichTextLabel>("Label");
		_shaderMaterial = (ShaderMaterial)node.Material;
		node2.SetTextAutoSize(new LocString("main_menu_ui", "INVITE").GetFormattedText());
		UpdateVisibility();
	}

	public void Initialize(StartRunLobby lobby)
	{
		_startRunLobby = lobby;
		UpdateVisibility();
	}

	/// <summary>
	/// Main menu screens are cached and only hidden, never freed, so the button outlives the lobby.
	/// </summary>
	public void Cleanup()
	{
		_startRunLobby = null;
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		_container.Visible = _startRunLobby != null && PlatformUtil.SupportsInviteDialog(_startRunLobby.NetService.Platform);
	}

	protected override void OnRelease()
	{
		if (_startRunLobby == null || !IsVisibleInTree() || PlatformUtil.TryOpenInviteDialog(_startRunLobby.NetService))
		{
			return;
		}
		string text = StringHelper.SnakeCase(_startRunLobby.NetService.Platform.ToString());
		NErrorPopup nErrorPopup = NErrorPopup.Create(new LocString("main_menu_ui", "INVITE.DISABLED." + text + ".title").GetFormattedText(), new LocString("main_menu_ui", "INVITE.DISABLED." + text + ".description").GetFormattedText(), showReportBugButton: false);
		if (nErrorPopup != null)
		{
			if (NModalContainer.Instance.OpenModal != null)
			{
				nErrorPopup.QueueFreeSafely();
			}
			else
			{
				NModalContainer.Instance.Add(nErrorPopup);
			}
		}
	}

	protected override void OnFocus()
	{
		_shaderMaterial.SetShaderParameter(_s, 1.1f);
		_shaderMaterial.SetShaderParameter(_v, 1.1f);
	}

	protected override void OnUnfocus()
	{
		_shaderMaterial.SetShaderParameter(_s, 1f);
		_shaderMaterial.SetShaderParameter(_v, 1f);
	}
}
