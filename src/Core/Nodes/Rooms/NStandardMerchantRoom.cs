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

public partial class NStandardMerchantRoom : NMerchantRoom
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("rooms/standard_merchant_room");

	private NMerchantButton _merchantButton;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public override NMerchantButton MerchantButton => _merchantButton;

	public static NStandardMerchantRoom? Create(IReadOnlyList<Player> players)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NStandardMerchantRoom nStandardMerchantRoom = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NStandardMerchantRoom>(PackedScene.GenEditState.Disabled);
		nStandardMerchantRoom._players.AddRange(players);
		nStandardMerchantRoom._dialogue = MerchantRoom.Dialogue;
		return nStandardMerchantRoom;
	}

	public override void _Ready()
	{
		base._Ready();
		Player me = LocalContext.GetMe(_players);
		_merchantButton = GetNode<NMerchantButton>("%MerchantButton");
		_merchantButton.IsLocalPlayerDead = me.Creature.IsDead;
		_merchantButton.PlayerDeadLines = _dialogue.PlayerDeadLines;
		_merchantButton.Connect(NMerchantButton.SignalName.MerchantOpened, Callable.From<NMerchantButton>(base.OnMerchantOpened));
		NMapScreen.Instance.SetTravelEnabled(enabled: true);
		NGame.Instance.SetScreenShakeTarget(this);
	}

	public override void OpenInventory()
	{
		if (!base.Inventory.IsOpen)
		{
			base.ProceedButton.Disable();
			base.Inventory.Open();
			MerchantButton.Disable();
			base.Inventory.Connect(NMerchantInventory.SignalName.InventoryClosed, Callable.From(delegate
			{
				MerchantButton.Enable();
				base.ProceedButton.Enable();
				base.ProceedButton.SetPulseState(isPulsing: true);
			}), 4u);
		}
	}
}
