using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Contains logical conditions to be checked for true/false (e.g. object is in a specific SOI etc.)
    /// </summary>
    public class Logic
    {
        public enum OperatorType { And, Or };

        public OperatorType Operator { get; set; } = OperatorType.And;
        public bool Inverse { get; set; } = false;

        public string InSOI { get; set; } = null;
        public string KerbalStatus { get; set; } = null;
        public double MissionTime { get; set; } = Double.NaN;
        public string GenderPresent { get; set; } = null;

        public List<Logic> Operands { get; set; } = new List<Logic>();

        bool Op(ref bool operand1, bool operand2)
        {
            switch (Operator)
            {
                case OperatorType.And: operand1 &= operand2; break;
                case OperatorType.Or: operand1 |= operand2; break;
            }
            return operand1;
        }

        public bool Test(ProtoCrewMember pcm)
        {
            Core.Log("Logic.Test('" + pcm.name + "')");
            bool res = true;
            if (pcm == null)
            {
                Core.Log("ProtoCrewMember argument in Logic.Test is null!", Core.LogLevel.Error);
                return res;
            }
            Vessel v = Core.KerbalVessel(pcm);
            if (InSOI != null)
            {
                if (v != null)
                {
                    Core.Log("Checking 'inSOI = " + InSOI + "' rule. " + pcm.name + " is in " + v.mainBody?.name + "'s SOI.");
                    Op(ref res, InSOI.Equals(v.mainBody.name, StringComparison.CurrentCultureIgnoreCase));
                }
                else
                {
                    Core.Log("Checking 'inSOI = " + InSOI + "' rule. " + pcm.name + " is not in a vessel => this logic is false.");
                    Op(ref res, false);
                }
            }
            if (KerbalStatus != null)
            {
                Core.Log("Checking 'kerbalStatus = " + KerbalStatus + "'. " + pcm.name + " is " + pcm.rosterStatus + ".");
                Op(ref res, KerbalStatus.Equals(pcm.rosterStatus.ToString(), StringComparison.CurrentCultureIgnoreCase));
            }
            if (!Double.IsNaN(MissionTime))
            {
                Core.Log("Checking 'missionTime = " + MissionTime + "'. MET is " + v?.missionTime + ".");
                if (v != null) Op(ref res, v.missionTime >= MissionTime);
                else Op(ref res, false);
            }
            if (GenderPresent != null)
            {
                ProtoCrewMember.Gender g;
                switch (GenderPresent.ToLower())
                {
                    case "female": g = ProtoCrewMember.Gender.Female; break;
                    case "male": g = ProtoCrewMember.Gender.Male; break;
                    case "same": g = pcm.gender; break;
                    case "other": g = pcm.gender == ProtoCrewMember.Gender.Female ? ProtoCrewMember.Gender.Male : ProtoCrewMember.Gender.Female; break;
                    default:
                        Core.Log("Unrecognized value for gender in 'genderPresent = " + GenderPresent + "'. Assuming 'other'.");
                        goto case "other";
                }
                Core.Log("Checking condition 'genderPresent = " + GenderPresent + "'. Looking for " + g + " crewmates.");
                bool found = false;
                if (v != null)
                    foreach (ProtoCrewMember crewmate in v.GetVesselCrew())
                        if (crewmate.gender == g)
                        {
                            found = true;
                            break;
                        }
                Core.Log(g + " crewmates " + (found ? "" : "not ") + "found.");
                Op(ref res, found);
            }
            foreach (Logic l in Operands)
                Op(ref res, l.Test(pcm));
            return res ^ Inverse;
        }

        public string Description(int level, string pad = "-")
        {
            string res = "";
            string indent1 = "";
            for (int i = 0; i < level; i++) indent1 += pad;
            string indent2 = indent1 + pad + " ";
            if (InSOI != null) res += "\n" + indent2 + "Kerbal is in the SOI of " + InSOI;
            if (KerbalStatus != null) res += "\n" + indent2 + "Kerbal is" + KerbalStatus;
            if (!Double.IsNaN(MissionTime)) res += "\n" + indent2 + "Mission lasts at least " + Core.ParseUT(MissionTime, false, 100);
            if (GenderPresent != null) res += "\n" + indent2 + GenderPresent + " gender kerbal(s) present in the vessel";
            foreach (Logic l in Operands) res += "\n" + l.Description(level + 1, pad);
            if (Core.CountChars(res, '\n') >= 2)
                res = indent1 + " " + (Operator == OperatorType.And ? (Inverse ? "One or more" : "All") : (Inverse ? "Any" : "None")) + " of the following conditions are " + ((Operator == OperatorType.And) && Inverse ? "false" : "true") + ":" + res;
            else if ((res != "") && Inverse) res = indent1 + " This is FALSE:" + res;
            else res = res.Trim('\n');
            return res;
        }

        public override string ToString() => Description(0);

        public ConfigNode ConfigNode
        {
            set
            {
                string s = Core.GetString(value, "operator") ?? Core.GetString(value, "logic");
                switch (s?.ToLower())
                {
                    case "and":
                    case "all":
                    case null:
                        Operator = OperatorType.And;
                        break;
                    case "or":
                    case "any":
                        Operator = OperatorType.Or;
                        break;
                    default:
                        Core.Log("Unrecognized Logic operator '" + s + "' in config node " + value.name + ".", Core.LogLevel.Error);
                        break;
                }
                Inverse = Core.GetBool(value, "inverse");
                InSOI = Core.GetString(value, "inSOI");
                if (InSOI?.ToLower() == "home") InSOI = FlightGlobals.GetHomeBodyName();
                KerbalStatus = Core.GetString(value, "kerbalStatus");
                MissionTime = Core.GetDouble(value, "missionTime", Double.NaN);
                GenderPresent = Core.GetString(value, "genderPresent");
                foreach (ConfigNode node in value.GetNodes("LOGIC"))
                    Operands.Add(new Logic(node));
            }
        }

        public Logic() { }

        public Logic(ConfigNode node) => ConfigNode = node;
    }
}
