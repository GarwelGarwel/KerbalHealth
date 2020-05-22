using System;
using System.Collections.Generic;
using System.Linq;

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

        public static readonly string Padding = "-";

        public string Situation { get; set; } = null;
        public string InSOI { get; set; } = null;
        public string KerbalStatus { get; set; } = null;
        public double MissionTime { get; set; } = Double.NaN;
        public string Gender { get; set; } = null;
        public string GenderPresent { get; set; } = null;
        public string TraitPresent { get; set; } = null;
        public string ConditionPresent { get; set; } = null;

        public List<Logic> Operands { get; set; } = new List<Logic>();

        bool Op(ref bool operand1, bool operand2)
        {
            switch (Operator)
            {
                case OperatorType.And:
                    operand1 &= operand2;
                    break;
                case OperatorType.Or:
                    operand1 |= operand2;
                    break;
            }
            return operand1;
        }

        public bool Test(ProtoCrewMember pcm)
        {
            bool res = true;
            if (pcm == null)
            {
                Core.Log("ProtoCrewMember argument in Logic.Test is null!", LogLevel.Error);
                return res;
            }
            Vessel v = Core.KerbalVessel(pcm);
            if (Situation != null)
                if (v != null)
                {
                    switch (Situation.ToLower())
                    {
                        case "prelaunch":
                            Op(ref res, v.situation == Vessel.Situations.PRELAUNCH);
                            break;
                        case "landed":
                            Op(ref res, v.situation == Vessel.Situations.LANDED);
                            break;
                        case "splashed":
                            Op(ref res, v.situation == Vessel.Situations.SPLASHED);
                            break;
                        case "ground":
                            Op(ref res, (v.situation == Vessel.Situations.LANDED) || (v.situation == Vessel.Situations.SPLASHED));
                            break;
                        case "flying":
                            Op(ref res, v.situation == Vessel.Situations.FLYING);
                            break;
                        case "suborbital":
                            Op(ref res, v.situation == Vessel.Situations.SUB_ORBITAL);
                            break;
                        case "orbiting":
                            Op(ref res, v.situation == Vessel.Situations.ORBITING);
                            break;
                        case "escaping":
                            Op(ref res, v.situation == Vessel.Situations.ESCAPING);
                            break;
                        case "in space":
                            Op(ref res, (v.situation == Vessel.Situations.SUB_ORBITAL) || (v.situation == Vessel.Situations.ORBITING) || (v.situation == Vessel.Situations.ESCAPING));
                            break;
                    }
                }
                else Op(ref res, false);

            if (InSOI != null)
                Op(ref res, v != null ? InSOI.Equals(v.mainBody.name, StringComparison.CurrentCultureIgnoreCase) : false);
            
            if (KerbalStatus != null)
                Op(ref res, KerbalStatus.Equals(pcm.rosterStatus.ToString(), StringComparison.CurrentCultureIgnoreCase));

            if (!Double.IsNaN(MissionTime))
                Op(ref res, v != null ? v.missionTime >= MissionTime : false);

            if (Gender != null)
            {
                ProtoCrewMember.Gender g = pcm.gender;
                switch (Gender.ToLower())
                {
                    case "female":
                        Op(ref res, g == ProtoCrewMember.Gender.Female);
                        break;
                    case "male":
                        Op(ref res, g == ProtoCrewMember.Gender.Male);
                        break;
                }
            }

            if (GenderPresent != null)
            {
                ProtoCrewMember.Gender g;
                switch (GenderPresent.ToLower())
                {
                    case "female":
                        g = ProtoCrewMember.Gender.Female;
                        break;
                    case "male":
                        g = ProtoCrewMember.Gender.Male;
                        break;
                    case "same":
                        g = pcm.gender;
                        break;
                    case "other":
                        g = pcm.gender == ProtoCrewMember.Gender.Female ? ProtoCrewMember.Gender.Male : ProtoCrewMember.Gender.Female;
                        break;
                    default:
                        Core.Log("Unrecognized value for gender in 'genderPresent = " + GenderPresent + "'. Assuming 'other'.");
                        goto case "other";
                }
                bool found = false;
                if (v != null)
                    foreach (ProtoCrewMember crewmate in v.GetVesselCrew())
                        if ((crewmate.gender == g) && (crewmate != pcm))
                        {
                            found = true;
                            break;
                        }
                Op(ref res, found);
            }

            if (TraitPresent != null)
            {
                bool found = false;
                if (v != null)
                    foreach (ProtoCrewMember crewmate in v.GetVesselCrew())
                        if ((crewmate.trait.ToLower() == TraitPresent.ToLower()) && (crewmate != pcm))
                        {
                            found = true;
                            break;
                        }
                Op(ref res, found);
            }

            if (ConditionPresent != null)
            {
                bool found = false;
                if (v != null)
                    foreach (ProtoCrewMember crewmate in v.GetVesselCrew())
                        if ((Core.KerbalHealthList[crewmate].HasCondition(ConditionPresent)) && (crewmate != pcm))
                        {
                            found = true;
                            break;
                        }
                Op(ref res, found);
            }

            foreach (Logic l in Operands)
                Op(ref res, l.Test(pcm));

            return res ^ Inverse;
        }

        public string Description(int level)
        {
            string res = "";
            string indent1 = "";
            for (int i = 0; i < level; i++)
                indent1 += Padding;
            string indent2 = indent1 + Padding + " ";
            if (level > 0)
                indent1 += " ";

            if (Situation != null)
                res += "\n" + indent2 + "Is " + Situation;
            if (InSOI != null)
                res += "\n" + indent2 + "Kerbal is in the SOI of " + InSOI;
            if (KerbalStatus != null)
                res += "\n" + indent2 + "Kerbal is" + KerbalStatus;
            if (!Double.IsNaN(MissionTime))
                res += "\n" + indent2 + "Mission lasts at least " + Core.ParseUT(MissionTime, false, 100);
            if (Gender != null)
                res += "\n" + indent2 + "Kerbal is " + Gender;
            if (GenderPresent != null)
                res += "\n" + indent2 + GenderPresent + " gender kerbal(s) present in the vessel";
            if (TraitPresent != null)
                res += "\n" + indent2 + TraitPresent + " kerbal(s) present in the vessel";
            if (ConditionPresent != null)
                res += "\n" + indent2 + "Kerbal(s) with " + ConditionPresent + " present in the vessel";
            foreach (Logic l in Operands)
                res += "\n" + l.Description(level + 1);
            if (res.Count(c => c == '\n') > 1)
                res = indent1
                    + (Operator == OperatorType.And ? (Inverse ? "One" : "All") : (Inverse ? "None" : "One"))
                    + " of the following conditions is "
                    + ((Operator == OperatorType.And) && Inverse ? "false" : "true")
                    + ":" + res;
            else res = (res.Length != 0) && Inverse ? indent1 + "This is FALSE:" + res : res.Trim('\n');
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
                        Core.Log("Unrecognized Logic operator '" + s + "' in config node " + value.name + ".", LogLevel.Error);
                        break;
                }
                Inverse = Core.GetBool(value, "inverse");
                Situation = Core.GetString(value, "situation");
                InSOI = Core.GetString(value, "inSOI");
                if (InSOI?.ToLower() == "home") InSOI = FlightGlobals.GetHomeBodyName();
                KerbalStatus = Core.GetString(value, "kerbalStatus");
                MissionTime = Core.GetDouble(value, "missionTime", Double.NaN);
                Gender = Core.GetString(value, "gender");
                GenderPresent = Core.GetString(value, "genderPresent");
                TraitPresent = Core.GetString(value, "traitPresent");
                ConditionPresent = Core.GetString(value, "conditionPresent");
                foreach (ConfigNode node in value.GetNodes("LOGIC"))
                    Operands.Add(new Logic(node));
            }
        }

        public Logic() { }

        public Logic(ConfigNode node) => ConfigNode = node;
    }
}
