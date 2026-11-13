using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;

public partial class NSendFeedbackCartoon : TextureRect
{
	private static readonly float _rot1 = Mathf.DegToRad(5f);

	private const float _rot2 = 0f;

	private Tween? _tween;

	[Export(PropertyHint.None, "")]
	public bool opposite;

	public void SetRotation1()
	{
		base.Rotation = (opposite ? _rot1 : 0f);
	}

	public void SetRotation2()
	{
		base.Rotation = (opposite ? 0f : _rot1);
	}
}
