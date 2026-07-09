using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

public partial class NCustomRunRandomizeButton : NButton
{
	private static readonly StringName _v = new StringName("v");

	private static readonly StringName _s = new StringName("s");

	private ShaderMaterial _shaderMaterial;

	private MegaRichTextLabel _label;

	protected override string[] Hotkeys => new string[1] { MegaInput.peek };

	public override void _Ready()
	{
		ConnectSignals();
		_label = GetNode<MegaRichTextLabel>("Label");
		_label.SetTextAutoSize(new LocString("main_menu_ui", "CUSTOM_RUN_SCREEN.RANDOMIZE").GetFormattedText());
		_shaderMaterial = (ShaderMaterial)GetNode<Control>("Background").Material;
	}

	protected override void OnFocus()
	{
		_shaderMaterial.SetShaderParameter(_s, 1.1f);
		_shaderMaterial.SetShaderParameter(_v, 1.1f);
	}

	protected override void OnUnfocus()
	{
		_shaderMaterial.SetShaderParameter(_s, 1f);
		_shaderMaterial.SetShaderParameter(_v, 1f);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		_shaderMaterial.SetShaderParameter(_s, 1f);
		_shaderMaterial.SetShaderParameter(_v, 1f);
		_label.Modulate = Colors.White;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		_shaderMaterial.SetShaderParameter(_s, 0f);
		_shaderMaterial.SetShaderParameter(_v, 0.5f);
		_label.Modulate = StsColors.gray;
	}
}
