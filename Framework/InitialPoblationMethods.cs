/* InitialPoblationMethods.cs
 * �ltima modificaci�n: 14/04/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.GA_Base;
using DARPTW_GA.Misc;
using DARPTW_GA.Framework.Genomes;
using DARPTW_GA.Framework.Routing;
using DARPTW_GA.DARP;

namespace DARPTW_GA.Framework
{
    /* Clase 'InitialPoblationMethods'
     *  Clase est�tica que contiene m�todos �tiles para la generaci�n de una poblaci�n inicial factible
     * en el algoritmo gen�tico. Los m�todos se aplican como operadores para cada modelo implementado.
     */
    public static class InitialPoblationMethods
    {
        /* M�todo 'RouteGeneration'
         *  Genera una poblaci�n inicial factible para el modelo basado en rutas.
         * 
         *  Par�metros:
         * - 'int pobSize': Tama�o de la poblaci�n que se generar�.
         */
        [Operator( "Generador de pob. inicial para modelo de rutas." )]
        public static List<Genome> RouteGeneration( int pobSize )
        {
            List<Genome> res = new List<Genome>( pobSize );

            for( int i = 0; i < pobSize; i++ )
                res.Add( RouteGenome.GenerateRandomGenome() );

            return res;
        }

        /* M�todo 'LLGAGeneration'
         *  Genera una poblaci�n inicial factible para el modelo LLGA.
         * 
         *  Par�metros:
         * - 'int pobSize': Tama�o de la poblaci�n que se generar�.
         */
        [Operator( "Generador de pob. inicial para modelo LLGA." )]
        public static List<Genome> LLGAGeneration( int pobSize )
        {
            List<Genome> res = new List<Genome>( pobSize );

            for( int i = 0; i < pobSize; i++ )
                res.Add( LLGAGenome.GenerateRandomGenome() );

            return res;
        }
    }
}
