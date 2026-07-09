using Godot;

namespace MegaCrit.Sts2.Core.Nodes.RestSite;

/// <summary>
/// A utility for particle systems whose textures can get downscaled
/// Scales up the particle system so that it matches the original texture's size.
/// Used mainly for the ground lighting in rest sites.
/// </summary>
public partial class NParticleSystemUpscaler : CpuParticles2D
{
	[Export(PropertyHint.None, "")]
	private Vector2 _originalResolution;

	public override void _Ready()
	{
		Texture2D texture = base.Texture;
		Vector2 size = texture.GetSize();
		float num = _originalResolution.X / size.X;
		base.ScaleAmountMin *= num;
		base.ScaleAmountMax = base.ScaleAmountMin;
	}
}
