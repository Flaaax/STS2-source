using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MegaCrit.Sts2.Core.Nodes.HoverTips;

public partial class NHoverTipCardContainer : Control
{
	private const string _cardHoverTipScenePath = "res://scenes/ui/card_hover_tip.tscn";

	private const float _padding = 4f;

	private IEnumerable<Control> Tips => GetChildren().OfType<Control>();

	public void Add(CardHoverTip cardTip)
	{
		Control control = PreloadManager.Cache.GetScene("res://scenes/ui/card_hover_tip.tscn").Instantiate<Control>(PackedScene.GenEditState.Disabled);
		this.AddChildSafely(control);
		NCard node = control.GetNode<NCard>("%Card");
		node.Model = cardTip.Card;
		node.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
	}

	/// <summary>
	/// Lays out cards vertically, then horizontally, then sets the position and the size of the container according
	/// to the passed global start position and alignment.
	/// </summary>
	/// <param name="globalStartLocation">Where to start positioning nodes.</param>
	/// <param name="alignment">Which side of the global start location the cards should be placed on.</param>
	/// <returns></returns>
	public void LayoutResizeAndReposition(Vector2 globalStartLocation, HoverTipAlignment alignment)
	{
		Vector2 size = NGame.Instance.GetViewportRect().Size;
		Vector2 size2 = Vector2.Zero;
		Vector2 zero = Vector2.Zero;
		float b = 0f;
		foreach (Control tip in Tips)
		{
			tip.Position = zero;
			size2 = new Vector2(Mathf.Max(zero.X + tip.Size.X, size2.X), Mathf.Max(zero.Y + tip.Size.Y, size2.Y));
			zero += Vector2.Down * (tip.Size.Y + 4f);
			b = Mathf.Max(tip.Size.X, b);
		}
		switch (alignment)
		{
		case HoverTipAlignment.Right:
			base.GlobalPosition = globalStartLocation;
			break;
		case HoverTipAlignment.Left:
			base.GlobalPosition = globalStartLocation + Vector2.Left * size2.X;
			break;
		}
		base.Size = size2;
	}
}
