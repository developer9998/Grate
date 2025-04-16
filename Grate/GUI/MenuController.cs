using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Grate.Gestures;
using Grate.Modules;
using Grate.Modules.Movement;
using Grate.Modules.Physics;
using Grate.Modules.Multiplayer;
using Grate.Modules.Teleportation;
using Grate.Tools;
using Grate.Interaction;
using Grate.Extensions;
using Player = GorillaLocomotion.GTPlayer;
using BepInEx.Configuration;
using UnityEngine.XR;
using Grate.Modules.Misc;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Threading;
using System.Collections;
using UnityEngine.Networking;
using PlayFab.ClientModels;

namespace Grate.GUI
{
    public class MenuController : MonoBehaviour
    {
        MenuController? instance;

        MenuController()
        {
            instance = this;
        }

        void Start()
        {
            if (instance != null && instance == this)
            {
                Debug.Log($"MenuController ({instance.gameObject.name}) Made");
            }

        }
    }
}
