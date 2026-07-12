using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

/// <summary>
/// The buttons in the monster list in the Bestiary entry. A text button.
/// </summary>
public partial class NBestiaryEntry : NButton
{
	private MegaRichTextLabel _label;

	private Control _highlight;

	private TextureRect _underConstructionIcon;

	private Color _defaultColor;

	private Tween? _tween;

	protected override string? HoveredSfx => null;

	private static string ScenePath => SceneHelper.GetScenePath("screens/bestiary/bestiary_entry");

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	public BestiaryEntry Entry { get; private set; }

	public bool IsDiscovered { get; private set; }

	public bool IsUnderConstruction => _underConstructionIcon.Visible;

	public static NBestiaryEntry Create(BestiaryEntry entry, bool isDiscovered)
	{
		NBestiaryEntry nBestiaryEntry = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NBestiaryEntry>(PackedScene.GenEditState.Disabled);
		nBestiaryEntry.Entry = entry;
		nBestiaryEntry.IsDiscovered = isDiscovered;
		return nBestiaryEntry;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_label = GetNode<MegaRichTextLabel>("%Label");
		_highlight = GetNode<Control>("%Highlight");
		_underConstructionIcon = GetNode<TextureRect>("%UnderConstructionIcon");
		if (!IsDiscovered)
		{
			Disable();
			_label.Text = new LocString("bestiary", "UNSEEN.monsterName").GetRawText();
			_label.SelfModulate = StsColors.gray;
			_underConstructionIcon.Visible = false;
		}
		else
		{
			_label.Text = Entry.GetEntryTitle();
			_defaultColor = Entry.roomType switch
			{
				RoomType.Boss => StsColors.red, 
				RoomType.Elite => StsColors.purple, 
				_ => StsColors.cream, 
			};
			_label.SelfModulate = _defaultColor;
			_underConstructionIcon.Visible = false;
		}
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_label, "scale", Vector2.One * 1.1f, 0.05);
		if (NControllerManager.Instance.IsUsingController)
		{
			_highlight.Visible = true;
		}
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_label, "scale", Vector2.One, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_highlight.Visible = false;
	}
}
