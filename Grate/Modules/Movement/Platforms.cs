using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Physics;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Movement
{
    public class Platform : MonoBehaviour
    {
        public bool isSticky, isActive, isLeft;
        Transform hand;
        private Material cloudMaterial;
        float spawnTime;
        Collider collider;
        Vector3 scale;
        string modelName;
        GameObject model;
        public GorillaClimbable Climbable;
        Transform wings;
        ParticleSystem rain;

        public void Initialize(bool isLeft)
        {
            try
            {
                this.isLeft = isLeft;
                this.name = "Grate Platform " + (isLeft ? "Left" : "Right");
                this.Scale = 1;
                foreach (Transform child in this.transform)
                {
                    child.gameObject.AddComponent<GorillaSurfaceOverride>().overrideIndex = 110;
                    var cloud = this.transform.Find("cloud");
                    cloudMaterial = cloud.GetComponent<Renderer>().material;
                    cloudMaterial.color = new Color(1, 1, 1, 0);
                    rain = cloud.GetComponent<ParticleSystem>();
                    wings = this.transform.Find("doug/wings");
                }
                var handObj = isLeft ? GTPlayer.Instance.leftControllerTransform : GTPlayer.Instance.rightControllerTransform;
                this.hand = handObj.transform;
                Climbable = CreateClimbable();
                Climbable.transform.SetParent(transform);
                Climbable.transform.localPosition = Vector3.zero;
                rain.loop = true;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        public void Activate()
        {
            isActive = true;
            this.spawnTime = Time.time;
            this.transform.position = hand.transform.position;
            this.transform.rotation = hand.transform.rotation;
            this.transform.localScale = scale * GTPlayer.Instance.scale;
            collider.gameObject.layer = NoClip.active ? NoClip.layer : 0;
            collider.gameObject.layer = NoClip.active ? NoClip.layer : 0;
            collider.enabled = !isSticky;
            Climbable.gameObject.SetActive(isSticky);
            this.model.SetActive(true);
            if (modelName == "storm cloud")
            {
                rain.Play();
            }
        }

        public GorillaClimbable CreateClimbable()
        {
            var climbable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            climbable.name = "Grate Climb Obj";
            climbable.AddComponent<GorillaClimbable>();
            climbable.layer = LayerMask.NameToLayer("GorillaInteractable");
            climbable.GetComponent<Renderer>().enabled = false;
            climbable.transform.localScale = Vector3.one * .15f;
            return climbable.GetComponent<GorillaClimbable>();
        }

        public void Deactivate()
        {
            isActive = false;
            collider.enabled = false;
            Climbable.gameObject.SetActive(false);
            this.model.SetActive(false);
        }

        void FixedUpdate()
        {
            if (isActive)
            {
                spawnTime = Time.time;

                float transparency = Mathf.Clamp((Time.time - spawnTime) / 1f, 0.2f, 1);
                float c = modelName == "storm cloud" ? .2f : 1;
                cloudMaterial.color = new Color(c, c, c, Mathf.Lerp(1, 0, transparency));
                if (model.name == "doug")
                {
                    wings.transform.localRotation = Quaternion.Euler(Time.frameCount % 2 == 0 ? -30 : 0, 0, 0);
                }
            }
        }


        public bool Sticky
        {
            set
            {
                this.isSticky = value;
            }
        }

        public float Scale
        {
            set
            {
                this.scale = new Vector3(isLeft ? -1 : 1, 1, 1) * value;
            }
        }


        public string Model
        {
            get
            {
                return this.modelName;
            }
            set
            {
                this.modelName = value;
                var path = this.modelName;
                if (modelName.Contains("cloud"))
                {
                    path = "cloud";
                }
                this.model = this.transform.Find(path).gameObject;
                this.transform.Find("cloud").gameObject.SetActive(path == "cloud");
                this.transform.Find("doug").gameObject.SetActive(path == "doug");
                this.transform.Find("invisible").gameObject.SetActive(path == "invisible");
                this.transform.Find("ice").gameObject.SetActive(path == "ice");
                collider = model.GetComponent<BoxCollider>();
            }
        }
    }

    public class Platforms : GrateModule
    {
        public static readonly string DisplayName = "Platforms";
        public static GameObject platformPrefab;
        public Platform left, right, main;
        public static GorillaHandClimber LeftC, RightC;
        InputTracker inputL, inputR;

        void Awake()
        {
            if (!platformPrefab)
            {
                platformPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Bark Platform");
            }
            foreach (GorillaHandClimber ghc in Resources.FindObjectsOfTypeAll<GorillaHandClimber>())
            {
                if (ghc.xrNode == XRNode.LeftHand)
                {
                    LeftC = ghc;
                }
                if (ghc.xrNode == XRNode.RightHand)
                {
                    RightC = ghc;
                }
            }
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                left = CreatePlatform(true);
                right = CreatePlatform(false);
                ReloadConfiguration();
                Plugin.menuController.GetComponent<Frozone>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        public Platform CreatePlatform(bool isLeft)
        {
            var platformObj = Instantiate(platformPrefab);
            var platform = platformObj.AddComponent<Platform>();
            platform.Initialize(isLeft);
            return platform;
        }

        public void OnActivate(InputTracker tracker)
        {
            if (enabled)
            {
                bool isLeft = (tracker.node == XRNode.LeftHand);

                main = isLeft ? left : right;

                var other = !isLeft ? left : right;

                main.Activate();

                if (Sticky.Value)
                {
                    GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                    other.Deactivate();
                }
            }
        }

        public void OnDeactivate(InputTracker tracker)
        {
            bool isLeft = (tracker.node == XRNode.LeftHand);

            var platform = isLeft ? left : right;

            platform.Deactivate();
        }

        protected override void Cleanup()
        {
            if (left != null)
            {
                GTPlayer.Instance.EndClimbing(LeftC, false);
                left.gameObject?.Obliterate();
            }
            if (right != null)
            {
                GTPlayer.Instance.EndClimbing(RightC, false);
                right.gameObject?.Obliterate();
            }
            Unsub();
            Plugin.menuController.GetComponent<Frozone>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        }
        private static Vector3 lastPositionL = Vector3.zero;
        private static Vector3 lastPositionR = Vector3.zero;
        private static Vector3 lastPositionHead = Vector3.zero;
        private static bool lHappen = false;
        private static bool rHappen = false;
        private static bool isVelocity = false;
        protected override void ReloadConfiguration()
        {
            left.Model = Model.Value;
            right.Model = Model.Value;
            left.Sticky = Sticky.Value;
            right.Sticky = Sticky.Value;

            float scale = MathExtensions.Map(Scale.Value, 0, 10, .5f, 1.5f);
            left.Scale = scale;
            right.Scale = scale;

            Unsub();
            if (Input.Value != "velocity")
            {
                inputL = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.LeftHand);
                inputL.OnPressed += OnActivate;
                inputL.OnReleased += OnDeactivate;

                inputR = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.RightHand);
                inputR.OnPressed += OnActivate;
                inputR.OnReleased += OnDeactivate;
                isVelocity = false;
            }
            else
            {
                isVelocity = true;
            }
        }

        void FixedUpdate()
        {
            if (isVelocity)
            {
                float threshold = 0.05f;

                Vector3 headMovementDelta = GorillaTagger.Instance.headCollider.transform.position - lastPositionHead;
                Vector3 leftHandMovementDelta = GorillaTagger.Instance.leftHandTransform.position - lastPositionL;
                Vector3 rightHandMovementDelta = GorillaTagger.Instance.rightHandTransform.position - lastPositionR;

                bool leftHandMovingWithHead = Vector3.Dot(headMovementDelta.normalized, leftHandMovementDelta.normalized) > 0.4f;
                bool rightHandMovingWithHead = Vector3.Dot(headMovementDelta.normalized, rightHandMovementDelta.normalized) > 0.4f;

                if (!leftHandMovingWithHead)
                {
                    if (GorillaTagger.Instance.leftHandTransform.position.y + threshold <= lastPositionL.y)
                    {
                        if (!lHappen)
                        {
                            lHappen = true;

                            OnActivate(inputL);
                        }
                    }
                    else
                    {
                        if (lHappen)
                        {
                            lHappen = false;
                            OnDeactivate(inputL);
                        }
                    }
                }

                if (!rightHandMovingWithHead)
                {
                    if (GorillaTagger.Instance.rightHandTransform.position.y + threshold <= lastPositionR.y)
                    {
                        if (!rHappen)
                        {
                            rHappen = true;

                            OnActivate(inputR);
                        }
                    }
                    else
                    {
                        if (rHappen)
                        {
                            rHappen = false;
                            OnDeactivate(inputR);
                        }
                    }
                }

                lastPositionL = GorillaTagger.Instance.leftHandTransform.position;
                lastPositionR = GorillaTagger.Instance.rightHandTransform.position;
                lastPositionHead = GorillaTagger.Instance.headCollider.transform.position;
            }
        }

        void Unsub()
        {
            if (inputL != null)
            {
                inputL.OnPressed -= OnActivate;
                inputL.OnReleased -= OnDeactivate;
            }
            if (inputR != null)
            {
                inputR.OnPressed -= OnActivate;
                inputR.OnReleased -= OnDeactivate;
            }
        }

        public static ConfigEntry<bool> Sticky;
        public static ConfigEntry<int> Scale;
        public static ConfigEntry<string> Input;
        public static ConfigEntry<string> Model;
        public static void BindConfigEntries()
        {
            try
            {
                Sticky = Plugin.configFile.Bind(
                    section: DisplayName,
                    key: "sticky",
                    defaultValue: false,
                    description: "Whether or not your hands stick to the platforms"
                );

                Scale = Plugin.configFile.Bind(
                    section: DisplayName,
                    key: "size",
                    defaultValue: 5,
                    description: "The size of the platforms"
                );

                Input = Plugin.configFile.Bind(
                    section: DisplayName,
                    key: "input",
                    defaultValue: "grip",
                    configDescription: new ConfigDescription(
                        "Which button you press to activate the platform",
                        new AcceptableValueList<string>("grip", "trigger", "stick", "a/x", "b/y", "velocity")
                    )
                );

                Model = Plugin.configFile.Bind(
                    section: DisplayName,
                    key: "model",
                    defaultValue: "cloud",
                    configDescription: new ConfigDescription(
                        "Which button you press to activate the platform",
                        new AcceptableValueList<string>("cloud", "storm cloud", "doug", "ice", "invisible")
                    )
                );

            }
            catch (Exception e) { Logging.Exception(e); }
        }

        public override string GetDisplayName()
        {
            return "Platforms";
        }

        public override string Tutorial()
        {

            return $"Press [{Input.Value}] to spawn a platform you can stand on. " +
                $"Release [{Input.Value}] to disable it.";
        }
    }

    public class NetworkedPlatformsHandler : MonoBehaviour
    {
        public GameObject? platformLeft, platformRight;
        public NetworkedPlayer? networkedPlayer;

        void Start()
        {
            try
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                Logging.Debug("Networked player", networkedPlayer.owner.NickName, "turned on platforms");
                platformLeft = Instantiate(Platforms.platformPrefab);
                platformRight = Instantiate(Platforms.platformPrefab);
                SetupPlatform(platformLeft);
                SetupPlatform(platformRight);
                platformLeft.name = networkedPlayer.owner.NickName + "'s Left Platform";
                platformRight.name = networkedPlayer.owner.NickName + "'s Right Platform";
                networkedPlayer.OnGripPressed += OnGripPressed;
                networkedPlayer.OnGripReleased += OnGripReleased;
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void SetupPlatform(GameObject platform)
        {
            try
            {
                platform.SetActive(false);
                var rs = platform.AddComponent<RoomSpecific>();
                rs.Owner = networkedPlayer?.owner;
                foreach (Transform child in platform.transform)
                {
                    if (!child.name.Contains("cloud"))
                    {
                        child.gameObject.Obliterate();
                    }
                    else
                    {
                        child.GetComponent<Collider>()?.Obliterate();
                        child.GetComponent<ParticleSystem>()?.Obliterate();
                    }
                }
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (isLeft)
            {
                var leftHand = networkedPlayer.rig.leftHandTransform;
                platformLeft.SetActive(true);
                platformLeft.transform.position = leftHand.TransformPoint(new Vector3(-12, 18, -10) / 200f);
                platformLeft.transform.rotation = leftHand.transform.rotation * Quaternion.Euler(215, 0, -15);
                platformLeft.transform.localScale = Vector3.one * networkedPlayer.rig.scaleFactor;
            }
            else
            {
                var rightHand = networkedPlayer.rig.rightHandTransform;
                platformRight.SetActive(true);
                platformRight.transform.localPosition = rightHand.TransformPoint(new Vector3(12, 18, 10) / 200f);
                platformRight.transform.localRotation = rightHand.transform.rotation * Quaternion.Euler(-45, -25, -190);
                platformLeft.transform.localScale = Vector3.one * networkedPlayer.rig.scaleFactor;
            }
        }

        void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (isLeft)
                platformLeft.SetActive(false);
            else
                platformRight.SetActive(false);
        }

        void OnDestroy()
        {
            platformLeft?.Obliterate();
            platformRight?.Obliterate();
            if (networkedPlayer != null)
            {
                networkedPlayer.OnGripPressed -= OnGripPressed;
                networkedPlayer.OnGripReleased -= OnGripReleased;
            }

        }
    }
}
