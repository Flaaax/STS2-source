using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.sts2.Core.Nodes.TopBar;

public partial class NTopBarModifier : NClickableControl
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("ui/top_bar/top_bar_modifier");

	private HoverTip _hoverTip;

	private TextureRect _icon;

	private ModifierModel _modifier;

	public static NTopBarModifier? Create(ModifierModel modifier)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NTopBarModifier nTopBarModifier = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NTopBarModifier>(PackedScene.GenEditState.Disabled);
		nTopBarModifier._modifier = modifier;
		return nTopBarModifier;
	}

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("Icon");
		_icon.Texture = _modifier.Icon;
		_hoverTip = new HoverTip(_modifier.Title, _modifier.Description);
		ConnectSignals();
	}

	protected override void OnFocus()
	{
		NHoverTipSet.CreateAndShow(this, _hoverTip)?.SetGlobalPosition(base.GlobalPosition + new Vector2(0f, base.Size.Y + 20f));
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove(this);
	}
}
