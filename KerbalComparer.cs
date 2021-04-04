using System.Collections.Generic;

namespace KerbalHealth
{
    /// <summary>
    /// Class used for ordering vessels in Health Monitor
    /// </summary>
    public class KerbalComparer : Comparer<ProtoCrewMember>
    {
        readonly bool sortByLocation;

        public KerbalComparer(bool sortByLocation) => this.sortByLocation = sortByLocation;

        public static int CompareLocation(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (x.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                return y.rosterStatus == ProtoCrewMember.RosterStatus.Assigned ? 1 : 0;
            if (y.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                return -1;
            Vessel xv = x.GetVessel(), yv = y.GetVessel();
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (xv.isActiveVessel)
                    return yv.isActiveVessel ? 0 : -1;
                if (yv.isActiveVessel)
                    return 1;
            }
            if (xv.isEVA)
                return yv.isEVA ? 0 : -1;
            return yv.isEVA ? 1 : string.Compare(xv.vesselName, yv.vesselName, true);
        }

        public override int Compare(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (sortByLocation)
            {
                int l = CompareLocation(x, y);
                return (l != 0) ? l : string.Compare(x.name, y.name, true);
            }
            return string.Compare(x.name, y.name, true);
        }
    }
}
