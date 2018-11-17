using System.Collections.Generic;
using BlaBlaCarStatisticAnalizer.Models;

namespace BlaBlaCarStatisticAnalizer.Common
{
    public class TripComparer : IEqualityComparer<TripModel>
    {
        public bool Equals(TripModel x, TripModel y)
        {
            return x?.TripId == y?.TripId;
        }

        public int GetHashCode(TripModel obj)
        {
            return obj.TripId.GetHashCode();
        }
    }
}
