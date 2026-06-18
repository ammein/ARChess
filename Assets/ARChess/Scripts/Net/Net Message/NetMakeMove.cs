using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetMakeMove : NetMessage
    {
        // When using Internet sending the package, we have to use int as simple as possible to transfer the package
        public int originalX;
        public int originalY;
        public int destinationX;
        public int destinationY;
        public int teamId;
        
        public NetMakeMove() // Making the package message
        {
            Code = OpCode.MAKE_MOVE;
        }

        public NetMakeMove(DataStreamReader reader) // Receiving the package message
        {
            Code = OpCode.MAKE_MOVE;
            Deserialize(reader);
        }

        public override void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((byte)Code);
            writer.WriteInt(originalX);
            writer.WriteInt(originalY);
            writer.WriteInt(destinationX);
            writer.WriteInt(destinationY);
            writer.WriteInt(teamId); 
        }

        public override void Deserialize(DataStreamReader reader)
        {
            originalX = reader.ReadInt();
            originalY = reader.ReadInt();
            destinationX = reader.ReadInt();
            destinationY = reader.ReadInt();
            teamId = reader.ReadInt();
        }

        public override void ReceivedOnClient()
        {
            NetUtility.C_MAKE_MOVE?.Invoke(this);
        }

        public override void ReceivedOnServer(NetworkConnection conn)
        {
            NetUtility.S_MAKE_MOVE?.Invoke(this, conn);
        }
    }
}
