using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

public partial class NSpineSpriteBoneFollower : Node2D
{
	[Export(PropertyHint.None, "")]
	private Node2D _target;

	[Export(PropertyHint.None, "")]
	private string _boneName = "";

	[Export(PropertyHint.None, "")]
	private bool _snap;

	[Export(PropertyHint.None, "")]
	private float _interpolationSpeed = 0.5f;

	private MegaSprite? _targetSprite;

	public override void _Ready()
	{
	}

	public void SetSpineSprite(Node2D target, string boneName)
	{
		SetSpineSprite(new MegaSprite(target), boneName);
	}

	public void SetSpineSprite(MegaSprite spineSprite, string boneName)
	{
		_targetSprite = spineSprite;
		_boneName = boneName;
	}

	public override void _Process(double delta)
	{
		if (_targetSprite == null || string.IsNullOrEmpty(_boneName))
		{
			return;
		}
		Transform2D? globalBoneTransform = _targetSprite.GetGlobalBoneTransform(_boneName);
		if (globalBoneTransform.HasValue)
		{
			if (_snap)
			{
				base.GlobalPosition = globalBoneTransform.Value.Origin;
			}
			else
			{
				base.GlobalPosition = base.GlobalPosition.Lerp(globalBoneTransform.Value.Origin, _interpolationSpeed);
			}
		}
	}
}
