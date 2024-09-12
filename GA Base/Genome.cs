using System;
using System.Collections.Generic;
using DARPTW_GA.Framework;

namespace DARPTW_GA.GA_Base
{
    public interface Genome
    {
        double Fitness { get; }
        Evaluation Evaluation { get; }
        string GetStringRoutes();
        string GetStringMasks();
    }

    public sealed class GenomeComparer : IComparer<Genome>
    {
        public static readonly GenomeComparer Instance = new GenomeComparer();
        
        public GenomeComparer()
        {
        }

        public int Compare( Genome x, Genome y )
        {
            if( x.Fitness > y.Fitness )
                return 1;
            else if( x.Fitness == y.Fitness )
                return 0;
            else
                return -1;
        }
    }
}
