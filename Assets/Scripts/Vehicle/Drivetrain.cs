using UnityEngine;

namespace TorqueTrainer
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class Drivetrain : MonoBehaviour
    {
        [Header("References")]
        public VehicleInput input;
        public EngineCurve engine;

        private Rigidbody _rb;

        [Header("Vehicle Setup")]
        public float vehicleMass = 1200f;       // kg
        public float wheelRadius = 0.32f;       // m

        [Header("Gearing")]
        public float[] gearRatios = new float[] { 3.2f, 2.1f, 1.5f, 1.15f, 0.95f };
        public float finalDrive = 3.42f;
        [Range(0.5f, 1f)] public float drivelineEfficiency = 0.9f;
        public int startGearIndex = 0;

        [Header("Traction & Losses")]
        public float muTire = 1.0f;
        public float rollingResistance = 0.012f;
        public float dragCoefficient = 0.32f;
        public float frontalArea = 2.2f;
        public float airDensity = 1.225f;

        [Header("Brakes")]
        public float maxBrakeForce = 9000f;

        [Header("Shift Behavior")]
        public float shiftCutMs = 120f;
        private float _shiftTimer;

        // Telemetry (read-only)
        public int CurrentGearIndex { get; private set; }
        public float EngineRPM { get; private set; }
        public float WheelRPM { get; private set; }
        public float WheelTorqueNm { get; private set; }
        public float DriveForceN { get; private set; }
        public float SpeedKPH { get; private set; }

        // internal
        // private bool _selfProvisionedEngine = false;

        void Awake()
        {
            // Rigidbody
            _rb = GetComponent<Rigidbody>();
            _rb.mass = Mathf.Max(1f, vehicleMass);
#if UNITY_6000_0_OR_NEWER
            _rb.linearDamping = 0f;   // Unity 6+
            _rb.angularDamping = 0f;
#else
            _rb.drag = 0f;            // 2021/2022/2023 LTS
            _rb.angularDrag = 0f;
#endif
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.None;
            _rb.WakeUp();             // sleepMode kullanmadan uyandır

            // Input referansı
            if (!input) input = GetComponent<VehicleInput>();

            // Engine yoksa basit bir default oluştur
            if (!engine)
            {
                engine = ScriptableObject.CreateInstance<EngineCurve>();
                engine.idleRPM = 900f;
                engine.redlineRPM = 6500f;
                engine.hardCutRPM = 7000f;
                engine.torqueCurve = new AnimationCurve(
                    new Keyframe(800f, 90f),
                    new Keyframe(2500f, 170f),
                    new Keyframe(4500f, 200f),
                    new Keyframe(6000f, 160f)
                );
                // _selfProvisionedEngine = true;
            }

            // Gear guard
            if (gearRatios == null || gearRatios.Length == 0)
                gearRatios = new float[] { 3.2f, 2.1f, 1.5f, 1.15f, 0.95f };

            CurrentGearIndex = Mathf.Clamp(startGearIndex, 0, gearRatios.Length - 1);
        }

        void Update()
        {
            // Hızlı test için: T'ye basınca tek seferlik itme
            if (Input.GetKeyDown(KeyCode.T))
            {
                _rb.AddForce(transform.forward * 5000f, ForceMode.Impulse);
                Debug.Log("[Drivetrain] IMPULSE TEST applied (T).");
            }

            HandleShifting();
        }

        void FixedUpdate()
        {
            Vector3 fwd = transform.forward;

            // İleri hız (aracın ekseninde)
            float v = Vector3.Dot(_rb.linearVelocity, fwd);
            SpeedKPH = Mathf.Abs(v) * 3.6f;

            // Teker RPM
            WheelRPM = (v / (2f * Mathf.PI * Mathf.Max(0.001f, wheelRadius))) * 60f;

            // Oranlar
            float overall = gearRatios[CurrentGearIndex] * finalDrive;

            // Motor RPM (debriyaj bağlı varsayımı)
            float targetEngineRPM = Mathf.Max(engine.idleRPM, Mathf.Abs(WheelRPM) * overall);

            if (_shiftTimer > 0f) _shiftTimer -= Time.deltaTime;

            bool limiter = targetEngineRPM >= engine.redlineRPM;
            EngineRPM = Mathf.Min(targetEngineRPM, engine.hardCutRPM);

            // Motor torku
            float baseNm = engine.EvaluateTorque(EngineRPM);
            float cut = (_shiftTimer > 0f) ? 0f : 1f;
            float limiterCut = limiter ? 0f : 1f;
            float clutchCut = (input && input.ClutchHeld) ? 0.25f : 1f;
            float throttle = input ? input.Throttle : 0f;
            float engTorque = baseNm * throttle * cut * limiterCut * clutchCut;

            // Teker torku → çekiş limiti
            WheelTorqueNm = engTorque * overall * drivelineEfficiency;
            float driveForce = Mathf.Abs(WheelTorqueNm) / Mathf.Max(0.001f, wheelRadius);

            float Fmax = muTire * _rb.mass * 9.81f;
            driveForce = Mathf.Min(driveForce, Fmax);
            DriveForceN = driveForce;

            // Kayıplar
            float Fdrag = 0.5f * airDensity * dragCoefficient * frontalArea * v * v;
            float Froll = rollingResistance * _rb.mass * 9.81f;
            float Fbrake = (input ? input.Brake : 0f) * maxBrakeForce;

            float signV = Mathf.Sign(v == 0f ? 1f : v);
            Vector3 F_drive = fwd * driveForce;
            Vector3 F_drag = -fwd * Fdrag * signV;
            Vector3 F_roll = -fwd * Froll * signV;
            Vector3 F_brake = -fwd * Fbrake * signV;

            Vector3 Fnet = F_drive + F_drag + F_roll + F_brake;
            _rb.AddForce(Fnet, ForceMode.Force);

            // Kalkış yardımcı dürtüsü (çok durgunken)
            if (SpeedKPH < 0.1f && (input ? input.Throttle : 0f) > 0.05f)
                _rb.AddForce(fwd * 50f, ForceMode.Impulse);
        }

        void HandleShifting()
        {
            if (input == null || gearRatios == null || gearRatios.Length == 0) return;

            if (input.GearUpPressed)
            {
                int next = Mathf.Clamp(CurrentGearIndex + 1, 0, gearRatios.Length - 1);
                if (next != CurrentGearIndex)
                {
                    CurrentGearIndex = next;
                    _shiftTimer = shiftCutMs / 1000f;
                }
            }

            if (input.GearDownPressed)
            {
                int next = Mathf.Clamp(CurrentGearIndex - 1, 0, gearRatios.Length - 1);
                if (next != CurrentGearIndex)
                {
                    CurrentGearIndex = next;
                    _shiftTimer = shiftCutMs / 1000f;
                }
            }
        }
    }
}
