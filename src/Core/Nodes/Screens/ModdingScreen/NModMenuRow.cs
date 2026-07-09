using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

public partial class NModMenuRow : NClickableControl
{
	private TextureRect _controllerIcon;

	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/modding/modding_screen_row");

	private const float _selectedAlpha = 0.25f;

	private Panel _selectionHighlight;

	private NTickbox _tickbox;

	private NModdingScreen _screen;

	private bool _isSelected;

	private string Hotkey => MegaInput.accept;

	public Mod? Mod { get; private set; }

	public static NModMenuRow? Create(NModdingScreen screen, Mod mod)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NModMenuRow nModMenuRow = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NModMenuRow>(PackedScene.GenEditState.Disabled);
		nModMenuRow.Mod = mod;
		nModMenuRow._screen = screen;
		return nModMenuRow;
	}

	public override void _Ready()
	{
		if (Mod != null)
		{
			_selectionHighlight = GetNode<Panel>("SelectionHighlight");
			_tickbox = GetNode<NTickbox>("Tickbox");
			MegaRichTextLabel node = GetNode<MegaRichTextLabel>("Title");
			TextureRect node2 = GetNode<TextureRect>("PlatformIcon");
			_controllerIcon = GetNode<TextureRect>("ControllerIcon");
			_tickbox = GetNode<NTickbox>("Tickbox");
			Panel selectionHighlight = _selectionHighlight;
			Color color = _selectionHighlight.Modulate;
			color.A = 0f;
			selectionHighlight.Modulate = color;
			_tickbox.IsTicked = !(SaveManager.Instance.SettingsSave.ModSettings?.IsModDisabled(Mod) ?? false);
			_tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(OnTickboxToggled));
			node.Text = Mod.manifest?.name ?? Mod.manifest?.id ?? "<null>";
			node2.Texture = GetPlatformIcon(Mod.modSource);
			ModLoadState state = Mod.state;
			switch (state)
			{
			case ModLoadState.Loaded:
				color = Colors.White;
				break;
			case ModLoadState.Failed:
				color = StsColors.red;
				break;
			case ModLoadState.Disabled:
				color = StsColors.gray;
				break;
			case ModLoadState.DisabledDuplicate:
				color = StsColors.gray;
				break;
			case ModLoadState.AddedAtRuntime:
				color = StsColors.gray;
				break;
			case ModLoadState.None:
				color = StsColors.purple;
				break;
			default:
				throw new System.Runtime.CompilerServices.SwitchExpressionException(state);
				break;
			}
			Color modulate = (node.Modulate = color);
			node2.Modulate = modulate;
			ConnectSignals();
			NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateControllerButton));
			NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateControllerButton));
			NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdateControllerButton));
			UpdateControllerButton();
		}
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		base._GuiInput(inputEvent);
		if (inputEvent.IsActionPressed(Hotkey) && _isSelected)
		{
			_tickbox.ForceToggleTick();
		}
	}

	protected override void OnFocus()
	{
		if (!_isSelected)
		{
			Panel selectionHighlight = _selectionHighlight;
			Color darkBlue = StsColors.darkBlue;
			darkBlue.A = 0.25f;
			selectionHighlight.Modulate = darkBlue;
			UpdateControllerButton();
		}
	}

	private void UpdateControllerButton()
	{
		_controllerIcon.SetVisible(_isSelected && NControllerManager.Instance.IsUsingController);
		_controllerIcon.Texture = NInputManager.Instance.GetHotkeyIcon(Hotkey);
	}

	protected override void OnUnfocus()
	{
		if (!_isSelected)
		{
			_selectionHighlight.Modulate = Colors.Transparent;
		}
	}

	protected override void OnRelease()
	{
		_screen.OnRowSelected(this);
	}

	public void SetSelected(bool isSelected)
	{
		if (_isSelected != isSelected)
		{
			_isSelected = isSelected;
			if (_isSelected)
			{
				Panel selectionHighlight = _selectionHighlight;
				Color blue = StsColors.blue;
				blue.A = 0.25f;
				selectionHighlight.Modulate = blue;
			}
			else if (base.IsFocused)
			{
				Panel selectionHighlight2 = _selectionHighlight;
				Color blue = StsColors.darkBlue;
				blue.A = 0.25f;
				selectionHighlight2.Modulate = blue;
			}
			else
			{
				_selectionHighlight.Modulate = Colors.Transparent;
			}
		}
		UpdateControllerButton();
	}

	private void OnTickboxToggled(NTickbox tickbox)
	{
		SettingsSave settingsSave = SaveManager.Instance.SettingsSave;
		if (settingsSave.ModSettings == null)
		{
			ModSettings modSettings = (settingsSave.ModSettings = new ModSettings());
		}
		foreach (SettingsSaveMod mod in SaveManager.Instance.SettingsSave.ModSettings.ModList)
		{
			if (mod.Id == Mod?.manifest?.id && mod.Source == Mod.modSource)
			{
				mod.IsEnabled = tickbox.IsTicked;
			}
		}
		_screen.OnModEnabledOrDisabled();
	}

	public static Texture2D GetPlatformIcon(ModSource modSource)
	{
		AssetCache cache = PreloadManager.Cache;
		return cache.GetTexture2D(modSource switch
		{
			ModSource.ModsDirectory => ImageHelper.GetImagePath("ui/mods/folder.png"), 
			ModSource.SteamWorkshop => ImageHelper.GetImagePath("ui/mods/steam_logo.png"), 
			_ => throw new ArgumentOutOfRangeException("modSource", modSource, null), 
		});
	}
}
