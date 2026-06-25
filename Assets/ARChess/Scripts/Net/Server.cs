using System;
using ARChess.Scripts.Net.Net_Message;
using ARChess.Scripts.Utility;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace ARChess.Scripts.Net
{
    
    public class Server : MonoBehaviour
    {
        #region Singleton implementation
        public static Server Instance { get; set; }
        private void Awake()
        {
            // --- THE SINGLETON PROTECTION ---
            // If an instance already exists, do NOT overwrite it. 
            // Just wait quietly for the NetworkManager to destroy this duplicate object.
            if (Instance != null && Instance != this) return; 

            Instance = this;
            // --------------------------------
        }
        #endregion
        
        public NetworkDriver driver;
        private NativeList<NetworkConnection> connections;

        private bool isActive = false;
        private const float keepAliveTickRate = 20.0f; // How long should I wake before I send message. Just to keep alive basically
        private float lastKeepAlive; // Just to make sure the connection does not drop.

        public Action connectionDropped;

        // Methods
        public void Init(ushort port)
        {
            // Initialize Driver
            driver = NetworkDriver.Create();
            NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
            endpoint.Port = port;

            if (driver.Bind(endpoint) != 0)
            {
                Log.LogThis("Failed to bind to port " + endpoint.Port, this);
                return;
            }
            else
            {
                driver.Listen();
                Log.LogThis("Listening on port " + endpoint.Port, this);
            }
            
            connections = new NativeList<NetworkConnection>(2, Allocator.Persistent); // We send amount of player that we have
            isActive = true;
        }

        public void Shutdown()
        {
            if (isActive)
            {
                driver.Dispose();
                connections.Dispose();
                isActive = false;
            }
        }

        public void OnDestroy()
        {
            Shutdown();
        }

        public void Update()
        {
            if (!isActive)
                return;

            KeepAlive();
            
            driver.ScheduleUpdate().Complete(); // Empty up queue messages coming in
            CleanupConnections(); // Is there anybody that we don't have to do access on but we still have the references?
            AcceptNewConnections(); // Is there anybody knocking on the door to enter our server
            UpdateMessagePump(); // Are they sending us a message, if so, do we have to reply?
        }

        private void KeepAlive()
        {
            if (Time.time - lastKeepAlive > keepAliveTickRate) // Every 20 seconds
            {
                lastKeepAlive = Time.time; // Resets 20 seconds timer...
                Broadcast(new NetKeepAlive()); // Send broadcast to every client...
            }
        }

        private void CleanupConnections()
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (!connections[i].IsCreated)
                {
                    connections.RemoveAtSwapBack(i);
                    --i;
                }
            }
        }

        private void AcceptNewConnections()
        {
            // Accept new connections
            NetworkConnection c;
            while ((c = driver.Accept()) != default(NetworkConnection))
            {
                connections.Add(c);
            }
        }

        private void UpdateMessagePump()
        {
            DataStreamReader stream; // Will be used for messages in case there is one
            for (int i = 0; i < connections.Length; i++) // Every single connection there is
            {
                NetworkEvent.Type cmd;
                while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
                { // Every network that has been sent ...
                    if (cmd == NetworkEvent.Type.Data) // Either "Data"
                    {
                        NetUtility.OnData(stream, connections[i], this);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect) // Or "Disconnect"
                    {
                        Log.LogThis("Client disconnected from server:  " + connections[i].ToString(), this);
                        connections[i] = default(NetworkConnection);
                        connectionDropped?.Invoke();
                        
                        Shutdown(); // This does not happen usually, its just because we're in a two person game
                        
                        // THE THE MEMORY LEAKED FIX
                        // Immediately exit the entire method. Do not allow the loop 
                        // to evaluate connections[i] again!
                        return; 
                    }
                }
            }
        }
        
        public void SendToClient(NetworkConnection connection, NetMessage msg) // send only to specific client
        {
            // Prevent crash if driver or connection is dead/uninitialized
            if (!driver.IsCreated || !connection.IsCreated) return;

            DataStreamWriter writer;
            int status = driver.BeginSend(connection, out writer); // The pipeline writes out to the "writer"
            
            // ONLY serialize and send if BeginSend was successful (returns 0)
            if (status == 0)
            {
                msg.Serialize(ref writer); // We can put our own message to the "writer"
                driver.EndSend(writer); // Give back to the driver to send out the message...
            }
            else
            {
                Debug.LogWarning("Server failed to begin send. Client connection may be invalid.");
            }
        }
        
        // Server specific
        public void Broadcast(NetMessage msg) // Broadcast to all clients
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].IsCreated)
                {
                    Log.LogThis($"Sending {msg.Code} to : {connections[i].ToString()}", this);
                    SendToClient(connections[i], msg);
                }
            }
        }
    }   
}
