﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
namespace Grate.Modules.Multiplayer
{
    public class Firefly : MonoBehaviour
    {
        public VRRig rig;
        public GameObject fly;
        public ParticleSystem particles, trail;
        Vector3 startPos;
        public Transform leftWing, rightWing;
        public float startTime;
        public static float duration = 1.5f;
        public ParticleSystemRenderer particleRenderer, trailRenderer;
        Renderer modelRenderer;
        public bool seek = false;
        public Transform hand;
        void Awake()
        {
            try
            {
                rig = this.gameObject.GetComponent<VRRig>();
                fly = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Firefly")).gameObject;
                modelRenderer = fly.transform.Find("Model").GetComponent<Renderer>();
                leftWing = fly.transform.Find("Model/Wing L");
                rightWing = fly.transform.Find("Model/Wing R");
                particles = fly.transform.Find("Particles").GetComponent<ParticleSystem>();
                trail = fly.transform.Find("Trail").GetComponent<ParticleSystem>();
                particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
                particleRenderer.material = Instantiate(particleRenderer.material);
                trailRenderer = trail.GetComponent<ParticleSystemRenderer>();
                trailRenderer.trailMaterial = Instantiate(trailRenderer.trailMaterial);
                particles.Play();
                trail.Play();
                startTime = Time.time;
            }
            catch (Exception e) { Logging.Exception(e); }
        }


