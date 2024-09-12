/* Locations.cs
 * �ltima modificaci�n: 13/03/2008
 */

using System;
using System.Collections.Generic;

namespace DARPTW_GA.DARP
{
    /* Clase 'Locations'
     *  Clase est�tica que almacena toda la informaci�n relativa a las locaciones, que se ha obtenido
     * de los datos de entrada. Se almacenan tanto las coordenadas de los nodos, como las distancias
     * entre ellos. Internamente contiene una lista de pares de coordenadas que son asumidos como los
     * nodos, en donde la posici�n de un nodo en esta lista es equivalente a su identificador.
     * 
     *  Atributos:
     * - 'int DefaultID': Entero est�tico que indica el valor de la locaci�n por defecto, al que es
     * usada para identificar nodos invalidos, no asignados o similares.
     * - 'List<double[]> m_NodeList': La lista interna que almacena los nodos como pares de 
     * coordenadas.
     * - 'double[,] m_DRTs': Arreglo bidimensional que almacena en forma de matriz, las distancias
     * entre cada par de nodos ingresados en la lista. Las coordenadas de la matriz hacen referencia
     * a las posiciones de los nodos en la lista.
     */
    public static class Locations
    {
        public static readonly int DefaultID = -1;
        private static List<double[]> m_NodeList = new List<double[]>();
        private static double[,] m_DRTs;

        public static double[,] DRTS { get { return m_DRTs; } }

        /* Propiedad 'DepotID' (s�lo lectura)
         *  Retorna un entero indicando el identificador asociado a los dep�sitos.
         */ 
        public static int DepotID { get { return 0; } }

        /* Propiedad 'Count' (s�lo lectura)
         *  Retorna un entero indicando la cantidad de nodos almacenada internamente.
         */ 
        public static int Count { get { return m_NodeList.Count; } }

        /* M�todo 'GetNodeID'
         *  Retorna el identificador de una locaci�n acorde a sus coordenadas. Adicionalmente
         * permite agregar en forma directa un nodo si es que las coordenadas especificadas no han
         * sido ya ingresadas. Este mecanismo permite reutilizar nodos cuando las coordenadas se
         * repiten.
         * 
         *  Par�metros:
         * - 'double locx': Coordenada x del nodo a obtener/agregar.
         * - 'double locy': Coordenada y del nodo a obtener/agregar.
         */
        public static int GetNodeID( double locx, double locy )
        {
            for( int i = 0; i < m_NodeList.Count; i++ )
            {
                double[] coords = m_NodeList[i];

                if( coords[0] == locx && coords[1] == locy )
                    return i;
            }

            double[] newNode = new double[] { locx, locy };
            m_NodeList.Add( newNode );

            return m_NodeList.Count - 1;
        }

        /* M�todo 'ClearInfo'
         *  Limpia la lista de nodos y la informaci�n vinculada a las distancias entre ellos.
         */ 
        public static void ClearInfo()
        {
            m_NodeList.Clear();
            m_DRTs = null;
        }

        /* M�todo 'GetXPos'
         *  Retorna el valor de la coordenada x de un nodo en base a su identificador. Si este
         * �ltimo es inv�lido, retorna NaN.
         * 
         *  Par�metros:
         * - 'int nodeID': Identificador del nodo cuya coordenada x se retornar�.
         */
        public static double GetXPos( int nodeID )
        {
            if( m_NodeList.Count - 1 < nodeID )
                return double.NaN;

            return m_NodeList[nodeID][0];
        }

        /* M�todo 'GetYPos'
         *  Retorna el valor de la coordenada y de un nodo en base a su identificador. Si este
         * �ltimo es inv�lido, retorna NaN.
         * 
         *  Par�metros:
         * - 'int nodeID': Identificador del nodo cuya coordenada y se retornar�.
         */
        public static double GetYPos( int nodeID )
        {
            if( m_NodeList.Count - 1 < nodeID )
                return double.NaN;

            return m_NodeList[nodeID][1];
        }

        /* M�todo 'UpdateDRTs'
         *  En base a la lista de nodos que se tenga actualmente, este m�todo construye la matriz de
         * distancias entre todos ellos. Se realiza por un par de ciclos anidados, calculando la mitad
         * de los valores de la matriz y replicandolos en la otra mitad en la posici�n que corresponda.
         * Para la diagonal principal de la matriz, se deja el valor 0 (distancia nula).
         */
        public static void UpdateDRTs()
        {            
            m_DRTs = new double[m_NodeList.Count, m_NodeList.Count];

            for( int i = 0; i < m_NodeList.Count; i++ )
            {
                for( int j = i; j < m_NodeList.Count; j++ )
                {
                    if( j == i )
                    {
                        m_DRTs[i, j] = 0.0;
                    }
                    else
                    {
                        double dist = m_DRTs[i, j] = m_DRTs[j, i] = CalculateDist( m_NodeList[i][0], m_NodeList[j][0], m_NodeList[i][1], m_NodeList[j][1] );
                    }
                }
            }
        }

        /* M�todo 'CalculateDist'
         *  M�todo auxiliar que retorna la distancia euclidiana entre dos nodos, en base a sus
         * coordenadas.
         * 
         *  Par�metros:
         * - 'double x1': Coordenada x del nodo 1.
         * - 'double y1': Coordenada y del nodo 1.
         * - 'double x2': Coordenada x del nodo 2.
         * - 'double y2': Coordenada y del nodo 2.
         */
        private static double CalculateDist( double x1, double y1, double x2, double y2 )
        {
            return Math.Sqrt( Math.Pow( x2 - x1, 2.0 ) + Math.Pow( y2 - y1, 2.0 ) ); 
        }
    }
}
