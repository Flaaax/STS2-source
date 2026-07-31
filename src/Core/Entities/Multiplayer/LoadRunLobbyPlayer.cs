using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MegaCrit.Sts2.Core.Entities.Multiplayer;

public struct LoadRunLobbyPlayer : IPacketSerializable
{
	public ulong id;

	public PeerVersionInfo versionInfo;

	public bool isReady;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteULong(id);
		writer.Write(versionInfo);
		writer.WriteBool(isReady);
	}

	public void Deserialize(PacketReader reader)
	{
		id = reader.ReadULong();
		versionInfo = reader.Read<PeerVersionInfo>();
		isReady = reader.ReadBool();
	}

	public override string ToString()
	{
		return $"Player {id}";
	}
}
