using Unity.Collections;
using Unity.Networking.Transport;

namespace ARChess.Scripts.Net.Net_Message
{
    public class NetMessage
    {
        public OpCode Code { set; get; }

        public virtual void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteByte((byte)Code); // Write the the message that has been inserted
        }
        public virtual void Deserialize(DataStreamReader reader) // Unpacking the message and putting into right place
        {
            
        }
        
        public virtual void ReceivedOnClient()
        {
            
        }
        public virtual void ReceivedOnServer(NetworkConnection conn)
        {
            
        }
    }
}
