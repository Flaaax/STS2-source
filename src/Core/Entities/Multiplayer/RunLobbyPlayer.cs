using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MegaCrit.Sts2.Core.Entities.Multiplayer;

public struct RunLobbyPlayer : IPacketSerializable
{
	public ulong id;

	public PeerVersionInfo versionInfo;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteULong(id);
		writer.Write(versionInfo);
	}

	public void Deserialize(PacketReader reader)
	{
		id = reader.ReadULong();
		versionInfo = reader.Read<PeerVersionInfo>();
	}
}
