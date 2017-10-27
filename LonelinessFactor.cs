namespace KerbalHealth
{
    class LonelinessFactor : HealthFactor
    {
        public override string Name => "Loneliness";

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().LonelinessFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor && !IsEnabledInEditor()) return 0;
            if ((Core.GetCrewCount(pcm) <= 1) && !pcm.isBadass) return BaseChangePerDay; else return 0;
        }
    }
}
