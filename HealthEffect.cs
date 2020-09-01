using System;

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
            Core.Log($"Applying effect:\n{this}");
            hms.ExposureMultiplier *= Exposure;
            hms.HPChange += HPChangePerDay;
            hms.RecuperationPower += Recuperation;
            hms.MaxRecuperaction = Math.Max(hms.MaxRecuperaction, Recuperation);
            hms.Decay += Decay;
            if (hms.FreeMultipliers.ContainsKey(MultiplyFactor))
                hms.FreeMultipliers[MultiplyFactor] *= Multiplier;
            else hms.FreeMultipliers[MultiplyFactor] = Multiplier;
            hms.Space += Space;
            hms.Shielding += Shielding;
            hms.PartsRadiation += Radioactivity;
        }

        public override string ToString()
        {
            string res = "";
            if (MaxHP != 1)
                res = $"{Core.SignValue(MaxHP - 1, "P0")} max HP";
            if (MaxHPBonus != 0)
                res += $"\n{Core.SignValue(MaxHPBonus, "F0")}x max HP";
            if (ExhaustedStart != 1)
                res += $"\n{ExhaustedStart:F2}x Exhausted condition start HP";
            if (ExhaustedEnd != 1)
                res += $"\n{ExhaustedEnd:F2}x Exhausted condition end HP";
            if (Exposure != 1)
                res += $"\n{Exposure:F2}x Radiation Exposure";

            if (HPChangePerDay != 0)
                res = $"\n{Core.SignValue(HPChangePerDay, "F1")} HP/day";
            if (Recuperation != 0)
                res += $"\n{Recuperation:F1}%/day Recuperation";
            if (Decay != 0)
                res += $"\n{Decay:F1}%/day Health Decay";
            if (Multiplier != 1)
                res += $"\n{Multiplier:F2}x {MultiplyFactor} factor";
            if (Space != 0)
                res += $"\n{Core.SignValue(Space, "F1")} Living Space";
            if (Shielding != 0)
                res += $"\n{Core.SignValue(Shielding, "F1")} Shielding";
            if (Radioactivity != 0)
                res += $"\n{Core.SignValue(Radioactivity, "N0")} banana/day radioactive emission";

            if (AccidentChance != 1)
                res += $"\n{AccidentChance:F2}x Accident chance";
            if (PanicAttackChance != 1)
                res += $"\n{PanicAttackChance:F2}x Panic attack chance";
            if (SicknessChance != 1)
                res += $"\n{SicknessChance:F2}x Sickness chance";
            if (CureChance != 1)
                res += $"\n{CureChance:F2}x Cure chance";
            if (LoseImmunityChance != 1)
                res += $"\n{LoseImmunityChance:F2}x Lose immunity chance";

            string l = Logic.ToString();
            if (l.Length != 0)
                res += $"\nLogic:\n{l}";

            return res.Trim();
        }

        public HealthEffect(ConfigNode node)
        {
            MaxHP = node.GetDouble("maxHP", 1);
            MaxHPBonus = node.GetDouble("maxHPBonus");
            ExhaustedStart = node.GetDouble("exhaustedStart", 1);
            ExhaustedEnd = node.GetDouble("exhaustedEnd", 1);
            Exposure = node.GetDouble("exposure", 1);

            HPChangePerDay = node.GetDouble("hpChangePerDay");
            Recuperation = node.GetDouble("recuperation");
            Decay = node.GetDouble("decay");
            MultiplyFactor = node.HasValue("multiplyFactor") ? node.GetValue("multiplyFactor") : "All";
            Multiplier = node.GetDouble("multiplier", 1);
            Space = node.GetDouble("space");
            Shielding = node.GetDouble("shielding");
            Radioactivity = node.GetDouble("radioactivity");

            AccidentChance = node.GetDouble("accidentChance", 1);
            PanicAttackChance = node.GetDouble("panicAttackChance", 1);
            SicknessChance = node.GetDouble("sicknessChance", 1);
            CureChance = node.GetDouble("cureChance", 1);
            LoseImmunityChance = node.GetDouble("loseImmunityChance", 1);

            Logic.ConfigNode = node;
        }

        private HealthEffect()
        { }
    }
}
