using System.Runtime.CompilerServices;

namespace MegaCrit.Sts2.Core.AutoSlay.Scenarios;

/// <summary>
/// One executable line of a scenario script. The subtypes are the complete set: a parsed script
/// cannot hold a line that is neither a directive nor a console command.
/// </summary>
public abstract record ScenarioStatement
{
	/// <summary>
	/// A line handed to the dev console verbatim.
	/// </summary>
	public sealed record ConsoleLine : ScenarioStatement
	{
		public string Text { get; init; }

		/// <summary>
		/// A line handed to the dev console verbatim.
		/// </summary>
		public ConsoleLine(int LineNumber, string Text)
		{
			this.Text = Text;
			base._002Ector(LineNumber);
		}

		[CompilerGenerated]
		public void Deconstruct(out int LineNumber, out string Text)
		{
			LineNumber = base.LineNumber;
			Text = this.Text;
		}
	}

	/// <summary>
	/// `wait &lt;condition&gt;`.
	/// </summary>
	public sealed record WaitForCondition : ScenarioStatement
	{
		public ScenarioWaitCondition Condition { get; init; }

		/// <summary>
		/// `wait &lt;condition&gt;`.
		/// </summary>
		public WaitForCondition(int LineNumber, ScenarioWaitCondition Condition)
		{
			this.Condition = Condition;
			base._002Ector(LineNumber);
		}

		[CompilerGenerated]
		public void Deconstruct(out int LineNumber, out ScenarioWaitCondition Condition)
		{
			LineNumber = base.LineNumber;
			Condition = this.Condition;
		}
	}

	/// <summary>
	/// `wait &lt;seconds&gt;`.
	/// </summary>
	public sealed record WaitForDuration : ScenarioStatement
	{
		public float Seconds { get; init; }

		/// <summary>
		/// `wait &lt;seconds&gt;`.
		/// </summary>
		public WaitForDuration(int LineNumber, float Seconds)
		{
			this.Seconds = Seconds;
			base._002Ector(LineNumber);
		}

		[CompilerGenerated]
		public void Deconstruct(out int LineNumber, out float Seconds)
		{
			LineNumber = base.LineNumber;
			Seconds = this.Seconds;
		}
	}

	/// <summary>
	/// `endturn`: end the player's turn.
	/// </summary>
	public sealed record EndTurn : ScenarioStatement
	{
		/// <summary>
		/// `endturn`: end the player's turn.
		/// </summary>
		public EndTurn(int LineNumber)
			: base(LineNumber)
		{
		}

		[CompilerGenerated]
		public void Deconstruct(out int LineNumber)
		{
			LineNumber = base.LineNumber;
		}
	}

	/// <summary>
	/// 1-based line in the source file, used for every failure message.
	/// </summary>
	public int LineNumber { get; }

	private protected ScenarioStatement(int lineNumber)
	{
		LineNumber = lineNumber;
	}
}
