﻿using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Movement
{

    public class BubbleMarker : MonoBehaviour
    {
        GameObject bubble;
        void Start()
        {
            bubble = Instantiate(Bubble.bubblePrefab);
            bubble.transform.SetParent(transform, false);
            bubble.transform.localPosition = new Vector3(0, -.1f, 0);
            bubble.gameObject.layer = GrateInteractor.InteractionLayer;
        }

        void OnDestroy()
        {
            Destroy(bubble);
        }
    }
    public class Bubble : GrateModule
    {
        public static readonly string DisplayName = "Bubble";
        public static GameObject bubblePrefab;
        public GameObject bubble;
        public GameObject colliderObject;
        public Vector3 targetPosition;
        public static Vector3 bubbleOffset = new Vector3(0, .15f, 0);

        void Awake()
        {
            if (!bubblePrefab)
            {
                bubblePrefab = Plugin.assetBundle.LoadAsset<GameObject>("BubbleP");
            }
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<BubbleMarker>()?.Obliterate();
        }

        void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod != DisplayName) return;
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<BubbleMarker>();
            else
                Destroy(player.Rig().gameObject.GetComponent<BubbleMarker>());
        }

        Rigidbody rb;
        void FixedUpdate()
        {
            if (!rb)
                rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            rb.AddForce(-UnityEngine.Physics.gravity * rb.mass * GTPlayer.Instance.scale);
            bubble.transform.localScale = Vector3.one * GTPlayer.Instance.scale * .75f;
        }

        bool leftWasTouching, rightWasTouching;
        float lastTouchLeft, lastTouchRight;
        float cooldown = .1f;
        void LateUpdate()
        {
            if (bubble != null)
            {
                bubble.transform.position = GTPlayer.Instance.headCollider.transform.position;
                bubble.transform.position -= bubbleOffset * GTPlayer.Instance.scale;

                Vector3 leftPos = GestureTracker.Instance.leftHand.transform.position,
                    rightPos = GestureTracker.Instance.rightHand.transform.position;

                if (Touching(leftPos))
                {
                    if (!leftWasTouching && Time.time - lastTouchLeft > cooldown)
                    {
                        OnTouch(leftPos, true);
                        lastTouchLeft = Time.time;
                    }
                    leftWasTouching = true;
                }
                else
                {
                    leftWasTouching = false;
                }

                if (Touching(rightPos))
                {
                    if (!rightWasTouching && Time.time - lastTouchRight > cooldown)
                    {
                        OnTouch(rightPos, false);
                        lastTouchRight = Time.time;
                    }
                    rightWasTouching = true;
                }
                else
                {
                    rightWasTouching = false;
                }
            }
        }

        float margin = .1f;
        float colliderScale = 1;
        bool Touching(Vector3 position)
        {
            float radius = GTPlayer.Instance.scale * colliderScale;
            float d = Vector3.Distance(position, bubble.transform.position);
            float m = margin * GTPlayer.Instance.scale;
            return d > radius - m && d < radius + m;
        }

        void OnTouch(Vector3 position, bool left)
        {
            Sounds.Play(110);
            position -= bubble.transform.position;
            GestureTracker.Instance.HapticPulse(left);
            GTPlayer.Instance.AddForce(position.normalized * GTPlayer.Instance.scale * BubbleSpeed.Value / 5);
        }


        float baseDrag = 0;
        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                ReloadConfiguration();
                bubble = Instantiate(bubblePrefab);
                bubble.AddComponent<GorillaSurfaceOverride>().overrideIndex = 110;
                bubble.GetComponent<Collider>().enabled = false;
                rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
                baseDrag = rb.drag;
                rb.drag = 1;
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        protected override void Cleanup()
        {
            if (bubble)
                Sounds.Play(84, 2);
            bubble?.Obliterate();
            if (rb)
                rb.drag = baseDrag;
        }
        protected override void ReloadConfiguration()
        {
            colliderScale = MathExtensions.Map(BubbleSize.Value, 0, 10, .45f, .65f);
        }

        public static ConfigEntry<int> BubbleSize;
        public static ConfigEntry<int> BubbleSpeed;
        public static void BindConfigEntries()
        {
            BubbleSize = Plugin.configFile.Bind(
                section: DisplayName,
                key: "bubble size",
                defaultValue: 5,
                description: "How far you have to reach to hit the bubble"
            );
            BubbleSpeed = Plugin.configFile.Bind(
                section: DisplayName,
                key: "bubble speed",
                defaultValue: 5,
                description: "How fast the bubble moves when you push it"
            );
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Creates a bubble around you so you can float. " +
                "Tap the side that you want to move towards to move.";
        }
    }
}
