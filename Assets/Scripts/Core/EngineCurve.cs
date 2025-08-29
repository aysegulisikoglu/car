using UnityEngine;

namespace TorqueTrainer
{
    [CreateAssetMenu(menuName = "TorqueTrainer/EngineCurve")]
    public class EngineCurve : ScriptableObject
    {
        [Header("Engine RPM Limits")]
        public float idleRPM = 900f;
        public float redlineRPM = 6500f;
        public float hardCutRPM = 7000f;

        [Header("Torque Curve (x: RPM, y: Torque Nm)")]
        public AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(800f, 90f),
            new Keyframe(1500f, 130f),
            new Keyframe(2500f, 170f),
            new Keyframe(3500f, 190f),
            new Keyframe(4500f, 200f),
            new Keyframe(5500f, 185f),
            new Keyframe(6500f, 150f)
        );

        public float EvaluateTorque(float rpm)
        {
            rpm = Mathf.Clamp(rpm, 0f, hardCutRPM);
            return Mathf.Max(0f, torqueCurve.Evaluate(rpm));
        }
    }
}

