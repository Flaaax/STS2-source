using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Proceed Button. The Man. The Myth. The Legend. A Button.
/// </summary>
public partial class NProceedButton : NButton
{
	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	private Control _outline;

	private Control _buttonImage;

	private MegaLabel _label;

	private ShaderMaterial _hsv;

	private Viewport _viewport;

	private NMultiplayerVoteContainer _voteContainer;

	private Color _defaultOutlineColor = StsColors.cream;

	private Color _hoveredOutlineColor = StsColors.gold;

	private Color _downColor = Colors.Gray;

	private Color _outlineColor = new Color("FFCC00C0");

	private Color _outlineTransparentColor = new Color("FF000000");

	private Tween? _animTween;

	private Tween? _glowTween;

	private Tween? _hoverTween;

	private static readonly Vector2 _hoverScale = Vector2.One * 1.05f;

	private float _elapsedTime;

	private bool _shouldPulse = true;

	private static readonly Vector2 _showPosRatio = new Vector2(1583f, 764f) / NGame.devResolution;

	private static readonly Vector2 _hidePosRatio = _showPosRatio + new Vector2(400f, 0f) / NGame.devResolution;

	private IRunState? _runState;

	public static LocString ProceedLoc => new LocString("gameplay_ui", "PROCEED_BUTTON");

	public static LocString SkipLoc => new LocString("gameplay_ui", "CHOOSE_CARD_SKIP_BUTTON");

	public bool IsSkip { get; private set; }

	protected override string[] Hotkeys => new string[1] { MegaInput.confirm };

	private Vector2 ShowPos => _showPosRatio * _viewport.GetVisibleRect().Size;

	private Vector2 HidePos => _hidePosRatio * _viewport.GetVisibleRect().Size;

	public override void _Ready()
	{
		ConnectSignals();
		_outline = GetNode<Control>("%Outline");
		_buttonImage = GetNode<Control>("%Image");
		_hsv = (ShaderMaterial)_buttonImage.GetMaterial();
		_label = GetNode<MegaLabel>("%Label");
		_voteContainer = GetNode<NMultiplayerVoteContainer>("%MultiplayerVoteContainer");
		_viewport = GetViewport();
		base.Position = HidePos;
		Disable();
		NGame.Instance.DebugToggleProceedButton += DebugToggleVisibility;
		RunManager.Instance.MapSelectionSynchronizer.PlayerVoteChanged += OnPlayerVoteChanged;
		RunManager.Instance.MapSelectionSynchronizer.PlayerVoteCancelled += OnPlayerVoteCancelled;
		RunManager.Instance.FlavorSynchronizer.MapPingReceived += OnMapPingReceived;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		NGame.Instance.DebugToggleProceedButton -= DebugToggleVisibility;
		RunManager.Instance.MapSelectionSynchronizer.PlayerVoteChanged -= OnPlayerVoteChanged;
		RunManager.Instance.MapSelectionSynchronizer.PlayerVoteCancelled -= OnPlayerVoteCancelled;
		RunManager.Instance.FlavorSynchronizer.MapPingReceived -= OnMapPingReceived;
	}

	public void Initialize(IRunState runState)
	{
		_runState = runState;
		_voteContainer.Initialize(ShouldShowMapVote, runState.Players);
	}

