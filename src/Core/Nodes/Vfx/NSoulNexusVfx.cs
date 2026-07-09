using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Vfx;

public partial class NSoulNexusVfx : Node
{
	private MegaSprite _megaSprite;

	private NBasicTrail _trail1;

	private NBasicTrail _trail2;

	private NBasicTrail _trail3;

	private TextureRect _fireTexture;

	public override void _Ready()
	{
		_megaSprite = new MegaSprite(GetParent<Node2D>());
		_fireTexture = GetNode<TextureRect>("../HeadFireSlot/FireTexture");
		_trail1 = GetNode<NBasicTrail>("../PathSlot1/Trail");
		_trail2 = GetNode<NBasicTrail>("../PathSlot2/Trail");
		_trail3 = GetNode<NBasicTrail>("../PathSlot3/Trail");
		_trail1.Visible = false;
		_trail2.Visible = false;
		_trail3.Visible = false;
		_megaSprite.ConnectAnimationEvent(Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>(OnAnimationEvent));
	}

	private void OnAnimationEvent(GodotObject _, GodotObject __, GodotObject ___, GodotObject spineEvent)
	{
		switch (new MegaEvent(spineEvent).GetData().GetEventName())
		{
		case "hide_fire":
			ShowFire(show: false);
			break;
		case "show_fire":
			ShowFire(show: true);
			break;
		case "path_1_start":
			StartPath1();
			break;
		case "path_1_stop":
			EndPath1();
			break;
		case "path_2_start":
			StartPath2();
			break;
		case "path_2_stop":
			EndPath2();
			break;
		case "path_3_start":
			StartPath3();
			break;
		case "path_3_stop":
			EndPath3();
			break;
		}
	}

	private void ShowFire(bool show)
	{
		_fireTexture.Visible = show;
	}

	private void StartPath1()
	{
		_trail1.Visible = true;
		_trail1.ClearPoints();
	}

	private void EndPath1()
	{
		_trail1.Visible = false;
	}

	private void StartPath2()
	{
		_trail2.Visible = true;
		_trail2.ClearPoints();
	}

	private void EndPath2()
	{
		_trail2.Visible = false;
	}

	private void StartPath3()
	{
		_trail3.Visible = true;
		_trail3.ClearPoints();
	}

	private void EndPath3()
	{
		_trail3.Visible = false;
	}
}
