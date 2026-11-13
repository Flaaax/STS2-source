using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Rooms;

public partial class NBlitzMerchantRoom : NMerchantRoom
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("rooms/blitz_merchant_room");

	private NBlitzMerchantButton _merchantButton;

	private Control _bgContainer;

	private BackgroundAssets _backgroundAssets;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public override NMerchantButton MerchantButton => _merchantButton;

	public static NBlitzMerchantRoom? Create(IReadOnlyList<Player> players, BackgroundAssets backgroundAssets)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NBlitzMerchantRoom nBlitzMerchantRoom = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBlitzMerchantRoom>(PackedScene.GenEditState.Disabled);
		nBlitzMerchantRoom._players.AddRange(players);
		nBlitzMerchantRoom._dialogue = MerchantRoom.Dialogue;
		nBlitzMerchantRoom._backgroundAssets = backgroundAssets;
		return nBlitzMerchantRoom;
	}

	public override void _Ready()
	{
		base._Ready();
		Player me = LocalContext.GetMe(_players);
		_merchantButton = GetNode<NBlitzMerchantButton>("%MerchantButton");
		_merchantButton.IsLocalPlayerDead = me.Creature.IsDead;
		_merchantButton.PlayerDeadLines = _dialogue.PlayerDeadLines;
		_merchantButton.Connect(NMerchantButton.SignalName.MerchantOpened, Callable.From<NMerchantButton>(base.OnMerchantOpened));
		_bgContainer = GetNode<Control>("%BgContainer");
		NCombatBackground child = NCombatBackground.Create(_backgroundAssets);
		_bgContainer.AddChildSafely(child);
		_merchantButton.Setup(base.Inventory.Inventory);
		NMapScreen.Instance.SetTravelEnabled(enabled: true);
		NGame.Instance.SetScreenShakeTarget(this);
	}

	public override void OpenInventory()
	{
		if (base.Inventory.IsOpen)
		{
			return;
		}
		base.ProceedButton.Disable();
		base.Inventory.Open();
		MerchantButton.Disable();
		base.Inventory.Connect(NMerchantInventory.SignalName.InventoryClosed, Callable.From(delegate
		{
			MerchantButton.Enable();
			base.ProceedButton.Enable();
			base.ProceedButton.SetPulseState(isPulsing: true);
			if (base.Inventory.Inventory.IsBlitzShop && base.Inventory.Inventory.BlitzChoicesRemaining <= 0)
			{
				_merchantButton.CompleteShop();
			}
		}), 4u);
	}
}
