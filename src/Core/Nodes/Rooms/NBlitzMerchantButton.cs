using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MegaCrit.Sts2.Core.Nodes.Rooms;

public partial class NBlitzMerchantButton : NMerchantButton
{
	private MerchantInventory? _inventory;

	private MegaSprite _megaSprite;

	private GpuParticles2D _greenBlast;

	private GpuParticles2D _greenEmbers;

	private GpuParticles2D _redEmbers;

	private GpuParticles2D _greenGlow;

	private GpuParticles2D _redGlow;

	private Sprite2D _greenFire;

	private Sprite2D _redFire;

	private Sprite2D[] TokenSlots;

	private Vector2 _redFireScale;

	private Vector2 _greenFireScale;

	private Tween? _redTween;

	private Tween? _greenTween;

	private bool _hasSwitchedToGreen;

	public override void _Ready()
	{
		ConnectSignals();
		_greenBlast = GetNode<GpuParticles2D>("BackVfx/GreenBlast");
		_greenEmbers = GetNode<GpuParticles2D>("BackVfx/GreenEmbers");
		_redEmbers = GetNode<GpuParticles2D>("BackVfx/RedEmbers");
		_greenGlow = GetNode<GpuParticles2D>("FrontVfx/GreenGlow");
		_redGlow = GetNode<GpuParticles2D>("FrontVfx/RedGlow");
		_greenFire = GetNode<Sprite2D>("BackVfx/GreenFire");
		_redFire = GetNode<Sprite2D>("BackVfx/RedFire");
		_merchantSelectionReticle = GetNode<NSelectionReticle>("%MerchantSelectionReticle");
		_megaSprite = new MegaSprite(GetNode("%MerchantVisual"));
		TokenSlots = new Sprite2D[6];
		for (int i = 0; i < 6; i++)
		{
			TokenSlots[i] = GetNode<Sprite2D>($"%Token{i + 1}");
		}
		this.RunWhenSpineReady(_megaSprite, delegate(MegaAnimationState animState)
		{
			_merchantSkeleton = _megaSprite.GetSkeleton();
			animState.SetAnimation("animation");
			if (base.IsFocused && !_focusedWhileTargeting)
			{
				_merchantSkeleton.SetSkinByName("outline");
				_merchantSkeleton.SetSlotsToSetupPose();
			}
		});
		_redFireScale = _redFire.Scale;
		_greenFireScale = _greenFire.Scale;
		_greenBlast.OneShot = true;
		ShowRed();
	}

	public void Setup(MerchantInventory inventory)
	{
		_inventory = inventory;
		IReadOnlyList<BlitzMerchantEntry> blitzEntries = inventory.BlitzEntries;
		foreach (BlitzMerchantEntry item in blitzEntries)
		{
			item.PurchaseCompleted += UpdateTokens;
		}
		UpdateTokens();
	}

	private void Teardown()
	{
		if (_inventory == null)
		{
			return;
		}
		IReadOnlyList<BlitzMerchantEntry> blitzEntries = _inventory.BlitzEntries;
		foreach (BlitzMerchantEntry item in blitzEntries)
		{
			item.PurchaseCompleted -= UpdateTokens;
		}
		_inventory = null;
	}

	private void UpdateTokens(PurchaseStatus _, MerchantEntry __)
	{
		UpdateTokens();
	}

	private void UpdateTokens()
	{
		if (_inventory != null)
		{
			IReadOnlyList<BlitzMerchantEntry> blitzEntries = _inventory.BlitzEntries;
			for (int i = 0; i < blitzEntries.Count; i++)
			{
				TokenSlots[i].Texture = blitzEntries[i].SmallIcon;
				TokenSlots[i].SetVisible(blitzEntries[i].IsStocked);
			}
		}
	}

	public void CompleteShop()
	{
		if (!_hasSwitchedToGreen)
		{
			_hasSwitchedToGreen = true;
			ShowGreen();
		}
	}

	private void ShowGreen()
	{
		_greenGlow.Restart();
		_greenFire.Visible = true;
		_greenTween = CreateTween();
		_greenTween.TweenInterval(0.5);
		_greenFire.Scale = Vector2.Zero;
		Vector2 vector = new Vector2(1.1f, 1.3f);
		_greenTween.TweenCallback(Callable.From(delegate
		{
			MegaAnimationState animationState = _megaSprite.GetAnimationState();
			animationState.SetAnimation("done", loop: false);
			animationState.AddAnimation("done_loop");
			_redGlow.Restart();
			_redGlow.Emitting = false;
		}));
		_greenTween.TweenProperty(_greenFire, "scale", _greenFireScale * vector, 0.10000000149011612).SetDelay(0.15000000596046448).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		_greenTween.TweenProperty(_greenFire, "scale", _greenFireScale, 0.10000000149011612).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
		_redTween = CreateTween();
		_redTween.TweenInterval(0.5);
		_redTween.TweenProperty(_redFire, "scale", Vector2.Zero, 0.15000000596046448).SetEase(Tween.EaseType.In);
		_redTween.TweenCallback(Callable.From(StartGreenSparks));
	}

	private void StartGreenSparks()
	{
		_redEmbers.Restart();
		_redEmbers.Emitting = false;
		_greenBlast.Restart();
		_greenEmbers.Restart();
	}

	private void ShowRed()
	{
		_redEmbers.Restart();
		_redGlow.Restart();
		_redFire.Visible = true;
		_redFire.Scale = _redFireScale;
		_greenBlast.Restart();
		_greenEmbers.Restart();
		_greenGlow.Restart();
		_greenBlast.Emitting = false;
		_greenEmbers.Emitting = false;
		_greenGlow.Emitting = false;
		_greenFire.Visible = false;
		_greenFire.Scale = _greenFireScale;
		MegaAnimationState animationState = _megaSprite.GetAnimationState();
		animationState.SetAnimation("animation");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_redTween?.Kill();
		_greenTween?.Kill();
		Teardown();
	}
}
