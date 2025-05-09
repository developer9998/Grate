using GorillaLocomotion;
using Grate.Gestures;
using UnityEngine;


namespace Grate.Interaction
{
    public class GrateGrabbable : GrateInteractable
    {
        private Vector3 _localPos, _mirrorPos;
        Vector3 mirrorScale = new Vector3(-1, 1, 1);
        public float throwForceMultiplier = 1f;

        public Vector3 LocalPosition
        {
            get { return _localPos; }
            set
            {
                _localPos = value;
                _mirrorPos = Vector3.Scale(value, mirrorScale);
            }
        }

        public Vector3 MirroredLocalPosition
        {
            get { return _mirrorPos; }
        }

        public Vector3 LocalRotation = Vector3.zero;
        public bool throwOnDetach;
        bool kinematicCache;
        GorillaVelocityEstimator velEstimator;

        protected override void Awake()
        {
            base.Awake();
            var gt = GestureTracker.Instance;
            validSelectors = new GrateInteractor[] { gt.leftPalmInteractor, gt.rightPalmInteractor };
            velEstimator = this.gameObject.AddComponent<GorillaVelocityEstimator>();
        }

        public override void OnSelect(GrateInteractor interactor)
        {
            if (this.GetComponent<Rigidbody>() is Rigidbody rb)
            {
                kinematicCache = rb.isKinematic;
                rb.isKinematic = true;
            }
            this.transform.SetParent(interactor.transform);
            if (interactor.IsLeft)
                this.transform.localPosition = this.LocalPosition;
            else
                this.transform.localPosition = this.MirroredLocalPosition;
            this.transform.localRotation = Quaternion.Euler(this.LocalRotation);
            base.OnSelect(interactor);
        }

        public override void OnDeselect(GrateInteractor interactor)
        {
            this.transform.SetParent(null);
            if (this.GetComponent<Rigidbody>() is Rigidbody rb)
            {
                if (throwOnDetach)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    // Apply the force to the rigidbody
                    rb.velocity = (GTPlayer.Instance.GetComponent<Rigidbody>().velocity) + velEstimator.linearVelocity * throwForceMultiplier;
                    rb.velocity *= 1 / GTPlayer.Instance.scale;
                    rb.angularVelocity = velEstimator.angularVelocity;
                }
                else
                    rb.isKinematic = kinematicCache;
            }
            base.OnDeselect(interactor);
        }
    }
}