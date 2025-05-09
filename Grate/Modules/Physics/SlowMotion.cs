﻿using BepInEx.Configuration;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Physics
{
    public class SlowMotion : GrateModule
    {
        public static readonly string DisplayName = "Slow motion";
        public static SlowMotion Instance;

        void Awake() { Instance = this; }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            Time.timeScale = TimeScale.Value / 10f;
        }

        protected override void Cleanup()
        {
            Time.timeScale = 1;
        }

        protected override void ReloadConfiguration()
        {
            if (enabled)
                Time.timeScale = TimeScale.Value / 10f;
        }

        public static ConfigEntry<int> TimeScale;
        //public static void BindConfigEntries()
        //{
        //    TimeScale = Plugin.configFile.Bind(
        //        section: DisplayName,
        //        key: "time scale",
        //        defaultValue: 5,
        //        description: "How slow time moves while the module is active (0 = time stop, 10 = normal time)"
        //    );
        //}

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Freezes you in place.";
        }

    }
}
