using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

public partial class NAscensionPanel : Control
{
	[Signal]
	public delegate void AscensionLevelChangedEventHandler();

	private static readonly StringName _tabLeftHotkey = MegaInput.viewDeckAndTabLeft;

	private static readonly StringName _tabRightHotkey = MegaInput.viewExhaustPileAndTabRight;

	private static readonly StringName _fontOutlineTheme = "font_outline_color";

	private static readonly StringName _h = new StringName("h");

	private static readonly StringName _v = new StringName("v");

	private static readonly Color _redLabelOutline = new Color("593400");

	private static readonly Color _blueLabelOutline = new Color("004759");

	private int _maxAscension;

	private NButton _leftArrow;

	private NButton _rightArrow;

	private MegaLabel _ascensionLevel;

	private MegaRichTextLabel _info;

	private NHotkeyIcon _leftTabIcon;

	private NHotkeyIcon _rightTabIcon;

	private TextureRect _standardIcon;

	private TextureRect _blitzIcon;

	private ShaderMaterial _standardIconHsv;

	private bool _arrowsVisible = true;

	private GameMode _gameMode = GameMode.Standard;

	private MultiplayerUiMode _uiMode = MultiplayerUiMode.Singleplayer;

	private Tween? _tween;

	public int Ascension { get; private set; }

