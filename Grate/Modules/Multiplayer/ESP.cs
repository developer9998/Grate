﻿using System.Collections.Generic;
using Grate.Extensions;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;

namespace Grate.Modules.Multiplayer
{
    internal class ESP : GrateModule
    {
        Shader esp = Shader.Find("GUI/Text Shader");
        Shader Uber = Shader.Find("GorillaTag/UberShader");
        List<VRRig> Espd = new List<VRRig>();
        public override string GetDisplayName()
        {
            return "ESP";
        }

        public override string Tutorial()
        {
            return "Makes You see Players Through Walls!";
        }

        protected override void OnEnable()
        {
            VRRigCachePatches.OnRigCached += OnRigCached;
            NetworkPropertyHandler.Instance.OnPlayerJoined += OnPlayerJoined;
            foreach (VRRig rig in GorillaParent.instance.vrrigs)
            {
                if (!rig.isOfflineVRRig)
                {
                    rig.skeleton.renderer.enabled = true;
                    rig.skeleton.renderer.material.shader = esp;
                    Espd.Add(rig);
                }
            }
        }

        private void OnPlayerJoined(NetPlayer player)
        {
            if (!player.IsLocal)
            {
                player.Rig().skeleton.renderer.enabled = true;
                player.Rig().skeleton.renderer.material.shader = esp;
                Espd.Add(player.Rig());
            }
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            if (!player.IsLocal)
            {
                rig.skeleton.renderer.enabled = false;
                rig.skeleton.renderer.material.shader = Uber;
                rig.skeleton.renderer.material.color = rig.playerColor;
                if (Espd.Contains(rig))
                {
                    Espd.Remove(rig);
                }
            }
        }

        void FixedUpdate()
        {
            foreach (VRRig rig in Espd)
            {
                rig.skeleton.renderer.material.color = Colours(rig);
                rig.skeleton.renderer.material.shader = esp;
            }
        }

        protected override void Cleanup()
        {
            VRRigCachePatches.OnRigCached -= OnRigCached;
            NetworkPropertyHandler.Instance.OnPlayerJoined -= OnPlayerJoined;
            foreach (VRRig rig in GorillaParent.instance.vrrigs)
            {
                rig.skeleton.renderer.enabled = false;
                rig.skeleton.renderer.material.shader = Uber;
                rig.skeleton.renderer.material.color = rig.playerColor;
                if (Espd.Contains(rig))
                {
                    Espd.Remove(rig);
                }
            }
        }

        Color Colours(VRRig rig)
        {
            switch (rig.setMatIndex)
            {
                default:
                    return rig.playerColor;
                case 1:
                    return Color.red;
                case 2:
                case 11:
                    return new Color(1, 0.3288f, 0, 1);
                case 3:
                case 7:
                    return Color.blue;
                case 12:
                    return Color.green;
            }
        }
    }
}
