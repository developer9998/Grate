using System;
using System.Collections.Generic;
using System.Text;

namespace Grate.Modules.Misc
{
    class JumpScare : GrateModule
    {
        public override string GetDisplayName()
        {
            return "JUMPSCARE";
        }

        public override string Tutorial()
        {
            return "";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //Photon Soundboard
            this.enabled = false;
        }

        protected override void Cleanup()
        {

        }
    }
}
