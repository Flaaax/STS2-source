using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

public partial class NDiscoveredItem : NButton
{
	private HoverTip _hoverTip;

	private static readonly Vector2 _hoverTipOffset = new Vector2(-76f, -200f);

	private NSelectionReticle _selectionReticle;

	private MegaLabel _label;

	public override void _Ready()
	{
		ConnectSignals();
		_selectionReticle = GetNode<NSelectionReticle>("%SelectionReticle");
		_label = GetNode<MegaLabel>("%Label");
	}

	public void SetHoverTip(HoverTip hoverTip)
	{
		_hoverTip = hoverTip;
	}

	public void SetText(string str)
	{
		_label.SetTextAutoSize(str);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		NHoverTipSet.CreateAndShow(this, _hoverTip)?.SetGlobalPosition(base.GlobalPosition + _hoverTipOffset);
		if (NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_selectionReticle.OnSelect();
		}
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
		_selectionReticle.OnDeselect();
	}

	protected override void OnPress()
	{
	}
}
