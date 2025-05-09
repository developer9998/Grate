using HarmonyLib;
using UnityEngine;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaQuitBox))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    class QuitBoxPatch
    {
        private static bool Prefix()
        {
            TeleportPatch.TeleportPlayer(new Vector3(-66.4845f, 11.7564f, -82.5688f), 0);
            foreach (var wawa in GorillaNetworking.PhotonNetworkController.Instance.enableOnStartup)
            {
                wawa.SetActive(true);
            }
            foreach (var wawa2 in GorillaNetworking.PhotonNetworkController.Instance.disableOnStartup)
            {
                wawa2.SetActive(false);
            }
            return !Plugin.WaWa_graze_dot_cc;
        }
    }
}