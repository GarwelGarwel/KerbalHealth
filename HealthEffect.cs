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

        public double HPChangePerDay { get; set; } = 0;  // How many raw HP per day every affected kerbal gains
        public double Recuperation { get; set; } = 0;  // Will increase HP by this % of (MaxHP - HP) per day
        public double Decay { get; set; } = 0;  // Will decrease by this % of (HP - MinHP) per day
        public string MultiplyFactor { get; set; } = "All";  // Name of factor whose effect is multiplied
        public double Multiplier { get; set; } = 1;  // How the factor is changed (e.g., 0.5 means factor's effect is halved)
        public double Space { get; set; } = 0;  // Points of living space provided by the part (used to calculate Confinement factor)
        public double Shielding { get; set; } = 0;  // Number of halving-thicknesses
        public double Radioactivity { get; set; } = 0;  // Radioactive emission, bananas/day

        public double AccidentChance { get; set; } = 1;  // Accident chance multiplier
        public double PanicAttackChance { get; set; } = 1;  // Panic attack chance multiplier
        public double SicknessChance { get; set; } = 1;  // Getting infected/sick chance multiplier
        public double CureChance { get; set; } = 1;  // Sickness cure chance multiplier
        public double LoseImmunityChance { get; set; } = 1;  // Lose immunity chance multiplier

        public Logic Logic { get; set; } = new Logic();

        public bool IsApplicable(KerbalHealthStatus khs) => Logic.Test(khs.PCM);

        public void Apply(HealthModifierSet hms)
        {
            Core.Log("Applying effect:\n" + this);
            hms.ExposureMultiplier *= Exposure;
            hms.HPChange += HPChangePerDay;
            hms.RecuperationPower += Recuperation;
            hms.MaxRecuperaction = Math.Max(hms.MaxRecuperaction, Recuperation);
            hms.Decay += Decay;
            if (hms.FreeMultipliers.ContainsKey(MultiplyFactor)) hms.FreeMultipliers[MultiplyFactor] *= Multiplier;
            else hms.FreeMultipliers[MultiplyFactor] = Multiplier;
            hms.Space += Space;
            hms.Shielding += Shielding;
            hms.PartsRadiation += Radioactivity;
        }

        public override string ToString()
        {
            string res = "";
            if (MaxHP != 1) res += "\n" + Core.SignValue(MaxHP - 1, "P0") + " max HP";
            if (MaxHPBonus != 0) res += "\n" + Core.SignValue(MaxHPBonus, "F0") + "x max HP";
            if (ExhaustedStart != 1) res += "\n" + ExhaustedStart.ToString("F2") + "x Exhausted condition start HP";
            if (ExhaustedEnd != 1) res += "\n" + ExhaustedEnd.ToString("F2") + "x Exhausted condition end HP";
            if (Exposure != 1) res += "\n" + Exposure.ToString("F2") + "x Radiation Exposure";

            if (HPChangePerDay != 0) res = "\n" + Core.SignValue(HPChangePerDay, "F1") + " HP/day";
            if (Recuperation != 0) res += "\n" + Recuperation.ToString("F1") + "%/day Recuperation";
            if (Decay != 0) res += "\n" + Decay.ToString("F1") + "%/day Health Decay";
            if (Multiplier != 1) res += "\n" + Multiplier.ToString("F2") + "x " + MultiplyFactor + " factor";
            if (Space != 0) res += "\n" + Core.SignValue(Space, "F1") + " Living Space";
            if (Shielding != 0) res += "\n" + Core.SignValue(Shielding, "F1") + " Shielding";
            if (Radioactivity != 0) res += "\n" + Core.SignValue(Radioactivity, "N0") + " banana/day radioactive emission";

            if (AccidentChance != 1) res += "\n" + AccidentChance.ToString("F2") + "x Accident chance";
            if (PanicAttackChance != 1) res += "\n" + PanicAttackChance.ToString("F2") + "x Panic attack chance";
            if (SicknessChance != 1) res += "\n" + SicknessChance.ToString("F2") + "x Sickness chance";
            if (CureChance != 1) res += "\n" + CureChance.ToString("F2") + "x Cure chance";
            if (LoseImmunityChance != 1) res += "\n" + LoseImmunityChance.ToString("F2") + "x Lose immunity chance";

            string l = Logic.ToString();
            if (l != "") res += "\nLogic:\n" + l;

            return res.Trim();
        }

        public HealthEffect(ConfigNode node)
        {
            MaxHP = Core.GetDouble(node, "maxHP", 1);
            MaxHPBonus = Core.GetDouble(node, "maxHPBonus");
            ExhaustedStart = Core.GetDouble(node, "exhaustedStart", 1);
            ExhaustedEnd = Core.GetDouble(node, "exhaustedEnd", 1);
            Exposure = Core.GetDouble(node, "exposure", 1);

            HPChangePerDay = Core.GetDouble(node, "hpChangePerDay");
            Recuperation = Core.GetDouble(node, "recuperation");
            Decay = Core.GetDouble(node, "decay");
            MultiplyFactor = node.HasValue("multiplyFactor") ? node.GetValue("multiplyFactor") : "All";
            Multiplier = Core.GetDouble(node, "multiplier", 1);
            Space = Core.GetDouble(node, "space");
            Shielding = Core.GetDouble(node, "shielding");
            Radioactivity = Core.GetDouble(node, "radioactivity");

            AccidentChance = Core.GetDouble(node, "accidentChance", 1);
            PanicAttackChance = Core.GetDouble(node, "panicAttackChance", 1);
            SicknessChance = Core.GetDouble(node, "sicknessChance", 1);
            CureChance = Core.GetDouble(node, "cureChance", 1);
            LoseImmunityChance = Core.GetDouble(node, "loseImmunityChance", 1);

            Logic.ConfigNode = node;
        }

        private HealthEffect()
        { }
    }
}