	public override void _Ready()
	{
		_leftTabIcon = GetNode<NHotkeyIcon>("%LeftTabIcon");
		_rightTabIcon = GetNode<NHotkeyIcon>("%RightTabIcon");
		_leftArrow = GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow");
		_rightArrow = GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow");
		_ascensionLevel = GetNode<MegaLabel>("HBoxContainer/AscensionContainer/AscensionLevel");
		_info = GetNode<MegaRichTextLabel>("HBoxContainer/AscensionDescription/Description");
		_standardIcon = GetNode<TextureRect>("%AscensionIconStandard");
		_blitzIcon = GetNode<TextureRect>("%AscensionIconBlitz");
		_standardIconHsv = (ShaderMaterial)_standardIcon.Material;
		_leftArrow.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			DecrementAscension();
		}));
		_rightArrow.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			IncrementAscension();
		}));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateControllerButton));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateControllerButton));
		NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdateControllerButton));
		UpdateControllerButton();
	}

	public void Initialize(MultiplayerUiMode mode, GameMode gameMode)
	{
		_uiMode = mode;
		SwitchGameMode(gameMode);
		if (_uiMode == MultiplayerUiMode.Host)
		{
			SetFireBlue();
			_arrowsVisible = true;
			if (_gameMode != GameMode.Blitz)
			{
				SetMaxAscension(SaveManager.Instance.Progress.MaxMultiplayerAscension);
				SetAscensionLevel(Math.Min(_maxAscension, SaveManager.Instance.Progress.PreferredMultiplayerAscension));
			}
			else
			{
				SetMaxAscension(SaveManager.Instance.Progress.MaxBlitzAscension);
				SetAscensionLevel(Math.Min(_maxAscension, SaveManager.Instance.Progress.PreferredBlitzAscension));
			}
			NHotkeyManager.Instance.PushHotkeyPressedBinding(_tabLeftHotkey, DecrementAscension, 0L);
			NHotkeyManager.Instance.PushHotkeyPressedBinding(_tabRightHotkey, IncrementAscension, 0L);
		}
		else if (_uiMode == MultiplayerUiMode.Singleplayer)
		{
			SetFireRed();
			_arrowsVisible = true;
			if (_gameMode != GameMode.Blitz)
			{
				SetMaxAscension(0);
				SetAscensionLevel(0);
			}
			else
			{
				SetMaxAscension(SaveManager.Instance.Progress.MaxBlitzAscension);
				SetAscensionLevel(Math.Min(_maxAscension, SaveManager.Instance.Progress.PreferredBlitzAscension));
			}
			NHotkeyManager.Instance.PushHotkeyPressedBinding(_tabLeftHotkey, DecrementAscension, 0L);
			NHotkeyManager.Instance.PushHotkeyPressedBinding(_tabRightHotkey, IncrementAscension, 0L);
		}
		else
		{
			MultiplayerUiMode uiMode = _uiMode;
			if ((uint)(uiMode - 3) <= 1u)
			{
				SetFireBlue();
				_arrowsVisible = false;
				SetMaxAscension(0);
			}
		}
	}

	public void SwitchGameMode(GameMode gameMode)
	{
		_gameMode = gameMode;
		_blitzIcon.Visible = gameMode == GameMode.Blitz;
		_standardIcon.Visible = gameMode != GameMode.Blitz;
	}

	private void SetFireBlue()
	{
		_standardIconHsv.SetShaderParameter(_h, 0.52f);
		_standardIconHsv.SetShaderParameter(_v, 1.2f);
		_ascensionLevel.AddThemeColorOverride(_fontOutlineTheme, _blueLabelOutline);
	}

	private void SetFireRed()
	{
		_standardIconHsv.SetShaderParameter(_h, 1f);
		_standardIconHsv.SetShaderParameter(_v, 1f);
		_ascensionLevel.AddThemeColorOverride(_fontOutlineTheme, _redLabelOutline);
	}

	public void Cleanup()
	{
		MultiplayerUiMode uiMode = _uiMode;
		if ((uint)(uiMode - 1) <= 1u)
		{
			NHotkeyManager.Instance.RemoveHotkeyPressedBinding(_tabLeftHotkey, DecrementAscension);
			NHotkeyManager.Instance.RemoveHotkeyPressedBinding(_tabRightHotkey, IncrementAscension);
		}
	}

	public void SetAscensionLevel(int ascension)
	{
		if (Ascension != ascension)
		{
			Ascension = ascension;
			EmitSignal(SignalName.AscensionLevelChanged);
		}
		RefreshAscensionText();
		RefreshArrowVisibility();
	}

	private void IncrementAscension()
	{
		if (Ascension < _maxAscension)
		{
			SetAscensionLevel(Ascension + 1);
		}
	}

	private void DecrementAscension()
	{
		if (Ascension > 0)
		{
			SetAscensionLevel(Ascension - 1);
		}
	}

	private void RefreshArrowVisibility()
	{
		_leftArrow.Visible = _arrowsVisible && Ascension != 0 && _gameMode != GameMode.Dread;
		_rightArrow.Visible = _arrowsVisible && Ascension != _maxAscension && _gameMode != GameMode.Dread;
	}

	public void SetMaxAscension(int maxAscension)
	{
		Log.Info($"Max ascension changed to {maxAscension}");
		_maxAscension = maxAscension;
		if (Ascension >= _maxAscension)
		{
			SetAscensionLevel(_maxAscension);
		}
		base.Visible = _maxAscension > 0;
		RefreshArrowVisibility();
	}

	private void RefreshAscensionText()
	{
		_ascensionLevel.SetTextAutoSize(Ascension.ToString());
		string formattedText = AscensionHelper.GetTitle(Ascension, _gameMode).GetFormattedText();
		string formattedText2 = AscensionHelper.GetDescription(Ascension, _gameMode).GetFormattedText();
		_info.Text = "[b][gold]" + formattedText + "[/gold][/b]\n" + formattedText2;
	}

	public void AnimIn()
	{
		if (base.Visible)
		{
			Color modulate = base.Modulate;
			modulate.A = 0f;
			base.Modulate = modulate;
			_tween?.FastForwardToCompletion();
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(this, "modulate:a", 1f, 0.2);
			_tween.TweenProperty(this, "position:y", base.Position.Y, 0.3).From(base.Position.Y + 30f).SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
		}
	}

	private void UpdateControllerButton()
	{
		MultiplayerUiMode uiMode = _uiMode;
		if ((uint)(uiMode - 1) <= 1u)
		{
			_leftTabIcon.Visible = NControllerManager.Instance.IsUsingDirectionalNavigation;
			_rightTabIcon.Visible = NControllerManager.Instance.IsUsingDirectionalNavigation;
			_leftTabIcon.UpdateInput(MegaInput.viewDeckAndTabLeft);
			_rightTabIcon.UpdateInput(MegaInput.viewExhaustPileAndTabRight);
		}
		else
		{
			_leftTabIcon.Visible = false;
			_rightTabIcon.Visible = false;
		}
	}

	public void Disable()
	{
		_leftArrow.Disable();
		_rightArrow.Disable();
		_leftTabIcon.Visible = false;
		_rightTabIcon.Visible = false;
	}
}
