using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Container for Modal Windows (Confirmation screens, FTUEs, etc). This is rendered above most UI!
/// There can never be two modals active at once. If so, Casey has failed in the UI department.
/// </summary>
public partial class NModalContainer : Control
{
	private ColorRect _backstop;

	private Tween? _backstopTween;

	public static NModalContainer? Instance { get; private set; }

	public IScreenContext? OpenModal { get; private set; }

	public override void _Ready()
	{
		if (Instance != null)
		{
			Log.Error("NModalContainer already exists.");
			this.QueueFreeSafely();
		}
		else
		{
			Instance = this;
			_backstop = GetNode<ColorRect>("Backstop");
		}
	}

	/// <summary>
	/// Used when we create a FTUE or modal popup. We add it to our ModalContainer!
	/// </summary>
	public void Add(Node modalToCreate, bool showBackstop = true)
	{
		if (OpenModal != null)
		{
			Log.Warn("There's another modal already open.");
			return;
		}
		OpenModal = (IScreenContext)modalToCreate;
		this.AddChildSafely(modalToCreate);
		ActiveScreenContext.Instance.Update();
		if (showBackstop)
		{
			ShowBackstop();
		}
	}

	public void Clear()
	{
		foreach (Node child in GetChildren())
		{
			if (child != _backstop)
			{
				child.QueueFreeSafely();
			}
		}
		OpenModal = null;
		ActiveScreenContext.Instance.Update();
		HideBackstop();
	}

	public void ShowBackstop()
	{
		base.MouseFilter = MouseFilterEnum.Stop;
		_backstop.Visible = true;
		_backstopTween?.Kill();
		_backstopTween = CreateTween();
		_backstopTween.TweenProperty(_backstop, "color:a", 0.85f, 0.3);
	}

	public void HideBackstop()
	{
		base.MouseFilter = MouseFilterEnum.Ignore;
		_backstopTween?.Kill();
		_backstopTween = CreateTween();
		_backstopTween.TweenProperty(_backstop, "color:a", 0f, 0.3);
		_backstopTween.TweenCallback(Callable.From(() => _backstop.Visible = false));
	}
}
