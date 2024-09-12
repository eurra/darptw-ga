/* TournamentSelection.cs
 * Última modificación: 14/04/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.GA_Base;
using DARPTW_GA.Misc;

namespace DARPTW_GA.TournamentSelection
{
    /* Clase 'InitialPoblationMethods'
     *  Clase estática que implementa el mecanismo de selección basado en torneo para el algoritmo genético.
     * permite personalizar el tamaño del torneo.
     * 
     * Atributos:
     * - 'int m_TournamentSize': Tamaño del torneo configurado para la selección.
     */
    public static class TournamentSelection
    {
        private static int m_TournamentSize = 2;

        [ParameterTag( "Tamaño del torneo", "tournsize", 1, Int32.MaxValue, 2 )]
        public static int TournamentSize { get { return m_TournamentSize; } set { m_TournamentSize = value; } }

        /* Método 'Run'
         *  Ejecuta la selección basada en torneo, en base a una lista de cromosomas que representan una
         * población, retornando un individual de la misma.
         * 
         *  Parámetros:
         * - 'int pobSize': Tamaño de la población que se generará.
         */
        [Operator( "Selección basada en Torneo" )]
        public static Genome Run( List<Genome> poblation )
        {
            // Se clona la población original para realizar la selección...
            List<Genome> orig = new List<Genome>( poblation );
            Genome winner = null;

            for( int i = 0; i < m_TournamentSize && orig.Count > 0; i++ )
            {
                // Se selecciona un individual aleatoriamente...
                Genome rand = orig[RandomTool.GetInt( orig.Count - 1 )];
                orig.Remove( rand );

                //  Si el seleccionado es mejor en calidad que el que se tenia como mejor hasta el momento,
                // se convierte en el nuevo ganador del torneo.
                if( winner == null || rand.Fitness < winner.Fitness )
                    winner = rand;
            }

            // En ciertas implementaciones cuando un individuo es seleccionado por este mecanismo, es además
            // eliminado de la población actual, en este caso se mantiene en la población.

            return winner;
        }
    }
}
