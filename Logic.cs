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
        public enum OperatorType
        {
            And,
            Or
        };

        public const string ConfigNodeName = "LOGIC";

        public static readonly string Padding = "-";
        public OperatorType Operator { get; set; } = OperatorType.And;
        public bool Inverse { get; set; } = false;
        public string Situation { get; set; } = null;
        public string InSOI { get; set; } = null;
        public string KerbalStatus { get; set; } = null;
        public double MissionTime { get; set; } = double.NaN;
        public string Gender { get; set; } = null;
        public string GenderPresent { get; set; } = null;
        public string TraitPresent { get; set; } = null;
        public string ConditionPresent { get; set; } = null;
        public string Quirk { get; set; } = null;

        public List<Logic> Operands { get; set; } = new List<Logic>();

        public void Load(ConfigNode node)
        {
            string s = node.GetString("operator") ?? node.GetString("logic");
            switch (s?.ToLowerInvariant())
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
                    Core.Log($"Unrecognized Logic operator '{s}' in config node {node.name}.", LogLevel.Error);
                    break;
            }
            Inverse = node.GetBool("inverse");
            Situation = node.GetString("situation");
            InSOI = node.GetString("inSOI");
            if (InSOI?.ToLower() == "home")
                InSOI = FlightGlobals.GetHomeBodyName();
            KerbalStatus = node.GetString("kerbalStatus");
            MissionTime = node.GetDouble("missionTime", double.NaN);
            Gender = node.GetString("gender");
            GenderPresent = node.GetString("genderPresent");
            TraitPresent = node.GetString("traitPresent");
            ConditionPresent = node.GetString("conditionPresent");
            Quirk = node.GetString("quirk");
            Operands = new List<Logic>(node.GetNodes(ConfigNodeName).Select(n => new Logic(n)));
        }

        public Logic()
        { }

        public Logic(ConfigNode node) => Load(node);

        public bool Test(ProtoCrewMember pcm)
        {
            bool? res = null;
            if (pcm == null)
            {
                Core.Log("ProtoCrewMember argument in Logic.Test is null!", LogLevel.Error);
                return false;
            }
            Vessel v = pcm.GetVessel();
            if (Situation != null)
                if (v != null)
                {
                    switch (Situation.ToLowerInvariant())
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
                            Op(ref res, v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.SPLASHED);
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
                            Op(ref res, v.situation == Vessel.Situations.SUB_ORBITAL || v.situation == Vessel.Situations.ORBITING || v.situation == Vessel.Situations.ESCAPING);
                            break;
                    }
                }
                else Op(ref res, false);

            if (InSOI != null)
                Op(ref res, v != null && InSOI.Equals(v.mainBody.name, StringComparison.InvariantCultureIgnoreCase));

            if (KerbalStatus != null)
                Op(ref res, KerbalStatus.Equals(pcm.rosterStatus.ToString(), StringComparison.InvariantCultureIgnoreCase));

            if (!double.IsNaN(MissionTime))
                Op(ref res, v != null && v.missionTime >= MissionTime);

            if (Gender != null)
            {
                ProtoCrewMember.Gender g = pcm.gender;
                switch (Gender.ToLowerInvariant())
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
                switch (GenderPresent.ToLowerInvariant())
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
                        Core.Log($"Unrecognized value for gender in 'genderPresent = {GenderPresent}'. Assuming 'other'.");
                        goto case "other";
                }
                Op(ref res, v != null && Core.GetCrew(pcm, false).Any(crewmate => crewmate.gender == g && crewmate != pcm));
            }

            if (TraitPresent != null)
                Op(ref res, v != null && Core.GetCrew(pcm, false).Any(crewmate => crewmate.trait.Equals(TraitPresent, StringComparison.OrdinalIgnoreCase) && crewmate != pcm));

            if (ConditionPresent != null)
                Op(ref res, v != null && Core.GetCrew(pcm, false).Any(crewmate => Core.KerbalHealthList[crewmate].HasCondition(ConditionPresent) && crewmate != pcm));

            if (Quirk != null)
                Op(ref res, Core.KerbalHealthList[pcm].HasQuirk(Quirk));

            foreach (Logic l in Operands)
                Op(ref res, l.Test(pcm));

            return (res ?? true) ^ Inverse;
        }

        public string Description(int level)
        {
            StringBuilder res = new StringBuilder();
            string indent1 = "";
            for (int i = 0; i < level; i++)
                indent1 += Padding;
            string indent2 = $"{indent1}{Padding} ";
            if (level > 0)
                indent1 += " ";

            if (Situation != null)
                res.AppendLine($"{indent2}Is {Situation}");
            if (InSOI != null)
                res.AppendLine($"{indent2}Kerbal is in the SOI of {InSOI}");
            if (KerbalStatus != null)
                res.AppendLine($"{indent2}Kerbal is {KerbalStatus}");
            if (!double.IsNaN(MissionTime))
                res.AppendLine($"{indent2}Mission lasts at least {Core.TimeToString(MissionTime, false, 100)}");
            if (Gender != null)
                res.AppendLine($"{indent2}Kerbal is {Gender}");
            if (GenderPresent != null)
                res.AppendLine($"{indent2}{GenderPresent} gender kerbal(s) present in the vessel");
            if (TraitPresent != null)
                res.AppendLine($"{indent2}{TraitPresent} kerbal(s) present in the vessel");
            if (ConditionPresent != null)
                res.AppendLine($"{indent2}Kerbal(s) with {ConditionPresent} present in the vessel");
            if (Quirk != null)
                res.AppendLine($"{indent2}Kerbal has {Core.GetQuirk(Quirk)?.Title ?? "UNKNOWN"} quirk");
                
            foreach (Logic l in Operands)
                res.AppendLine(l.Description(level + 1));
            if (res.ToString().Count(c => c == '\n') > 1)
                res.Insert(0, $"{indent1}{(Operator == OperatorType.And ? (Inverse ? "One" : "All") : (Inverse ? "None" : "One"))} of the following conditions is {((Operator == OperatorType.And) && Inverse ? "false" : "true")}\n");
            else if (Inverse && res.Length != 0)
                res.Insert(0, $"{indent1}This is FALSE:\n");
            return res.ToStringAndRelease();
        }

        public override string ToString() => Description(0);

        bool Op(ref bool? operand1, bool operand2)
        {
            if (operand1 == null)
                return operand2;
            switch (Operator)
            {
                case OperatorType.And:
                    operand1 &= operand2;
                    break;

                case OperatorType.Or:
                    operand1 |= operand2;
                    break;
            }
            return (bool)operand1;
        }
    }
}
