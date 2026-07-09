using System.Collections.Generic;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Entities.Multiplayer;

/// <summary>
/// Class for holding extra info to display in multiplayer errors if the error is a ConnectionFailureReason.
/// </summary>
public record ConnectionFailureExtraInfo
{
	/// <summary>
	/// The mods that the host has that we are missing on our end.
	/// Only set on ConnectionFailureReason.ModMismatch.
	/// </summary>
	public List<string>? missingModsOnLocal;

	/// <summary>
	/// The mods that we have that are missing on the host's end.
	/// Only set on ConnectionFailureReason.ModMismatch.
	/// </summary>
	public List<string>? missingModsOnHost;

	/// <summary>
	/// The version of the game the host is playing on.
	/// </summary>
	public string? hostVersion;

	/// <summary>
	/// The steam branch the host is playing on.
	/// </summary>
	public PlatformBranch? hostBranch;

	/// <summary>
	/// The ModelDb hash reported by the host.
	/// </summary>
	public ulong? hostHash;

	/// <summary>
	/// The version of the game that we are playing on.
	/// </summary>
	public string? localVersion;

	/// <summary>
	/// The steam branch that we are playing on.
	/// </summary>
	public PlatformBranch? localBranch;

	/// <summary>
	/// The ModelDb hash reported by us.
	/// </summary>
	public ulong? localHash;
}
