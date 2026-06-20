using System;
using ARChess.Scripts.Net.Net_Message;
using ARChess.Scripts.Utility;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace ARChess.Scripts.Net
{
    public class Client : MonoBehaviour
    {
        #region Singleton implementation
        public static Client Instance { get; set; }
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
        private NetworkConnection connection;

        private bool isActive = false;
        private const float keepAliveTickRate = 20.0f; // How long should I wake before I send message. Just to keep alive basically
        private float lastKeepAlive; // Just to make sure the connection does not drop.

        public Action connectionDropped;
        
        // Methods
        public void Init(string ip, ushort port)
        {
            // Initialize Driver
            driver = NetworkDriver.Create();
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(ip, port);
            
            connection = driver.Connect(endpoint);
            Log.LogThis($"Attempting to connect to Server on {endpoint.Address}", this);
            isActive = true;

            RegisterToEvent();
        }

        public void Shutdown()
        {
            if (isActive)
            {
                UnregisterToEvent();
                driver.Dispose();
                isActive = false;
                connection = default(NetworkConnection);
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
            
            driver.ScheduleUpdate().Complete(); // Empty up queue messages coming in
            CheckAlive();
            
            UpdateMessagePump(); // Are they sending us a message, if so, do we have to reply?
        }

        private void CheckAlive()
        {
            if (!connection.IsCreated && isActive)
            {
                Log.LogThis("Something went wrong, lost connection to server", this);
                connectionDropped?.Invoke();
                Shutdown();
            }
        }

        private void UpdateMessagePump()
        {
            DataStreamReader stream; // Will be used for messages in case there is one
            NetworkEvent.Type cmd;
            while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            { // Every network that has been sent ...
                if (cmd == NetworkEvent.Type.Connect)
                {
                    SendToServer(new NetWelcome());
                    Debug.Log("We're connected!");
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, default(NetworkConnection));
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Log.LogThis("Client got disconnected from server", this);
                    connection = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    
                    Shutdown(); // This disposes the driver
                    
                    // --- THE MEMORY LEAKED FIX ---
                    // Immediately exit the method so the while loop doesn't 
                    // try to evaluate a disposed driver!
                    return; 
                    // ---------------
                }
            }
        }

        public bool SendToServer(NetMessage msg)
        {
            // Prevent crash if driver/connection is dead or not initialized
            if (!driver.IsCreated || !connection.IsCreated) return false;

            DataStreamWriter writer;
            int status = driver.BeginSend(connection, out writer);
            
            // ONLY serialize and send if BeginSend was successful (returns 0)
            if (status == 0)
            {
                msg.Serialize(ref writer);
                driver.EndSend(writer);
                return true; // Successfully queued!
            }
            else
            {
                Debug.LogWarning("Failed to begin send. Network queue may be invalid.");
                return false; // Failed to send
            }
        }
        
        // Event parsing
        private void RegisterToEvent()
        {
            NetUtility.C_KEEP_ALIVE += OnKeepAlive;
        }

        private void UnregisterToEvent()
        {
            NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
        }

        private void OnKeepAlive(NetMessage msg)
        {
            // Send it back, to keep both side alive
            SendToServer(msg);
        }
    }
}
