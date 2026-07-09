using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

/// <summary>
/// The current filter the player is viewing for the Bestiary.
/// Unlike the other tickboxes, this one is a radio-button style.
/// Clicking on a pool filter will deselect the others. If this filter
/// is already active, then you can't click on it.
/// </summary>
public partial class NBestiaryCharacterFilter : NButton
{
	[Signal]
	public delegate void ToggledEventHandler(NBestiaryCharacterFilter filter);

	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	private static readonly string _scenePath = SceneHelper.GetScenePath("screens/bestiary/bestiary_character_filter");

	private bool _isSelected;

	private bool _isLocked;

	public CharacterModel? character;

	private TextureRect _image;

	private ShaderMaterial _hsv;

	private NSelectionReticle _controllerSelectionReticle;

	private Tween? _tween;

	private const float _focusedMultiplier = 1.2f;

	private const float _pressDownMultiplier = 0.8f;

	private static readonly Vector2 _enabledScale = Vector2.One * 1.2f;

	private static readonly Vector2 _disabledScale = Vector2.One * 0.95f;

	public int kills;

	public int deaths;

	public int Total => kills + deaths;

	private double WinRateValue
	{
		get
		{
			if (Total <= 0)
			{
				return 0.0;
			}
			return (double)kills / (double)Total * 100.0;
		}
	}

	public string WinRate
	{
		get
		{
			if (WinRateValue % 1.0 == 0.0)
			{
				return $"{WinRateValue:F0}";
			}
			return $"{WinRateValue:F1}";
		}
	}

	public string BestiarySeenQuote
	{
		get
		{
			if (character != null)
			{
				return character.BestiarySeenQuote.GetFormattedText();
			}
			return string.Empty;
		}
	}

	public LocString? BestiaryKillQuote => character?.BestiaryKillQuote;

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			OnToggle();
		}
	}

	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			_isLocked = value;
			SetLockedState();
		}
	}

	/// <summary>
	/// If a null character is passed, then this filter is set to "All Characters"
	/// </summary>
	public static NBestiaryCharacterFilter Create(CharacterModel? character)
	{
		NBestiaryCharacterFilter nBestiaryCharacterFilter = PreloadManager.Cache.GetAsset<PackedScene>(_scenePath).Instantiate<NBestiaryCharacterFilter>(PackedScene.GenEditState.Disabled);
		nBestiaryCharacterFilter.character = character;
		return nBestiaryCharacterFilter;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_image = GetNode<TextureRect>("%Image");
		if (character != null)
		{
			_image.Texture = character.IconTexture;
			GetNode<TextureRect>("%Shadow").Texture = character.IconTexture;
		}
		_controllerSelectionReticle = GetNode<NSelectionReticle>("%SelectionReticle");
		_hsv = (ShaderMaterial)_image.GetMaterial();
	}

	private void OnToggle()
	{
		_tween?.Kill();
		_hsv.SetShaderParameter(_s, _isSelected ? 1.1f : 0.5f);
		_hsv.SetShaderParameter(_v, _isSelected ? 1.1f : 0.75f);
		if (!_isSelected)
		{
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(_image, "scale", _disabledScale, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		}
		else
		{
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(_image, "scale", _enabledScale, 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		}
	}

	private void SetLockedState()
	{
		_image.SelfModulate = (_isLocked ? new Color(1f, 1f, 1f, 0.2f) : Colors.White);
	}

	protected override void OnRelease()
	{
		if (!_isSelected && !_isLocked)
		{
			base.OnRelease();
			IsSelected = !IsSelected;
			EmitSignal(SignalName.Toggled, this);
		}
	}

	protected override void OnFocus()
	{
		if (!_isSelected && !_isLocked)
		{
			base.OnFocus();
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(_image, "scale", (_isSelected ? _enabledScale : _disabledScale) * 1.2f, 0.05);
			if (NControllerManager.Instance.IsUsingController)
			{
				_controllerSelectionReticle.OnSelect();
			}
		}
	}

	protected override void OnUnfocus()
	{
		if (!_isSelected && !_isLocked)
		{
			base.OnUnfocus();
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(_image, "scale", _isSelected ? _enabledScale : _disabledScale, 0.3);
			_controllerSelectionReticle.OnDeselect();
		}
	}

	protected override void OnPress()
	{
		if (!_isSelected && !_isLocked)
		{
			base.OnPress();
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(_image, "scale", (_isSelected ? _enabledScale : _disabledScale) * 0.8f, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		}
	}

	public void Deselect()
	{
		IsSelected = false;
	}
}
