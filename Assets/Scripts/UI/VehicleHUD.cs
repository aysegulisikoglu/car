using UnityEngine;
using UnityEngine.UI;

namespace TorqueTrainer
{
    public class VehicleHUD : MonoBehaviour
    {
        public Drivetrain car;
        public Text speedText;
        public Text rpmText;
        public Text gearText;
        public Text torqueText;
        public Text helpText;

        void Start()
        {
            if (helpText)
            {
                helpText.text = "W: Gaz  |  S: Fren  |  Q/E: Vites -/+  |  LShift: Debriyaj\n" +
                                "Amaç: Gaz ver → tork eğrisi → teker torku → çekiş limiti → hız!";
            }
        }

        void Update()
        {
            if (!car) return;
            if (speedText) speedText.text = $"Speed: {car.SpeedKPH:0} km/h";
            if (rpmText) rpmText.text = $"RPM: {car.EngineRPM:0}";
            if (gearText) gearText.text = $"Gear: {(car.CurrentGearIndex + 1)}";
            if (torqueText) torqueText.text = $"Wheel Torque: {car.WheelTorqueNm:0} Nm";
        }
    }
}

