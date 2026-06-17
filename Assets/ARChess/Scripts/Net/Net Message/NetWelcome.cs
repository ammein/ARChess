using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetWelcome : NetMessage
    {
        public int AssignedTeam { set; get; }
        
        public NetWelcome()
        {
            Code = OpCode.WELCOME;
        }
        public NetWelcome(DataStreamReader reader) // This is where we receive the package
        {
            Code = OpCode.WELCOME;
            Deserialize(reader);
        }

        public override void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((Byte)Code);
            writer.WriteInt(AssignedTeam);
        }
        public override void Deserialize(DataStreamReader reader)
        {
            // We already read the byte in the NetUtility::OnData
            AssignedTeam = reader.ReadInt();
        }

        public override void ReceivedOnClient()
        {
            NetUtility.C_WELCOME?.Invoke(this);
        }
        public override void ReceivedOnServer(NetworkConnection cnn)
        {
            NetUtility.S_WELCOME?.Invoke(this, cnn);
        }
    }
}
