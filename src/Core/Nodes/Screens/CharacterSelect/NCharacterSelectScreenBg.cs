using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

public partial class NCharacterSelectScreenBg : Control
{
	private Window _window;

	private const float _sixteenByNine = 1.7777778f;

	private const float _fourByThree = 1.3333334f;

	private const float _defaultBgScale = 1.1f;

	private const float _narrowBgScale = 1.153f;

	public override void _Ready()
	{
		_window = GetTree().Root;
		_window.Connect(Viewport.SignalName.SizeChanged, Callable.From(OnWindowChange));
	}

	private void OnWindowChange()
	{
		float num = Mathf.Max(1.3333334f, (float)_window.Size.X / (float)_window.Size.Y);
		if (num < 1.7777778f)
		{
			float p = (num - 1.3333334f) / 0.44444442f;
			base.Scale = Vector2.One * Mathf.Remap(Ease.CubicOut(p), 0f, 1f, 1.2683f, 1.1f);
		}
		else
		{
			base.Scale = Vector2.One * 1.1f;
		}
	}
}
