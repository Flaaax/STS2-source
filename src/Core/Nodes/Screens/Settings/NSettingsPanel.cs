using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Settings;

/// <summary>
/// A script specifically used to make the Settings panels (General Settings, Graphics Settings, Audio Settings) fade in nicely
/// </summary>
public partial class NSettingsPanel : Control
{
	private float _minPadding = 50f;

	private Control? _firstControl;

	private Tween? _tween;

	public VBoxContainer Content { get; private set; }

	public Control? DefaultFocusedControl => _firstControl;

	public override void _Ready()
	{
		Content = GetNode<VBoxContainer>("VBoxContainer");
		Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnVisibilityChange));
		GetViewport().Connect(Viewport.SignalName.SizeChanged, Callable.From(RefreshSize));
		RefreshSize();
		List<Control> list = new List<Control>();
		GetSettingsOptionsRecursive(Content, list, includeInvisibleSettings: true);
		foreach (Control item in list)
		{
			item.Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(UpdateNavigation));
		}
		UpdateNavigation();
	}

	private void RefreshSize()
	{
		Vector2 size = GetParent<Control>().Size;
		Vector2 minimumSize = Content.GetMinimumSize();
		if (minimumSize.Y + _minPadding >= size.Y)
		{
			base.Size = new Vector2(Content.Size.X, minimumSize.Y + size.Y * 0.4f);
		}
		else
		{
			base.Size = new Vector2(Content.Size.X, minimumSize.Y);
		}
	}

	protected virtual void OnVisibilityChange()
	{
		if (base.Visible)
		{
			_tween?.Kill();
			_tween = CreateTween().SetParallel();
			_tween.TweenProperty(this, "modulate", Colors.White, 0.5).From(StsColors.transparentBlack).SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);
		}
	}

	protected virtual void UpdateNavigation()
	{
		List<Control> list = new List<Control>();
		GetSettingsOptionsRecursive(Content, list);
		for (int i = 0; i < list.Count; i++)
		{
			list[i].FocusNeighborLeft = list[i].GetPath();
			list[i].FocusNeighborRight = list[i].GetPath();
			list[i].FocusNeighborTop = ((i > 0) ? list[i - 1].GetPath() : list[i].GetPath());
			list[i].FocusNeighborBottom = ((i < list.Count - 1) ? list[i + 1].GetPath() : list[i].GetPath());
		}
		_firstControl = list.FirstOrDefault();
	}

	private void GetSettingsOptionsRecursive(Control parent, List<Control> ancestors, bool includeInvisibleSettings = false)
	{
		foreach (Control item in parent.GetChildren().OfType<Control>())
		{
			if (!IsSettingsOption(item))
			{
				GetSettingsOptionsRecursive(item, ancestors, includeInvisibleSettings);
			}
			else if ((item.GetParent<Control>().IsVisible() || includeInvisibleSettings) && item.FocusMode == FocusModeEnum.All)
			{
				ancestors.Add(item);
			}
		}
	}

	private bool IsSettingsOption(Control c)
	{
		if (c is NButton nButton)
		{
			return nButton.IsEnabled;
		}
		if (c is NPaginator || c is NTickbox || c is NButton || c is NDropdownPositioner || c is NSettingsSlider)
		{
			return true;
		}
		return false;
	}
}
