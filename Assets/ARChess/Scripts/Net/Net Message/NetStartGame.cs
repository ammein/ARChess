

using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetStartGame : NetMessage
    {
        
        public NetStartGame()
        {
            Code = OpCode.START_GAME;
        }
        public NetStartGame(DataStreamReader reader) // This is where we receive the package
        {
            Code = OpCode.START_GAME;
            Deserialize(reader);
        }

        public override void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((Byte)Code);
        }
        public override void Deserialize(DataStreamReader reader)
        {
            // We already read the byte in the NetUtility::OnData
        }

        public override void ReceivedOnClient()
        {
            NetUtility.C_START_GAME?.Invoke(this);
        }
        public override void ReceivedOnServer(NetworkConnection cnn)
        {
            NetUtility.S_START_GAME?.Invoke(this, cnn);
        }
    }   
}
