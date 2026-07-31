using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace MegaCrit.Sts2.Core.Debug;

public partial class NSpineSpriteCopierTester : Node2D
{
	[Export(PropertyHint.None, "")]
	private NSpineSpriteCopier _copier;

	[Export(PropertyHint.None, "")]
	private Node2D _targetSpine;

	public override void _Ready()
	{
		_copier.Initialize(new MegaSprite(_targetSpine), _targetSpine);
	}
}
