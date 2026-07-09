using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Map;

/// <summary>
/// An abstract base class to manage the drawing on the map
/// Inherited classes manage map drawing for different types of inputs.
/// Is created and destroyed on demand.
/// </summary>
public abstract partial class NMapDrawingInput : Control
{
	[Signal]
	public delegate void FinishedEventHandler();

	protected NMapDrawings _drawings;

	public DrawingMode DrawingMode { get; private set; }

	public static NMapDrawingInput Create(NMapDrawings drawings, DrawingMode drawingMode, bool stopOnMouseRelease = false)
	{
		NMapDrawingInput nMapDrawingInput = (stopOnMouseRelease ? new NMouseHeldMapDrawingInput() : ((!NControllerManager.Instance.IsUsingController) ? new NMouseModeMapDrawingInput() : NControllerMapDrawingInput.Create()));
		nMapDrawingInput._drawings = drawings;
		nMapDrawingInput.DrawingMode = drawingMode;
		nMapDrawingInput._drawings.SetDrawingModeLocal(drawingMode);
		return nMapDrawingInput;
	}

	public override void _Ready()
	{
		NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(StopDrawing));
		NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(StopDrawing));
	}

	public override void _EnterTree()
	{
		ActiveScreenContext.Instance.Updated += StopDrawing;
	}

	public override void _ExitTree()
	{
		ActiveScreenContext.Instance.Updated -= StopDrawing;
	}

	public void StopDrawing()
	{
		if (_drawings.IsLocalDrawing())
		{
			_drawings.StopLineLocal();
		}
		_drawings.SetDrawingModeLocal(DrawingMode.None);
		EmitSignalFinished();
		this.QueueFreeSafely();
	}
}
