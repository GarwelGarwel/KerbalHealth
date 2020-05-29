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
            return Core.KerbalHealthList[pcm].IsOnEVA ? BaseChangePerDay : 0;
        }
    }
}
