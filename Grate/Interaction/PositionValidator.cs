﻿using System;
using GorillaLocomotion;
using Grate.Modules.Multiplayer;
using Grate.Modules.Physics;
using Grate.Tools;
using UnityEngine;
namespace Grate.Gestures
{
    //TODO - Add a timeout on meat beat actions so you can't slowly accumulate them and accidentally trigger the menu
    public class PositionValidator : MonoBehaviour
    {
        public static PositionValidator Instance;
        public bool isValid, isValidAndStable, hasValidPosition;
        public Vector3 lastValidPosition;
        private float stabilityPeriod = 1f;
        private float stabilityPeriodStart;
        void Awake() { Instance = this; }

        void FixedUpdate()
        {
            try
            {

                Collider[] collisions = Physics.OverlapSphere(
                    GTPlayer.Instance.lastHeadPosition,
                    .15f * GTPlayer.Instance.scale,
                    GTPlayer.Instance.locomotionEnabledLayers
                );

                bool wasValid = isValid;
                isValid = collisions.Length == 0;
                if (!wasValid && isValid)
                {
                    stabilityPeriodStart = Time.time;
                }
                else if (isValid && Time.time - stabilityPeriodStart > stabilityPeriod)
                {
                    lastValidPosition = GTPlayer.Instance.bodyCollider.transform.position;
                    hasValidPosition = true;
                    isValidAndStable = true;
                    if (NoClip.Instance?.button)
                        NoClip.Instance.button.RemoveBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
                    if (Piggyback.Instance?.button)
                        Piggyback.Instance.button.RemoveBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
                }
                else if (!isValid)
                {
                    isValidAndStable = false;
                    if (NoClip.Instance?.button)
                        NoClip.Instance.button.AddBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
                    if (Piggyback.Instance?.button)
                        Piggyback.Instance.button.AddBlocker(ButtonController.Blocker.NOCLIP_BOUNDARY);
                }

            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
