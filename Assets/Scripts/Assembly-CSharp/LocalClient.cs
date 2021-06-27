﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Steamworks;
using UnityEngine;

public class LocalClient : MonoBehaviour
{
    private void Awake()
    {
        if (LocalClient.instance == null)
        {
            LocalClient.instance = this;
            return;
        }
        if (LocalClient.instance != this)
        {
            Debug.Log("Instance already exists, destroying object");
            Destroy(this);
        }
    }

    private void Start()
    {
        this.StartProtocols();
    }

    private void StartProtocols()
    {
        this.tcp = new LocalClient.TCP();
        this.udp = new LocalClient.UDP();
    }

    public void ConnectToServer(string ip, string username)
    {
        this.ip = ip;
        this.StartProtocols();
        LocalClient.InitializeClientData();
        this.isConnected = true;
        this.tcp.Connect();
    }

    public static void InitializeClientData()
    {
        LocalClient.packetHandlers = new Dictionary<int, LocalClient.PacketHandler>
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
            { (int)ServerPackets.playerDisconnect, ClientHandle.DisconnectPlayer },
            { (int)ServerPackets.playerDied, ClientHandle.PlayerDied },
            { (int)ServerPackets.pingPlayer, ClientHandle.ReceivePing },
            { (int)ServerPackets.connectionSuccessful, ClientHandle.ConnectionEstablished },
            // ServerPackets.sendLevel
            { (int)ServerPackets.sendStatus, ClientHandle.ReceiveStatus },
            { (int)ServerPackets.gameOver, ClientHandle.GameOver },
            { (int)ServerPackets.startGame, ClientHandle.StartGame },
            { (int)ServerPackets.clock, ClientHandle.Clock },
            { (int)ServerPackets.openDoor, ClientHandle.OpenDoor },
            { (int)ServerPackets.ready, ClientHandle.Ready },
            // ServerPackets.taskProgress
            { (int)ServerPackets.dropItem, ClientHandle.DropItem },
            { (int)ServerPackets.pickupItem, ClientHandle.PickupItem },
            { (int)ServerPackets.weaponInHand, ClientHandle.WeaponInHand },
            { (int)ServerPackets.playerHitObject, ClientHandle.PlayerHitObject },
            { (int)ServerPackets.dropResources, ClientHandle.DropResources },
            { (int)ServerPackets.animationUpdate, ClientHandle.AnimationUpdate },
            { (int)ServerPackets.finalizeBuild, ClientHandle.FinalizeBuild },
            { (int)ServerPackets.openChest, ClientHandle.OpenChest },
            { (int)ServerPackets.updateChest, ClientHandle.UpdateChest },
            { (int)ServerPackets.pickupInteract, ClientHandle.PickupInteract },
            { (int)ServerPackets.dropItemAtPosition, ClientHandle.DropItemAtPosition },
            { (int)ServerPackets.playerHit, ClientHandle.PlayerHit },
            { (int)ServerPackets.mobSpawn, ClientHandle.MobSpawn },
            { (int)ServerPackets.mobMove, ClientHandle.MobMove },
            { (int)ServerPackets.mobSetDestination, ClientHandle.MobSetDestination },
            { (int)ServerPackets.mobAttack, ClientHandle.MobAttack },
            { (int)ServerPackets.playerDamageMob, ClientHandle.PlayerDamageMob },
            { (int)ServerPackets.shrineCombatStart, ClientHandle.ShrineCombatStart },
            { (int)ServerPackets.dropPowerupAtPosition, ClientHandle.DropPowerupAtPosition },
            { (int)ServerPackets.MobZoneSpawn, ClientHandle.MobZoneSpawn },
            { (int)ServerPackets.MobZoneToggle, ClientHandle.MobZoneToggle },
            { (int)ServerPackets.PickupZoneSpawn, ClientHandle.PickupSpawnZone },
            { (int)ServerPackets.SendMessage, ClientHandle.ReceiveChatMessage },
            { (int)ServerPackets.playerPing, ClientHandle.ReceivePlayerPing },
            { (int)ServerPackets.sendArmor, ClientHandle.ReceivePlayerArmor },
            { (int)ServerPackets.playerHp, ClientHandle.PlayerHp },
            { (int)ServerPackets.respawnPlayer, ClientHandle.RespawnPlayer },
            { (int)ServerPackets.shootArrow, ClientHandle.ShootArrowFromPlayer },
            { (int)ServerPackets.removeResource, ClientHandle.RemoveResource },
            { (int)ServerPackets.mobProjectile, ClientHandle.MobSpawnProjectile },
            { (int)ServerPackets.newDay, ClientHandle.NewDay },
            { (int)ServerPackets.knockbackMob, ClientHandle.KnockbackMob },
            { (int)ServerPackets.spawnEffect, ClientHandle.SpawnEffect },
            { (int)ServerPackets.playerFinishedLoading, ClientHandle.PlayerFinishedLoading },
            { (int)ServerPackets.revivePlayer, ClientHandle.RevivePlayer },
            { (int)ServerPackets.spawnGrave, ClientHandle.SpawnGrave },
            { (int)ServerPackets.interact, ClientHandle.Interact },
            { (int)ServerPackets.setTarget, ClientHandle.MobSetTarget },
			{ (int)ServerPackets.shipUpdate, ClientHandle.ShipUpdate },
			{ (int)ServerPackets.dragonUpdate, ClientHandle.DragonUpdate },

            { (int)ServerPackets.moveVehicle, ClientHandle.UpdateCar },
            { (int)ServerPackets.enterVehicle, ClientHandle.EnterVehicle },
            { (int)ServerPackets.exitVehicle, ClientHandle.ExitVehicle },
            { (int)ServerPackets.loadSave, ClientHandle.LoadSave },
            { (int)ServerPackets.dontDestroy, ClientHandle.DontDestroy },
        };
        Debug.Log("Initializing packets.");
    }

    private void OnApplicationQuit()
    {
        this.Disconnect();
    }

    public void Disconnect()
    {
        if (this.isConnected)
        {
            ClientSend.PlayerDisconnect();
            this.isConnected = false;
            this.tcp.socket.Close();
            this.udp.socket.Close();
            Debug.Log("Disconnected from server.");
        }
    }

    public static LocalClient instance;

    public static int dataBufferSize = 4096;

    public SteamId serverHost;

    public string ip = "127.0.0.1";

    public int port = 26950;

    public int myId;

    public LocalClient.TCP tcp;

    public LocalClient.UDP udp;

    public static bool serverOwner;

    private bool isConnected;

    public static Dictionary<int, LocalClient.PacketHandler> packetHandlers;

    public static int byteDown;

    public static int packetsReceived;

    public delegate void PacketHandler(Packet packet);

    public class TCP
    {
        public void Connect()
        {
            this.socket = new TcpClient
            {
                ReceiveBufferSize = LocalClient.dataBufferSize,
                SendBufferSize = LocalClient.dataBufferSize
            };
            this.receiveBuffer = new byte[LocalClient.dataBufferSize];
            this.socket.BeginConnect(LocalClient.instance.ip, LocalClient.instance.port, new AsyncCallback(this.ConnectCallback), this.socket);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            this.socket.EndConnect(result);
            if (!this.socket.Connected)
            {
                return;
            }
            this.stream = this.socket.GetStream();
            this.receivedData = new Packet();
            this.stream.BeginRead(this.receiveBuffer, 0, LocalClient.dataBufferSize, new AsyncCallback(this.ReceiveCallback), null);
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (this.socket != null)
                {
                    this.stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception arg)
            {
                Debug.Log(string.Format("Error sending data to server via TCP: {0}", arg));
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int num = this.stream.EndRead(result);
                if (num <= 0)
                {
                    LocalClient.instance.Disconnect();
                }
                else
                {
                    byte[] array = new byte[num];
                    Array.Copy(this.receiveBuffer, array, num);
                    this.receivedData.Reset(this.HandleData(array));
                    this.stream.BeginRead(this.receiveBuffer, 0, LocalClient.dataBufferSize, new AsyncCallback(this.ReceiveCallback), null);
                }
            }
            catch
            {
                this.Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            LocalClient.packetsReceived++;
            int packetLength = 0;
            this.receivedData.SetBytes(data);
            if (this.receivedData.UnreadLength() >= 4)
            {
                packetLength = this.receivedData.ReadInt(true);
                if (packetLength <= 0)
                {
                    return true;
                }
            }
            while (packetLength > 0 && packetLength <= this.receivedData.UnreadLength())
            {
                byte[] packetBytes = this.receivedData.ReadBytes(packetLength, true);
                ThreadManagerClient.ExecuteOnMainThread(delegate
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int num = packet.ReadInt(true);
                        LocalClient.byteDown += packetLength;
                        Debug.Log("received packet: " + (ServerPackets)num);
                        LocalClient.packetHandlers[num](packet);
                    }
                });
                packetLength = 0;
                if (this.receivedData.UnreadLength() >= 4)
                {
                    packetLength = this.receivedData.ReadInt(true);
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }
            return packetLength <= 1;
        }

        private void Disconnect()
        {
            LocalClient.instance.Disconnect();
            this.stream = null;
            this.receivedData = null;
            this.receiveBuffer = null;
            this.socket = null;
        }

        public TcpClient socket;

        private NetworkStream stream;

        private Packet receivedData;

        private byte[] receiveBuffer;
    }

    public class UDP
    {
        public UDP()
        {
            this.endPoint = new IPEndPoint(IPAddress.Parse(LocalClient.instance.ip), LocalClient.instance.port);
        }

        public void Connect(int localPort)
        {
            this.socket = new UdpClient(localPort);
            this.socket.Connect(this.endPoint);
            this.socket.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
            using (Packet packet = new Packet())
            {
                this.SendData(packet);
            }
        }

        public void SendData(Packet packet)
        {
            try
            {
                packet.InsertInt(LocalClient.instance.myId);
                if (this.socket != null)
                {
                    this.socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
            }
            catch (Exception arg)
            {
                Debug.Log(string.Format("Error sending data to server via UDP: {0}", arg));
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                byte[] array = this.socket.EndReceive(result, ref this.endPoint);
                this.socket.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
                if (array.Length < 4)
                {
                    LocalClient.instance.Disconnect();
                    Debug.Log("UDP failed due to packets being split, in Client class");
                }
                else
                {
                    this.HandleData(array);
                }
            }
            catch
            {
                this.Disconnect();
            }
        }

        private void HandleData(byte[] data)
        {
            LocalClient.packetsReceived++;
            using (Packet packet = new Packet(data))
            {
                int num = packet.ReadInt(true);
                LocalClient.byteDown += num;
                data = packet.ReadBytes(num, true);
            }
            ThreadManagerClient.ExecuteOnMainThread(delegate
            {
                using (Packet packet2 = new Packet(data))
                {
                    int key = packet2.ReadInt(true);
                    LocalClient.packetHandlers[key](packet2);
                }
            });
        }

        private void Disconnect()
        {
            LocalClient.instance.Disconnect();
            this.endPoint = null;
            this.socket = null;
        }

        public UdpClient socket;

        public IPEndPoint endPoint;
    }
}
