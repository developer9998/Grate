using Grate.GUI;

namespace Grate.Modules.Misc
{
    public class Lobby : GrateModule
    {
        int timesPressed;

        public static readonly string DisplayName = "Grate Code";


        protected override void Start()
        {
            base.Start();
            timesPressed = 0;
        }
        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            timesPressed++;
            if (timesPressed >= 3)
            {
                Plugin.Instance.JoinLobby("GRATE");
                timesPressed = 0;
                return;
            }
            this.enabled = false;
        }
        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Join Grate Code after Pressing 3 times";
        }

        protected override void Cleanup() { }
    }
}
