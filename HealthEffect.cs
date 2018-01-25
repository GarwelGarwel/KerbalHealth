using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class HealthEffect
    {
        public double MaxHP { get; set; } = 1;  // Max HP multiplier, 1 means no change, 2 means 200% etc.
        public double MaxHPBonus { get; set; } = 0;  // Max HP change
        public double ExhaustedStart { get; set; } = 1;  // Exhausted start level multiplier
        public double ExhaustedEnd { get; set; } = 1;  // Exhausted end level multiplier
        public double Exposure { get; set; } = 1;  // Exposure multiplier

        public double AccidentChance { get; set; } = 1;  // Accident chance multiplier
        public double PanicAttackChance { get; set; } = 1;  // Panic attack chance multiplier
        public double SicknessChance { get; set; } = 1;  // Getting infected/sick chance multiplier
        public double CureChance { get; set; } = 1;  // Sickness cure chance multiplier
        public double LoseImmunityChance { get; set; } = 1;  // Lose immunity chance multiplier

        public double HPChangePerDay { get; set; } = 0;  // How many raw HP per day every affected kerbal gains
        public double Recuperation { get; set; } = 0;  // Will increase HP by this % of (MaxHP - HP) per day
        public double Decay { get; set; } = 0;  // Will decrease by this % of (HP - MinHP) per day
        public string MultiplyFactor { get; set; } = "All";  // Name of factor whose effect is multiplied
        public double Multiplier { get; set; } = 1;  // How the factor is changed (e.g., 0.5 means factor's effect is halved)
        public double Space { get; set; } = 0;  // Points of living space provided by the part (used to calculate Crowded factor)
        public double Shielding { get; set; } = 0;  // Number of halving-thicknesses
        public double Radioactivity { get; set; } = 0;  // Radioactive emission, bananas/day

        public void Apply(KerbalHealthStatus khs)
        {
            khs.VesselHealthInfo.HPChange += HPChangePerDay;
            khs.VesselHealthInfo.RecuperationPower += Recuperation;
            khs.VesselHealthInfo.MaxRecuperaction = Math.Max(khs.VesselHealthInfo.MaxRecuperaction, Recuperation);
            khs.VesselHealthInfo.Decay += Decay;
            if (khs.VesselHealthInfo.FreeMultipliers.ContainsKey(MultiplyFactor)) khs.VesselHealthInfo.FreeMultipliers[MultiplyFactor] *= Multiplier;
            else khs.VesselHealthInfo.FreeMultipliers[MultiplyFactor] = Multiplier;
            khs.VesselHealthInfo.Space += Space;
            khs.VesselHealthInfo.Shielding += Shielding;
            khs.VesselHealthInfo.PartsRadiation += Radioactivity;
        }

        public override string ToString()
        {
            string res = "";
            if (HPChangePerDay != 0) res = "\nHealth points: " + HPChangePerDay.ToString("F1") + "/day";
            if (Recuperation != 0) res += "\nRecuperation: " + Recuperation.ToString("F1") + "%/day";
            if (Decay != 0) res += "\nHealth decay: " + Decay.ToString("F1") + "%/day";
            if (Multiplier != 1) res += "\n" + Multiplier.ToString("F2") + "x " + MultiplyFactor;
            if (Space != 0) res += "\nSpace: " + Space.ToString("F1");
            if (Shielding != 0) res += "\nShielding rating: " + Shielding.ToString("F1");
            if (Radioactivity != 0) res += "\nRadioactive emission: " + Radioactivity.ToString("N0") + "/day";
            return res;
        }

        public HealthEffect(ConfigNode node)
        {
            HPChangePerDay = Core.GetDouble(node, "hpChangePerDay");
            Recuperation = Core.GetDouble(node, "recuperation");
            Decay = Core.GetDouble(node, "decay");
            MultiplyFactor = node.HasValue("multiplyFactor") ? node.GetValue("multiplyFactor") : "All";
            Multiplier = Core.GetDouble(node, "multiplier", 1);
            Space = Core.GetDouble(node, "space");
            Shielding = Core.GetDouble(node, "shielding");
            Radioactivity = Core.GetDouble(node, "radioactivity");
        }
    }
}
