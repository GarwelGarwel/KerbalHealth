namespace KerbalHealth
{
    public class ConditionsFactor : HealthFactor
    {
        public override string Name => "Conditions";

        // Not applicable to this factor
        public override double BaseChangePerDay => 0;

        public override bool Cachable => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (!Core.ConditionsEnabled) return 0;
            KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
            if (khs == null) return 0;
            float k = Core.ConditionsEffect;
            double res = 0;
            foreach (HealthCondition hc in khs.Conditions)
                res += hc.HPChangePerDay * k;
            Core.Log("Conditions HP chande per day: " + res);
            return Core.IsInEditor ? (IsEnabledInEditor() ? res : 0) : res;
        }
    }
}
