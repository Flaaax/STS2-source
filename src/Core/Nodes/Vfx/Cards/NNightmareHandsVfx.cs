using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

public partial class NNightmareHandsVfx : Control
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("vfx/cards/nightmare_hands_vfx");

	private CancellationTokenSource? _cts;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	public override void _ExitTree()
	{
		_cts?.Cancel();
	}

	public static NNightmareHandsVfx? Create()
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<NNightmareHandsVfx>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(SelfDestruct());
	}

	private async Task SelfDestruct()
	{
		_cts = new CancellationTokenSource();
		await Task.Delay(2000, _cts.Token);
		this.QueueFreeSafely();
	}
}
