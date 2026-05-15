using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using tarkin.doordash.Patches;
using Fika.Core.Networking;
using Fika.Core.Main.Utils;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Main.Components;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using EFT;
using EFT.Interactive;
using Comfort.Common;
using UnityEngine;

namespace tarkin.doordash
{

    public enum GameObjectType
    {
        Door = 1,
    }
    [BepInPlugin("com.tarkin.doordash", "DoorDash-Fika", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private readonly NetPacketProcessor packetProcessor = new();
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> VelocityThresholdSqr;
        internal static ConfigEntry<float> RayDistance;

        internal static ConfigEntry<float> DislodgeChance;
        internal static ConfigEntry<float> DislodgeForce;

        internal static ConfigEntry<float> ArmDamageBase;
        internal static ConfigEntry<float> ContusionStrength;
        internal static ConfigEntry<float> RecoilHands;
        internal static ConfigEntry<float> RecoilCamera;
        internal static ConfigEntry<bool> BurnStamina;
        internal static ConfigEntry<EBodyPart> BodyPartToHurt;

        private void Awake()
        {
            Log = Logger;

            InitConfiguration();

            new Patch_Door_KickOpen().Enable();

            FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStarted);
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);

            packetProcessor.SubscribeNetSerializable<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);
        }

        private void OnGameWorldStarted(GameWorldStartedEvent obj)
        {
            if (Application.isBatchMode)
                return;
            
            obj.GameWorld.MainPlayer.gameObject.GetOrAddComponent<RaycastBreacher>();
        }

        void OnFikaNetworkManagerCreated (FikaNetworkManagerCreatedEvent ev)
        {
            switch (ev.Manager)
            {
                case FikaServer server:
                    server.RegisterPacket<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);                    
                break;
                case FikaClient client:
                    client.RegisterPacket<SyncOpenStatePacket, NetPeer>(OnSyncOpenStatePacketReceived);
                break;
            }
        }

        private void OnSyncOpenStatePacketReceived(SyncOpenStatePacket packet, NetPeer peer)
        {
            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler) ||
                !coopHandler.Players.TryGetValue(packet.netID, out _)) 
                return;

            WorldInteractiveObject worldInteractiveObject = Singleton<GameWorld>.Instance.FindDoor(packet.objectID);

            if (worldInteractiveObject is null || 
                !worldInteractiveObject.isActiveAndEnabled) 
                return;
            
            Door door = (Door)worldInteractiveObject;

            if (door.DoorState != EDoorState.Open)
            {
                door.DoorState = EDoorState.Shut;
                door.KickOpen(true);
                coopHandler.MyPlayer.UpdateInteractionCast();
            }

            if (FikaBackendUtils.IsServer)
                Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }

        private void InitConfiguration()
        {
            Enabled = Config.Bind("", "Enabled", true);

            VelocityThresholdSqr = Config.Bind("Sprint Ram", "Velocity Threshold", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RayDistance = Config.Bind("Sprint Ram", "Ray Distance", 0.7f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            DislodgeChance = Config.Bind("Physical Door", "Chance To Dislodge On Breach", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            DislodgeForce = Config.Bind("Physical Door", "Dislodge Force", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            ArmDamageBase = Config.Bind("Player Effect", "Arm Damage", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ContusionStrength = Config.Bind("Player Effect", "Contusion Strength", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RecoilHands = Config.Bind("Player Effect", "Recoil Hands", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RecoilCamera = Config.Bind("Player Effect", "Recoil Camera", 4f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            BurnStamina = Config.Bind("Player Effect", "Burn Stamina", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            BodyPartToHurt = Config.Bind("Player Effect", "BodyPartToHurt", EBodyPart.LeftArm, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
        }
    }
}
