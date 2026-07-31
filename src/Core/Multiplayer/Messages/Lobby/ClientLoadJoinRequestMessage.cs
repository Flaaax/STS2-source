using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;

/// <summary>
/// Sent by a client to the host as their first message, requesting that they join a loaded lobby.
/// Only sent if the lobby was started from a loaded game.
/// </summary>
public struct ClientLoadJoinRequestMessage : INetMessage, IPacketSerializable
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
