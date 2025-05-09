﻿using System;
using System.Collections;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Physics;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using Random = UnityEngine.Random;
namespace Grate.Modules.Multiplayer
{
    public class Kamehameha : GrateModule
    {
        public static readonly string DisplayName = "Kamehameha";

        public static Transform orb;
        public static LineRenderer bananaLine;
        private ParticleSystem Effects;
        private Rigidbody orbBody;
        public bool isCharging, isFiring;
        string state;

        public static readonly float maxOrbSize = .4f;

        public static readonly string KamehamehaKey = "KameState";
        public static readonly string KamehamehaColorKey = "KameColor";

        protected override void Start()
        {
            base.Start();
            bananaLine = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Banana Line")).GetComponent<LineRenderer>();
            bananaLine.material = Plugin.assetBundle.LoadAsset<Material>("Laser Sight Material");
            bananaLine.gameObject.SetActive(false);

            orb = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            orb.localScale = new Vector3(maxOrbSize, maxOrbSize, maxOrbSize);
            orb.gameObject.GetComponent<Collider>().isTrigger = true;
            orbBody = orb.gameObject.GetOrAddComponent<Rigidbody>();
            orbBody.isKinematic = true;
            orbBody.useGravity = false;
            orb.gameObject.layer = GrateInteractor.InteractionLayer;
            orb.gameObject.GetComponent<Renderer>().material = bananaLine.material;

            Effects = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Kahme PSystem")).GetComponent<ParticleSystem>();
            Effects.transform.SetParent(orbBody.transform, false);
            Effects.transform.localPosition = Vector3.zero;
            NetworkPropertyHandler.Instance?.ChangeProperty(KamehamehaKey, "None");
            orb.gameObject.SetActive(false);
            ReloadConfiguration();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (MenuController.Instance.Built)
            {
                GestureTracker.Instance.OnKamehameha += OnKamehameha;
                ReloadConfiguration();
            }
        }

        private void OnKamehameha()
        {
            if (enabled && !isCharging && !isFiring)
            {
                StartCoroutine(GrowBananas());
            }

        }

        IEnumerator GrowBananas()
        {
            isCharging = true;
            orb.gameObject.SetActive(true);
            orbBody.isKinematic = true;
            orbBody.velocity = Vector3.zero;
            GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(Random.Range(40, 56), false, 0.1f);
            Transform
                leftHand = GestureTracker.Instance.leftPalmInteractor.transform,
                rightHand = GestureTracker.Instance.rightPalmInteractor.transform;
            float diameter = 0;
            float lastHaptic = Time.time;
            float hapticDuration = .1f;
            while (GestureTracker.Instance.PalmsFacingEachOther())
            {
                float scale = GTPlayer.Instance.scale;
                if (Time.time - lastHaptic > hapticDuration)
                {
                    float strength = Mathf.SmoothStep(0, 1, diameter / maxOrbSize * ((float)Math.Sqrt(scale)));
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(Random.Range(40, 48), false, strength / 10f);
                    GestureTracker.Instance.leftController.SendHapticImpulse(0u, strength, hapticDuration);
                    GestureTracker.Instance.rightController.SendHapticImpulse(0u, strength, hapticDuration);
                    lastHaptic = Time.time;
                }
                diameter = Vector3.Distance(leftHand.position, rightHand.position);
                diameter = Mathf.Clamp(diameter, 0, maxOrbSize * ((float)Math.Sqrt(scale)));
                orb.transform.position = (leftHand.position + rightHand.position) / 2;
                orb.transform.localScale = Vector3.one * diameter * ((float)Math.Sqrt(scale));
                yield return new WaitForEndOfFrame();
            }
            isCharging = false;
            Logging.Debug("Charging is done");
            float chargeTime = Time.time;
            while (Time.time - chargeTime < 1f)
            {
                if (GestureTracker.Instance.PalmsFacingSameWay())
                    break;
                yield return new WaitForEndOfFrame();
            }

            bananaLine.gameObject.SetActive(true);
            isFiring = true;
            while (GestureTracker.Instance.PalmsFacingSameWay() && HandProximity() < .6f)
            {
                if (Time.time - lastHaptic > hapticDuration)
                {
                    float strength = 1;
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(Random.Range(40, 56), false, strength / 10f);
                    GestureTracker.Instance.leftController.SendHapticImpulse(0u, strength, hapticDuration);
                    GestureTracker.Instance.rightController.SendHapticImpulse(0u, strength, hapticDuration);
                    lastHaptic = Time.time;
                }
                float scale = ((float)Math.Sqrt(GTPlayer.Instance.scale));
                diameter = Vector3.Distance(leftHand.position, rightHand.position);
                diameter = Mathf.Clamp(diameter, 0, maxOrbSize * scale * 2);
                bananaLine.startWidth = diameter * scale;
                bananaLine.endWidth = diameter * scale;
                Vector3 direction =
                    (GestureTracker.Instance.leftHandVectors.palmNormal +
                    GestureTracker.Instance.rightHandVectors.palmNormal) / 2;
                Vector3 start = (leftHand.position + rightHand.position) / 2 + direction * .1f;
                orb.position = start;
                orb.transform.localScale = Vector3.one * diameter * scale;
                bananaLine.SetPosition(0, start);
                bananaLine.SetPosition(1, start + direction * 100f);
                GTPlayer.Instance.AddForce(direction * -40 * diameter * Time.fixedDeltaTime);
                yield return new WaitForEndOfFrame();
            }
            Logging.Debug("Firing is done");
            orb.gameObject.SetActive(false);
            bananaLine.gameObject.SetActive(false);
            isFiring = false;
        }

