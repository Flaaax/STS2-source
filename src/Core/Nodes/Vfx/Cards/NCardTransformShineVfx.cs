using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

public partial class NCardTransformShineVfx : Control
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/ui/card/vfx_card_transform");

	[Export(PropertyHint.None, "")]
	private Control _overlay;

	[Export(PropertyHint.None, "")]
	private Control _borderGlow;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _revealParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _shineParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _endParticles;

	[Export(PropertyHint.None, "")]
	private CurveXyzTexture _anticipationScaleCurve;

	[Export(PropertyHint.None, "")]
	private CurveXyzTexture _revealScaleCurve;

	[Export(PropertyHint.None, "")]
	private float _overlayShowDuration = 0.75f;

	[Export(PropertyHint.None, "")]
	private float _overlayShowShortDuration = 0.75f;

	[Export(PropertyHint.None, "")]
	private float _overlayIdleDuration = 0.125f;

	[Export(PropertyHint.None, "")]
	private float _overlayIdleShortDuration = 0.75f;

	[Export(PropertyHint.None, "")]
	private float _overlayHideDuration = 0.25f;

	[Export(PropertyHint.None, "")]
	private float _glowFadeDuration = 0.25f;

	[Export(PropertyHint.None, "")]
	private float _glowTopScale = 1.5f;

	[Export(PropertyHint.None, "")]
	private float _shineDelay = 0.125f;

	[Export(PropertyHint.None, "")]
	private float _endParticlesDelay = 0.4f;

	private NCard _cardNode;

	private CardModel _endCard;

	private IEnumerable<RelicModel>? _relicsToFlash;

	private Tween? _tween;

	private Color _whiteOpaque = new Color(1f, 1f, 1f);

	private Color _whiteClear = new Color(1f, 1f, 1f, 0f);

	private static Vector2 _originalCardScale = new Vector2(1f, 1f);

	public static NCardTransformShineVfx? Create(NCard cardNode, CardModel endCard, IEnumerable<RelicModel>? relicsToFlash)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardTransformShineVfx nCardTransformShineVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NCardTransformShineVfx>(PackedScene.GenEditState.Disabled);
		nCardTransformShineVfx._cardNode = cardNode;
		nCardTransformShineVfx._endCard = endCard;
		nCardTransformShineVfx._relicsToFlash = relicsToFlash;
		cardNode.CardVfxContainer.AddChildSafely(nCardTransformShineVfx);
		return nCardTransformShineVfx;
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}

	private async Task<bool> WaitAndInterruptIfNecessary(float seconds, NCard cardNode)
	{
		float num = 0f;
		while (num <= seconds)
		{
			if (!cardNode.IsInsideTree() || _endCard.Pile == null)
			{
				return false;
			}
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		return true;
	}

	private static void UpdateCard(NCard cardNode, CardModel endCard)
	{
		if (endCard.Pile != null)
		{
			NPlayerHand.Instance?.TryCancelCardPlay(cardNode.Model);
			cardNode.Model = endCard;
			cardNode.UpdateVisuals(endCard.Pile.Type, CardPreviewMode.Normal);
			if (NCombatRoom.Instance?.Ui.Hand.GetCardHolder(endCard) is NHandCardHolder nHandCardHolder)
			{
				nHandCardHolder.UpdateCard();
			}
		}
	}

	public float GetEffectiveDuration(bool shortVersion = false)
	{
		float num = (shortVersion ? _overlayShowShortDuration : _overlayShowDuration);
		float num2 = (shortVersion ? _overlayIdleShortDuration : _overlayIdleDuration);
		return num + num2;
	}

	public async Task PlayAnimation(bool shortVersion = false)
	{
		_overlay.SelfModulate = _whiteClear;
		_borderGlow.SelfModulate = _whiteClear;
		float num = (shortVersion ? _overlayShowShortDuration : _overlayShowDuration);
		float num2 = (shortVersion ? _overlayIdleShortDuration : _overlayIdleDuration);
		TaskHelper.RunSafely(AnimatingCardScale(_anticipationScaleCurve, num + num2));
		_tween = CreateTween();
		_tween.TweenProperty(_overlay, "self_modulate", _whiteOpaque, num).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quart);
		if (!(await WaitAndInterruptIfNecessary(num + num2, _cardNode)))
		{
			_cardNode.Scale = _originalCardScale;
			this.QueueFreeSafely();
			return;
		}
		UpdateCard(_cardNode, _endCard);
		TaskHelper.RunSafely(AnimatingCardScale(_revealScaleCurve, _glowFadeDuration));
		_borderGlow.SelfModulate = _whiteOpaque;
		_borderGlow.Scale = Vector2.One;
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_overlay, "self_modulate", _whiteClear, _overlayHideDuration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart);
		_tween.TweenProperty(_borderGlow, "scale", Vector2.One * _glowTopScale, _glowFadeDuration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
		_tween.TweenProperty(_borderGlow, "self_modulate", _whiteClear, _glowFadeDuration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
		if (_relicsToFlash != null)
		{
			foreach (RelicModel item in _relicsToFlash)
			{
				item.Flash();
				_cardNode.FlashRelicOnCard(item);
			}
		}
		_revealParticles.Restart();
		if (!(await WaitAndInterruptIfNecessary(_shineDelay, _cardNode)))
		{
			_cardNode.Scale = _originalCardScale;
			this.QueueFreeSafely();
			return;
		}
		_shineParticles.Restart();
		if (!(await WaitAndInterruptIfNecessary(_endParticlesDelay, _cardNode)))
		{
			_cardNode.Scale = _originalCardScale;
			this.QueueFreeSafely();
		}
		else
		{
			_endParticles.Restart();
			TaskHelper.RunSafely(DelayedFree());
		}
	}

	private async Task DelayedFree()
	{
		await Cmd.Wait(2f);
		this.QueueFreeSafely();
	}

	private async Task AnimatingCardScale(CurveXyzTexture curve, float duration)
	{
		float num = 0f;
		Vector2 one = Vector2.One;
		while (num < duration)
		{
			float offset = num / duration;
			float x = curve.CurveX.Sample(offset);
			float y = curve.CurveY.Sample(offset);
			one.X = x;
			one.Y = y;
			_cardNode.Scale = _originalCardScale * one;
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		one.X = curve.CurveX.Sample(1f);
		one.Y = curve.CurveY.Sample(1f);
		_cardNode.Scale = _originalCardScale * one;
	}
}
