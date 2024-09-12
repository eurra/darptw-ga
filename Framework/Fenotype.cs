/* Fenotype.cs
 * Última modificación: 11/04/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.DARP;

namespace DARPTW_GA.Framework
{
    /* Clase 'Evaluation'
     *  Clase que permite almacenar la información relativa a la evaluación de una solución, esto incluye
     * tiempos de viaje, espera de clientes, entre otros factores importantes del problema. También entrega
     * propiedades públicas y estáticas para la configuración de los parámetros importantes del problema.
     * 
     *  Atributos:
     * - 'double m_TravelTimeFactor': Factor estático que describe el peso (importancia) del tiempo de viaje
     * de los vehículos, en la función objetivo.
     * - 'double m_SlackTimeFactor': Factor estático que describe el peso (importancia) de la duración de 
     * los slacks, en la función objetivo.
     * - 'double m_VQFactor': Factor estático que describe el peso (importancia) de la cantidad de vehículos
     * de la solución, en la función objetivo.
     * - 'double m_ExcessRideTimeFactor': Factor estático que describe el peso (importancia) del exceso en el
     * tiempo de viaje de los clientes de la solución, en la función objetivo.
     * - 'double m_WaitTimeFactor': Factor estático que describe el peso (importancia) del tiempo de espera 
     * de los clientes de la solución, en la función objetivo.
     * - 'double m_RideTimeFactor': Factor estático que describe el peso (importancia) del tiempo de viaje 
     * de los clientes de la solución, en la función objetivo.
     * - 'double m_TravelTime': Tiempo de viaje de los vehículos almacenado de la solución.
     * - 'double m_SlackTime': Tiempo de duración de los slacks en la ruta almacenado de la solución.
     * - 'double m_VQ': Cantidad de vehículos almacenado de la solución.
     * - 'double m_ExcessRideTime': Exceso en el tiempo de viaje de los clientes almacenado de la solución.
     * - 'double m_WaitTime': Tiempo de espera de los clientes almacenado de la solución.
     * - 'double m_RideTime': Tiempo de viaje de los clientes almacenado de la solución.
     * - 'double m_Result': Resultado de la evaluación total de los parámetros de la solución, de acuerdo
     * a la función objetivo.
     */
    public class Evaluation
    {
        private static double m_TravelTimeFactor = 1;
        private static double m_SlackTimeFactor = 1;
        private static double m_VQFactor = 0;
        private static double m_ExcessRideTimeFactor = 0;
        private static double m_WaitTimeFactor = 0;
        private static double m_RideTimeFactor = 0;

        [ParameterTag( "Factor del tiempo de duración de las rutas", "ttimefactor", 0.0, Double.MaxValue, 1 )]
        public static double TravelTimeFactor { get { return m_TravelTimeFactor; } set { m_TravelTimeFactor = value; } }

        [ParameterTag( "Factor del tiempo de slacks", "stimefactor", 0.0, Double.MaxValue, 1 )]
        public static double SlackTimeFactor { get { return m_SlackTimeFactor; } set { m_SlackTimeFactor = value; } }

        [ParameterTag( "Factor de la cantidad de vehículos", "vqfactor", 0.0, Double.MaxValue, 1 )]
        public static double VQFactor { get { return m_VQFactor; } set { m_VQFactor = value; } }

        [ParameterTag( "Factor del exceso en el tiempo de transporte de clientes", "ertfactor", 0.0, Double.MaxValue, 1 )]
        public static double ExcessRideTimeFactor { get { return m_ExcessRideTimeFactor; } set { m_ExcessRideTimeFactor = value; } }

        [ParameterTag( "Factor del tiempo de espera de clientes", "wtimefactor", 0.0, Double.MaxValue, 1 )]
        public static double WaitTimeFactor { get { return m_WaitTimeFactor; } set { m_WaitTimeFactor = value; } }

        [ParameterTag( "Factor del tiempo de viaje de clientes", "rtimefactor", 0.0, Double.MaxValue, 1 )]
        public static double RideTimeFactor { get { return m_RideTimeFactor; } set { m_RideTimeFactor = value; } }

        private double m_TravelTime;
        private double m_SlackTime;
        private int m_VQ;
        private double m_ExcessRideTime;
        private double m_WaitTime;
        private double m_RideTime;
        private double m_Result;

        private TimeSpan m_Time;

        public double TravelTime { get { return m_TravelTime; } }
        public double SlackTime { get { return m_SlackTime; } }
        public int VQ { get { return m_VQ; } }
        public double ExcessRideTime { get { return m_ExcessRideTime; } }
        public double WaitTime { get { return m_WaitTime; } }
        public double RideTime { get { return m_RideTime; } }
        public double Result { get { return m_Result; } }

        public TimeSpan Time
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        /* Constructor
         *  Genera una nueva instancia de evaluación acorde a un arreglo de rutas que se asignen.
         * 
         *  Parámetros:
         * - 'Route[] routes': Arreglo de rutas que se usará como base para construir la evaluación.
         */
        public Evaluation( Route[] routes )
        {
            for( int i = 0; i < routes.Length; i++ )
            {
                // Se descartan las rutas vacías, considerandose como vehículos no utilizados...
                if( routes[i] != null && !routes[i].IsVoidRoute )
                {
                    m_VQ++;

                    // Eventualmente, la ruta que se revisa en el momento puede no estar evaluada...
                    if( !routes[i].IsEvaluated )
                        routes[i].EvaluateRoute();

                    //  Se suman los resultados individuales de la ruta a los resultados generales de la
                    // solución...
                    m_TravelTime += routes[i].TravelTime;
                    m_SlackTime += routes[i].SlackTime;
                    m_ExcessRideTime += routes[i].ExcessRideTime;
                    m_WaitTime += routes[i].WaitTime;
                    m_RideTime += routes[i].RideTime;
                }
            }

            // Finalmente, se calcula el resultado acorde a la función objetivo.
            m_Result = m_TravelTime * m_TravelTimeFactor + m_SlackTime * m_SlackTimeFactor + m_VQ * m_VQFactor + m_ExcessRideTime * m_ExcessRideTimeFactor + m_WaitTime * m_WaitTimeFactor + m_RideTime * m_RideTimeFactor;
        }
    }
}
