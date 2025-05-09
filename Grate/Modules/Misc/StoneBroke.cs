﻿using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Misc
{
    class StoneBroke : GrateModule
    {
        Awsomepnix LocalP;
        public static GameObject wawa;

        public static InputTracker inputL, inputR;
        public override string GetDisplayName()
        {
            return "StoneBroke :3";
        }

        public override string Tutorial()
        {
            return "MuskEnjoyer";
        }
        void Awake()
        {
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        protected override void Start()
        {
            base.Start();
            wawa = Plugin.assetBundle.LoadAsset<GameObject>("bs");
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            LocalP = GorillaTagger.Instance.offlineVRRig.AddComponent<Awsomepnix>();
        }
        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            if (rig?.gameObject?.GetComponent<Awsomepnix>() != null)
            {
                rig?.gameObject?.GetComponent<Awsomepnix>()?.ps.Obliterate();
                rig?.gameObject?.GetComponent<Awsomepnix>()?.Obliterate();
            }
        }


        private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
        {
            if (mod == GetDisplayName() && player.UserId == "CA8FDFF42B7A1836")
            {
                if (enabled)
                {
                    player.Rig().gameObject.GetOrAddComponent<Awsomepnix>();
                }
                else
                {
                    player.Rig().gameObject.GetComponent<Awsomepnix>().ps.gameObject.Obliterate();
                    player.Rig().gameObject.GetComponent<Awsomepnix>().Obliterate();
                }
            }
        }

        protected override void Cleanup()
        {
            LocalP?.ps.Obliterate();
            LocalP?.Obliterate();

        }

        class Awsomepnix : MonoBehaviour
        {
            public GameObject ps;
            NetworkedPlayer wa;

            void Start()
            {
                ps = Instantiate(wawa, gameObject.transform);
                wa = gameObject.GetComponent<NetworkedPlayer>();

                wa.OnGripPressed += Boom;
                if (PhotonNetwork.LocalPlayer.UserId == "CA8FDFF42B7A1836")
                {
                    inputL = GestureTracker.Instance.GetInputTracker("grip", XRNode.LeftHand);
                    inputL.OnPressed += LocalBoom;

                    inputR = GestureTracker.Instance.GetInputTracker("grip", XRNode.RightHand);
                    inputR.OnPressed += LocalBoom;
                }
            }

            private void LocalBoom(InputTracker tracker)
            {
                ps.GetComponentInChildren<AudioSource>().Play();
            }

            void OnDestroy()
            {
                wa.OnGripPressed -= Boom;
                if (PhotonNetwork.LocalPlayer.UserId == "CA8FDFF42B7A1836")
                {
                    inputL.OnPressed -= LocalBoom;
                    inputR.OnPressed -= LocalBoom;
                }
            }

            private void Boom(NetworkedPlayer player, bool arg2)
            {
                ps.GetComponentInChildren<AudioSource>().Play();
            }
        }
    }
}
