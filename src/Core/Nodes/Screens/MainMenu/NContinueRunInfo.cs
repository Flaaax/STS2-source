using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

/// <summary>
/// Shows run info in the main menu! Handy!
/// </summary>
public partial class NContinueRunInfo : Control
{
	private Tween? _visTween;

	private Vector2 _initPosition;

	private Control _runInfoContainer;

	private Control _errorContainer;

	private MegaRichTextLabel _dateLabel;

	private TextureRect _goldIcon;

	private MegaRichTextLabel _goldLabel;

	private MegaRichTextLabel _healthLabel;

	private MegaRichTextLabel _progressLabel;

	private MegaRichTextLabel _ascensionLabel;

	private TextureRect _charIcon;

	private bool _isShown;

	public bool HasResult { get; private set; }

	public override void _Ready()
	{
		_initPosition = base.Position;
		base.Modulate = StsColors.transparentWhite;
		_runInfoContainer = GetNode<VBoxContainer>("%RunInfoContainer");
		_errorContainer = GetNode<Control>("%ErrorContainer");
		_dateLabel = GetNode<MegaRichTextLabel>("%DateLabel");
		_goldIcon = GetNode<TextureRect>("%GoldIcon");
		_goldLabel = GetNode<MegaRichTextLabel>("%GoldLabel");
		_healthLabel = GetNode<MegaRichTextLabel>("%HealthLabel");
		_progressLabel = GetNode<MegaRichTextLabel>("%ProgressLabel");
		_charIcon = GetNode<TextureRect>("%CharacterIcon");
		_ascensionLabel = GetNode<MegaRichTextLabel>("%AscensionLabel");
	}

	public void AnimShow()
	{
		_visTween?.Kill();
		_visTween = CreateTween().SetParallel();
		_visTween.TweenProperty(this, "position", _initPosition + new Vector2(0f, -20f), 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		_visTween.TweenProperty(this, "modulate:a", 1f, 0.2);
		_isShown = true;
	}

	public void AnimHide()
	{
		if (_isShown)
		{
			_visTween?.Kill();
			_visTween = CreateTween().SetParallel();
			_visTween.TweenProperty(this, "position", _initPosition, 0.2);
			_visTween.TweenProperty(this, "modulate:a", 0f, 0.2);
			_isShown = false;
		}
	}

	public void SetResult(ReadSaveResult<SerializableRun>? result)
	{
		if (result != null && result.Success && result.SaveData != null)
		{
			try
			{
				ShowInfo(result.SaveData);
			}
			catch (Exception value)
			{
				Log.Warn($"Exception occurred while displaying continue run info: {value}");
				ShowError();
			}
		}
		else if (result != null)
		{
			ShowError();
		}
		HasResult = result != null;
	}

	private void ShowInfo(SerializableRun save)
	{
		_errorContainer.Visible = false;
		_runInfoContainer.Visible = true;
		DateTimeFormatInfo dateTimeFormat = LocManager.Instance.CultureInfo.DateTimeFormat;
		DateTime dateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(save.SaveTime).UtcDateTime, TimeZoneInfo.Local);
		string rawText = new LocString("main_menu_ui", "CONTINUE_RUN_INFO.savedTimeFormat").GetRawText();
		string variable = dateTime.ToString(rawText, dateTimeFormat);
		LocString locString = new LocString("main_menu_ui", "CONTINUE_RUN_INFO.saved");
		locString.Add("LastSavedTime", variable);
		_dateLabel.SetTextAutoSize(locString.GetFormattedText());
		if (save.GameMode == GameMode.Dread)
		{
			List<string> list = new List<string>();
			LocString locString2 = new LocString("main_menu_ui", "CONTINUE_RUN_INFO.dread");
			list.Add("[aqua]" + locString2.GetFormattedText() + "[/aqua]");
			if (save.IsCustomGame)
			{
				list.Add(new LocString("main_menu_ui", "CONTINUE_RUN_INFO.custom").GetFormattedText());
			}
			_ascensionLabel.SetTextAutoSize(string.Join(" ", list));
		}
		else
		{
			List<string> list2 = new List<string>();
			GameMode gameMode = save.GameMode;
			if ((gameMode == GameMode.Daily || gameMode == GameMode.Blitz) ? true : false)
			{
				GameMode gameMode2 = save.GameMode;
				string text = ((gameMode2 != GameMode.Daily) ? "CONTINUE_RUN_INFO.blitz" : "CONTINUE_RUN_INFO.daily");
				string locEntryKey = text;
				list2.Add(new LocString("main_menu_ui", locEntryKey).GetFormattedText());
			}
			if (save.IsCustomGame)
			{
				list2.Add(new LocString("main_menu_ui", "CONTINUE_RUN_INFO.custom").GetFormattedText());
			}
			if (save.Ascension > 0)
			{
				LocString locString3 = new LocString("main_menu_ui", "CONTINUE_RUN_INFO.ascension");
				list2.Add($"[orange]{locString3.GetFormattedText()} {save.Ascension}[/orange]");
			}
			if (list2.Count > 0)
			{
				_ascensionLabel.SetTextAutoSize(string.Join(" ", list2));
			}
			else
			{
				_ascensionLabel.Visible = false;
			}
		}
		ActModel byId = ModelDb.GetById<ActModel>(save.Acts[save.CurrentActIndex].Id);
		SerializablePlayer serializablePlayer = save.Players[0];
		_charIcon.Texture = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId).IconTexture;
		string formattedText = byId.Title.GetFormattedText();
		string formattedText2 = new LocString("main_menu_ui", "CONTINUE_RUN_INFO.floor").GetFormattedText();
		int num = save.VisitedMapCoords.Count;
		for (int i = 0; i < save.CurrentActIndex; i++)
		{
			num += save.MapPointHistory[i].Count;
		}
		_progressLabel.SetTextAutoSize($"{formattedText} [blue]- {formattedText2} {num}[/blue]");
		_healthLabel.SetTextAutoSize($"[red]{serializablePlayer.CurrentHp}/{serializablePlayer.MaxHp}[/red]");
		bool flag = save.GameMode != GameMode.Blitz;
		_goldIcon.Visible = flag;
		_goldLabel.Visible = flag;
		if (flag)
		{
			_goldLabel.SetTextAutoSize($"[gold]{serializablePlayer.Gold}[/gold]");
		}
	}

	private void ShowError()
	{
		_runInfoContainer.Visible = false;
		_errorContainer.Visible = true;
	}
}
