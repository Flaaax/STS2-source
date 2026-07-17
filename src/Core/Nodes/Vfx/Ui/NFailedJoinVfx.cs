using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Ui;

public partial class NFailedJoinVfx : Control
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/ui/vfx_failed_join");

	private Tween? _tween;

	private MegaRichTextLabel _label;

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlayAndSelfDestruct());
	}

	public static NFailedJoinVfx? Create(string text)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NFailedJoinVfx nFailedJoinVfx = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NFailedJoinVfx>(PackedScene.GenEditState.Disabled);
		nFailedJoinVfx.GetNode<MegaRichTextLabel>("%Label").SetTextAutoSize(text);
		return nFailedJoinVfx;
	}

	private async Task PlayAndSelfDestruct()
	{
		base.Modulate = StsColors.transparentWhite;
		Vector2 position = base.Position;
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(this, "modulate:a", 1f, 0.05);
		for (int i = 0; i < 5; i++)
		{
			float num = 1f - (float)i / 5f;
			float num2 = 24f * num * (float)((i % 2 == 0) ? 1 : (-1));
			_tween.Chain().TweenProperty(this, "position:x", position.X + num2, 0.05000000074505806).SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);
		}
		_tween.Chain().TweenProperty(this, "position:x", position.X, 0.05000000074505806).SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_tween.TweenInterval(3.0);
		_tween.Chain();
		_tween.TweenProperty(this, "modulate:a", 0f, 0.5);
		if (await _tween.AwaitFinished(this))
		{
			this.QueueFreeSafely();
		}
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}
}
