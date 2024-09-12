using System;
using System.Collections.Generic;

namespace DARPTW_GA.Misc
{
    public class RandomUniqueSelector
    {
        private ulong m_SelectionMask;
        private int m_ActualMax;
        private int m_Max;
        private int m_SelectedCount;

        public bool IsCompleted { get { return ( m_SelectionMask == 0 ); } }
        public int Remaining { get { return m_Max + 1 - m_SelectedCount; } }

        public RandomUniqueSelector( int max )
        {
            m_ActualMax = m_Max = max;
            m_SelectedCount = 0;
            m_SelectionMask = (ulong)Math.Pow( 2, m_ActualMax + 1 ) - 1;
        }

        public bool this[int index]
        {
            get
            {
                if( index > m_ActualMax )
                {
                    return false;
                }
                else if( index == m_ActualMax )
                {
                    return true;
                }                
                else
                {
                    ulong bitMask = (ulong)Math.Pow( 2, index );
                    return ( ( m_SelectionMask & bitMask ) == bitMask );
                }                
            }
        }

        public void ClearValue( int index )
        {
            if( index <= m_Max )
            {
                ulong bitMask = (ulong)Math.Pow( 2, index );

                if( ( m_SelectionMask & bitMask ) == bitMask )
                {
                    m_SelectionMask = m_SelectionMask ^ bitMask;
                    m_SelectedCount--;

                    if( index == m_ActualMax )
                    {
                        do
                        {
                            m_ActualMax--;
                            bitMask = bitMask >> 1;
                        }
                        while( ( m_SelectionMask & bitMask ) == 0 );
                    }
                }
            }
        }

        public int Next()
        {
            if( m_SelectionMask == 0 )
                return -1;

            int randomPos = RandomTool.GetInt( m_ActualMax );
            ulong randomPosBit = (ulong)Math.Pow( 2, randomPos );

            int originalRandomPos = randomPos;
            ulong originalRandomPosBit = randomPosBit;            

            if( ( m_SelectionMask & randomPosBit ) == 0 )
            {
                bool upAndDown = true;
                
                if( ( ( randomPosBit - 1 ) & randomPosBit ) == 0 )
                    upAndDown = false;

                if( upAndDown && RandomTool.GetDouble() > 0.5 )
                {
                    do
                    {
                        randomPos--;
                        randomPosBit = randomPosBit >> 1;
                    }
                    while( ( m_SelectionMask & randomPosBit ) == 0 );
                }
                else
                {
                    do
                    {
                        randomPos++;
                        randomPosBit = randomPosBit << 1;
                    }
                    while( ( m_SelectionMask & randomPosBit ) == 0 );
                }                
            }

            m_SelectionMask ^= randomPosBit;
            m_SelectedCount++;

            if( m_SelectionMask > 0 && randomPosBit > m_SelectionMask )
            {
                ulong actualMaxBit = originalRandomPosBit;
                m_ActualMax = originalRandomPos;
                
                do
                {
                    m_ActualMax--;
                    actualMaxBit = actualMaxBit >> 1;
                }
                while( ( m_SelectionMask & actualMaxBit ) == 0 );
            }

            return randomPos;
        }

        public override string ToString()
        {
            string ret = "";

            ulong checkingMask = (ulong)Math.Pow( 2, m_Max );

            while( checkingMask > 0 )
            {
                if( ( checkingMask & m_SelectionMask ) == 0 )
                    ret += "0";
                else
                    ret += "1";

                checkingMask = checkingMask >> 1;
            }

            return ret;
        }
    }
    
    public static class RandomTool
    {
        public static readonly Random m_Random = new Random();

        public static double GetDouble()
        {
            return m_Random.NextDouble();
        }

        public static int GetInt( int max )
        {
            if( max <= 0 )
                return 0;
            
            return m_Random.Next( max + 1 );
        }

        public static int GetInt( int min, int max )
        {
            if( min >= max )
                return max;
            
            return m_Random.Next( min, max + 1 );
        }

        public static bool[] GetBits( int length )
        {
            bool[] ret = new bool[length];

            for( int i = 0; i < ret.Length; i++ )
                ret[i] = ( GetDouble() > 0.5 );

            return ret;
        }

        public static bool[] GetBits( int length, int maxOnes )
        {
            bool[] ret = new bool[length];

            List<int> randPos = new List<int>( length );

            for( int i = 0; i < length; i++ )
                randPos.Add( i );

            for( int i = 0; i < maxOnes; i++ )
            {
                int select = randPos[GetInt( randPos.Count - 1 )];
                randPos.Remove( select );

                ret[select] = true;
            }

            return ret;
        }
    }
}
