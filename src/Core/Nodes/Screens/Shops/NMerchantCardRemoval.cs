using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Shops;

public partial class NMerchantCardRemoval : NMerchantSlot
{
	private const string _locTable = "merchant_room";

	private Sprite2D _removalVisual;

	private AnimationPlayer _animator;

	private Control _costContainer;

	private bool _isUnavailable;

	private MerchantCardRemovalEntry _removalEntry;

	private LocString Title => new LocString("merchant_room", "MERCHANT.cardRemovalService.title");

	private LocString Description => new LocString("merchant_room", "MERCHANT.cardRemovalService.description");

	public override MerchantEntry Entry => _removalEntry;

	protected override CanvasItem Visual => _removalVisual;

	public override void _Ready()
	{
		ConnectSignals();
		_removalVisual = GetNode<Sprite2D>("%Visual");
		_animator = GetNode<AnimationPlayer>("%Animation");
		_costContainer = GetNode<Control>("Cost");
	}

	public void FillSlot(MerchantCardRemovalEntry removalEntry)
	{
		_removalEntry = removalEntry;
		_removalEntry.EntryUpdated += UpdateVisual;
		_removalEntry.PurchaseFailed += base.OnPurchaseFailed;
		_removalEntry.PurchaseCompleted += OnSuccessfulPurchase;
		UpdateVisual();
	}

	protected override void UpdateVisual()
	{
		base.UpdateVisual();
		if (!_isUnavailable)
		{
			if (_removalEntry.Used)
			{
				_hitbox.MouseFilter = MouseFilterEnum.Ignore;
				_animator.CurrentAnimation = "Used";
				_isUnavailable = true;
				_animator.Play();
				_costLabel.Visible = false;
				_costContainer.Visible = false;
				base.FocusMode = FocusModeEnum.None;
			}
			else
			{
				base.MouseFilter = MouseFilterEnum.Stop;
				_costLabel.Visible = true;
				_costLabel.SetTextAutoSize(_removalEntry.Cost.ToString());
				_costLabel.Modulate = (_removalEntry.EnoughGold ? StsColors.cream : StsColors.red);
				_costContainer.Visible = true;
				base.FocusMode = FocusModeEnum.All;
			}
			ClearHoverTip();
		}
	}

	protected void OnSuccessfulPurchase(PurchaseStatus _, MerchantEntry __)
	{
		TriggerMerchantHandToPointHere();
		UpdateVisual();
	}

	public void OnCardRemovalUsed()
	{
		UpdateVisual();
	}

	protected override void CreateHoverTip()
	{
		LocString title = Title;
		LocString description = Description;
		description.Add("Amount", MerchantCardRemovalEntry.PriceIncrease);
		NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, new HoverTip(title, description));
		if (nHoverTipSet != null)
		{
			nHoverTipSet.GlobalPosition = base.GlobalPosition;
			bool flag = base.GlobalPosition.X > GetViewport().GetVisibleRect().Size.X * 0.5f;
			nHoverTipSet.SetAlignment(GetHoveredHitboxGlobalRect(), flag ? HoverTipAlignment.Left : HoverTipAlignment.Right);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (base.IsInitialized)
		{
			_removalEntry.EntryUpdated -= UpdateVisual;
			_removalEntry.PurchaseFailed -= base.OnPurchaseFailed;
			_removalEntry.PurchaseCompleted -= OnSuccessfulPurchase;
		}
	}
}
