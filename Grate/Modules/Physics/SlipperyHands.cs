﻿using Grate.GUI;
using Grate.Patches;

namespace Grate.Modules.Physics
{
    public class SlipperyHands : GrateModule
    {
        public static readonly string DisplayName = "Slippery Hands";
        public static SlipperyHands Instance;

        void Awake() { Instance = this; }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            if (NoSlip.Instance)
                NoSlip.Instance.enabled = false;
        }

        protected override void Cleanup()
        {
            string s = $"The functionality for this module is in {nameof(SlidePatch)}";
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: All surfaces become slippery.";
        }

    }
}
