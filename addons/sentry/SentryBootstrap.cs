using Godot;
using MegaCrit.Sts2.Core.Debug;

namespace MegaCrit.Sts2.Core.Nodes;

/// <summary>
/// First autoload: brings Sentry up (both the .NET and native layers, via the unified sentry-godot SDK) as early
/// as possible, ahead of every other autoload. The native crash handler must be live before FMOD and other
/// subsystems initialize so crashes during early boot are captured. Registered first in project.godot's [autoload].
/// </summary>
public partial class SentryBootstrap : Node
{
	public override void _EnterTree()
	{
		SentryService.Initialize();
	}
}
