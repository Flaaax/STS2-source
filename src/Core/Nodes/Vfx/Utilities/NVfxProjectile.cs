using Godot;

public partial class NVfxProjectile : Node2D
{
	[Export(PropertyHint.None, "")]
	private Node2D? _projectileHead;

	[Export(PropertyHint.None, "")]
	private GpuParticles2D[] _particles;

	[Export(PropertyHint.None, "")]
	private bool _alignToVelocity;

	public bool AlignToVelocity => _alignToVelocity;

	public void SetEmitting(bool emitting)
	{
		if (_projectileHead != null)
		{
			_projectileHead.Visible = emitting;
		}
		for (int i = 0; i < _particles.Length; i++)
		{
			_particles[i].Emitting = emitting;
		}
	}
}
