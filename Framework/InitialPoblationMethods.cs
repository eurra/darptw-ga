/* InitialPoblationMethods.cs
 * Última modificación: 14/04/2008
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
     *  Clase estática que contiene métodos útiles para la generación de una población inicial factible
     * en el algoritmo genético. Los métodos se aplican como operadores para cada modelo implementado.
     */
    public static class InitialPoblationMethods
    {
        /* Método 'RouteGeneration'
         *  Genera una población inicial factible para el modelo basado en rutas.
         * 
         *  Parámetros:
         * - 'int pobSize': Tamaño de la población que se generará.
         */
        [Operator( "Generador de pob. inicial para modelo de rutas." )]
        public static List<Genome> RouteGeneration( int pobSize )
        {
            List<Genome> res = new List<Genome>( pobSize );

            for( int i = 0; i < pobSize; i++ )
                res.Add( RouteGenome.GenerateRandomGenome() );

            return res;
        }

        /* Método 'LLGAGeneration'
         *  Genera una población inicial factible para el modelo LLGA.
         * 
         *  Parámetros:
         * - 'int pobSize': Tamaño de la población que se generará.
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
