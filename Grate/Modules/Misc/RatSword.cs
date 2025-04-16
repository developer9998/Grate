using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Movement;
using GorillaLocomotion;
using Grate.Networking;
using Grate.Tools;
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using NetworkPlayer = NetPlayer;
using Grate.Modules.Multiplayer;

namespace Grate.Modules.Misc
{
    public class RatSword : GrateModule
    {
        public static readonly string DisplayName = "Rat Sword";
        static GameObject? Sword;

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            if (Sword == null)
            {
                Sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Rat Sword"));
                Sword.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
                Sword.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
                Sword.transform.localRotation = Quaternion.Euler(9, 0, 0);
                Sword.transform.localScale /= 2;
                Sword.layer = GrateInteractor.InteractionLayer;
                Sword.AddComponent<BoxingGlove>();
                Sword.AddComponent<MeshCollider>().isTrigger = true;
                Sword.SetActive(false);
            }
            try
            {                
                NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
                Patches.VRRigCachePatches.OnRigCached += OnRigCached;
                GestureTracker.Instance.rightGrip.OnPressed += ToggleRatSwordOn;
                GestureTracker.Instance.rightGrip.OnReleased += ToggleRatSwordOff;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
        void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer)
            {
                if (enabled)
                {
                    player.Rig().gameObject.GetOrAddComponent<NetSword>();
                }
                else
                {
                    Destroy(player.Rig().gameObject.GetComponent<NetSword>());
                }
            }
        }


        void ToggleRatSwordOn(InputTracker tracker)
        {
            Sword?.SetActive(true);
        }

        void ToggleRatSwordOff(InputTracker tracker)
        {
            Sword?.SetActive(false);
        }

        protected override void Cleanup()
        {
            Sword?.Obliterate();
            if (GestureTracker.Instance != null)
            {
                GestureTracker.Instance.rightGrip.OnPressed -= ToggleRatSwordOn;
                GestureTracker.Instance.rightGrip.OnReleased -= ToggleRatSwordOff;
            }
            if (NetworkPropertyHandler.Instance != null)
            {
                NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;
            }
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<NetSword>()?.Obliterate();
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "A ghostly rat sword!";
        }

        class NetSword : MonoBehaviour
        {
            NetworkedPlayer networkedPlayer;
            GameObject sword;

            void OnEnable()
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                var rightHand = networkedPlayer.rig.rightHandTransform;

                sword = Instantiate(Sword);

                sword.transform.SetParent(rightHand);
                sword.transform.localPosition =  new Vector3(0.04f, 0.05f, - 0.02f);
                sword.transform.localRotation = Quaternion.Euler(78.4409f,0,0);
                sword.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                networkedPlayer.OnGripPressed += OnGripPressed;
                networkedPlayer.OnGripReleased += OnGripReleased;
            }

            void OnGripPressed(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                {
                    sword.SetActive(true);
                }
            }
            void OnGripReleased(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                {
                    sword.SetActive(false);
                }
            }
        }
    }
}
