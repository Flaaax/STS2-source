using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.Multiplayer;

/// <summary>
/// Displays a list of all the players in the session.
/// </summary>
public partial class NMultiplayerPlayerStateContainer : Control
{
	private IRunState _runState;

	private readonly List<NMultiplayerPlayerState> _nodes = new List<NMultiplayerPlayerState>();

	private Tween? _tween;

	private bool _hidden;

	public NMultiplayerPlayerState? FirstPlayerState => GetChild<NMultiplayerPlayerState>(0);

	public override void _EnterTree()
	{
		ActiveScreenContext.Instance.Updated += UpdateNavigation;
	}

	public override void _ExitTree()
	{
		ActiveScreenContext.Instance.Updated -= UpdateNavigation;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionReleased(DebugHotkey.hideMpHealthBars))
		{
			base.Visible = !base.Visible;
			NGame.Instance.AddChildSafely(NFullscreenTextVfx.Create((!base.Visible) ? "Hide MP Health bars" : "Show MP Health bars"));
		}
	}

	public void Initialize(RunState runState)
	{
		_runState = runState;
		if (_runState.Players.Count <= 1)
		{
			return;
		}
		Player me = LocalContext.GetMe(_runState);
		NMultiplayerPlayerState nMultiplayerPlayerState = NMultiplayerPlayerState.Create(me);
		this.AddChildSafely(nMultiplayerPlayerState);
		_nodes.Add(nMultiplayerPlayerState);
		foreach (Player item in _runState.Players.Except(new global::_003C_003Ez__ReadOnlySingleElementList<Player>(me)))
		{
			NMultiplayerPlayerState nMultiplayerPlayerState2 = NMultiplayerPlayerState.Create(item);
			this.AddChildSafely(nMultiplayerPlayerState2);
			_nodes.Add(nMultiplayerPlayerState2);
		}
		UpdatePosition();
		NRun.Instance.GlobalUi.RelicInventory.Connect(NRelicInventory.SignalName.RelicsChanged, Callable.From(UpdatePositionAfterOneFrame));
		GetViewport().Connect(Viewport.SignalName.SizeChanged, Callable.From(UpdatePositionAfterOneFrame));
		for (int i = 0; i < GetChildCount(); i++)
		{
			Control hitbox = GetChild<NMultiplayerPlayerState>(i).Hitbox;
			hitbox.FocusNeighborLeft = hitbox.GetPath();
			hitbox.FocusNeighborTop = ((i > 0) ? GetChild<NMultiplayerPlayerState>(i - 1).Hitbox.GetPath() : null);
			hitbox.FocusNeighborBottom = ((i < GetChildCount() - 1) ? GetChild<NMultiplayerPlayerState>(i + 1).Hitbox.GetPath() : null);
		}
	}

	private void UpdateNavigation()
	{
		Control activeScreenProxy = NRun.Instance.GlobalUi.TopBar.ActiveScreenProxy;
		NodePath nodePath = (activeScreenProxy.IsValid() ? activeScreenProxy.GetPath() : null);
		for (int i = 0; i < GetChildCount(); i++)
		{
			Control hitbox = GetChild<NMultiplayerPlayerState>(i).Hitbox;
			hitbox.FocusNeighborTop = ((i > 0) ? GetChild<NMultiplayerPlayerState>(i - 1).Hitbox.GetPath() : null);
			hitbox.FocusNeighborBottom = ((i < GetChildCount() - 1) ? GetChild<NMultiplayerPlayerState>(i + 1).Hitbox.GetPath() : nodePath);
		}
	}

	/// <summary>
	/// Lock navigation so you cannot navigate away from the player states
	/// Important for controller navigation while potion targeting an ally
	/// </summary>
	public void LockNavigation()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			Control hitbox = GetChild<NMultiplayerPlayerState>(i).Hitbox;
			hitbox.FocusNeighborTop = ((i > 0) ? GetChild<NMultiplayerPlayerState>(i - 1).Hitbox.GetPath() : hitbox.GetPath());
			hitbox.FocusNeighborBottom = ((i < GetChildCount() - 1) ? GetChild<NMultiplayerPlayerState>(i + 1).Hitbox.GetPath() : hitbox.GetPath());
			hitbox.FocusNeighborLeft = hitbox.GetPath();
			hitbox.FocusNeighborRight = hitbox.GetPath();
		}
	}

	public void UnlockNavigation()
	{
		UpdateNavigation();
	}

	private void UpdatePositionAfterOneFrame()
	{
		TaskHelper.RunSafely(UpdatePositionAfterOneFrameAsync());
	}

	private async Task UpdatePositionAfterOneFrameAsync()
	{
		await this.AwaitProcessFrame();
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		base.Position = GetTargetPosition();
	}

	private Vector2 GetTargetPosition()
	{
		NRelicInventory relicInventory = NRun.Instance.GlobalUi.RelicInventory;
		int lineCount = relicInventory.GetLineCount();
		Vector2 result;
		if (lineCount == 0 || relicInventory.GetChildCount() == 0)
		{
			result = relicInventory.GetDefaultPosition();
		}
		else
		{
			float y = relicInventory.GetChild<Control>(0).Size.Y;
			float num = relicInventory.GetThemeConstant(ThemeConstants.FlowContainer.VSeparation, "FlowContainer");
			result = relicInventory.GetDefaultPosition() + (float)lineCount * (y + num) * Vector2.Down;
		}
		if (_hidden)
		{
			result.X = 0f - base.Size.X;
		}
		return result;
	}

	public void HighlightPlayer(Player player)
	{
		_nodes.FirstOrDefault((NMultiplayerPlayerState n) => n.Player == player)?.OnCreatureHovered();
	}

	public void UnhighlightPlayer(Player player)
	{
		_nodes.FirstOrDefault((NMultiplayerPlayerState n) => n.Player == player)?.OnCreatureUnhovered();
	}

	public void FlashPlayerReady(Player player)
	{
		_nodes.FirstOrDefault((NMultiplayerPlayerState n) => n.Player == player)?.FlashPlayerReady();
	}

	public void AnimHide()
	{
		_hidden = true;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(this, "position", GetTargetPosition(), 0.20000000298023224).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
		_tween.TweenProperty(this, "modulate:a", 0f, 0.20000000298023224).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
	}

	public void AnimShow()
	{
		_hidden = false;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(this, "position", GetTargetPosition(), 0.25).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		_tween.TweenProperty(this, "modulate:a", 1f, 0.15000000596046448).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
	}

	public void ShowImmediately()
	{
		_tween?.Kill();
		_hidden = false;
		base.Position = GetTargetPosition();
		Color modulate = base.Modulate;
		modulate.A = 1f;
		base.Modulate = modulate;
	}

	public void HideImmediately()
	{
		_tween?.Kill();
		_hidden = true;
		base.Position = GetTargetPosition();
		Color modulate = base.Modulate;
		modulate.A = 0f;
		base.Modulate = modulate;
	}
}
