using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

public partial class NRunHistoryPlayerIcon : NButton
{
	private static readonly StringName _fontOutlineTheme = "font_outline_color";

	private static readonly Color _redLabelOutline = new Color("593400");

	private static readonly Color _blueLabelOutline = new Color("004759");

	private static readonly StringName _h = new StringName("h");

	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	public static readonly string scenePath = SceneHelper.GetScenePath("screens/run_history_screen/run_history_player_icon");

	private Control _achievementLock;

	private TextureRect _ascensionIconStandard;

	private TextureRect _ascensionIconDread;

	private TextureRect _ascensionIconBlitz;

	private MegaLabel _ascensionLabel;

	private NSelectionReticle _selectionReticle;

	private ShaderMaterial _hsv;

	private TextureRect _icon;

	private readonly List<IHoverTip> _hoverTips = new List<IHoverTip>();

	private GameMode _gameMode;

	private Tween? _tween;

	private static readonly Vector2 _enabledScale = Vector2.One * 1.1f;

	private static readonly Vector2 _disabledScale = Vector2.One * 0.95f;

	public RunHistoryPlayer Player { get; private set; }

	public override void _Ready()
	{
		ConnectSignals();
		_achievementLock = GetNode<Control>("%AchievementLock");
		_ascensionIconStandard = GetNode<TextureRect>("%AscensionIconStandard");
		_ascensionIconDread = GetNode<TextureRect>("%AscensionIconDread");
		_ascensionIconBlitz = GetNode<TextureRect>("%AscensionIconBlitz");
		_ascensionLabel = GetNode<MegaLabel>("%AscensionLabel");
		_selectionReticle = GetNode<NSelectionReticle>("%SelectionReticle");
		_icon = GetNode<TextureRect>("%Icon");
		_hsv = (ShaderMaterial)_icon.GetMaterial();
		Deselect();
	}

	public void LoadRun(RunHistoryPlayer player, RunHistory history)
	{
		Player = player;
		_gameMode = history.GameMode;
		int num = AscensionManager.ClampToMode(history.GameMode, history.Ascension);
		CharacterModel characterModel = SaveUtil.CharacterOrDeprecated(player.Character);
		_icon.Texture = characterModel.IconTexture;
		LocString locString = new LocString("ascension", "PORTRAIT_TITLE");
		locString.Add("character", characterModel.Title);
		locString.Add("ascension", num);
		LocString locString2 = new LocString("ascension", "PORTRAIT_DESCRIPTION");
		List<string> list = new List<string>();
		for (int i = 1; i <= num; i++)
		{
			list.Add(AscensionHelper.GetTitle(i, history.GameMode).GetFormattedText());
		}
		locString2.Add("ascensions", list);
		_achievementLock.Visible = GameModeHelper.AreAchievementsAndEpochsLockedForHistory(history);
		_ascensionLabel.SetTextAutoSize((num > 0) ? num.ToString() : string.Empty);
		LocString locString3 = new LocString("run_history", "PLAYER_HOVER");
		if (history.Players.Count > 1)
		{
			locString3.Add("PlayerName", PlatformUtil.GetPlayerName(history.PlatformType, player.Id));
			locString3.Add("CharacterName", characterModel.Title.GetFormattedText());
			SetFireBlue();
		}
		else
		{
			locString3.Add("PlayerName", characterModel.Title.GetFormattedText());
			locString3.Add("CharacterName", string.Empty);
			SetFireRed();
		}
		if (num > 0 || GameModeHelper.AreAchievementsAndEpochsLockedForHistory(history))
		{
			_hoverTips.Add(AscensionHelper.GetHoverTip(characterModel, num, history.GameMode, history.IsCustomGame));
		}
		else
		{
			_hoverTips.Add(new HoverTip(locString3));
		}
	}

	private void SetFireBlue()
	{
		ShaderMaterial shaderMaterial = (ShaderMaterial)_ascensionIconStandard.Material;
		shaderMaterial.SetShaderParameter(_h, 0.52f);
		shaderMaterial.SetShaderParameter(_v, 1.2f);
		_ascensionLabel.AddThemeColorOverride(_fontOutlineTheme, _blueLabelOutline);
	}

	private void SetFireRed()
	{
		ShaderMaterial shaderMaterial = (ShaderMaterial)_ascensionIconStandard.Material;
		shaderMaterial.SetShaderParameter(_h, 1f);
		shaderMaterial.SetShaderParameter(_v, 1f);
		_ascensionLabel.AddThemeColorOverride(_fontOutlineTheme, _redLabelOutline);
	}

	public void Select()
	{
		_hsv.SetShaderParameter(_s, 1f);
		_hsv.SetShaderParameter(_v, 1f);
		HideAscensionVisuals();
		_ascensionLabel.Visible = true;
		if (_ascensionLabel.Text == string.Empty)
		{
			if (_gameMode == GameMode.Dread)
			{
				_ascensionIconDread.Visible = true;
			}
		}
		else
		{
			_ascensionIconStandard.Visible = _gameMode != GameMode.Blitz && _gameMode != GameMode.Dread;
			_ascensionIconDread.Visible = _gameMode == GameMode.Dread;
			_ascensionIconStandard.Visible = _gameMode != GameMode.Blitz && _gameMode != GameMode.Dread;
			_ascensionIconBlitz.Visible = _gameMode == GameMode.Blitz;
		}
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", _enabledScale, 0.05);
	}

	public void Deselect()
	{
		_hsv.SetShaderParameter(_s, 0.3f);
		_hsv.SetShaderParameter(_v, 0.55f);
		HideAscensionVisuals();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", _disabledScale, 0.05);
	}

	private void HideAscensionVisuals()
	{
		_ascensionIconStandard.Visible = false;
		_ascensionIconDread.Visible = false;
		_ascensionIconBlitz.Visible = false;
		_ascensionLabel.Visible = false;
	}

	protected override void OnFocus()
	{
		NHoverTipSet.CreateAndShow(this, _hoverTips)?.SetGlobalPosition(base.GlobalPosition + new Vector2(0f, base.Size.Y + 20f));
		if (NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_selectionReticle.OnSelect();
		}
	}

	protected override void OnUnfocus()
	{
		_selectionReticle.OnDeselect();
		NHoverTipSet.Remove(this);
	}
}
