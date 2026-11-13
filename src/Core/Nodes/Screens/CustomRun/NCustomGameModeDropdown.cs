using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

/// <summary>
/// The display mode dropdown in the SettingsScreen.
/// </summary>
public partial class NCustomGameModeDropdown : NDropdown
{
	[Signal]
	public delegate void GameModeUpdatedEventHandler();

	private NSelectionReticle _selectionReticle;

	private NDropdownContainer _dropdownContainer;

	private static string DropdownItemScenePath => SceneHelper.GetScenePath("screens/custom_run/custom_game_mode_dropdown_item");

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(DropdownItemScenePath);

	public GameMode SelectedGameMode { get; private set; }

	public override void _Ready()
	{
		ConnectSignals();
		_dropdownContainer = GetNode<NDropdownContainer>("Container/DropdownContainer");
		_currentOptionLabel.SetTextAutoSize(GetGameModeLoc(GameMode.Standard).GetFormattedText());
		_selectionReticle = GetNode<NSelectionReticle>("SelectionReticle");
	}

	public void SetOptions(List<GameMode> gameModes)
	{
		ClearDropdownItems();
		foreach (GameMode gameMode in gameModes)
		{
			AddDropdownItem(gameMode);
		}
		if (!gameModes.Contains(SelectedGameMode))
		{
			SetSelectedGameMode(GameMode.Standard);
		}
		_dropdownContainer.RefreshLayout();
	}

	private void AddDropdownItem(GameMode gameMode)
	{
		NCustomGameModeDropdownItem nCustomGameModeDropdownItem = PreloadManager.Cache.GetScene(DropdownItemScenePath).Instantiate<NCustomGameModeDropdownItem>(PackedScene.GenEditState.Disabled);
		_dropdownItems.AddChildSafely(nCustomGameModeDropdownItem);
		nCustomGameModeDropdownItem.Connect(NDropdownItem.SignalName.Selected, Callable.From<NCustomGameModeDropdownItem>(OnDropdownItemSelected));
		nCustomGameModeDropdownItem.Init(gameMode);
	}

	private void OnDropdownItemSelected(NCustomGameModeDropdownItem item)
	{
		CloseDropdown();
		SetSelectedGameMode(item.GameMode);
	}

	public void SetSelectedGameMode(GameMode gameMode)
	{
		if (SelectedGameMode != gameMode)
		{
			SelectedGameMode = gameMode;
			_currentOptionLabel.SetTextAutoSize(GetGameModeLoc(gameMode).GetFormattedText());
			EmitSignalGameModeUpdated();
		}
	}

	protected override void OnDisable()
	{
		base.Modulate = StsColors.gray;
	}

	protected override void OnEnable()
	{
		base.Modulate = Colors.White;
	}

	public static LocString GetGameModeLoc(GameMode gameMode)
	{
		return new LocString("main_menu_ui", gameMode.ToString().ToUpperInvariant() + ".title");
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_currentOptionHighlight.Modulate = new Color("3C5B6B");
		if (NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_selectionReticle.OnSelect();
		}
	}

	protected override void OnUnfocus()
	{
		_selectionReticle.OnDeselect();
		_currentOptionHighlight.Modulate = new Color("2C434F");
	}
}
