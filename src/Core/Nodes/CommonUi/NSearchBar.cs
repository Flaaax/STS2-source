using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

public partial class NSearchBar : Control
{
	[Signal]
	public delegate void QueryChangedEventHandler(string query);

	[Signal]
	public delegate void QuerySubmittedEventHandler(string query);

	private LineEdit _textArea;

	private NButton _clearButton;

	public string Text => _textArea.Text;

	public LineEdit TextArea => _textArea;

	/// <remarks>
	/// Pattern:<br />
	/// <code>[\\t\\r\\n]</code><br />
	/// Explanation:<br />
	/// <code>
	/// ○ Match a character in the set [\t\n\r].<br />
	/// </code>
	/// </remarks>
	[GeneratedRegex("[\\t\\r\\n]")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.12.31616")]
	private static Regex NonSpaceWhitespaceCharacters()
	{
		return _003CRegexGenerator_g_003EFACC081AAF3D765EFF87A82C4FBB77F6FD3EA759AA2D03D993988F88E97CC0B5B__NonSpaceWhitespaceCharacters_5.Instance;
	}

	/// <remarks>
	/// Pattern:<br />
	/// <code>\\s{2,}</code><br />
	/// Explanation:<br />
	/// <code>
	/// ○ Match a whitespace character atomically at least twice.<br />
	/// </code>
	/// </remarks>
	[GeneratedRegex("\\s{2,}")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.12.31616")]
	private static Regex ConsecutiveSpaces()
	{
		return _003CRegexGenerator_g_003EFACC081AAF3D765EFF87A82C4FBB77F6FD3EA759AA2D03D993988F88E97CC0B5B__ConsecutiveSpaces_6.Instance;
	}

	/// <remarks>
	/// Pattern:<br />
	/// <code>&lt;.*?&gt;</code><br />
	/// Explanation:<br />
	/// <code>
	/// ○ Match '&lt;'.<br />
	/// ○ Match a character other than '\n' lazily any number of times.<br />
	/// ○ Match '&gt;'.<br />
	/// </code>
	/// </remarks>
	[GeneratedRegex("<.*?>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.12.31616")]
	private static Regex HtmlTags()
	{
		return _003CRegexGenerator_g_003EFACC081AAF3D765EFF87A82C4FBB77F6FD3EA759AA2D03D993988F88E97CC0B5B__HtmlTags_7.Instance;
	}

	public override void _Ready()
	{
		_textArea = GetNode<LineEdit>("TextArea");
		_textArea.Connect(LineEdit.SignalName.TextChanged, Callable.From<string>(TextUpdated));
		_textArea.Connect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(TextSubmitted));
		_textArea.SetPlaceholder(new LocString("card_library", "SEARCH_PLACEHOLDER").GetRawText());
		_clearButton = GetNode<NButton>("ClearButton");
		_clearButton.Connect(NClickableControl.SignalName.Released, Callable.From((Action<NButton>)ClearText));
	}

	private void TextUpdated(string _)
	{
		EmitSignal(SignalName.QueryChanged, _textArea.Text);
	}

	private void TextSubmitted(string _)
	{
		EmitSignal(SignalName.QuerySubmitted, _textArea.Text);
	}

	private void ClearText(NButton _)
	{
		ClearText();
	}

	public void ClearText()
	{
		_textArea.TryGrabFocus();
		if (!string.IsNullOrWhiteSpace(_textArea.Text))
		{
			_textArea.Text = "";
			EmitSignal(SignalName.QueryChanged, _textArea.Text);
		}
	}

	public static string Normalize(string text)
	{
		string input = NonSpaceWhitespaceCharacters().Replace(text.Trim(), " ");
		return ConsecutiveSpaces().Replace(input, " ").ToLowerInvariant();
	}

	public static string RemoveHtmlTags(string text)
	{
		return HtmlTags().Replace(text, string.Empty);
	}
}
