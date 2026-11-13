using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Dread;

public partial class NDreadBeginRotationTickbox : NTickbox
{
	private HoverTip _hoverTip;

	private Control _rotatingImage;

	private MegaRichTextLabel _rotatingExplanation;

	private NDreadScreen _dreadScreen;

	private Tween? _rotatingTween;

	public override void _Ready()
	{
		ConnectSignals();
		_rotatingImage = GetNode<Control>("%TickboxRotatingImage");
		_rotatingExplanation = GetNode<MegaRichTextLabel>("%RotatingExplanation");
		_dreadScreen = this.GetAncestorOfType<NDreadScreen>();
		_hoverTip = new HoverTip(new LocString("static_hover_tips", "DREAD_ROTATION.title"), new LocString("static_hover_tips", "DREAD_ROTATION.description"));
	}

	/// <summary>
	/// Setting <see cref="P:MegaCrit.Sts2.Core.Nodes.CommonUi.NTickbox.IsTicked" /> doesn't update the rotating image, so use this instead.
	/// </summary>
	public void InitTickedState(bool isTicked)
	{
		base.IsTicked = isTicked;
		UpdateRotatingImage();
	}

	protected override void OnTick()
	{
		UpdateRotatingImage();
		_dreadScreen.BeginRotationTickboxChanged(beginRotation: true);
	}

	protected override void OnUntick()
	{
		UpdateRotatingImage();
		_dreadScreen.BeginRotationTickboxChanged(beginRotation: false);
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
		if (nHoverTipSet != null)
		{
			nHoverTipSet.GlobalPosition = base.GlobalPosition + Vector2.Right * (base.Size.X + 10f);
		}
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		NHoverTipSet.Remove(this);
	}

	private void UpdateRotatingImage()
	{
		if (base.IsTicked)
		{
			_rotatingImage.Modulate = Colors.White;
			if (_rotatingTween == null)
			{
				_rotatingTween = CreateTween();
				_rotatingTween.TweenProperty(_rotatingImage, "rotation", (float)Math.PI * 2f, 10.0).From(0f).SetTrans(Tween.TransitionType.Linear);
				_rotatingTween.SetLoops();
			}
			else
			{
				_rotatingTween.Play();
			}
		}
		else if (!base.IsTicked)
		{
			_rotatingTween?.Pause();
			_rotatingImage.Modulate = StsColors.gray;
		}
	}
}
