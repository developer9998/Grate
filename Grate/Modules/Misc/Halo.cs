﻿using System;
using Grate.Extensions;
using Grate.GUI;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc
{

    public class HaloMarker : MonoBehaviour
    {
        GameObject halo, lightBeam;
        void Start()
        {
            try
            {
                halo = Instantiate(Halo.haloPrefab);
                lightBeam = Instantiate(Halo.lightBeamPrefab);
                var rig = this.GetComponent<VRRig>();
                halo.transform.SetParent(rig.headMesh.transform, false);
                halo.transform.localPosition = new Vector3(0, .15f, 0);
                halo.transform.localRotation = Quaternion.Euler(69, 0, 0);
                lightBeam.transform.SetParent(rig.transform, false);
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

        }

        Quaternion rotation = Quaternion.Euler(180, 0, 0);
        void FixedUpdate()
        {
            lightBeam.transform.rotation = rotation;
        }

        void OnDestroy()
        {
            Destroy(halo);
            Destroy(lightBeam);
        }
    }


    public class Halo : GrateModule
    {

        public static readonly string DisplayName = "Halo";
        public static GameObject haloPrefab, lightBeamPrefab;
        HaloMarker myMarker;

        void Awake()
        {
            if (!haloPrefab)
            {
                haloPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Halo");
                lightBeamPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Light Beam");
            }
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();

            try
            {
                myMarker = GorillaTagger.Instance.offlineVRRig.gameObject.AddComponent<HaloMarker>();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<HaloMarker>()?.Obliterate();
        }

        void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod == DisplayName && player.UserId == "JD3moEFc6tOGYSAp4MjKsIwVycfrAUR5nLkkDNSvyvE=".DecryptString())
            {
                if (enabled)
                    player.Rig().gameObject.GetOrAddComponent<HaloMarker>();
                else
                    Destroy(player.Rig().gameObject.GetComponent<HaloMarker>());
            }
        }

        protected override void Cleanup()
        {
            //foreach (var marker in FindObjectsOfType<HaloMarker>())
            Destroy(myMarker);
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Proof of Kyle";
        }
    }
}
