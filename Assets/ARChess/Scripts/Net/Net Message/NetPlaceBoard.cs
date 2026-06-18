using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetPlaceBoard : NetMessage
    {
        public int Team { set; get; } = -1;
        
        public NetPlaceBoard()
        {
            Code = OpCode.PLACE_BOARD;
        }
        public NetPlaceBoard(DataStreamReader reader) // This is where we receive the package
        {
            Code = OpCode.PLACE_BOARD;
            Deserialize(reader);
        }

        public override void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((Byte)Code);
            writer.WriteInt(Team);
        }
        public override void Deserialize(DataStreamReader reader)
        {
            // We already read the byte in the NetUtility::OnData
            Team = reader.ReadInt();
        }

        public override void ReceivedOnClient()
        {
            NetUtility.C_PLACE_BOARD?.Invoke(this);
        }
        public override void ReceivedOnServer(NetworkConnection cnn)
        {
            NetUtility.S_PLACE_BOARD?.Invoke(this, cnn);
        }
    }
}
