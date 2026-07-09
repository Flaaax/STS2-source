using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

/// <summary>
/// The buttons which are in the Action list for the monsters in the Bestiary.
/// Click on these buttons to play the associated animation: Attack, Hurt, Die, etc.
/// </summary>
public partial class NBestiaryMoveButton : NButton
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary_move_button");

	private Control _buttonAnimator;

	private MegaLabel _label;

	private Tween? _tween;

	private Tween? _clickTween;

	private string[] _hotkeys;

	protected override string? ClickedSfx => null;

	protected override string? HoveredSfx => null;

	public BestiaryMonsterMove Move { get; private set; }

	protected override string[] Hotkeys => _hotkeys;

	public override void _Ready()
	{
		ConnectSignals();
		_label = GetNode<MegaLabel>("%Label");
		_buttonAnimator = GetNode<Control>("%ButtonAnimator");
		_label.SetTextAutoSize(Move.displayName);
		_label.PivotOffset = new Vector2(0f, _label.Size.Y * 0.5f);
		_buttonAnimator.PivotOffset = new Vector2(0f, _buttonAnimator.Size.Y * 0.5f);
	}

	public static NBestiaryMoveButton Create(BestiaryMonsterMove move, StringName setHotkey)
	{
		NBestiaryMoveButton nBestiaryMoveButton = PreloadManager.Cache.GetAsset<PackedScene>(_scenePath).Instantiate<NBestiaryMoveButton>(PackedScene.GenEditState.Disabled);
		nBestiaryMoveButton._hotkeys = new string[1] { setHotkey };
		nBestiaryMoveButton.Move = move;
		return nBestiaryMoveButton;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		_label.Modulate = StsColors.green;
		_clickTween?.Kill();
		_clickTween = CreateTween().SetParallel();
		_clickTween.TweenProperty(_buttonAnimator, "scale", Vector2.One, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
		_clickTween.TweenProperty(_label, "modulate", StsColors.cream, 0.2);
	}

	protected override void OnPress()
	{
		base.OnPress();
		_clickTween?.Kill();
		_clickTween = CreateTween().SetParallel();
		_clickTween.TweenProperty(_buttonAnimator, "scale", Vector2.One * 0.9f, 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_clickTween.TweenProperty(_label, "modulate", StsColors.lightGray, 0.05);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_label, "scale", Vector2.One * 1.05f, 0.05);
	}

	protected override void OnUnfocus()
	{
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_label, "scale", Vector2.One, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_clickTween?.Kill();
		_clickTween = CreateTween().SetParallel();
		_clickTween.TweenProperty(_buttonAnimator, "scale", Vector2.One, 0.1).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_clickTween.TweenProperty(_label, "modulate", StsColors.cream, 0.1);
	}
}
