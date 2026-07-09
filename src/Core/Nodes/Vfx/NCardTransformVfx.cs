using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

/// <summary>
/// Manages full card transformations that take place in the center of the screen
/// (ie transformations for cards in the deck/draw/discard)
/// Centers the card and plays the transform animation. The actual transform animation
/// logic lives in NCardTransformShineVfx
/// </summary>
public partial class NCardTransformVfx : Node2D
{
	private Tween? _tween;

	private CardModel _startCard;

	private CardModel _endCard;

	private IEnumerable<RelicModel>? _relicsToFlash;

	private static string ScenePath => SceneHelper.GetScenePath("vfx/vfx_card_transform");

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	public static NCardTransformVfx? Create(CardModel startCard, CardModel endCard, IEnumerable<RelicModel>? relicsToFlash)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardTransformVfx nCardTransformVfx = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NCardTransformVfx>(PackedScene.GenEditState.Disabled);
		nCardTransformVfx._startCard = startCard;
		nCardTransformVfx._endCard = endCard;
		nCardTransformVfx._relicsToFlash = relicsToFlash;
		return nCardTransformVfx;
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlayAnimation());
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

	public override void _ExitTree()
	{
		_tween?.Kill();
	}

	private async Task PlayAnimation()
	{
		SfxCmd.Play("event:/sfx/ui/cards/card_transform");
		Control node = GetNode<Control>("%CardContainer");
		NCard cardNode = NCard.Create(_startCard);
		node.AddChildSafely(cardNode);
		cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
		_tween = CreateTween();
		_tween.TweenProperty(cardNode, "scale", Vector2.One * 1f, 0.25).From(Vector2.Zero).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		if (!(await WaitAndInterruptIfNecessary(0.75f, cardNode)))
		{
			this.QueueFreeSafely();
			return;
		}
		NCardTransformShineVfx nCardTransformShineVfx = NCardTransformShineVfx.Create(cardNode, _endCard, _relicsToFlash);
		if (nCardTransformShineVfx != null)
		{
			await nCardTransformShineVfx.PlayAnimation();
		}
		if (!(await WaitAndInterruptIfNecessary(0.3f, cardNode)))
		{
			this.QueueFreeSafely();
			return;
		}
		if (_relicsToFlash != null)
		{
			foreach (RelicModel item in _relicsToFlash)
			{
				item.Flash();
				cardNode.FlashRelicOnCard(item);
			}
		}
		if (!(await WaitAndInterruptIfNecessary(0.5f, cardNode)))
		{
			this.QueueFreeSafely();
			return;
		}
		if (_endCard.Pile == null)
		{
			this.QueueFreeSafely();
			return;
		}
		cardNode.Reparent(this);
		cardNode.Position = Vector2.Zero;
		NCardFlyVfx nCardFlyVfx = NCardFlyVfx.Create(cardNode, _endCard.Pile.Type, isAddingToPile: false, _endCard.Owner.Character.TrailPath);
		((_endCard.Pile.Type != PileType.Deck) ? NCombatRoom.Instance?.CombatVfxContainer : NRun.Instance?.GlobalUi.TopBar.TrailContainer)?.AddChildSafely(nCardFlyVfx);
		if (nCardFlyVfx?.SwooshAwayCompletion != null)
		{
			await nCardFlyVfx.SwooshAwayCompletion.Task;
		}
		this.QueueFreeSafely();
	}
}
