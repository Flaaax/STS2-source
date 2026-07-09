using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

/// <summary>
/// Overridden so the <see cref="T:MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NContinueRunInfo" /> node appear when this button is hovered. That's it.
/// Same exact behavior for the AbandonRun button btw. They should be identical.
/// </summary>
public partial class NMainMenuContinueButton : NMainMenuTextButton
{
	private NMainMenu _mainMenu;

	public override void _Ready()
	{
		ConnectSignals();
		_mainMenu = GetParent().GetParent<NMainMenu>();
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		if (_mainMenu.ContinueRunInfo.HasResult)
		{
			_mainMenu.ContinueRunInfo.AnimShow();
		}
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_mainMenu.ContinueRunInfo.AnimHide();
	}
}
