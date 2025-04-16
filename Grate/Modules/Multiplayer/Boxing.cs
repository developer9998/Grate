using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Tools;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using NetworkPlayer = NetPlayer;
using Player = GorillaLocomotion.GTPlayer;
using UnityEngine.UIElements;
using System.Collections;
namespace Grate.Modules.Multiplayer
{
    public class BoxingGlove : MonoBehaviour
    {
        public VRRig rig;
        public GorillaVelocityEstimator velocity;
        public static int uuid;

        void Start()
        {
            velocity = this.gameObject.AddComponent<GorillaVelocityEstimator>();
        }
    }

    public class Boxing : GrateModule
    {
        public static readonly string DisplayName = "Boxing";
        public float forceMultiplier = 5000;
        private Collider punchCollider;
        private List<BoxingGlove> gloves = new List<BoxingGlove>();
        private List<VRRig> glovedRigs = new List<VRRig>();

        private float lastPunch;

        protected override void Start()
        {
            try
            {
                ReloadConfiguration();
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "GratePunchDetector";
                capsule.transform.SetParent(Player.Instance.bodyCollider.transform, false);
                capsule.layer = GrateInteractor.InteractionLayer;
                capsule.GetComponent<MeshRenderer>().enabled = false;

                punchCollider = capsule.GetComponent<Collider>();
                punchCollider.isTrigger = true;
                punchCollider.transform.localScale = new Vector3(.5f, .35f, .5f);
                punchCollider.transform.localPosition += new Vector3(0, .3f, 0);

                var observer = capsule.AddComponent<CollisionObserver>();
                observer.OnTriggerEntered += (obj, collider) =>
                {
                    if (collider.GetComponent<BoxingGlove>() is BoxingGlove glove)
                    {
                        DoPunch(glove);
                    }
                };
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }
        public IEnumerator DelGloves()
        {
            /*foreach (var g in Resources.FindObjectsOfTypeAll<BoxingGlove>())
            {
                g.Obliterate();
            }*/
            foreach (var g in gloves)
            {
                g.Obliterate();
            }
            gloves.Clear();
            yield return new WaitForEndOfFrame();
        }

        void FixedUpdate()
        {
            //if (Time.frameCount % 300 == 0) CreateGloves();
        }

        private void DoPunch(BoxingGlove glove)
        {
            if (Time.time - lastPunch < 1) return;
            Vector3 force = glove.velocity.linearVelocity;
            if (force.magnitude < .5f * Player.Instance.scale) return;
            force.Normalize();
            force *= forceMultiplier;
            Player.Instance.bodyCollider.attachedRigidbody.velocity += force;
            lastPunch = Time.time;
            GestureTracker.Instance.HapticPulse(false);
            GestureTracker.Instance.HapticPulse(true);

        }

        protected override void ReloadConfiguration()
        {
            forceMultiplier = (PunchForce.Value) * 5;
        }

        public static ConfigEntry<int> PunchForce;
        public static void BindConfigEntries()
        {
            Logging.Debug("Binding", DisplayName, "to config");
            PunchForce = Plugin.configFile.Bind(
                section: DisplayName,
                key: "punch force",
                defaultValue: 5,
                description: "How much force will be applied to you when you get punched"
            );
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Other players can punch you around.";
        }

        protected override void Cleanup()
        {
        }
    }
}
