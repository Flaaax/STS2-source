using System;
using MegaCrit.Sts2.Core.Entities.Multiplayer;

namespace MegaCrit.Sts2.Core.Multiplayer.Connection;

public class ClientConnectionFailedException : Exception
{
	public NetErrorInfo info;

	public ConnectionFailureReason rawReason;

	public ClientConnectionFailedException(string message, ConnectionFailureReason rawReason, ConnectionFailureExtraInfo? extraInfo)
		: base(message)
	{
		this.rawReason = rawReason;
		info = new NetErrorInfo(rawReason, extraInfo);
	}

	public ClientConnectionFailedException(string message, NetErrorInfo info)
		: base(message)
	{
		rawReason = ConnectionFailureReason.None;
		this.info = info;
	}
}
