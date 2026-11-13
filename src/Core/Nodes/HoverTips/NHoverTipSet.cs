using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.HoverTips;

public partial class NHoverTipSet : Control
{
	/// <summary>
	/// Blocks hover tips from displaying. Useful for situations such as while you are using card/potion targeting
	/// WARNING: Be very careful when modifying this!!! Make sure that if you set it to true, that you set it back to false!
	/// </summary>
	public static bool shouldBlockHoverTips = false;

	private static readonly StringName _cardHoverTipContainerStr = new StringName("cardHoverTipContainer");

	private static readonly StringName _textHoverTipContainerStr = new StringName("textHoverTipContainer");

	private const float _hoverTipSpacing = 5f;

	public const float hoverTipWidth = 360f;

	private const string _tipScenePath = "res://scenes/ui/hover_tip.tscn";

	private const string _tipSetScenePath = "res://scenes/ui/hover_tip_set.tscn";

	private const string _debuffMatPath = "res://materials/ui/hover_tip_debuff.tres";

	/// <summary>
	/// A replacement drops the tip entry and keeps the handler entry, which is how
	/// <see cref="M:MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.ConnectTreeExiting(Godot.Control)" /> knows the owner is already connected. So a handler with no tip is
	/// ordinary; a tip with no handler is not, and every path that adds to the first must call
	/// ConnectTreeExiting.
	/// </summary>
	private static readonly Dictionary<Control, NHoverTipSet> _activeHoverTips = new Dictionary<Control, NHoverTipSet>();

	private static readonly Dictionary<Control, Callable> _treeExitingHandlers = new Dictionary<Control, Callable>();

	private VFlowContainer _textHoverTipContainer;

	private NHoverTipCardContainer _cardHoverTipContainer;

	private Control _owner;

	private bool _followOwner;

	private Vector2 _followOffset;

	private Vector2 _extraOffset = Vector2.Zero;

	/// <summary>
	/// Tests have no <see cref="T:MegaCrit.Sts2.Core.Nodes.NGame" />, so they supply their own container. Null in the running game.
	/// </summary>
	internal static Node? ContainerForTests { get; set; }

	private static Node HoverTipsContainer => ContainerForTests ?? NGame.Instance.HoverTipsContainer;

	private Vector2 TextHoverTipDimensions => _textHoverTipContainer.Size;

