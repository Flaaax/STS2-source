using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

public partial class NCardExhaustQuickVfx : Control
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/ui/card/vfx_card_exhaust_quick");

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _anticipationParticlesContainer;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _particlesContainer;

	[Export(PropertyHint.None, "")]
	private float _anticipationDuration = 0.4f;

	private bool _isFinishing;

	private NCard _cardNode;

	public static NCardExhaustQuickVfx? Create(NCard cardNode)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardExhaustQuickVfx nCardExhaustQuickVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NCardExhaustQuickVfx>(PackedScene.GenEditState.Disabled);
		nCardExhaustQuickVfx._cardNode = cardNode;
		nCardExhaustQuickVfx._anticipationParticlesContainer.Modulate = new Color(1f, 1f, 1f);
		cardNode.CardVfxContainer.AddChildSafely(nCardExhaustQuickVfx);
		return nCardExhaustQuickVfx;
	}

	public async Task PlayAnimation()
	{
		_isFinishing = false;
		_anticipationParticlesContainer.Restart();
		await Cmd.Wait(_anticipationDuration);
		_isFinishing = true;
		_anticipationParticlesContainer.Modulate = new Color(1f, 1f, 1f, 0f);
		Node parent = GetParent();
		Vector2 globalPosition = base.GlobalPosition;
		float rotation = base.Rotation;
		Vector2 scale = _cardNode.Scale;
		parent?.RemoveChildSafely(this);
		if (NCombatRoom.Instance != null)
		{
			NCombatRoom.Instance.Ui.AddChildSafely(this);
			base.GlobalPosition = globalPosition;
			base.Rotation = rotation;
			base.Scale = scale;
		}
		_cardNode.QueueFreeSafely();
		_particlesContainer.Restart();
		TaskHelper.RunSafely(DelayedFree());
	}

	private async Task DelayedFree()
	{
		await Cmd.Wait(2f);
		this.QueueFreeSafely();
	}

	public override void _ExitTree()
	{
		if (!_isFinishing && GodotObject.IsInstanceValid(_cardNode))
		{
			_cardNode.QueueFreeSafely();
		}
	}
}
