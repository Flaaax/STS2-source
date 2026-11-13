using System;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Combat;

/// <summary>
/// Represents one of the symbols the Fool can create during combat.
/// Is part of a special UI specifically for the Fool.
/// </summary>
public partial class NFoolCombatSymbol : NClickableControl
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("combat/fool_combat_symbol");

	private TextureRect _symbol;

	private TextureRect _glow;

	public CardTag symbolType;

	private Vector2 _originalPosition;

	private float _timeOffset;

	private const float _wobbleRadius = 2f;

	private const float _wobbleSpeed = 1f;

	private Tween? _spawnTween;

	private Tween? _moveTween;

	private LocString Title => new LocString("static_hover_tips", symbolType.ToString().ToUpperInvariant() + "_MELODY.title");

	private LocString Description => new LocString("static_hover_tips", symbolType.ToString().ToUpperInvariant() + "_MELODY.description");

	public static NFoolCombatSymbol Create(CardTag melodyTag)
	{
		NFoolCombatSymbol nFoolCombatSymbol = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NFoolCombatSymbol>(PackedScene.GenEditState.Disabled);
		nFoolCombatSymbol.symbolType = melodyTag;
		return nFoolCombatSymbol;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_symbol = GetNode<TextureRect>("%Symbol");
		_glow = GetNode<TextureRect>("%Glow");
		_originalPosition = _symbol.Position;
		_timeOffset = Rng.Chaotic.NextFloat(0f, (float)Math.PI * 2f);
		SetVisualsAndHoverTip();
		_symbol.Modulate = StsColors.transparentWhite;
		_spawnTween = CreateTween().SetParallel();
		_spawnTween.TweenProperty(_symbol, "modulate:a", 1f, 0.3);
		_spawnTween.TweenProperty(_symbol, "scale", Vector2.One, 0.3).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out)
			.From(Vector2.One * 0.5f);
	}

	private void SetVisualsAndHoverTip()
	{
		switch (symbolType)
		{
		case CardTag.Wah:
			_symbol.Texture = GD.Load<Texture2D>("res://images/packed/fool/symbol_wah.png");
			_glow.Modulate = new Color("FF009980");
			break;
		case CardTag.Cho:
			_symbol.Texture = GD.Load<Texture2D>("res://images/packed/fool/symbol_cho.png");
			_glow.Modulate = new Color("0048FFC0");
			break;
		case CardTag.Ki:
			_symbol.Texture = GD.Load<Texture2D>("res://images/packed/fool/symbol_ki.png");
			_glow.Modulate = new Color("FF770080");
			break;
		default:
			throw new InvalidOperationException("Invalid Card Tag for the fool song symbol");
		}
	}

	protected override void OnFocus()
	{
		LocString description = Description;
		NHoverTipSet.CreateAndShow(this, new HoverTip(Title, description))?.SetGlobalPosition(base.GlobalPosition + new Vector2(100f, 0f));
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
	}

	public override void _Process(double delta)
	{
		float num = (float)Time.GetTicksMsec() * 0.001f * 1f + _timeOffset;
		float x = Mathf.Sin(num) * 2f;
		float y = Mathf.Sin(num * 0.7f + 1.3f) * 2f;
		_symbol.Position = _originalPosition + new Vector2(x, y);
	}

	public void MoveTo(Vector2 targetPosition)
	{
		_moveTween?.Kill();
		_moveTween = CreateTween().SetParallel();
		_moveTween.TweenProperty(this, "global_position", targetPosition, 0.5).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}
}
