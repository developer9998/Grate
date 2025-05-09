using System;
using System.Collections.Generic;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Multiplayer
{
    public class Grab : GrateModule
    {
        public static readonly string DisplayName = "Grab";
        public static Grab Instance;
        private List<GBMarker> markers = new List<GBMarker>();
        public SphereCollider gbCollider;
        GBMarker grabber;
        void Awake() { Instance = this; }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                ReloadConfiguration();
                var prefab = Plugin.assetBundle.LoadAsset<GameObject>("TK Hitbox");
                var hitbox = Instantiate(prefab);
                hitbox.name = "Grate GB Hitbox";
                hitbox.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
                hitbox.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                hitbox.layer = GrateInteractor.InteractionLayer;
                gbCollider = hitbox.GetComponent<SphereCollider>();
                gbCollider.isTrigger = true;
                DistributeGrabbyThings();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }
        Joint joint;
        void FixedUpdate()
        {
            if (Time.frameCount % 300 == 0)
                DistributeGrabbyThings();

            if (!grabber) TryGetGrabber();

            if (grabber)
            {
                var rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
                if (!grabber.IsGripping())
                {
                    grabber = null;
                    rb.velocity = GTPlayer.Instance.bodyVelocityTracker.GetAverageVelocity(true, 0.15f, false) * 4.6f;
                    return;
                }

                Vector3 direction = grabber.controllingHand.position - GTPlayer.Instance.bodyCollider.transform.position;
                rb.AddForce(direction * 30, ForceMode.Impulse);
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, .1f);
            }

        }

        void TryGetGrabber()
        {
            foreach (var tk in markers)
            {
                try
                {
                    if (tk && tk.IsGripping() && tk.GrabbingMe())
                    {
                        grabber = tk;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        void DistributeGrabbyThings()
        {

            foreach (var rig in GorillaParent.instance.vrrigs)
            {
                try
                {
                    if (rig.OwningNetPlayer.IsLocal ||
                        rig.gameObject.GetComponent<GBMarker>()) continue;

                    markers.Add(rig.gameObject.AddComponent<GBMarker>());
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        protected override void Cleanup()
        {
            foreach (GBMarker m in markers)
            {
                m?.Obliterate();
            }
            gbCollider?.gameObject?.Obliterate();
            joint?.Obliterate();
            grabber = null;
            markers.Clear();
            gbCollider = null;
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Allows players to grab you!";
        }

        public class GBMarker : MonoBehaviour
        {
            public VRRig rig;
            bool grippingRight, grippingLeft;
            public Transform leftHand, rightHand, controllingHand;
            public Rigidbody controllingBody;
            DebugRay dr;

            public static int count;
            int uuid;
            void Awake()
            {
                this.rig = GetComponent<VRRig>();
                this.uuid = count++;
                leftHand = SetupHand("L");
                rightHand = SetupHand("R");
                dr = new GameObject($"{uuid} (Debug Ray)").AddComponent<DebugRay>();
            }

            public Transform SetupHand(string hand)
            {
                var handTransform = transform.Find(
                    string.Format(GestureTracker.palmPath, hand).Substring(1)
                );
                var rb = handTransform.gameObject.GetOrAddComponent<Rigidbody>();

                rb.isKinematic = true;
                return handTransform;
            }

            public bool IsGripping()
            {
                grippingRight =
                    rig.rightMiddle.calcT > .5f;
                //rig.rightThumb.calcT > .5f;

                grippingLeft =
                    rig.leftMiddle.calcT > .5f;
                //rig.leftThumb.calcT > .5f;
                return grippingRight || grippingLeft;
            }

            public bool GrabbingMe()
            {
                try
                {
                    if (!(grippingRight || grippingLeft)) return false;
                    Transform hand = grippingRight ? rightHand : leftHand;
                    controllingHand = hand;
                    if (!hand) return false;
                    controllingBody = hand?.GetComponent<Rigidbody>();
                    if (!controllingBody) return false;

                    var collider = Instance.gbCollider;
                    float checkRadius = 0.05f * GTPlayer.Instance.scale;
                    Collider[] hits = UnityEngine.Physics.OverlapSphere(hand.position, checkRadius);

                    foreach (var hit in hits)
                    {
                        if (hit == collider)
                            return true;
                    }
                }
                catch (Exception e) { Logging.Exception(e); }
                return false;
            }

            void OnDestroy()
            {
                dr?.gameObject?.Obliterate();
                leftHand?.GetComponent<Rigidbody>()?.Obliterate();
                rightHand?.GetComponent<Rigidbody>()?.Obliterate();
            }
        }
    }
}
