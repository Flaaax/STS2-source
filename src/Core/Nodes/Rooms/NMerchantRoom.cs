using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Rooms;

/// <summary>
/// Manages the starting UI flow for a merchant room. i.e. opening the inventory and proceeding to the map.
/// This does not include the merchant items themselves. Those are handled by <see cref="T:MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory" />.
/// </summary>
public abstract partial class NMerchantRoom : Control, IScreenContext, IRoomWithProceedButton
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("rooms/merchant_room");

	protected readonly List<Player> _players = new List<Player>();

	protected MerchantDialogueSet _dialogue;

	private NProceedButton _proceedButton;

	private Control _characterContainer;

	private readonly List<NMerchantCharacter> _playerVisuals = new List<NMerchantCharacter>();

	public static NMerchantRoom? Instance => NRun.Instance?.MerchantRoom;

	public NProceedButton ProceedButton => _proceedButton;

	public NMerchantInventory Inventory { get; private set; }

	public abstract NMerchantButton MerchantButton { get; }

	public IReadOnlyList<NMerchantCharacter> PlayerVisuals => _playerVisuals;

	public Control DefaultFocusedControl => this;

	public override void _Ready()
	{
		_proceedButton = GetNode<NProceedButton>("%ProceedButton");
		_proceedButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnProceedButtonReleased));
		_proceedButton.UpdateText(NProceedButton.ProceedLoc);
		_proceedButton.SetPulseState(isPulsing: false);
		_proceedButton.Initialize(_players[0].RunState);
		_proceedButton.Enable();
		Inventory = GetNode<NMerchantInventory>("%Inventory");
		Inventory.MouseFilter = MouseFilterEnum.Ignore;
		Inventory.Initialize(RunManager.Instance.MerchantSynchronizer.GetLocalInventory(), _dialogue);
		_characterContainer = GetNode<Control>("%CharacterContainer");
		NMapScreen.Instance.SetTravelEnabled(enabled: true);
		NGame.Instance.SetScreenShakeTarget(this);
		AfterRoomIsLoaded();
	}

	public override void _EnterTree()
	{
		ActiveScreenContext.Instance.Updated += OnActiveScreenUpdated;
		NMapScreen.Instance.Connect(NMapScreen.SignalName.Opened, Callable.From(ToggleMerchantTrack));
		NMapScreen.Instance.Connect(NMapScreen.SignalName.Closed, Callable.From(ToggleMerchantTrack));
	}

	public override void _ExitTree()
	{
		NGame.Instance.ClearScreenShakeTarget();
		ActiveScreenContext.Instance.Updated -= OnActiveScreenUpdated;
		NMapScreen.Instance.Disconnect(NMapScreen.SignalName.Opened, Callable.From(ToggleMerchantTrack));
		NMapScreen.Instance.Disconnect(NMapScreen.SignalName.Closed, Callable.From(ToggleMerchantTrack));
	}

	public void FoulPotionThrown(FoulPotion potion)
	{
		SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
		LocString locString = Rng.Chaotic.NextItem(_dialogue.FoulPotionLines);
		if (locString != null)
		{
			NSpeechBubbleVfx nSpeechBubbleVfx = MerchantButton.PlayDialogue(locString);
			if (nSpeechBubbleVfx != null)
			{
				NGame.Instance?.ScreenRumble(ShakeStrength.Medium, ShakeDuration.Short, RumbleStyle.Rumble);
			}
		}
	}

	private void ToggleMerchantTrack()
	{
		NRunMusicController.Instance?.ToggleMerchantTrack();
	}

	private void AfterRoomIsLoaded()
	{
		Player me = LocalContext.GetMe(_players);
		_players.Remove(me);
		_players.Insert(0, me);
		int num = Mathf.CeilToInt(Mathf.Sqrt(_players.Count));
		for (int i = 0; i < num; i++)
		{
			float num2 = -140f * (float)i;
			for (int j = 0; j < num; j++)
			{
				int num3 = i * num + j;
				if (num3 >= _players.Count)
				{
					break;
				}
				NMerchantCharacter nMerchantCharacter = PreloadManager.Cache.GetScene(_players[num3].Character.MerchantAnimPath).Instantiate<NMerchantCharacter>(PackedScene.GenEditState.Disabled);
				_characterContainer.AddChildSafely(nMerchantCharacter);
				_characterContainer.MoveChildSafely(nMerchantCharacter, 0);
				nMerchantCharacter.Position = new Vector2(num2, -50f * (float)i);
				if (i > 0)
				{
					nMerchantCharacter.Modulate = new Color(0.5f, 0.5f, 0.5f);
				}
				num2 -= 275f;
				_playerVisuals.Add(nMerchantCharacter);
			}
		}
	}

	private void OnProceedButtonReleased(NButton _)
	{
		if (!MerchantFtueCheck())
		{
			RunManager.Instance.Proceed();
		}
	}

	private bool MerchantFtueCheck()
	{
		if (!SaveManager.Instance.SeenFtue("merchant_ftue"))
		{
			NModalContainer.Instance.Add(NMerchantFtue.Create(this));
			SaveManager.Instance.MarkFtueAsComplete("merchant_ftue");
			return true;
		}
		return false;
	}

	protected void OnMerchantOpened(NMerchantButton _)
	{
		OpenInventory();
	}

	public abstract void OpenInventory();

	private void OnActiveScreenUpdated()
	{
		this.UpdateControllerNavEnabled();
		if (ActiveScreenContext.Instance.IsCurrent(this))
		{
			MerchantButton.Enable();
			_proceedButton.Enable();
		}
		else
		{
			MerchantButton.Disable();
			_proceedButton.Disable();
		}
	}
}