        float HandProximity()
        {
            return Vector3.Distance(
                GestureTracker.Instance.leftPalmInteractor.transform.position,
                GestureTracker.Instance.rightPalmInteractor.transform.position
            );
        }

        void FixedUpdate()
        {
            if (isCharging && !isFiring)
            {
                state = "Charging";
            }
            if (isFiring && !isCharging)
            {
                state = "FIRE!";
            }
            if (!isCharging && !isFiring)
            {
                state = "None";
            }
            if (NetworkSystem.Instance.LocalPlayer.GetProperty<string>(KamehamehaKey) != state)
            {
                NetworkPropertyHandler.Instance.ChangeProperty(KamehamehaKey, state);
            }
        }

        public static ConfigEntry<string> c_khameColor;
        public static ConfigEntry<bool> c_Networked;
        Color khameColor;
        public static ConfigEntry<bool> networked;
        public static void BindConfigEntries()
        {
            AcceptableValueList<string> colorNames = new AcceptableValueList<string>(
                Color.red.ColorName(),
                Color.green.ColorName(),
                Color.blue.ColorName(),
                Color.yellow.ColorName(),
                Color.magenta.ColorName(),
                Color.cyan.ColorName(),
                Color.white.ColorName(),
                Color.black.ColorName(),
                Color.gray.ColorName(),
                Color.clear.ColorName(),
                "#5c3a93");

            ConfigDescription kahdesk = new ConfigDescription(
                "Color for your Ultimate Power!", colorNames
            );
            c_khameColor = Plugin.configFile.Bind(
                section: DisplayName,
                key: "Color",
                defaultValue: Color.yellow.ColorName(),
                kahdesk
            );

            c_Networked = Plugin.configFile.Bind(DisplayName, "Network?", true, "Decide weather you want to see Other peoples power!");
        }

        protected override void ReloadConfiguration()
        {
            khameColor = c_khameColor.Value.StringToColor();
            orb.GetComponent<Renderer>().material.color = khameColor;
            bananaLine.SetColors(khameColor, khameColor);
            Effects.GetComponent<Renderer>().material.color = khameColor;
            if (c_Networked.Value == false)
            {
                foreach (NetworkedKaemeManager manager in Resources.FindObjectsOfTypeAll<NetworkedKaemeManager>())
                {
                    Destroy(manager);
                }
            }
            NetworkPropertyHandler.Instance.ChangeProperty(KamehamehaColorKey, khameColor.ColorName());
        }

