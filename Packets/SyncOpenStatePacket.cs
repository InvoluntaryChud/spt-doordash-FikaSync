using Fika.Core.Networking.LiteNetLib.Utils;

namespace tarkin.doordash
{
    public struct SyncOpenStatePacket : INetSerializable
    {
        public int netID;
        public string objectID;

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(netID);
            writer.Put(objectID);
        }

        public void Deserialize(NetDataReader reader)
        {
            netID = reader.GetInt();
            objectID = reader.GetString();
        }
    }

}
