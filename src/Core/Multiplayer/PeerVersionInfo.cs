using System.Collections.Generic;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Multiplayer;

public struct PeerVersionInfo : IPacketSerializable
{
	/// <summary>
	/// The version of the game the host is running.
	/// </summary>
	public string version;

	/// <summary>
	/// The steam branch that the host is playing on.
	/// DO NOT compare this to the client's when joining to check versions. The same game version might be on different
	/// steam branches. Only compare <see cref="F:MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo.version" /> and only use this for diagnostic purposes.
	/// </summary>
	public PlatformBranch branch;

	/// <summary>
	/// A hash of all the IDs in the model database.
	/// </summary>
	public uint idDatabaseHash;

	/// <summary>
	/// A list of all the gameplay-affecting mods that the host has installed, if any.
	/// </summary>
	public List<string>? gameplayAffectingMods;

	/// <summary>
	/// A list of all the non-gameplay-affecting mods that the host has installed, if any.
	/// </summary>
	public List<string>? otherMods;

	/// <summary>
	/// Returns a PeerVersionInfo with data filled in from various singletons.
	/// </summary>
	public static PeerVersionInfo LocalDefault()
	{
		return new PeerVersionInfo
		{
			version = NGame.GetGameVersion(),
			branch = PlatformUtil.GetPlatformBranch(),
			idDatabaseHash = ModelIdSerializationCache.Hash,
			gameplayAffectingMods = ModManager.GetGameplayRelevantModNameList(),
			otherMods = ModManager.GetNonGameplayRelevantModNameList()
		};
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteString(version);
		writer.WriteEnum(branch);
		writer.WriteUInt(idDatabaseHash);
		writer.WriteBool(gameplayAffectingMods != null);
		if (gameplayAffectingMods != null)
		{
			writer.WriteInt(gameplayAffectingMods.Count);
			foreach (string gameplayAffectingMod in gameplayAffectingMods)
			{
				writer.WriteString(gameplayAffectingMod);
			}
		}
		writer.WriteBool(otherMods != null);
		if (otherMods == null)
		{
			return;
		}
		writer.WriteInt(otherMods.Count);
		foreach (string otherMod in otherMods)
		{
			writer.WriteString(otherMod);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		version = reader.ReadString();
		branch = reader.ReadEnum<PlatformBranch>();
		idDatabaseHash = reader.ReadUInt();
		if (reader.ReadBool())
		{
			int num = reader.ReadInt();
			gameplayAffectingMods = new List<string>();
			for (int i = 0; i < num; i++)
			{
				gameplayAffectingMods.Add(reader.ReadString());
			}
		}
		if (reader.ReadBool())
		{
			int num2 = reader.ReadInt();
			otherMods = new List<string>();
			for (int j = 0; j < num2; j++)
			{
				otherMods.Add(reader.ReadString());
			}
		}
	}
}
