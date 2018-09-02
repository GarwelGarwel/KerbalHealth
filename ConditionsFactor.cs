namespace KerbalHealth
{
    public class ConditionsFactor : HealthFactor
    {
        public override string Name => "Conditions";

        // Not applicable to this factor
        public override double BaseChangePerDay => 0;//HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().SicknessFactor;

        public override bool Cachable => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (!Core.SicknessEnabled) return 0;
            KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
            if (khs == null) return 0;
            double res = 0;
            foreach (HealthCondition hc in khs.Conditions)
                res += hc.HPChangePerDay;
            Core.Log("Conditions HP chande per day: " + res);
            return Core.IsInEditor ? (IsEnabledInEditor() ? res : 0) : res;
        }
    }
}
