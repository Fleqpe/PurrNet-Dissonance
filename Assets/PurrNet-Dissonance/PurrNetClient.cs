﻿using System;
using System.Collections.Generic;
using Dissonance.Networking;
using JetBrains.Annotations;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Pooling;
using PurrNet.Transports;

namespace Dissonance.Integrations.PurrNet
{
    public class PurrNetClient : BaseClient<PurrNetServer, PurrNetClient, PlayerID>
    {
        private readonly PurrNetCommsNetwork _network;

        public PurrNetClient(PurrNetCommsNetwork network) : base(network)
        {
            _network = network;
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            if (NetworkManager.main != null && NetworkManager.main.sceneModule != null)
                if (NetworkManager.main.clientState == ConnectionState.Connected)
                    if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene))
                    {
                        var bytes = ByteArrayPool.Rent(packet.Count);
                        Buffer.BlockCopy(packet.Array, packet.Offset, bytes, 0, packet.Count);
                        PurrNetServer.ServerReceiveDataReliable(bytes, scene);
                    }
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            if (NetworkManager.main != null && NetworkManager.main.sceneModule != null)
                if (NetworkManager.main.clientState == ConnectionState.Connected)
                    if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene))
                    {
                        var bytes = ByteArrayPool.Rent(packet.Count);
                        Buffer.BlockCopy(packet.Array, packet.Offset, bytes, 0, packet.Count);
                        PurrNetServer.ServerReceiveDataUnreliable(bytes, scene);
                    }
        }

        public override void Connect()
        {
            base.Connected();
        }

        private static Dictionary<SceneID, Queue<byte[]>> _receivedData = new();

        [TargetRpc(Channel.Unreliable)]
        public static void ClientReceiveDataUnreliable([UsedImplicitly] PlayerID target, SceneID scene, byte[] data)
        {
            ReceiveData(scene, data);
        }
        [TargetRpc(Channel.ReliableOrdered)]
        public static void ClientReceiveDataReliable([UsedImplicitly] PlayerID target, SceneID scene, byte[] data)
        {
            ReceiveData(scene, data);
        }

        private static void ReceiveData(SceneID scene, byte[] data)
        {
            if (!_receivedData.TryGetValue(scene, out var queue))
            {
                var newQueue = QueuePool<byte[]>.Instantiate();
                _receivedData[scene] = queue = newQueue;
            }

            queue.Enqueue(data);
        }

        protected override void ReadMessages()
        {
            if (NetworkManager.main.sceneModule.TryGetSceneID(_network.gameObject.scene, out var scene) &&
                _receivedData.TryGetValue(scene, out var dataQueue))
            {
                while (dataQueue.Count > 0)
                {
                    var data = dataQueue.Dequeue();
                    base.NetworkReceivedPacket(data);
                    ByteArrayPool.Return(data);
                }
            }
        }
    }
}