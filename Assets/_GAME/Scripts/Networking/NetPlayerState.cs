using System;
using Unity.Netcode;

public enum PlayerState
{
    MaskOff,
    MaskOn,
    DBNO,
    Dead,
    Error
}

public struct NetPlayerState : INetworkSerializable, IEquatable<NetPlayerState>
{
    public PlayerState state;
    public ulong clientID;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out state);
            reader.ReadValueSafe(out clientID);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(state);
            writer.WriteValueSafe(clientID);
        }
    }

    public bool Equals(NetPlayerState other)
    {
        if (other.state == state)
            return true;
        return false;
    }
}