        void FixedUpdate()
        {
            try
            {
                if (!NetworkSystem.Instance.PlayerListOthers.Contains(rig.OwningNetPlayer) || !Fireflies.instance.enabled || !NetworkSystem.Instance.InRoom)
                {
                    fly.Obliterate();
                    Fireflies.fireflies.Remove(this);
                    this.Obliterate();
                }
                else
                {
                    int y = Time.frameCount % 2 == 0 ? 30 : 0;
                    leftWing.transform.localRotation = Quaternion.Euler(0, -y, 0);
                    rightWing.transform.localRotation = Quaternion.Euler(0, y, 0);
                    Transform target = rig?.transform;
                    if (target != null)
                    {
                        Color color = rig.playerColor;
                        modelRenderer.materials[1].color = color;
                        //flyRenderer.material.SetColor("_EmissionColor", color);
                        particleRenderer.material.color = color;
                        //particleRenderer.material.SetColor("_EmissionColor", color);
                        trailRenderer.trailMaterial.color = color;
                        //trailRenderer.trailMaterial.SetColor("_EmissionColor", color);

                        Vector3 targetPos = target.position + Vector3.up * .4f * rig.scaleFactor;
                        fly.transform.LookAt(targetPos);

                        if (seek)
                        {
                            float t = (Time.time - startTime) / duration;
                            if (t < 1)
                            {
                                fly.transform.position = Vector3.Slerp(startPos, targetPos, t);
                                fly.transform.localScale = Vector3.Lerp(
                                    Vector3.one * GTPlayer.Instance.scale,
                                    Vector3.one * rig.scaleFactor, t);
                            }
                            else
                            {
                                //make the fly circle around the player
                                float angle = (Time.time * 5) % (Mathf.PI * 2);
                                float x = Mathf.Cos(angle);
                                float z = Mathf.Sin(angle);
                                Vector3 offset = new Vector3(x, 0, z) * .2f * rig.scaleFactor;
                                fly.transform.position = targetPos + offset;
                                fly.transform.localScale = Vector3.one * rig.scaleFactor;

                            }
                            //trail.transform.localScale = Vector3.Lerp(
                            //    Vector3.one * Player.Instance.scale,
                            //    Vector3.one * rig.scaleFactor, .1f);
                        }
                    }
                }
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        public void Reset(VRRig rig, Transform hand)
        {
            particles.Stop();
            trail.Stop();
            particles.Clear();
            trail.Clear();
            this.rig = rig;
            this.hand = hand;
            seek = false;
            fly.transform.localScale = Vector3.one * GTPlayer.Instance.scale;
            fly.transform.position = hand.position;
        }

        public void Launch()
        {
            startTime = Time.time;
            startPos = fly.transform.position;
            seek = true;
            particles.Play();
            trail.Play();
        }

        void OnDestroy()
        {
            fly?.Obliterate();
        }
    }

    public class Fireflies : GrateModule
    {
        public static readonly string DisplayName = "Fireflies";
        public static List<Firefly> fireflies = new List<Firefly>();
        public static Fireflies instance;
        bool charging = false;
        Transform hand;

        protected override void OnEnable()
        {
            base.OnEnable();
            try
            {
                ReloadConfiguration();
                GestureTracker.Instance.leftTrigger.OnPressed += OnTriggerPressed;
                GestureTracker.Instance.rightTrigger.OnPressed += OnTriggerPressed;
                GestureTracker.Instance.leftTrigger.OnReleased += OnTriggerReleased;
                GestureTracker.Instance.rightTrigger.OnReleased += OnTriggerReleased;
                VRRigCachePatches.OnRigCached += OnRigCached;
                instance = this;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void OnTriggerPressed(InputTracker tracker)
        {
            StopAllCoroutines();
            bool isLeft = tracker == GestureTracker.Instance.leftTrigger;
            var interactor = isLeft ? GestureTracker.Instance.leftPalmInteractor : GestureTracker.Instance.rightPalmInteractor;
            hand = interactor.transform;
            StartCoroutine(SpawnFireflies(hand, isLeft));
            charging = true;
        }

        void FixedUpdate()
        {
            if (!charging || !hand)
            {
                fireflies.RemoveAll(fly => fly is null);
                return;
            }
            for (int i = 0; i < fireflies.Count; i++)
            {
                float angle = (i * Mathf.PI * 2 / fireflies.Count) + Time.time;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                Vector3 offset = new Vector3(x, z, 0);
                var fly = fireflies[i].fly;
                fly.transform.position = hand.transform.TransformPoint(offset * 2);
                fly.transform.localScale = Vector3.one * GTPlayer.Instance.scale;
            }
        }

        void OnTriggerReleased(InputTracker tracker)
        {
            if (
                tracker == GestureTracker.Instance.leftTrigger && hand == GestureTracker.Instance.leftPalmInteractor.transform
                ||
                tracker == GestureTracker.Instance.rightTrigger && hand == GestureTracker.Instance.rightPalmInteractor.transform)
            {
                StartCoroutine(ReleaseFireflies());
            }
        }

        IEnumerator ReleaseFireflies()
        {
            charging = false;
            foreach (var firefly in fireflies)
            {
                firefly.hand = null;
            }

            foreach (var firefly in fireflies)
            {
                firefly.Launch();
                Sounds.Play(Sounds.Sound.BeeSqueeze, .1f, hand == GestureTracker.Instance.leftPalmInteractor.transform);
                yield return new WaitForSeconds(.05f);
            }
        }

        IEnumerator SpawnFireflies(Transform hand, bool isLeft)
        {
            var rigs = GorillaParent.instance.vrrigs;
            int count = rigs.Count;
            Sounds.Play(Sounds.Sound.BeeSqueeze, .1f, isLeft);
            for (int i = 0; i < count; i++)
            {
                VRRig rig = rigs[i];
                try
                {
                    if (rig != null && !rig.isOfflineVRRig)
                    {
                        var firefly = rig.gameObject.GetOrAddComponent<Firefly>();
                        firefly.Reset(rig, hand);
                        if (!fireflies.Contains(firefly))
                            fireflies.Add(firefly);
                    }
                }
                catch (Exception e) { Logging.Exception(e); }
                yield return new WaitForFixedUpdate();
            }
        }

        protected override void Cleanup()
        {
            GestureTracker.Instance.leftTrigger.OnPressed -= OnTriggerPressed;
            GestureTracker.Instance.rightTrigger.OnPressed -= OnTriggerPressed;
            GestureTracker.Instance.leftTrigger.OnReleased -= OnTriggerReleased;
            GestureTracker.Instance.rightTrigger.OnReleased -= OnTriggerReleased;
            VRRigCachePatches.OnRigCached -= OnRigCached;
            fireflies.Clear();
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            Firefly target = rig.GetComponent<Firefly>();
            if (target != null)
            {
                fireflies.Remove(target);
                target.Obliterate();
            }
        }

        //public static ConfigEntry<int> PunchForce;
        //public static void BindConfigEntries()
        //{
        //Logging.Debug("Binding", DisplayName, "to config");
        //PunchForce = Plugin.configFile.Bind(
        //    section: DisplayName,
        //    key: "punch force",
        //    defaultValue: 5,
        //    description: "How much force will be applied to you when you get punched"
        //);
        //}

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Hold [Trigger] to summon fireflies that will follow each player upon release";
        }
    }
}