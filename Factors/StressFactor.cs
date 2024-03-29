﻿using KSP.Localization;

namespace KerbalHealth
{
    public class StressFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = -2;

        public override string Name => "Stress";

        public override string Title => Localizer.Format("#KH_Factor_Stress");

        public override bool ConstantForUnloaded => false;

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.StressEffect;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if ((Core.IsInEditor && !IsEnabledInEditor()) || (!Core.IsInEditor && khs.ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned))
                return 0;
            return BaseChangePerDay * (1 - khs.GetTrainingLevel(Core.IsInEditor && KerbalHealthEditorReport.SimulateTrained)) / Core.GetColleaguesCount(khs.ProtoCrewMember);
        }
    }
}
