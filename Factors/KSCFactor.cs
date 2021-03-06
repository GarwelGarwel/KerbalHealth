﻿using KSP.Localization;

namespace KerbalHealth
{
    public class KSCFactor : HealthFactor
    {
        public override string Name => "KSC";

        public override string Title => Localizer.Format("#KH_Factor_KSC");

        public override bool ConstantForUnloaded => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.KSCFactor;
        
        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return (khs.ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available) ? BaseChangePerDay : 0;
        }
    }
}
