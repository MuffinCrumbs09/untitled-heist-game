using Unity.Netcode;

public class SubtitleData : INetworkSerializable
{
    public ulong SenderClientId;
    public NetString Username;
    public NetString Message;
    public bool IsGlobal;
    public float DisplayDuration;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SenderClientId);
        serializer.SerializeValue(ref Username);
        serializer.SerializeValue(ref Message);
        serializer.SerializeValue(ref IsGlobal);
        serializer.SerializeValue(ref DisplayDuration);
    }
}
