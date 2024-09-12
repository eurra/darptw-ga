/* TournamentSelection.cs
 * �ltima modificaci�n: 14/04/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.GA_Base;
using DARPTW_GA.Misc;

namespace DARPTW_GA.TournamentSelection
{
    /* Clase 'InitialPoblationMethods'
     *  Clase est�tica que implementa el mecanismo de selecci�n basado en torneo para el algoritmo gen�tico.
     * permite personalizar el tama�o del torneo.
     * 
     * Atributos:
     * - 'int m_TournamentSize': Tama�o del torneo configurado para la selecci�n.
     */
    public static class TournamentSelection
    {
        private static int m_TournamentSize = 2;

        [ParameterTag( "Tama�o del torneo", "tournsize", 1, Int32.MaxValue, 2 )]
        public static int TournamentSize { get { return m_TournamentSize; } set { m_TournamentSize = value; } }

        /* M�todo 'Run'
         *  Ejecuta la selecci�n basada en torneo, en base a una lista de cromosomas que representan una
         * poblaci�n, retornando un individual de la misma.
         * 
         *  Par�metros:
         * - 'int pobSize': Tama�o de la poblaci�n que se generar�.
         */
        [Operator( "Selecci�n basada en Torneo" )]
        public static Genome Run( List<Genome> poblation )
        {
            // Se clona la poblaci�n original para realizar la selecci�n...
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

            // En ciertas implementaciones cuando un individuo es seleccionado por este mecanismo, es adem�s
            // eliminado de la poblaci�n actual, en este caso se mantiene en la poblaci�n.

            return winner;
        }
    }
}
