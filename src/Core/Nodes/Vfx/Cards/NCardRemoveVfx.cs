using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

public partial class NCardRemoveVfx : Control
{
	public const float deleteCardDelay = 0.4f;

	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/ui/card/vfx_card_remove");

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _anticipationParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _slashStartParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _slashEndParticles;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _cardParticles;

	[Export(PropertyHint.None, "")]
	private float _anticipationDuration = 0.25f;

	[Export(PropertyHint.None, "")]
	private float _slashEndDelay = 0.1f;

	private NCard _cardNode;

	public static NCardRemoveVfx? Create(NCard cardNode)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardRemoveVfx nCardRemoveVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NCardRemoveVfx>(PackedScene.GenEditState.Disabled);
		nCardRemoveVfx._cardNode = cardNode;
		return nCardRemoveVfx;
	}

	public override void _Ready()
	{
		base.GlobalPosition = _cardNode.GlobalPosition;
		base.Rotation = _cardNode.Rotation;
		_cardParticles.SetEmitting(emitting: false);
		TaskHelper.RunSafely(PlayAnimation());
	}

	private async Task PlayAnimation()
	{
		_anticipationParticles.Restart();
		await Cmd.Wait(_anticipationDuration);
		_slashStartParticles.Restart();
		await Cmd.Wait(_slashEndDelay);
		_slashEndParticles.Restart();
		_cardParticles.Restart();
		TaskHelper.RunSafely(DelayedFree());
	}

	private async Task DelayedFree()
	{
		await Cmd.Wait(2f);
		this.QueueFreeSafely();
	}
}
