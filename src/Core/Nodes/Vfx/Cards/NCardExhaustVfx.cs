using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

public partial class NCardExhaustVfx : Control
{
	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/ui/card/vfx_card_exhaust");

	[Export(PropertyHint.None, "")]
	private Control _cardParentContainer;

	[Export(PropertyHint.None, "")]
	private Control _materialContainer;

	[Export(PropertyHint.None, "")]
	private NParticlesContainer _particlesContainer;

	[Export(PropertyHint.None, "")]
	private float _exhaustDuration = 0.4f;

	[Export(PropertyHint.None, "")]
	private Curve _exhaustCurve;

	[Export(PropertyHint.None, "")]
	private Vector2 _erosionBaseRange;

	[Export(PropertyHint.None, "")]
	private Vector2 _particleHeightRange;

	private NCard _cardNode;

	private Vector2 _position;

	private static readonly StringName _erosionBaseParameter = new StringName("instance_shader_parameters/erosion_base");

	private static readonly StringName _erosionOffsetParameter = new StringName("instance_shader_parameters/erosion_texture_x_offset");

	public static NCardExhaustVfx? Create(NCard cardNode)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NCardExhaustVfx nCardExhaustVfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<NCardExhaustVfx>(PackedScene.GenEditState.Disabled);
		nCardExhaustVfx.SetParticlesPlaying(isPlaying: false);
		nCardExhaustVfx.SetProgress(0f);
		nCardExhaustVfx._position = cardNode.GlobalPosition;
		cardNode.GetParent()?.RemoveChildSafely(cardNode);
		nCardExhaustVfx._cardParentContainer.AddChildSafely(cardNode);
		nCardExhaustVfx._cardNode = cardNode;
		return nCardExhaustVfx;
	}

	private void SetParticlesPlaying(bool isPlaying)
	{
		_particlesContainer.SetEmitting(isPlaying);
	}

	private void SetProgress(float progress)
	{
		float weight = _exhaustCurve.Sample(progress);
		float num = Mathf.Lerp(_erosionBaseRange.X, _erosionBaseRange.Y, weight);
		float y = Mathf.Lerp(_particleHeightRange.X, _particleHeightRange.Y, weight);
		_materialContainer.Set(_erosionBaseParameter, num);
		_particlesContainer.Position = new Vector2(0f, y);
	}

	public async Task PlayAnimation()
	{
		base.GlobalPosition = _position;
		_cardNode.Position = _cardParentContainer.Size / 2f;
		_materialContainer.SelfModulate = new Color(1f, 1f, 1f);
		SetParticlesPlaying(isPlaying: true);
		SetProgress(0f);
		_materialContainer.Set(_erosionOffsetParameter, GD.Randf());
		float num = 0f;
		while (num < _exhaustDuration)
		{
			float progress = num / _exhaustDuration;
			SetProgress(progress);
			float num2 = num;
			num = num2 + await this.AwaitProcessFrame();
		}
		SetProgress(1f);
		SetParticlesPlaying(isPlaying: false);
		_materialContainer.SelfModulate = new Color(1f, 1f, 1f, 0f);
		TaskHelper.RunSafely(DelayedFree());
	}

	private async Task DelayedFree()
	{
		await Cmd.Wait(2f);
		_cardNode.QueueFreeSafely();
		this.QueueFreeSafely();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_cardNode) && IsAncestorOf(_cardNode))
		{
			_cardNode.QueueFreeSafely();
		}
	}
}
