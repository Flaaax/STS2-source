using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;

public partial class NStatsTabManager : Control
{
	private Control _tabContainer;

	private List<NSettingsTab> _tabs;

	private NSettingsTab? _currentTab;

	public override void _Ready()
	{
		_tabContainer = GetNode<Control>("TabContainer");
		_tabs = _tabContainer.GetChildren().OfType<NSettingsTab>().ToList();
		foreach (NSettingsTab nSettingsTab in _tabs)
		{
			nSettingsTab.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(delegate
			{
				SwitchToTab(nSettingsTab);
			}));
		}
	}

	public void ResetTabs()
	{
		SwitchToTab(_tabContainer.GetChild<NSettingsTab>(0));
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (IsVisibleInTree() && !NDevConsole.IsConsoleVisible)
		{
			Control control = GetViewport().GuiGetFocusOwner();
			if (control is TextEdit || control is LineEdit)
			{
				bool flag = true;
			}
			else
			{
				bool flag = false;
			}
		}
	}

	private void SwitchToTab(NSettingsTab tab)
	{
		_currentTab = tab;
		foreach (NSettingsTab tab2 in _tabs)
		{
			if (tab2 != _currentTab)
			{
				tab2.Deselect();
			}
			else
			{
				tab2.Select();
			}
		}
	}
}
