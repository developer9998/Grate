namespace Grate.Modules.Multiplayer
{
    class PateronSupporter : GrateModule
    {
        public override string GetDisplayName()
        {
            return "Supporter";
        }

        public override string Tutorial()
        {
            return "Thanks you so much for showing your support";
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Cleanup()
        {

        }
    }
}
