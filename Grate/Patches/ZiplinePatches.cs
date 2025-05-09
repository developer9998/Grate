﻿using System;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using Grate.Modules.Movement;
using Grate.Tools;
using HarmonyLib;
using UnityEngine;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaZipline))]
    [HarmonyPatch("Update", MethodType.Normal)]
    public class ZiplineUpdatePatch
    {
        private static void Postfix(GorillaZipline __instance, BezierSpline ___spline, float ___currentT,
            GorillaHandClimber ___currentClimber)
        {
            if (!Plugin.WaWa_graze_dot_cc) return;
            try
            {
                var rockets = Rockets.Instance;
                if (!rockets || !rockets.enabled || !___currentClimber) return;
                Vector3 curDir = __instance.GetCurrentDirection();
                Vector3 rocketDir = rockets.AddedVelocity();
                var currentSpeed = Traverse.Create(__instance).Property("currentSpeed");
                float speedDelta = Vector3.Dot(curDir, rocketDir) * Time.deltaTime * rocketDir.magnitude * 1000f;
                currentSpeed.SetValue(currentSpeed.GetValue<float>() + speedDelta);
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
