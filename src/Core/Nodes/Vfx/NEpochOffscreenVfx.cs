using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

namespace MegaCrit.Sts2.Core.Nodes.Vfx;

/// <summary>
/// Special Vfx to help players find offscreen revealable Epochs.
/// </summary>
public partial class NEpochOffscreenVfx : Control
{
	public static readonly string scenePath = SceneHelper.GetScenePath("timeline_screen/epoch_offscreen_vfx");

	private NEpochSlot _slot;

	private Tween? _tween;

	private bool _showVfx;

	private float _viewportSizeX;

	public static NEpochOffscreenVfx Create(NEpochSlot slot)
	{
		NEpochOffscreenVfx nEpochOffscreenVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NEpochOffscreenVfx>(PackedScene.GenEditState.Disabled);
		nEpochOffscreenVfx._slot = slot;
		return nEpochOffscreenVfx;
	}

	public override void _Ready()
	{
		_viewportSizeX = GetViewportRect().Size.X;
	}

	public override void _Process(double delta)
	{
		if (!_showVfx)
		{
			if (_slot.GlobalPosition.X < 0f)
			{
				_showVfx = true;
				_tween?.Kill();
				_tween = CreateTween();
				_tween.TweenProperty(this, "modulate:a", 0.75f, 0.5);
				base.GlobalPosition = new Vector2(0f, _slot.GlobalPosition.Y + 60f);
			}
			else if (_slot.GlobalPosition.X > _viewportSizeX)
			{
				_showVfx = true;
				_tween?.Kill();
				_tween = CreateTween();
				_tween.TweenProperty(this, "modulate:a", 0.75f, 0.5);
				base.GlobalPosition = new Vector2(_viewportSizeX, _slot.GlobalPosition.Y + 60f);
			}
		}
		else if (_slot.GlobalPosition.X > 0f && _slot.GlobalPosition.X < _viewportSizeX)
		{
			_showVfx = false;
			_tween?.Kill();
			_tween = CreateTween();
			_tween.TweenProperty(this, "modulate:a", 0f, 0.2);
		}
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}
}