        protected override void Cleanup()
        {
            GestureTracker.Instance.OnKamehameha -= OnKamehameha;
            if (orb != null)
            {
                orb?.gameObject.SetActive(false);
                bananaLine?.gameObject.SetActive(false);
            }
            state = "None";
            isCharging = false;
            isFiring = false;
            NetworkPropertyHandler.Instance.ChangeProperty(KamehamehaKey, state);
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Copy the Show!";
        }

    }

    class NetworkedKaemeManager : MonoBehaviour
    {
        private Transform? orb;
        private LineRenderer? bananaLine;
        private ParticleSystem? Effects;
        Color khameColor;
        string state = "";
        private ConfigEntry<bool>? networked;
        public ConfigEntry<bool>? IsNetworked => networked;

        public NetworkedPlayer? networkedPlayer;

        void Start()
        {
            try
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                orb = Instantiate(Kamehameha.orb);
                orb.AddComponent<RoomSpecific>().Owner = networkedPlayer.owner;
                bananaLine = Instantiate(Kamehameha.bananaLine);
                bananaLine.AddComponent<RoomSpecific>().Owner = networkedPlayer.owner;
                bananaLine.gameObject.SetActive(true);
                orb.name = $"{networkedPlayer.owner.NickName}s Orb";
                bananaLine.name = $"{networkedPlayer.owner.NickName}s Line";
                Effects = orb.GetComponentInChildren<ParticleSystem>();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void HandleStuff()
        {
            Transform
            leftHand = networkedPlayer.rig.leftHandTransform,
            rightHand = networkedPlayer.rig.rightHandTransform;
            float diameter = 0;
            float scale = (float)Math.Sqrt(networkedPlayer.owner.GetProperty<float>(Potions.playerSizeKey));
            diameter = Vector3.Distance(leftHand.position, rightHand.position);
            diameter = Mathf.Clamp(diameter, 0, Kamehameha.maxOrbSize * scale * 2);
            bananaLine.startWidth = diameter * scale;
            bananaLine.endWidth = diameter * scale;
            Vector3 direction =
                (leftHand.right +
                rightHand.right * -1) / 2;
            Vector3 start = (leftHand.position + rightHand.position) / 2 + direction * .1f;
            orb.position = start;
            orb.transform.localScale = Vector3.one * diameter * scale;
            bananaLine.SetPosition(0, start);
            bananaLine.SetPosition(1, start - direction * 100f);
        }

        void OnDestroy()
        {
            if (orb != null)
            {
                orb?.gameObject.Obliterate();
                bananaLine?.gameObject.Obliterate();
            }
        }

        void FixedUpdate()
        {
            try
            {
                khameColor = networkedPlayer.owner.GetProperty<string>(Kamehameha.KamehamehaColorKey).StringToColor();
                orb.GetComponent<Renderer>().material.color = khameColor;
                bananaLine.SetColors(khameColor, khameColor);
                Effects.GetComponent<Renderer>().material.color = khameColor;
                state = networkedPlayer.owner.GetProperty<string>(Kamehameha.KamehamehaKey);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError(e.Source);
                Debug.LogError(e.StackTrace);
                Destroy(this);
            }
            switch (state)
            {
                case "None":
                    orb.gameObject.SetActive(false);
                    bananaLine.forceRenderingOff = true;
                    break;
                case "Charging":
                    orb.gameObject.SetActive(true);
                    bananaLine.forceRenderingOff = true;
                    HandleStuff();
                    break;
                case "FIRE!":
                    orb.gameObject.SetActive(true);
                    bananaLine.forceRenderingOff = false;
                    HandleStuff();
                    break;
            }
        }
    }
}
