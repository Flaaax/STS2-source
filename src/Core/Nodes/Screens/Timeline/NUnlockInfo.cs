using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

/// <summary>
/// The screen which opens when you click on an Epoch to view the bigger image + flavor text.
/// Note that there are two UX for this screen. One for first time open (plays the animation of
/// the text crawling in) and the second time open, which skips this animation.
/// </summary>
public partial class NUnlockInfo : Control
{
	private TextureRect _icon;

	private MegaRichTextLabel _label;

	private Tween? _tween;

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("%Icon");
		_label = GetNode<MegaRichTextLabel>("%Text");
	}

	public void HideImmediately()
	{
		Color modulate = base.Modulate;
		modulate.A = 0f;
		base.Modulate = modulate;
	}

	public void AnimIn(string text)
	{
		Color modulate = base.Modulate;
		modulate.A = 0f;
		base.Modulate = modulate;
		_label.Text = text;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(this, "modulate:a", 0.8f, 1.0);
	}

	public async Task AnimInViaPaginator(string text)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(this, "modulate:a", 0f, 0.1);
		if (await _tween.AwaitFinished(this))
		{
			_label.Text = text;
			_tween?.Kill();
			_tween = CreateTween();
			_tween.TweenProperty(this, "modulate:a", 0.8f, 1.0);
		}
	}
}
