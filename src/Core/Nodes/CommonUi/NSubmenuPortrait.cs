using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// Renders the images of the submenu buttons (for both large submenu buttons and the small ones seen in the Compendium)
/// </summary>
public partial class NSubmenuPortrait : TextureRect
{
	private TextureRect _lock;

	private TextureRect _frame;

	public static string GetIconPath(string name)
	{
		return ImageHelper.GetImagePath("ui/main_menu/submenu_" + name.ToLowerInvariant() + ".png");
	}

	public override void _Ready()
	{
		_lock = GetNode<TextureRect>("Lock");
		_frame = GetNode<TextureRect>("Frame");
	}

	public void SetUnlocked()
	{
		_lock.Visible = false;
		base.SelfModulate = Colors.White;
	}

	public void SetLocked()
	{
		_lock.Visible = true;
		base.SelfModulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
	}
}
