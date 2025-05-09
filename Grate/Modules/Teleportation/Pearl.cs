﻿using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Teleportation
{
    public class Pearl : GrateModule
    {
        public static readonly string DisplayName = "Pearl";
        public static Pearl Instance;
        private GameObject pearlPrefab;
        ThrowablePearl pearl;

        void Awake()
        {
            try
            {
                Instance = this;
                pearlPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Pearl");
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        void Setup()
        {
            try
            {
                pearl = SetupPearl(Instantiate(pearlPrefab), false);
                ReloadConfiguration();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        ThrowablePearl SetupPearl(GameObject pearlObj, bool isLeft)
        {
            try
            {
                pearlObj.name = "Grate Pearl";
                var pearl = pearlObj.AddComponent<ThrowablePearl>();
                return pearl;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
            return null;
        }

        protected override void Cleanup()
        {
            try
            {
                pearl?.gameObject?.Obliterate();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            Setup();
        }

        protected override void ReloadConfiguration()
        {
            pearl.throwForceMultiplier = (ThrowForce.Value);
        }

        public static ConfigEntry<int> ThrowForce;
        public static void BindConfigEntries()
        {
            ThrowForce = Plugin.configFile.Bind(
                section: DisplayName,
                key: "throw force",
                defaultValue: 5,
                description: "How much to multiply the throw speed by on release"
            );
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return $"Hold either [Grip] to summon a pearl. Throw it and you will to teleport where it lands.";

        }
    }

    public class ThrowablePearl : GrateGrabbable
    {
        GestureTracker gt;
        Rigidbody rigidbody;
        AudioSource audioSource;
        LayerMask mask;
        bool thrown = false, landed = true;
        Material monkeMat, trailMat;
        VRRig playerRig;
        ParticleSystem trail;
        protected override void Awake()
        {
            try
            {
                base.Awake();
                this.throwOnDetach = true;
                this.throwForceMultiplier = 5;
                this.LocalRotation = new Vector3(0, -90f, 0);
                this.LocalPosition = Vector3.right * .8f;
                this.monkeMat = GetComponentInChildren<SkinnedMeshRenderer>().material;
                this.trailMat = GetComponentInChildren<ParticleSystemRenderer>().material;
                this.trail = GetComponentInChildren<ParticleSystem>();
                gameObject.layer = GrateInteractor.InteractionLayer;
                rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
                rigidbody.useGravity = true;
                gt = GestureTracker.Instance;
                gt.rightGrip.OnPressed += Attach;
                gt.leftGrip.OnPressed += Attach;
                mask = GTPlayer.Instance.locomotionEnabledLayers;
                audioSource = gameObject.GetComponent<AudioSource>();
                playerRig = GorillaTagger.Instance.offlineVRRig;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void Attach(InputTracker tracker)
        {
            try
            {
                bool isLeft = (tracker == gt.leftGrip);
                var parent = isLeft ? gt.leftPalmInteractor : gt.rightPalmInteractor;
                if (!this.CanBeSelected(parent)) return;
                float dir = isLeft ? 1 : -1;
                this.transform.parent = null;
                this.transform.localScale = Vector3.one * GTPlayer.Instance.scale * .1f;
                this.LocalRotation = new Vector3(0, 90f * dir, 0);
                parent.Select(this);

                monkeMat.color = playerRig.playerColor;
                trailMat.color = playerRig.playerColor;
                trail.Stop();
                Sounds.Play(Sounds.Sound.crystalhandtap, .05f);
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        Ray ray = new Ray();
        void FixedUpdate()
        {
            if (!thrown) return;
            ray.origin = this.transform.position;
            ray.direction = this.rigidbody.velocity;
            RaycastHit hit;
            UnityEngine.Physics.Raycast(ray, out hit, ray.direction.magnitude, mask);

            if (hit.collider != null)
            {
                Vector3 position = base.transform.position;
                Vector3 vector = Camera.main.transform.position - position;
                Vector3 wawa = hit.point + hit.normal * GTPlayer.Instance.scale / 2f;
                Vector3 position2 = wawa - vector;
                TeleportPatch.TeleportPlayer(wawa, 0);
                audioSource.Play();
                thrown = false;
                landed = true;
                trail.Stop();
                this.transform.position = Vector3.down * 1000;
            }
        }

        public override void OnDeselect(GrateInteractor interactor)
        {
            base.OnDeselect(interactor);
            this.thrown = true;
            trail.Play();

        }

        public void SetupInteraction()
        {
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!gt) return;
            gt.leftGrip.OnPressed -= Attach;
            gt.rightGrip.OnPressed -= Attach;
        }
    }
}
