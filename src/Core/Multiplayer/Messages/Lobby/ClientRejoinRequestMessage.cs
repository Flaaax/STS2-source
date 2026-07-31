using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;

public struct ClientRejoinRequestMessage : INetMessage, IPacketSerializable
{
	public PeerVersionInfo versionInfo;

	public bool ShouldBroadcast => false;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Info;

	public bool ShouldBuffer => true;

	public void Serialize(PacketWriter writer)
	{
		writer.Write(versionInfo);
	}

	public void Deserialize(PacketReader reader)
	{
		versionInfo = reader.Read<PeerVersionInfo>();
	}
}
