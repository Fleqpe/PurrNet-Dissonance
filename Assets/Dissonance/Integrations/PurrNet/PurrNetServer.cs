using System;
using System.Collections.Generic;
using Dissonance.Networking;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace Dissonance.Integrations.PurrNet
{
    public class PurrNetServer : BaseServer<PurrNetServer, PurrNetClient, PlayerID>
    {
        private readonly PurrNetCommsNetwork _network;

        public PurrNetServer(PurrNetCommsNetwork network)
        {
            _network = network;
        }

        protected override void SendReliable(PlayerID connection, ArraySegment<byte> packet)
        {
            if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene))
            {
                PurrNetClient.ClientReceiveDataReliable(connection, scene, packet.ToArray());
            }
            //PurrLogger.Log($"Sending reliable | PlayerID {connection}");
        }

        protected override void SendUnreliable(PlayerID connection, ArraySegment<byte> packet)
        {
            if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene))
            {
                PurrNetClient.ClientReceiveDataUnreliable(connection, scene, packet.ToArray());
            }
            
            //PurrLogger.Log($"Sending unreliable | PlayerID {connection}");
        }
        
        private static Dictionary<SceneID, SceneQueue> _receivedData = new();

        [ServerRpc(requireOwnership: false, channel: Channel.ReliableOrdered)]
        public static void ServerReceiveDataReliable(byte[] data, SceneID scene, RPCInfo info = default)
        {
            ReceiveData(info.sender, scene, data);
        }
        [ServerRpc(requireOwnership: false, channel: Channel.Unreliable)]
        public static void ServerReceiveDataUnreliable(byte[] data, SceneID scene, RPCInfo info = default)
        {
            ReceiveData(info.sender, scene, data);
        }
        
        private static void ReceiveData(PlayerID player, SceneID scene, byte[] data)
        {
            if (!_receivedData.TryGetValue(scene, out var sceneQueue))
            {
                sceneQueue = new SceneQueue();
                sceneQueue.data = new();
                _receivedData[scene] = sceneQueue;
            }
            
            if(!sceneQueue.data.ContainsKey(player))
                sceneQueue.data[player] = new Queue<byte[]>();

            sceneQueue.data[player].Enqueue(data);
        }

        protected override void ReadMessages()
        {
            if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene) && _receivedData.TryGetValue(scene, out var dataQueue))
            {
                foreach (var (player, queue) in dataQueue.data)
                {
                    while (queue.Count > 0)
                        base.NetworkReceivedPacket(player, queue.Dequeue());
                }
            }
        }

        private struct SceneQueue
        {
            public Dictionary<PlayerID, Queue<byte[]>> data;
        }
    }
}