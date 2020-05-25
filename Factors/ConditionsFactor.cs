using KSP.Localization;

namespace KerbalHealth
{
    public class ConditionsFactor : HealthFactor
    {
        public override string Name => "Conditions";
        public override string Title => Localizer.Format("#KH_Condition");//Conditions

        // Not applicable to this factor
        public override double BaseChangePerDay => 0;

        public override bool Cachable => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (!KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
                return 0;
            KerbalHealthStatus khs = Core.KerbalHealthList[pcm];
            if (khs == null)
                return 0;
            double res = 0;
            foreach (HealthCondition hc in khs.Conditions)
                res += hc.HPChangePerDay * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            Core.Log("Conditions HP chande per day: " + res);
            return Core.IsInEditor ? (IsEnabledInEditor() ? res : 0) : res;
        }
    }
}
