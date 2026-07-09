using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Ftue;

/// <summary>
/// This is a popup that lets you know that you have unlocked Ascensions and briefly explains it.
/// This is NOT a true FTUE as disabling tutorials will not prevent this from showing up.
/// </summary>
public partial class NAscensionSingleplayerFtue : NFtue
{
	public const string id = "ascension_singleplayer_ftue";

	private static readonly string _scenePath = SceneHelper.GetScenePath("ftue/ascension_singleplayer_ftue");

	public override void _Ready()
	{
		GetNode<MegaLabel>("%Header").SetTextAutoSize(new LocString("ftues", "ASCENSION_SINGLEPLAYER_FTUE_TITLE").GetFormattedText());
		GetNode<MegaRichTextLabel>("%Description").SetTextAutoSize(new LocString("ftues", "ASCENSION_SINGLEPLAYER_FTUE_DESCRIPTION").GetFormattedText());
		GetNode<MegaRichTextLabel>("%Disclaimer").SetTextAutoSize(new LocString("ftues", "ASCENSION_SINGLEPLAYER_FTUE_DISCLAIMER").GetFormattedText());
		GetNode<NButton>("%FtueConfirmButton").Connect(NClickableControl.SignalName.Released, Callable.From((Action<NButton>)CloseFtue));
		Tween tween = CreateTween().SetParallel();
		Color modulate = base.Modulate;
		modulate.A = 0f;
		base.Modulate = modulate;
		tween.TweenProperty(this, "position:y", base.Position.Y, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back)
			.From(base.Position.Y + 100f)
			.SetDelay(1.0);
		tween.TweenProperty(this, "modulate:a", 1f, 0.3).SetEase(Tween.EaseType.Out).SetDelay(1.0);
	}

	public static NAscensionSingleplayerFtue? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NAscensionSingleplayerFtue>(PackedScene.GenEditState.Disabled);
	}

	private void CloseFtue(NButton _)
	{
		SaveManager.Instance.MarkFtueAsComplete("ascension_singleplayer_ftue");
		CloseFtue();
	}
}
