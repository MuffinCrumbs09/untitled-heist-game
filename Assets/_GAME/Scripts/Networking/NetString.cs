using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetString : INetworkSerializable, System.IEquatable<NetString>
{
    ForceNetworkSerializeByMemcpy<FixedString128Bytes> st;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out st);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(st);
        }
    }
    public bool Equals(NetString other)
    {
        if (String.Equals(other.st.ToString(), st.ToString(), StringComparison.CurrentCultureIgnoreCase))
            return true;
        return false;
    }

    public override string ToString()
    {
        return st.Value.ToString();
    }

    public static implicit operator string(NetString s) => s.ToString();
    public static implicit operator NetString(string s) => new NetString() { st = new FixedString128Bytes(s) };
}