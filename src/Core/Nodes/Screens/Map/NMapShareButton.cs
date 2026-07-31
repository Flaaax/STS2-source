using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Platform;
using Steamworks;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Map;

public partial class NMapShareButton : NButton
{
	private static readonly Color _activeButtonColor = new Color("7B1B15");

	private static readonly Color _inactiveButtonColor = new Color("000000C0");

	private static readonly Color _activeLabelColor = Colors.White;

	private static readonly Color _inactiveLabelColor = StsColors.halfTransparentWhite;

	private TextureRect _buttonImage;

	private MegaLabel _label;

	private Control _labelContainer;

	private HoverTip _hoverTip;

	private Tween? _tween;

	private NMapScreen? _mapScreen;

	private Control? _mapContainer;

	private Control? _mapBgContainer;

	private SubViewport? _subViewport;

	private Vector2 _toastPosition;

	private MegaLabel? _toast;

	public override void _Ready()
	{
		ConnectSignals();
		_buttonImage = GetNode<TextureRect>("ButtonImage");
		_labelContainer = GetNode<Control>("LabelContainer");
		_label = GetNode<MegaLabel>("LabelContainer/HBoxContainer/Label");
		LocString locString = new LocString("map", "SHARE.title");
		LocString description = ((PlatformUtil.PrimaryPlatform != PlatformType.Steam) ? new LocString("map", "SHARE.description.other") : new LocString("map", "SHARE.description.steam"));
		_hoverTip = new HoverTip(locString, description);
		_label.SetTextAutoSize(locString.GetFormattedText());
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One * 1.05f, 0.05);
		_tween.TweenProperty(_buttonImage, "modulate", _activeButtonColor, 0.05);
		_tween.TweenProperty(_labelContainer, "modulate", _activeLabelColor, 0.05);
		NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
		nHoverTipSet?.SetGlobalPosition(base.GlobalPosition - nHoverTipSet.Size + new Vector2(-10f, base.Size.Y));
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "scale", Vector2.One, 0.5).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		_tween.TweenProperty(_buttonImage, "modulate", _inactiveButtonColor, 0.1);
		_tween.TweenProperty(_labelContainer, "modulate", _inactiveLabelColor, 0.1);
		NHoverTipSet.Remove(this);
	}

	public void Initialize(NMapScreen mapScreen, Control mapContainer, Control mapBgContainer)
	{
		_mapScreen = mapScreen;
		_mapContainer = mapContainer;
		_mapBgContainer = mapBgContainer;
		_toast = mapScreen.GetNode<MegaLabel>("%ShareToast");
		_toastPosition = _toast.Position;
		_toast.SelfModulate = Colors.Transparent;
	}

	protected override void OnRelease()
	{
		if (!IsTakingScreenshot())
		{
			TaskHelper.RunSafely(CopyMapScreenshot());
		}
	}

	public bool IsTakingScreenshot()
	{
		return _subViewport != null;
	}

	private async Task CopyMapScreenshot()
	{
		if (_mapScreen == null || _mapContainer == null || _mapBgContainer == null)
		{
			Log.Error("Tried to take map screenshot when share button wasn't ready yet!");
			return;
		}
		Control topBar = NRun.Instance.GlobalUi.TopBar;
		Control debugInfo = NRun.Instance.GlobalUi.DebugInfo;
		NRelicInventory relicInventory = NRun.Instance.GlobalUi.RelicInventory;
		float topBarSize = relicInventory.GetBottomOfInventory().Y;
		_subViewport = new SubViewport();
		_subViewport.Size = new Vector2I(Mathf.RoundToInt(_mapBgContainer.Size.X), Mathf.RoundToInt(_mapBgContainer.Size.Y + topBarSize));
		List<(Control node, Node originalParent, Node tempParent, Vector2 position, int index)> borrowed = new List<(Control, Node, Node, Vector2, int)>();
		try
		{
			Control subViewportParent = new Control();
			_subViewport.AddChildSafely(subViewportParent);
			Borrow(_mapContainer, subViewportParent);
			_mapScreen.AddChildSafely(_subViewport);
			_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
			await this.AwaitProcessFrame();
			if (!GodotObject.IsInstanceValid(_mapScreen))
			{
				return;
			}
			subViewportParent.Size = _mapScreen.Size;
			subViewportParent.Position = -_mapBgContainer.Position + topBarSize * Vector2.Down;
			_mapContainer.Size = subViewportParent.Size;
			_mapContainer.Position = Vector2.Zero;
			Borrow(topBar, _subViewport);
			topBar.Position = Vector2.Zero;
			Borrow(debugInfo, _subViewport);
			Borrow(relicInventory, _subViewport);
			await _subViewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			Image image = _subViewport.GetTexture().GetImage();
			if (image == null)
			{
				Log.Error("Failed to capture map screenshot: subViewport produced no image.");
				return;
			}
			if (PlatformUtil.PrimaryPlatform == PlatformType.Steam)
			{
				if (image.GetFormat() != Image.Format.Rgb8)
				{
					image.Convert(Image.Format.Rgb8);
				}
				byte[] array = image.Data["data"].AsByteArray();
				SteamScreenshots.WriteScreenshot(array, (uint)array.Length, image.GetWidth(), image.GetHeight());
				ShowToast(new LocString("map", "SHARE_TOAST.description.steam"));
				return;
			}
			string text = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			string text2 = "user://map_screenshot_" + text + ".png";
			Error error = image.SavePng(text2);
			if (error != Error.Ok)
			{
				Log.Error($"Error {error}: Failed to save map screenshot to '{text2}'.");
			}
			else
			{
				TaskHelper.RunSafely(ShowConfirmation(text2));
			}
		}
		finally
		{
			try
			{
				foreach (var (control, node, node2, position, val) in borrowed.OrderBy<(Control, Node, Node, Vector2, int), int>(((Control node, Node originalParent, Node tempParent, Vector2 position, int index) entry) => entry.index))
				{
					if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(node) && control.GetParent() == node2)
					{
						control.Reparent(node, keepGlobalTransform: false);
						control.Position = position;
						node.MoveChildSafely(control, Math.Min(val, node.GetChildCount() - 1));
					}
				}
			}
			finally
			{
				_subViewport.QueueFreeSafely();
				_subViewport = null;
			}
		}
		void Borrow(Control control2, Node tempParent)
		{
			borrowed.Add((control2, control2.GetParent(), tempParent, control2.Position, control2.GetIndex()));
			control2.Reparent(tempParent, keepGlobalTransform: false);
		}
	}

	private static async Task ShowConfirmation(string screenshotPath)
	{
		screenshotPath = ProjectSettings.GlobalizePath(screenshotPath);
		LocString locString = new LocString("map", "SHARE_POPUP.description");
		locString.Add("path", screenshotPath);
		NGenericPopup nGenericPopup = NGenericPopup.Create();
		NModalContainer.Instance.Add(nGenericPopup);
		if (await nGenericPopup.WaitForConfirmation(locString, new LocString("map", "SHARE_POPUP.title"), new LocString("main_menu_ui", "GENERIC_POPUP.ok"), new LocString("map", "SHARE_POPUP.open")))
		{
			Error error = OS.ShellShowInFileManager(screenshotPath);
			if (error != Error.Ok)
			{
				Log.Error($"Error {error}: Cannot open OS file manager. Screenshot saved to '{screenshotPath}'");
			}
		}
	}

	private void ShowToast(LocString locString)
	{
		if (_toast == null)
		{
			throw new InvalidOperationException("Tried to show toast before initialized!");
		}
		_toast.SetTextAutoSize(locString.GetFormattedText());
		_toast.Position = _toastPosition;
		_toast.SelfModulate = Colors.White;
		Tween tween = _toast.CreateTween();
		tween.TweenProperty(_toast, "position:y", _toastPosition.Y - 40f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		tween.TweenInterval(2.0);
		tween.TweenProperty(_toast, "self_modulate:a", 0f, 0.5);
	}
}
