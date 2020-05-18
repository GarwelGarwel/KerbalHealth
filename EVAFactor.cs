namespace KerbalHealth
{
    class EVAFactor : HealthFactor
    {
        public override string Name => "EVA";

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.EVAFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (Core.KerbalHealthList[pcm].IsOnEVA)
            {
                Core.Log("EVA factor is on.");
                return BaseChangePerDay;
            }
            return 0;
        }
    }
}
