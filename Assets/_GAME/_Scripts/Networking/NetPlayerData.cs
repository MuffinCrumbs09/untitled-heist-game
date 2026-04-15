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

public struct NetPlayerData : INetworkSerializable, IEquatable<NetPlayerData>
{
    #region Player Identifiers
    public NetString USERNAME;
    public ulong CLIENTID;
    #endregion

    #region Gameplay
    public int KILLS;
    public PlayerState STATE;
    public int SKILLS;
    #endregion

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out USERNAME);
            reader.ReadValueSafe(out CLIENTID);
            reader.ReadValueSafe(out KILLS);
            reader.ReadValueSafe(out STATE);
            reader.ReadValueSafe(out SKILLS);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(USERNAME);
            writer.WriteValueSafe(CLIENTID);
            writer.WriteValueSafe(KILLS);
            writer.WriteValueSafe(STATE);
            writer.WriteValueSafe(SKILLS);
        }
    }

    public bool Equals(NetPlayerData other)
    {
        return other.CLIENTID == CLIENTID &&
       other.USERNAME.Equals(USERNAME) &&
       other.KILLS == KILLS &&
       other.STATE == STATE &&
         other.SKILLS == SKILLS;
    }

    // Constructor
    public NetPlayerData(NetString user, ulong id)
    {
        USERNAME = user;
        CLIENTID = id;
        KILLS = 0;
        STATE = PlayerState.MaskOff;
        SKILLS = 0;
    }
}