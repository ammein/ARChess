using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetRematch : NetMessage
    {
        // When using Internet sending the package, we have to use int/byte as simple as possible to transfer the package
        public int teamId;
        public byte wantRematch;
        
        public NetRematch() // Making the package message
        {
            Code = OpCode.REMATCH;
        }

        public NetRematch(DataStreamReader reader) // Receiving the package message
        {
            Code = OpCode.REMATCH;
            Deserialize(reader);
        }

        public override void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((byte)Code);
            writer.WriteInt(teamId); 
            writer.WriteByte(wantRematch); // Internet package does not have boolean. So byte is a simple 1 & 0 . It acts as boolean
        }

        public override void Deserialize(DataStreamReader reader)
        {
            teamId = reader.ReadInt();
            wantRematch = reader.ReadByte();
        }

        public override void ReceivedOnClient()
        {
            NetUtility.C_REMATCH?.Invoke(this);
        }

        public override void ReceivedOnServer(NetworkConnection conn)
        {
            NetUtility.S_REMATCH?.Invoke(this, conn);
        }
    }
}
