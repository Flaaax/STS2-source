using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NByrdEvolutionVfx : Control, IOverlayScreen, IScreenContext
{
	private const string _byrdEvolutionMusic = "event:/music/byrdlett_evolution_music";

	private const string _byrdEvolutionSfx = "event:/sfx/byrdpip/byrdlett_evolution";

	private GpuParticles2D _finaleParticles;

	private GpuParticles2D _powerParticles;

	private MegaSprite _byrdpipSprite;

	private MegaSprite _byrdineSprite;

	private Node2D _byrdpipContainer;

	private Node2D _byrdineContainer;

	private float _curTweenDuration = 0.5f;

	private float _minTweenDuration = 0.05f;

	private float _accelerationMultiplier = 0.85f;

	private int _shrinkReps = 10;

	private int _repsToShowPowerAt = 10;

	private int _repsToStopPowerAt;

	private float _powerParticlesSpeedIncrement = 1.11f;

	private float _powerParticlesBaseSpeed = 3f;

	private float _pipTranslateTime = 0.5f;

	private float _pipHoldTime = 0.25f;

	private Tween? _byrdpipTween;

	private Tween? _byrdineTween;

	private Tween? _particleDelayTween;

	private Tween? _lifetimeTween;

	private Tween? _startTween;

	private bool _audioStarted;

	private bool _finaleStarted;

	private Vector2 _basePipPosition;

	private Tween? _fadeTween;

	private Byrdpip _byrdpip;

	private Byrdine _byrdine;

	protected readonly TaskCompletionSource _completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	private static string ScenePath => SceneHelper.GetScenePath("vfx/byrd_evolution_vfx");

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(ScenePath);

	public Control DefaultFocusedControl => this;

	public NetScreenType ScreenType => NetScreenType.None;

	public bool UseSharedBackstop => true;

	public static NByrdEvolutionVfx ShowScreen(Byrdpip byrdpip, Byrdine byrdine)
	{
		NByrdEvolutionVfx nByrdEvolutionVfx = PreloadManager.Cache.GetScene(ScenePath).Instantiate<NByrdEvolutionVfx>(PackedScene.GenEditState.Disabled);
		nByrdEvolutionVfx._byrdpip = byrdpip;
		nByrdEvolutionVfx._byrdine = byrdine;
		NOverlayStack.Instance.Push(nByrdEvolutionVfx);
		return nByrdEvolutionVfx;
	}

	public override void _Ready()
	{
		_finaleParticles = GetNode<GpuParticles2D>("%FinaleParticles");
		_powerParticles = GetNode<GpuParticles2D>("%PowerParticles");
		_byrdpipContainer = GetNode<Node2D>("%ByrdpipContainer");
		_byrdineContainer = GetNode<Node2D>("%ByrdineContainer");
		Node2D node = GetNode<Node2D>("%ByrdpipSprite");
		Node2D node2 = GetNode<Node2D>("%ByrdineSprite");
		_byrdpipSprite = new MegaSprite(node);
		_byrdineSprite = new MegaSprite(node2);
		_byrdineContainer.Scale = Vector2.Zero;
		this.RunWhenSpineReady(_byrdpipSprite, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("stand_still");
		});
		this.RunWhenSpineReady(_byrdineSprite, delegate(MegaAnimationState animState)
		{
			animState.SetAnimation("stand_still");
		});
		_byrdpipContainer.Modulate = new Color(10f, 10f, 10f);
		_byrdineContainer.Modulate = new Color(10f, 10f, 10f);
		_finaleParticles.Emitting = false;
		_finaleParticles.OneShot = true;
		_powerParticles.Emitting = false;
		_powerParticles.SpeedScale = _powerParticlesBaseSpeed;
		_basePipPosition = _byrdpipContainer.Position;
		_byrdpipContainer.Translate(Vector2.Down * 800f);
		SetSkins();
	}

	private void SetSkins()
	{
		MegaSkeleton skeleton = _byrdpipSprite.GetSkeleton();
		MegaSkeletonDataResource data = skeleton.GetData();
		MegaSkin megaSkin = _byrdpipSprite.NewSkin("custom-skin");
		megaSkin.AddSkin(data.FindSkin(_byrdpip.Skin));
		skeleton.SetSkin(megaSkin);
		skeleton.SetSlotsToSetupPose();
		MegaSkeleton skeleton2 = _byrdineSprite.GetSkeleton();
		MegaSkeletonDataResource data2 = skeleton2.GetData();
		CharacterModel character = _byrdpip.Owner.Character;
		string text = ((character is Necrobinder) ? "necro" : character.Id.Entry.ToLowerInvariant());
		MegaSkin megaSkin2 = data2.FindSkin("eye/" + text);
		if (megaSkin2 == null)
		{
			megaSkin2 = data2.FindSkin("eye/ironclad");
		}
		megaSkin = _byrdineSprite.NewSkin("custom-skin");
		megaSkin.AddSkin(data2.FindSkin(_byrdine.Hair));
		megaSkin.AddSkin(megaSkin2);
		skeleton2.SetSkin(megaSkin);
		skeleton2.SetSlotsToSetupPose();
	}

	public async Task PlayAnimation()
	{
		SfxCmd.PlayLoop("event:/sfx/byrdpip/byrdlett_evolution", usesLoopParam: false);
		SfxCmd.PlayLoop("event:/music/byrdlett_evolution_music", usesLoopParam: false);
		_audioStarted = true;
		_startTween = CreateTween();
		_startTween.TweenCallback(Callable.From(StartEvolution)).SetDelay(0.75);
		await _completionSource.Task;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!_completionSource.Task.IsCompleted && ActiveScreenContext.Instance.GetCurrentScreen() == this && ((inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed) || inputEvent.IsActionPressed(MegaInput.select) || inputEvent.IsActionPressed(MegaInput.cancel)))
		{
			GetViewport().SetInputAsHandled();
			SkipToFinale();
		}
	}

	private void SkipToFinale()
	{
		if (!_finaleStarted)
		{
			StopAnimation();
			_fadeTween?.FastForwardToCompletion();
			FireFinale();
		}
	}

	private void StartEvolution()
	{
		_byrdpipTween = CreateTween();
		_byrdpipTween.TweenProperty(_byrdpipContainer, "position", _basePipPosition, _pipTranslateTime).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		_byrdpipTween.TweenCallback(Callable.From(ShowByrdine)).SetDelay(_pipHoldTime);
	}

	private void ShowByrdine()
	{
		Accelerate();
		_byrdpipTween = CreateTween();
		_byrdpipTween.TweenProperty(_byrdpipContainer, "scale", Vector2.Zero, _curTweenDuration).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		_byrdpipTween.TweenInterval(_curTweenDuration);
		_byrdineTween = CreateTween();
		_byrdineTween.TweenInterval(_curTweenDuration);
		if (_shrinkReps > 0)
		{
			_byrdineTween.TweenProperty(_byrdineContainer, "scale", Vector2.One, _curTweenDuration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		}
		else
		{
			_byrdineTween.TweenProperty(_byrdineContainer, "scale", Vector2.One, _curTweenDuration * 0.6f).SetTrans(Tween.TransitionType.Linear);
		}
		_byrdineTween.TweenCallback(Callable.From(ShowByrdpip));
	}

	private void ShowByrdpip()
	{
		Accelerate();
		if (_shrinkReps > 0)
		{
			_byrdpipTween = CreateTween();
			_byrdpipTween?.TweenInterval(_curTweenDuration);
			_byrdpipTween?.TweenProperty(_byrdpipContainer, "scale", Vector2.One, _curTweenDuration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
			_byrdineTween = CreateTween();
			_byrdineTween?.TweenProperty(_byrdineContainer, "scale", Vector2.Zero, _curTweenDuration).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
			_byrdineTween?.TweenInterval(_curTweenDuration);
			_byrdineTween?.TweenCallback(Callable.From(ShowByrdine));
		}
		else
		{
			_particleDelayTween = CreateTween();
			_particleDelayTween.TweenInterval(_curTweenDuration);
			_particleDelayTween.TweenCallback(Callable.From(FireFinale));
		}
		if (_shrinkReps == _repsToShowPowerAt)
		{
			_powerParticles.Restart();
		}
		else if (_shrinkReps == _repsToStopPowerAt)
		{
			_powerParticles.Emitting = false;
		}
		_shrinkReps--;
	}

	private void Accelerate()
	{
		if (_curTweenDuration * _accelerationMultiplier >= _minTweenDuration)
		{
			_curTweenDuration *= _accelerationMultiplier;
		}
		_powerParticles.SpeedScale *= (double)_powerParticlesSpeedIncrement;
	}

	private void FireFinale()
	{
		if (!_finaleStarted)
		{
			_finaleStarted = true;
			_byrdpipContainer.Scale = Vector2.Zero;
			_byrdineContainer.Scale = Vector2.One;
			_finaleParticles.Restart();
			_powerParticles.Restart();
			_powerParticles.Emitting = false;
			_byrdineContainer.Modulate = new Color(1f, 1f, 1f);
			_byrdineSprite.GetAnimationState().SetAnimation("be_born", loop: false);
			_lifetimeTween = CreateTween();
			_lifetimeTween.TweenCallback(Callable.From(CompleteAnimation)).SetDelay(3.0);
		}
	}

	private void CompleteAnimation()
	{
		if (_completionSource.TrySetResult())
		{
			StopAnimation();
			NOverlayStack.Instance?.Remove(this);
		}
	}

	private void StopAnimation()
	{
		_startTween?.Kill();
		_byrdpipTween?.Kill();
		_byrdineTween?.Kill();
		_particleDelayTween?.Kill();
		_lifetimeTween?.Kill();
		StopAudio();
	}

	private void StopAudio()
	{
		if (_audioStarted)
		{
			SfxCmd.StopLoop("event:/sfx/byrdpip/byrdlett_evolution");
			SfxCmd.StopLoop("event:/music/byrdlett_evolution_music");
			_audioStarted = false;
		}
	}

	public override void _ExitTree()
	{
		_startTween?.Kill();
		_byrdpipTween?.Kill();
		_byrdineTween?.Kill();
		_particleDelayTween?.Kill();
		_lifetimeTween?.Kill();
		_fadeTween?.Kill();
		StopAudio();
		_completionSource.TrySetResult();
	}

	public void AfterOverlayOpened()
	{
	}

	public void AfterOverlayClosed()
	{
		_fadeTween?.FastForwardToCompletion();
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(this, "modulate:a", 0, 0.25);
		_fadeTween.TweenCallback(Callable.From(this.QueueFreeSafely));
	}

	public void AfterOverlayShown()
	{
		_fadeTween?.FastForwardToCompletion();
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(this, "modulate:a", 1.0, 0.5).From(0f);
	}

	public void AfterOverlayHidden()
	{
	}
}
