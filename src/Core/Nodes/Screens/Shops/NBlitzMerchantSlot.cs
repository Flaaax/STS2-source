using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Shops;

/// <summary>
/// UI wrapper class for MerchantEntry.
/// </summary>
public partial class NBlitzMerchantSlot : NMerchantSlot
{
	private BlitzMerchantEntry _blitzEntry;

	private TextureRect _icon;

	private TextureRect _shadow;

	private GpuParticles2D _rareVfx;

	private GpuParticles2D _cursedVfx;

	private bool _alreadyDisabled;

	public override MerchantEntry Entry => _blitzEntry;

	protected override CanvasItem Visual => _icon;

	protected override Vector2 HoverScale => Vector2.One * 1.1f;

	protected override Vector2 SmallScale => Vector2.One;

	public override Vector2 MerchantHandOffset => base.MerchantHandOffset + Vector2.Left * 18f;

	public override void _Ready()
	{
		ConnectSignals();
		_icon = GetNode<TextureRect>("%Icon");
		_shadow = GetNode<TextureRect>("%Shadow");
		_rareVfx = GetNode<GpuParticles2D>("%RareVfx");
		_cursedVfx = GetNode<GpuParticles2D>("%CursedVfx");
	}

	public void FillSlot(BlitzMerchantEntry entry)
	{
		_blitzEntry = entry;
		_blitzEntry.EntryUpdated += UpdateVisual;
		_blitzEntry.PurchaseFailed += base.OnPurchaseFailed;
		_blitzEntry.PurchaseCompleted += OnSuccessfulPurchase;
		Texture2D icon = _blitzEntry.Icon;
		_icon.Texture = icon;
		_shadow.Texture = icon;
		_rareVfx.Visible = _blitzEntry.IsRare;
		_cursedVfx.Visible = _blitzEntry.IsCursed;
		UpdateVisual();
	}

	protected override void UpdateVisual()
	{
		base.UpdateVisual();
		if (_blitzEntry.Status != BlitzMerchantEntry.BlitzEntryStatus.Available)
		{
			if (!_alreadyDisabled)
			{
				_hitbox.MouseFilter = MouseFilterEnum.Ignore;
				_alreadyDisabled = true;
				base.FocusMode = FocusModeEnum.None;
				if (_blitzEntry.Status == BlitzMerchantEntry.BlitzEntryStatus.Used)
				{
					_icon.SetVisible(visible: false);
					_rareVfx.SetVisible(visible: false);
					_cursedVfx.SetVisible(visible: false);
				}
				else
				{
					base.Modulate = Colors.DarkGray;
				}
			}
		}
		else
		{
			base.MouseFilter = MouseFilterEnum.Stop;
			_costLabel.Visible = true;
			base.FocusMode = FocusModeEnum.All;
		}
		ClearHoverTip();
	}

	private void OnSuccessfulPurchase(PurchaseStatus _, MerchantEntry __)
	{
		TriggerMerchantHandToPointHere();
		UpdateVisual();
	}

	protected override void CreateHoverTip()
	{
		HoverTipAlignment alignment = ((GetIndex() % 3 == 2) ? HoverTipAlignment.Left : HoverTipAlignment.Right);
		NHoverTipSet.CreateAndShow(this, _blitzEntry.HoverTips)?.SetAlignment(_hitbox, alignment);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (base.IsInitialized)
		{
			_blitzEntry.EntryUpdated -= UpdateVisual;
			_blitzEntry.PurchaseFailed -= base.OnPurchaseFailed;
			_blitzEntry.PurchaseCompleted -= OnSuccessfulPurchase;
		}
	}
}
