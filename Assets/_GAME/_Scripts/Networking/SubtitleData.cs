using Unity.Netcode;

public enum SubtitleType
{
    Player,
    NPC
}

public struct SubtitleData : INetworkSerializable
{
    public ulong SenderClientId;
    public string Username;
    public string Message;
    public SubtitleType Type;
    public float DisplayDuration;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SenderClientId);
        serializer.SerializeValue(ref Username);
        serializer.SerializeValue(ref Message);
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref DisplayDuration);
    }
}
