using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;

/// <summary>
/// Sent by a client when they detected an error during connection negotiation.
/// Usually caused by version or mod mismatch. Can also be an echo of a host error (lobby full).
/// </summary>
public struct ClientConnectionFailedMessage : INetMessage, IPacketSerializable
{
	public ConnectionFailureReason disconnectionReason;

	public PeerVersionInfo versionInfo;

	public bool ShouldBroadcast => false;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Debug;

	public bool ShouldBuffer => false;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteEnum(disconnectionReason);
		writer.Write(versionInfo);
	}

	public void Deserialize(PacketReader reader)
	{
		disconnectionReason = reader.ReadEnum<ConnectionFailureReason>();
		versionInfo = reader.Read<PeerVersionInfo>();
	}

	public LocString GetLocString(PeerVersionInfo localVersion)
	{
		ConnectionFailureExtraInfo extraInfo = new ConnectionFailureExtraInfo
		{
			remoteInfo = versionInfo,
			localInfo = localVersion,
			localIsHost = true
		};
		NetErrorInfo info = new NetErrorInfo(disconnectionReason, extraInfo);
		bool showReportBugButton;
		return NErrorPopup.LocStringFromNetError(info, out showReportBugButton);
	}
}
