﻿using System;
using System.Collections;
using GorillaLocomotion;
using Grate.GUI;
using Grate.Modules.Movement;
using Grate.Modules.Multiplayer;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Physics
{
    public class NoClip : GrateModule
    {
        public static readonly string DisplayName = "No Clip";
        public static NoClip Instance;
        public LayerMask baseMask;
        public bool baseHeadIsTrigger, baseBodyIsTrigger;
        public static int layer = 29, layerMask = 1 << layer;
        public static bool active;
        bool FirstTimeworkaround;
        Vector3 enablePos;
        bool flyWasEnabled;

        private struct GorillaTriggerInfo
        {
            public Collider collider;
            public bool wasEnabled;
        }

        void Awake()
        {
            baseMask = GTPlayer.Instance.locomotionEnabledLayers;
            Instance = this;
        }

        protected override void OnEnable()
        {
            try
            {
                if (!MenuController.Instance.Built) return;
                base.OnEnable();
                enablePos = GTPlayer.Instance.headCollider.transform.position;
                if (!Piggyback.mounted)
                {
                    try
                    {
                        var fly = Plugin.menuController.GetComponent<Fly>();
                        flyWasEnabled = fly.enabled;
                        fly.enabled = true;
                        fly.button.AddBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
                    }
                    catch
                    {
                        Logging.Debug("Failed to enable fly for noclip.");
                    }
                }

                Logging.Debug("Disabling triggers");
                TriggerBoxPatches.triggersEnabled = false;
                GTPlayer.Instance.locomotionEnabledLayers = layerMask;

                baseBodyIsTrigger = GTPlayer.Instance.bodyCollider.isTrigger;
                GTPlayer.Instance.bodyCollider.isTrigger = true;

                baseHeadIsTrigger = GTPlayer.Instance.headCollider.isTrigger;
                GTPlayer.Instance.headCollider.isTrigger = true;
                active = true;
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        protected override void Cleanup()
        {
            if (!FirstTimeworkaround)
            {
                FirstTimeworkaround = true;
                return;
            }
            StartCoroutine(CleanupRoutine());
        }

        IEnumerator CleanupRoutine()
        {
            Plugin.menuController.GetComponent<Fly>().button.RemoveBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
            GTPlayer.Instance.locomotionEnabledLayers = baseMask;
            GTPlayer.Instance.bodyCollider.isTrigger = baseBodyIsTrigger;
            GTPlayer.Instance.headCollider.isTrigger = baseHeadIsTrigger;
            TeleportPatch.TeleportPlayer(enablePos);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            TriggerBoxPatches.triggersEnabled = true;
            Plugin.menuController.GetComponent<Fly>().enabled = flyWasEnabled;
            Logging.Debug("Enabling triggers");
            active = false;
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Disables collisions. Automatically enables Fly (Use the sticks to move).";
        }

    }
}