	private Vector2 CardHoverTipDimensions => _cardHoverTipContainer.Size;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "res://scenes/ui/hover_tip.tscn", "res://scenes/ui/hover_tip_set.tscn", "res://materials/ui/hover_tip_debuff.tres" });

	/// <summary>
	/// Allows the HoverTip to follow the node it's tied to.
	/// </summary>
	public void SetFollowOwner()
	{
		_followOwner = true;
		_followOffset = _owner.GlobalPosition - base.GlobalPosition;
	}

	public static NHoverTipSet? CreateAndShow(Control owner, IHoverTip hoverTip, HoverTipAlignment alignment = HoverTipAlignment.None)
	{
		return CreateAndShow(owner, new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(hoverTip), alignment);
	}

	public static NHoverTipSet? CreateAndShow(Control owner, IEnumerable<IHoverTip> hoverTips, HoverTipAlignment alignment = HoverTipAlignment.None)
	{
		NHoverTipSet nHoverTipSet = CreateAndRegister(owner, hoverTips);
		nHoverTipSet?.SetAlignment(owner, alignment);
		return nHoverTipSet;
	}

	/// <summary>
	/// Builds the tip node and takes the owner's slot in <see cref="F:MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet._activeHoverTips" />. Split out from
	/// CreateAndShow because aligning needs a live <see cref="T:MegaCrit.Sts2.Core.Nodes.NGame" /> and this does not.
	/// </summary>
	internal static NHoverTipSet? CreateAndRegister(Control owner, IEnumerable<IHoverTip> hoverTips)
	{
		if (shouldBlockHoverTips)
		{
			return null;
		}
		ReplaceExistingTip(owner);
		NHoverTipSet nHoverTipSet = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip_set.tscn").Instantiate<NHoverTipSet>(PackedScene.GenEditState.Disabled);
		HoverTipsContainer.AddChildSafely(nHoverTipSet);
		_activeHoverTips.Add(owner, nHoverTipSet);
		nHoverTipSet.Init(owner, hoverTips);
		if (NGame.IsDebugHidingHoverTips)
		{
			nHoverTipSet.Visible = false;
		}
		ConnectTreeExiting(owner);
		return nHoverTipSet;
	}

	/// <summary>
	/// Frees the tip an owner is already showing, if any, so the new one can take its place. A control may only
	/// have one tip, and callers reach this from both the mouse and the focus path, so the previous one is not
	/// always cleared first.
	/// </summary>
	private static void ReplaceExistingTip(Control owner)
	{
		if (_activeHoverTips.Remove(owner, out NHoverTipSet value))
		{
			value.QueueFreeSafely();
		}
	}

	/// <summary>
	/// The handler is stored so <see cref="M:MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.Remove(Godot.Control)" /> can disconnect it again, and is trusted only while the
	/// owner still reports it connected, so a disconnect from elsewhere costs a reconnect rather than leaving
	/// the owner with no handler at all.
	/// </summary>
	private static void ConnectTreeExiting(Control owner)
	{
		if (!_treeExitingHandlers.TryGetValue(owner, out var value) || !owner.IsConnected(Node.SignalName.TreeExiting, value))
		{
			Callable callable = Callable.From(delegate
			{
				Remove(owner);
			});
			_treeExitingHandlers[owner] = callable;
			owner.Connect(Node.SignalName.TreeExiting, callable);
		}
	}

	public static NHoverTipSet CreateAndShowMapPointHistory(Control owner, NMapPointHistoryHoverTip historyHoverTip)
	{
		ReplaceExistingTip(owner);
		NHoverTipSet nHoverTipSet = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip_set.tscn").Instantiate<NHoverTipSet>(PackedScene.GenEditState.Disabled);
		nHoverTipSet._owner = owner;
		HoverTipsContainer.AddChildSafely(nHoverTipSet);
		_activeHoverTips.Add(owner, nHoverTipSet);
		nHoverTipSet._textHoverTipContainer.AddChildSafely(historyHoverTip);
		historyHoverTip.ApplyRtlLayout();
		if (NGame.IsDebugHidingHoverTips)
		{
			nHoverTipSet.Visible = false;
		}
		ConnectTreeExiting(owner);
		return nHoverTipSet;
	}

	public override void _Ready()
	{
		_textHoverTipContainer = new VFlowContainer();
		_textHoverTipContainer.Name = _textHoverTipContainerStr;
		_textHoverTipContainer.MouseFilter = MouseFilterEnum.Ignore;
		this.AddChildSafely(_textHoverTipContainer);
		_cardHoverTipContainer = new NHoverTipCardContainer();
		_cardHoverTipContainer.Name = _cardHoverTipContainerStr;
		_cardHoverTipContainer.MouseFilter = MouseFilterEnum.Ignore;
		this.AddChildSafely(_cardHoverTipContainer);
	}

	public override void _Process(double delta)
	{
		if (_followOwner && _owner != null)
		{
			base.GlobalPosition = _owner.GlobalPosition - _followOffset + _extraOffset;
		}
	}

	private void Init(Control owner, IEnumerable<IHoverTip> hoverTips)
	{
		_owner = owner;
		foreach (IHoverTip item in IHoverTip.RemoveDupes(hoverTips))
		{
			if (item is HoverTip hoverTip)
			{
				Control control = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip.tscn").Instantiate<Control>(PackedScene.GenEditState.Disabled);
				_textHoverTipContainer.AddChildSafely(control);
				control.ApplyRtlLayout();
				MegaLabel node = control.GetNode<MegaLabel>("%Title");
				if (hoverTip.Title == null)
				{
					node.Visible = false;
				}
				else
				{
					node.SetTextAutoSize(hoverTip.Title);
				}
				control.GetNode<MegaRichTextLabel>("%Description").Text = hoverTip.Description;
				control.GetNode<MegaRichTextLabel>("%Description").AutowrapMode = (TextServer.AutowrapMode)(hoverTip.ShouldOverrideTextOverflow ? 0 : 3);
				control.GetNode<TextureRect>("%Icon").Texture = hoverTip.Icon;
				if (hoverTip.IsDebuff)
				{
					control.GetNode<CanvasItem>("%Bg").Material = PreloadManager.Cache.GetMaterial("res://materials/ui/hover_tip_debuff.tres");
				}
				control.ResetSize();
				if (_textHoverTipContainer.Size.Y + control.Size.Y + 5f < NGame.Instance.GetViewportRect().Size.Y - 50f)
				{
					_textHoverTipContainer.Size = new Vector2(360f, _textHoverTipContainer.Size.Y + control.Size.Y + 5f);
				}
				else
				{
					_textHoverTipContainer.Alignment = FlowContainer.AlignmentMode.Center;
				}
			}
			else
			{
				_cardHoverTipContainer.Add((CardHoverTip)item);
			}
			AbstractModel canonicalModel = item.CanonicalModel;
			if (!(canonicalModel is CardModel card))
			{
				if (!(canonicalModel is RelicModel relic))
				{
					if (canonicalModel is PotionModel potion)
					{
						SaveManager.Instance.MarkPotionAsSeen(potion);
					}
				}
				else
				{
					SaveManager.Instance.MarkRelicAsSeen(relic);
				}
			}
			else
			{
				SaveManager.Instance.MarkCardAsSeen(card);
			}
		}
	}

	/// <summary>
	/// Sets the position of the hover tip relative to the given node based on the alignment set.
	/// </summary>
	/// <param name="node">The control node that we are aligning the hover tip to</param>.
	/// <param name="alignment">Dictates the position of the hover tip relative to the given node.
	/// ie: top left side of the node, Top right side of the node</param>
	public void SetAlignment(Control node, HoverTipAlignment alignment)
	{
		switch (alignment)
		{
		case HoverTipAlignment.Center:
			_textHoverTipContainer.Position = Vector2.Zero;
			base.GlobalPosition = node.GlobalPosition + Vector2.Down * node.Size.Y * 1.5f;
			_cardHoverTipContainer.GlobalPosition += Vector2.Down * _textHoverTipContainer.Size.Y;
			_cardHoverTipContainer.LayoutResizeAndReposition(_cardHoverTipContainer.GlobalPosition, alignment);
			break;
		default:
			SetAlignment(new Rect2(node.GlobalPosition, node.Size * node.Scale), alignment);
			return;
		case HoverTipAlignment.None:
			break;
		}
		CorrectVerticalOverflow();
		CorrectHorizontalOverflow();
	}

	/// <summary>
	/// Left/Right alignment against an arbitrary global rect. Useful when the target is mid-tween and
	/// we want to align to where it will end up rather than where it currently is.
	/// </summary>
	public void SetAlignment(Rect2 globalRect, HoverTipAlignment alignment)
	{
		_textHoverTipContainer.Position = Vector2.Zero;
		switch (alignment)
		{
		case HoverTipAlignment.Left:
			_textHoverTipContainer.GlobalPosition = globalRect.Position;
			_textHoverTipContainer.Position += Vector2.Left * _textHoverTipContainer.Size.X;
			_textHoverTipContainer.ReverseFill = true;
			_cardHoverTipContainer.LayoutResizeAndReposition(globalRect.Position + new Vector2(globalRect.Size.X, 0f), HoverTipAlignment.Right);
			break;
		case HoverTipAlignment.Right:
			_cardHoverTipContainer.LayoutResizeAndReposition(globalRect.Position, HoverTipAlignment.Left);
			_textHoverTipContainer.GlobalPosition = globalRect.Position + new Vector2(globalRect.Size.X, 0f);
			break;
		}
		CorrectVerticalOverflow();
		CorrectHorizontalOverflow();
	}

	/// <summary>
	/// Align the hover tip for the given relic. We separate it because there are special positioning
	/// rules for relics.
	/// </summary>
	/// <param name="relic">The relic we are aligning too</param>
	public void SetAlignmentForRelic(NRelic relic)
	{
		HoverTipAlignment hoverTipAlignment = HoverTip.GetHoverTipAlignment(relic);
		Vector2 vector = relic.Icon.Size * relic.GetGlobalTransform().Scale;
		_textHoverTipContainer.GlobalPosition = relic.GlobalPosition + Vector2.Down * (vector.Y + 10f);
		if (hoverTipAlignment == HoverTipAlignment.Left)
		{
			_textHoverTipContainer.Position += Vector2.Left * (_textHoverTipContainer.Size.X - vector.X);
		}
		_cardHoverTipContainer.LayoutResizeAndReposition(_textHoverTipContainer.GlobalPosition + Vector2.Down * _textHoverTipContainer.Size.Y, hoverTipAlignment);
		if (hoverTipAlignment == HoverTipAlignment.Left)
		{
			_cardHoverTipContainer.GlobalPosition = new Vector2(_textHoverTipContainer.GlobalPosition.X, _cardHoverTipContainer.GlobalPosition.Y);
		}
		float y = NGame.Instance.GetViewportRect().Size.Y;
		if (relic.GlobalPosition.Y > y * 0.75f)
		{
			_textHoverTipContainer.GlobalPosition = relic.GlobalPosition + Vector2.Up * _textHoverTipContainer.Size.Y;
		}
		CorrectVerticalOverflow();
		CorrectHorizontalOverflow();
		if (_textHoverTipContainer.GetRect().Intersects(_cardHoverTipContainer.GetRect()))
		{
			if (hoverTipAlignment == HoverTipAlignment.Left)
			{
				_cardHoverTipContainer.GlobalPosition = _textHoverTipContainer.GlobalPosition + _textHoverTipContainer.Size.X * Vector2.Right;
			}
			else
			{
				_cardHoverTipContainer.GlobalPosition = _textHoverTipContainer.GlobalPosition + _cardHoverTipContainer.Size.X * Vector2.Left;
			}
			CorrectVerticalOverflow();
		}
	}

	/// <summary>
	/// Positions the hovertips given a Card. We separate it because there are special positioning
	/// rules for cards.
	/// </summary>
	/// <param name="holder">The holder that we are aligning to</param>
	public void SetAlignmentForCardHolder(NCardHolder holder)
	{
		HoverTipAlignment hoverTipAlignment = HoverTip.GetHoverTipAlignment(holder);
		_textHoverTipContainer.Position = Vector2.Zero;
		Control hitbox = holder.Hitbox;
		if (hoverTipAlignment == HoverTipAlignment.Left)
		{
			_textHoverTipContainer.GlobalPosition = hitbox.GlobalPosition;
			_textHoverTipContainer.Position += Vector2.Left * _textHoverTipContainer.Size.X - new Vector2(10f, 0f);
			_textHoverTipContainer.ReverseFill = true;
			_cardHoverTipContainer.LayoutResizeAndReposition(hitbox.GlobalPosition + new Vector2(hitbox.Size.X, 0f) * hitbox.Scale, HoverTipAlignment.Right);
		}
		else
		{
			Vector2 globalPosition = hitbox.GlobalPosition;
			if (holder.CardModel != null && (holder.CardModel.CurrentStarCost > 0 || holder.CardModel.HasStarCostX))
			{
				globalPosition += Vector2.Left * 15f;
			}
			_cardHoverTipContainer.LayoutResizeAndReposition(globalPosition, HoverTipAlignment.Left);
			_textHoverTipContainer.GlobalPosition = hitbox.GlobalPosition + new Vector2(hitbox.Size.X + 10f, 0f) * hitbox.Scale * holder.Scale;
		}
		CorrectVerticalOverflow();
		CorrectHorizontalOverflow();
		SetFollowOwner();
	}

	private void CorrectVerticalOverflow()
	{
		float y = NGame.Instance.GetViewportRect().Size.Y;
		if (_textHoverTipContainer.GlobalPosition.Y + _textHoverTipContainer.Size.Y > y)
		{
			_textHoverTipContainer.GlobalPosition = new Vector2(_textHoverTipContainer.GlobalPosition.X, y - _textHoverTipContainer.Size.Y);
		}
		if (_cardHoverTipContainer.GlobalPosition.Y + _cardHoverTipContainer.Size.Y > y)
		{
			_cardHoverTipContainer.GlobalPosition = new Vector2(_cardHoverTipContainer.GlobalPosition.X, y - _cardHoverTipContainer.Size.Y);
		}
	}

	private void CorrectHorizontalOverflow()
	{
		float x = NGame.Instance.GetViewportRect().Size.X;
		Vector2 globalPosition = _cardHoverTipContainer.GlobalPosition;
		float x2 = _cardHoverTipContainer.Size.X;
		Vector2 globalPosition2 = _textHoverTipContainer.GlobalPosition;
		float x3 = _textHoverTipContainer.Size.X;
		if (globalPosition.X + x2 <= x && globalPosition2.X + x3 > x)
		{
			float x4 = globalPosition.X - x3;
			_textHoverTipContainer.GlobalPosition = new Vector2(x4, globalPosition.Y);
		}
		else if (globalPosition.X + x2 > x || globalPosition2.X + x3 > x)
		{
			float x5 = globalPosition2.X + x3 - x2;
			_cardHoverTipContainer.GlobalPosition = new Vector2(x5, globalPosition.Y);
			_textHoverTipContainer.GlobalPosition += Vector2.Left * x2;
		}
		else if (globalPosition.X < 0f || globalPosition2.X < 0f)
		{
			float x6 = globalPosition2.X;
			_cardHoverTipContainer.GlobalPosition = new Vector2(x6, globalPosition.Y);
			_textHoverTipContainer.GlobalPosition += Vector2.Right * x2;
		}
	}

	public static void Clear()
	{
		foreach (Control key in _activeHoverTips.Keys)
		{
			Remove(key);
		}
	}

	public static void Remove(Control owner)
	{
		if (_activeHoverTips.Remove(owner, out NHoverTipSet value))
		{
			value.QueueFreeSafely();
		}
		if (_treeExitingHandlers.Remove(owner, out var value2) && GodotObject.IsInstanceValid(owner) && owner.IsConnected(Node.SignalName.TreeExiting, value2))
		{
			owner.Disconnect(Node.SignalName.TreeExiting, value2);
		}
	}

	/// <summary>
	/// Adds a static offset to a HoverTip when it's following a node.
	/// Used when the default offset doesn't have enough padding.
	/// See NLabPotionHolder for example.
	/// </summary>
	public void SetExtraFollowOffset(Vector2 offset)
	{
		_extraOffset = offset;
	}
}
