using System;
using DARPTW_GA.DARP;

namespace DARPTW_GA.Misc
{
    public static class TimeComparer
    {
        public static TimeSpan Max( TimeSpan a, TimeSpan b )
        {
            if( a.CompareTo( b ) < 0 )
                return b;
            else
                return a;
        }

        public static TimeSpan Min( TimeSpan a, TimeSpan b )
        {
            if( a.CompareTo( b ) >= 0 )
                return b;
            else
                return a;
        }
    }
}