	/// <summary>
	/// Call when we want this button to animate in.
	/// We refresh a lot of values here as the screen may not have been disposed and we reuse the button.
	/// May need to separate this logic into a Refresh() call for organization.
	/// </summary>
	protected override void OnEnable()
	{
		base.OnEnable();
		if (NGame.IsDebugHidingProceedButton)
		{
			base.Modulate = Colors.Transparent;
		}
		base.Scale = Vector2.One;
		_buttonImage.SelfModulate = StsColors.gray;
		UpdateShaderS(1f);
		UpdateShaderV(1f);
		_animTween?.Kill();
		_animTween = CreateTween().SetParallel();
		_animTween.TweenProperty(this, "position", ShowPos, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_animTween.TweenProperty(_buttonImage, "self_modulate", Colors.White, 0.5);
		_hoverTween?.Kill();
		_hoverTween = CreateTween().SetParallel();
		_hoverTween.TweenProperty(_outline, "modulate", Colors.White, 0.5);
		if (_shouldPulse)
		{
			StartGlowTween();
		}
	}

	/// <summary>
	/// Call when we want this button to hide this button (Disables clickability/hotkeys)
	/// </summary>
	protected override void OnDisable()
	{
		base.OnDisable();
		_animTween?.Kill();
		_animTween = CreateTween().SetParallel();
		_animTween.TweenProperty(this, "position", HidePos, 0.8).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_animTween.TweenProperty(_buttonImage, "self_modulate", StsColors.gray, 0.8);
		_animTween.TweenProperty(_outline, "modulate", StsColors.transparentWhite, 0.8);
		_glowTween?.Kill();
	}

	protected override void OnRelease()
	{
		if (_isEnabled)
		{
			_hoverTween?.Kill();
			_hoverTween = CreateTween().SetParallel();
			_hoverTween.TweenProperty(this, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
			_hoverTween.TweenProperty(_buttonImage, "modulate", Colors.White, 0.5);
			_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderV), _hsv.GetShaderParameter(_v), 1f, 0.05);
			_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderS), _hsv.GetShaderParameter(_s), 1f, 0.05);
		}
	}

	protected override void OnFocus()
	{
		_hoverTween?.Kill();
		_hoverTween = CreateTween().SetParallel();
		_hoverTween.TweenProperty(this, "scale", _hoverScale, 0.05);
		_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderV), _hsv.GetShaderParameter(_v), 1.4f, 0.05);
		_glowTween?.Kill();
		_glowTween = CreateTween();
		_glowTween.TweenProperty(_outline, "self_modulate:a", 1f, 0.05);
	}

	protected override void OnUnfocus()
	{
		_hoverTween?.Kill();
		_hoverTween = CreateTween().SetParallel();
		_hoverTween.TweenProperty(this, "scale", Vector2.One, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_hoverTween.TweenProperty(_buttonImage, "modulate", Colors.White, 0.5);
		_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderV), _hsv.GetShaderParameter(_v), 1f, 0.05);
		_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderS), _hsv.GetShaderParameter(_s), 1f, 0.05);
		if (_shouldPulse)
		{
			StartGlowTween();
		}
		else
		{
			StopGlowTween();
		}
	}

	protected override void OnPress()
	{
		_hoverTween?.Kill();
		_hoverTween = CreateTween().SetParallel();
		_hoverTween.TweenProperty(this, "scale", Vector2.One * 0.95f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
		_hoverTween.TweenProperty(_buttonImage, "modulate", StsColors.gray, 0.5);
		_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderV), _hsv.GetShaderParameter(_v), 1f, 0.25);
		_hoverTween.TweenMethod(Callable.From<float>(UpdateShaderS), _hsv.GetShaderParameter(_s), 0.8f, 0.25);
		StopGlowTween();
	}

	public void UpdateText(LocString loc)
	{
		_label.SetTextAutoSize(loc.GetFormattedText());
		IsSkip = loc.LocEntryKey == SkipLoc.LocEntryKey;
	}

	private void DebugToggleVisibility()
	{
		base.Modulate = (NGame.IsDebugHidingProceedButton ? Colors.Transparent : Colors.White);
	}

	public void SetPulseState(bool isPulsing)
	{
		_shouldPulse = isPulsing;
		if (isPulsing)
		{
			StartGlowTween();
		}
		else
		{
			StopGlowTween();
		}
	}

	private void StartGlowTween()
	{
		_glowTween?.Kill();
		_glowTween = CreateTween().SetLoops();
		_glowTween.TweenProperty(_outline, "self_modulate:a", 0.25f, 0.5);
		_glowTween.TweenProperty(_outline, "self_modulate:a", 0.75f, 0.5);
	}

	private void StopGlowTween()
	{
		_glowTween?.Kill();
		_glowTween = CreateTween();
		_glowTween.TweenProperty(_outline, "self_modulate:a", 0f, 0.5);
	}

	private void UpdateShaderS(float value)
	{
		_hsv.SetShaderParameter(_s, value);
	}

	private void UpdateShaderV(float value)
	{
		_hsv.SetShaderParameter(_v, value);
	}

	/// <summary>
	/// Called when a player votes for a map point.
	/// This is used to refresh the player heads container when in Blitz mode, where pressing the proceed button votes
	/// to move on to the next map point.
	/// </summary>
	private void OnPlayerVoteChanged(Player player, MapVote? oldLocation, MapVote? newLocation)
	{
		_voteContainer.RefreshPlayerVotes();
	}

	/// <summary>
	/// Called when a player cancels their vote to move on.
	/// This is used to refresh the player heads container when in Blitz mode, where pressing the proceed button votes
	/// to move on to the next map point.
	/// </summary>
	private void OnPlayerVoteCancelled(Player player, MapVote? oldLocation)
	{
		_voteContainer.RefreshPlayerVotes();
	}

	/// <summary>
	/// Called when a player attempts to ping.
	/// This is called when ANY ping occurs, but should only do something when the proceed button is double-pressed in
	/// Blitz mode.
	/// </summary>
	private void OnMapPingReceived(Player player, MapCoord coord)
	{
		if (player.RunState.GameMode == GameMode.Blitz && base.Visible)
		{
			NMapPingVfx nMapPingVfx = NMapPingVfx.Create();
			nMapPingVfx.Modulate = player.Character.MapDrawingColor;
			this.AddChildSafely(nMapPingVfx);
			this.MoveChildSafely(nMapPingVfx, 0);
			nMapPingVfx.Size *= base.Size.Y * (1f / 64f);
			nMapPingVfx.PivotOffset = nMapPingVfx.Size * 0.5f;
			nMapPingVfx.Position = base.Size * 0.5f - nMapPingVfx.PivotOffset;
			NRun.Instance.GlobalUi.MultiplayerPlayerContainer.FlashPlayerReady(player);
			NDebugAudioManager.Instance.Play("map_ping.mp3", 1f, PitchVariance.Medium);
		}
	}

	/// <summary>
	/// Called by the player vote container to know whether it should show the player head for the given player.
	/// </summary>
	private bool ShouldShowMapVote(Player player)
	{
		IRunState? runState = _runState;
		if (runState == null || runState.GameMode != GameMode.Blitz)
		{
			return false;
		}
		return RunManager.Instance.MapSelectionSynchronizer.GetVote(player).HasValue;
	}
}
