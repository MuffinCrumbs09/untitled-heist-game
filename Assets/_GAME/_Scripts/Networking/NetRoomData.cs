using Unity.Netcode;

public struct NetRoomData : INetworkSerializable, System.IEquatable<NetRoomData>
{
    public NetString AreaName;
    public NetString RoomType;

    public NetRoomData(string areaName, string roomType)
    {
        AreaName = areaName;
        RoomType = roomType;
    }

    public bool Equals(NetRoomData other)
    {
        return other.AreaName == AreaName && other.RoomType == RoomType;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out AreaName);
            reader.ReadValueSafe(out RoomType);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(AreaName);
            writer.WriteValueSafe(RoomType);
        }
    }
}