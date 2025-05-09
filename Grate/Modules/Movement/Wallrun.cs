using System.Reflection;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.GUI;
using Grate.Modules.Physics;
using UnityEngine;

namespace Grate.Modules.Movement
{
    public class Wallrun : GrateModule
    {
        public static readonly string DisplayName = "Wall Run";
        private Vector3 baseGravity;
        private RaycastHit hit;
        void Awake()
        {
            baseGravity = UnityEngine.Physics.gravity;
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
        }

        protected void FixedUpdate()
        {
            GTPlayer player = GTPlayer.Instance;
            if (player.wasLeftHandColliding || player.wasRightHandColliding)
            {
                FieldInfo fieldInfo = typeof(GTPlayer).GetField("lastHitInfoHand", BindingFlags.NonPublic | BindingFlags.Instance);
                hit = (RaycastHit)fieldInfo.GetValue(player);
                UnityEngine.Physics.gravity = hit.normal * -baseGravity.magnitude * GravScale();
            }
            else
            {
                if (Vector3.Distance(player.bodyCollider.transform.position, hit.point) > 2 * GTPlayer.Instance.scale)
                    Cleanup();
            }
        }
        public float GravScale()
        {
            return LowGravity.Instance.active ? LowGravity.Instance.gravityScale : (Power.Value * 0.15f) + 0.25f;
        }

        public static ConfigEntry<int> Power;
        public static void BindConfigEntries()
        {
            Power = Plugin.configFile.Bind(
                section: DisplayName,
                key: "power",
                defaultValue: 5,
                description: "Wall Run Strength \n" +
                "5 means it will have normal gravity power in the direction of the last hit wall"
            );
        }

        protected override void Cleanup()
        {
            UnityEngine.Physics.gravity = baseGravity;
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Allows you to walk on any surface, no matter the angle.";
        }

    }
}


