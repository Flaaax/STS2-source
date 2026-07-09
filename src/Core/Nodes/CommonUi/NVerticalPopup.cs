using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// A popup ui/modal which has a Yes and No button.
/// Used for important popups like Abandon Run confirmation and the "Enable Tutorials?" popup.
/// </summary>
public partial class NVerticalPopup : Control
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("ui/vertical_popup");

	private bool _nodesAreSet;

	private Callable? _yesCallable;

	private Callable? _noCallable;

	public static IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(_scenePath);

	private MegaLabel TitleLabel { get; set; }

	private MegaRichTextLabel BodyLabel { get; set; }

	public NPopupYesNoButton YesButton { get; private set; }

	public NPopupYesNoButton NoButton { get; private set; }

	public override void _Ready()
	{
		EnsureNodesAreSet();
	}

	/// <summary>
	/// This is to ensure that the node parameters are set. We have to do this because
	/// AddChildSafely may defer the call to add child (and thus defer the _Ready), and
	/// can lead to timing issues in NGenericPopup where we try to SetText before the label
	/// parameters have been set. While this is a vulnerability that can techinically happen
	/// for any node added via AddChildSafely, I think we are seeing a particularly high
	/// number of errors here because the vertical popup is bieng created at the start of the game
	/// for a startup errors.
	/// </summary>
	private void EnsureNodesAreSet()
	{
		if (!_nodesAreSet)
		{
			TitleLabel = GetNode<MegaLabel>("Header");
			BodyLabel = GetNode<MegaRichTextLabel>("Description");
			YesButton = GetNode<NPopupYesNoButton>("YesButton");
			NoButton = GetNode<NPopupYesNoButton>("NoButton");
			_nodesAreSet = true;
		}
	}

	public void SetText(LocString title, LocString body)
	{
		EnsureNodesAreSet();
		TitleLabel.SetTextAutoSize(title.GetFormattedText());
		BodyLabel.SetTextAutoSize(body.GetFormattedText());
	}

	/// <summary>
	/// Sets the popup text using raw strings instead of localization.
	/// Use this when localization may be broken (e.g., showing localization errors).
	/// </summary>
	public void SetText(string title, string body)
	{
		EnsureNodesAreSet();
		TitleLabel.SetTextAutoSize(title);
		BodyLabel.SetTextAutoSize(body);
	}

	/// <summary>
	/// Initializes the yes button.
	/// If this is not called, then the yes button is hidden.
	/// </summary>
	public void InitYesButton(LocString yesButton, Action<NButton> onPressed)
	{
		EnsureNodesAreSet();
		_yesCallable = Callable.From(onPressed);
		YesButton.IsYes = true;
		YesButton.SetText(yesButton.GetFormattedText());
		YesButton.Connect(NClickableControl.SignalName.Released, _yesCallable.Value);
		YesButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(Close));
	}

	public void InitNoButton(LocString noButton, Action<NButton> onPressed)
	{
		EnsureNodesAreSet();
		_noCallable = Callable.From(onPressed);
		NoButton.Visible = true;
		NoButton.IsYes = false;
		NoButton.SetText(noButton.GetFormattedText());
		NoButton.Connect(NClickableControl.SignalName.Released, _noCallable.Value);
		NoButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(Close));
	}

	private void Close(NButton _)
	{
		NModalContainer.Instance.Clear();
	}

	public void HideNoButton()
	{
		NoButton.Visible = false;
	}

	public void DisconnectSignals()
	{
		if (_yesCallable.HasValue)
		{
			YesButton.Disconnect(NClickableControl.SignalName.Released, _yesCallable.Value);
			YesButton.Disconnect(NClickableControl.SignalName.Released, Callable.From<NButton>(Close));
		}
		if (_noCallable.HasValue)
		{
			NoButton.Disconnect(NClickableControl.SignalName.Released, _noCallable.Value);
			NoButton.Disconnect(NClickableControl.SignalName.Released, Callable.From<NButton>(Close));
		}
	}

	public void DisconnectHotkeys()
	{
		if (_yesCallable.HasValue)
		{
			YesButton.DisconnectHotkeys();
		}
		if (_noCallable.HasValue)
		{
			NoButton.DisconnectHotkeys();
		}
	}
}
