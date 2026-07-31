using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
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
	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	public static readonly string scenePath = SceneHelper.GetScenePath("screens/run_history_screen/run_history_player_icon");

	private readonly List<IHoverTip> _hoverTips = new List<IHoverTip>();

	private Control _achievementLock;

	private Control _ascensionIcon;

	private MegaLabel _ascensionLabel;

	private NSelectionReticle _selectionReticle;

	private ShaderMaterial _hsv;

	private TextureRect _icon;

	private Tween? _tween;

	private static readonly Vector2 _enabledScale = Vector2.One * 1.1f;

	private static readonly Vector2 _disabledScale = Vector2.One * 0.95f;

	public RunHistoryPlayer Player { get; private set; }

	public override void _Ready()
	{
		ConnectSignals();
		_achievementLock = GetNode<Control>("%AchievementLock");
		_ascensionIcon = GetNode<Control>("%AscensionIcon");
		_ascensionLabel = GetNode<MegaLabel>("%AscensionLabel");
		_selectionReticle = GetNode<NSelectionReticle>("%SelectionReticle");
		_icon = GetNode<TextureRect>("%Icon");
		_hsv = (ShaderMaterial)_icon.GetMaterial();
		Deselect();
	}

	public void LoadRun(RunHistoryPlayer player, RunHistory history)
	{
		Player = player;
		CharacterModel characterModel = SaveUtil.CharacterOrDeprecated(player.Character);
		_icon.Texture = characterModel.IconTexture;
		LocString locString = new LocString("ascension", "PORTRAIT_TITLE");
		locString.Add("character", characterModel.Title);
		locString.Add("ascension", history.Ascension);
		LocString locString2 = new LocString("ascension", "PORTRAIT_DESCRIPTION");
		List<string> list = new List<string>();
		for (int i = 1; i <= history.Ascension; i++)
		{
			list.Add(AscensionHelper.GetTitle(i).GetFormattedText());
		}
		locString2.Add("ascensions", list);
		_achievementLock.Visible = history.GameMode.AreAchievementsAndEpochsLocked();
		_ascensionIcon.Visible = false;
		_ascensionLabel.SetTextAutoSize((history.Ascension > 0) ? history.Ascension.ToString() : string.Empty);
		LocString locString3 = new LocString("run_history", "PLAYER_HOVER");
		if (history.Players.Count > 1)
		{
			locString3.Add("PlayerName", PlatformUtil.GetPlayerName(history.PlatformType, player.Id));
			locString3.Add("CharacterName", characterModel.Title.GetFormattedText());
		}
		else
		{
			locString3.Add("PlayerName", characterModel.Title.GetFormattedText());
			locString3.Add("CharacterName", string.Empty);
		}
		if (history.Ascension > 0 || history.GameMode.AreAchievementsAndEpochsLocked())
		{
			_hoverTips.Add(AscensionHelper.GetHoverTip(characterModel, history.Ascension, history.GameMode.AreAchievementsAndEpochsLocked()));
		}
		else
		{
			_hoverTips.Add(new HoverTip(locString3));
		}
	}

	public void Select()
	{
		_hsv.SetShaderParameter(_s, 1f);
		_hsv.SetShaderParameter(_v, 1f);
		_ascensionIcon.Visible = _ascensionLabel.Text != string.Empty;
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", _enabledScale, 0.05);
	}

	public void Deselect()
	{
		_hsv.SetShaderParameter(_s, 0.3f);
		_hsv.SetShaderParameter(_v, 0.55f);
		_ascensionIcon.Visible = false;
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", _disabledScale, 0.05);
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
