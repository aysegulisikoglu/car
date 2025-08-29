using UnityEngine;

namespace TorqueTrainer
{
    public class VehicleInput : MonoBehaviour
    {
        [Header("Keys")]
        public KeyCode throttleKey = KeyCode.W;
        public KeyCode brakeKey = KeyCode.S;
        public KeyCode gearUpKey = KeyCode.E;
        public KeyCode gearDownKey = KeyCode.Q;
        public KeyCode clutchKey = KeyCode.LeftShift; // opsiyonel

        [Header("Analogue Feel")]
        [Range(0.5f, 10f)] public float throttleRise = 6f;
        [Range(0.5f, 10f)] public float throttleFall = 8f;
        [Range(0.5f, 10f)] public float brakeRise = 10f;
        [Range(0.5f, 10f)] public float brakeFall = 12f;

        public float Throttle { get; private set; } // 0..1
        public float Brake { get; private set; }    // 0..1
        public bool ClutchHeld { get; private set; }
        public bool GearUpPressed { get; private set; }
        public bool GearDownPressed { get; private set; }

        void Update()
        {
            bool th = Input.GetKey(throttleKey);
            bool br = Input.GetKey(brakeKey);

            float dt = Time.deltaTime;
            Throttle = Mathf.MoveTowards(Throttle, th ? 1f : 0f, (th ? throttleRise : throttleFall) * dt);
            Brake = Mathf.MoveTowards(Brake, br ? 1f : 0f, (br ? brakeRise : brakeFall) * dt);

            ClutchHeld = Input.GetKey(clutchKey);
            GearUpPressed = Input.GetKeyDown(gearUpKey);
            GearDownPressed = Input.GetKeyDown(gearDownKey);
        }

    }
}

