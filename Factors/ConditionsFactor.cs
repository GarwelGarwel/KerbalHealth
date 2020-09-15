using KSP.Localization;

namespace KerbalHealth
{
    public class ConditionsFactor : HealthFactor
    {
        public override string Name => "Conditions";

        public override string Title => Localizer.Format("#KH_Factor_Conditions");//Conditions

        // Not applicable to this factor
        public override double BaseChangePerDay => 0;

        public override bool ConstantForUnloaded => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (!KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
                return 0;
            double res = 0;
            foreach (HealthCondition hc in khs.Conditions)
                res += hc.HPChangePerDay * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            Core.Log($"Conditions HP change per day: {res}");
            return Core.IsInEditor ? (IsEnabledInEditor() ? res : 0) : res;
        }
    }
}
