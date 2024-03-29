﻿using KSP.Localization;

namespace KerbalHealth
{
    class LonelinessFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = -1;

        public override string Name => "Loneliness";

        public override string Title => Localizer.Format("#KH_Factor_Loneliness");//Loneliness

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.LonelinessEffect;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor && !IsEnabledInEditor())
                return 0;
            return Core.GetCrewCount(khs.ProtoCrewMember, true) == 1 && !khs.ProtoCrewMember.isBadass ? BaseChangePerDay : 0;
        }
    }
}
