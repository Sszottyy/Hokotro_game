using Unity.Netcode;
using System;

[Serializable]
public struct PlayerData : INetworkSerializable
{
    public string Name;
    public string TeamName;
    public string Role;
    public ulong ClientId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref TeamName);
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref ClientId);
    }
}