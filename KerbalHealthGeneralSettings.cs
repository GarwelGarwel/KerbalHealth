using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class KerbalHealthGeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "General Settings"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Kerbal Health"; } }
        public override string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 1; } }

        [GameParameters.CustomParameterUI("Mod Enabled", toolTip = "Turn Kerbal Health mechanics on/off")]
        public bool modEnabled = true;

        [GameParameters.CustomParameterUI("Use Message System", toolTip = "Post alerts about important health events as opposed to displaying screen messages")]
        public bool useMessageSystem = true;

        [GameParameters.CustomParameterUI("Use Blizzy's Toolbar", toolTip = "Use Blizzy's Toolbar mod (is installed) instead of stock app launcher. May need a scene change")]
        public bool useBlizzysToolbar = true;

        [GameParameters.CustomFloatParameterUI("Update Interval", toolTip = "Number of GAME seconds between health updates\nDoesn't affect health rates. Increase if performance too slow", minValue = 0.04f, maxValue = 60)]
        public float UpdateInterval = 10;

        [GameParameters.CustomFloatParameterUI("Minimum Update Interval", toolTip = "Minimum number of REAL seconds between updated on high time warp\nMust be <= Update Interval", minValue = 0.04f, maxValue = 60)]
        public float MinUpdateInterval = 1;

        [GameParameters.CustomFloatParameterUI("Base Max HP", toolTip = "Max number of Health Points for 0-star kerbals", minValue = 10, maxValue = 200, stepCount = 20)]
        public float BaseMaxHP = 100;

        [GameParameters.CustomFloatParameterUI("HP per Level", toolTip = "Health Points increase per level (star) of a kerbal", minValue = 0, maxValue = 50, stepCount = 51)]
        public float HPPerLevel = 10;

        [GameParameters.CustomParameterUI("Death Enabled", toolTip = "Allow kerbals to die of poor health")]
        public bool deathEnabled = true;

        [GameParameters.CustomFloatParameterUI("Exhaustion Start", toolTip = "Health level when kerbals turn Exhausted (becomes Tourist)", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float ExhaustionStartHealth = 0.2f;

        [GameParameters.CustomFloatParameterUI("Exhaustion End Level", toolTip = "Health level when kerbals leave Exhausted state (must be greater than or equal to Exhaustion start)", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float ExhaustionEndHealth = 0.25f;

        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "Controls amount of logging")]
        public bool debugMode = false;
    }
}
