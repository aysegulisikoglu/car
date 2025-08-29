using UnityEngine;

namespace TorqueTrainer
{
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2.5f, -6f);
        public float followLerp = 8f;
        public float lookLerp = 12f;
        public bool autoFindTarget = true;

        void Start()
        {
            if (!target && autoFindTarget)
            {
#if UNITY_6000_0_OR_NEWER
                var dt = Object.FindFirstObjectByType<Drivetrain>(); // veya FindAnyObjectByType<Drivetrain>();
#else
                var dt = Object.FindObjectOfType<Drivetrain>();
#endif
                if (dt) target = dt.transform;
            }

            if (target)
            {
                transform.position = target.TransformPoint(offset);
                transform.rotation = Quaternion.LookRotation(target.position + Vector3.up - transform.position);
            }
        }

        void LateUpdate()
        {
            if (!target && autoFindTarget)
            {
#if UNITY_6000_0_OR_NEWER
                var dt = Object.FindFirstObjectByType<Drivetrain>(); // veya FindAnyObjectByType<Drivetrain>();
#else
                var dt = Object.FindObjectOfType<Drivetrain>();
#endif
                if (dt) target = dt.transform;
            }
            if (!target) return;

            Vector3 desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followLerp);

            Quaternion lookRot = Quaternion.LookRotation(target.position + Vector3.up - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * lookLerp);
        }
    }
}
