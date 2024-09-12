/* Fenotype.cs
 * �ltima modificaci�n: 11/04/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.DARP;

namespace DARPTW_GA.Framework
{
    /* Clase 'Evaluation'
     *  Clase que permite almacenar la informaci�n relativa a la evaluaci�n de una soluci�n, esto incluye
     * tiempos de viaje, espera de clientes, entre otros factores importantes del problema. Tambi�n entrega
     * propiedades p�blicas y est�ticas para la configuraci�n de los par�metros importantes del problema.
     * 
     *  Atributos:
     * - 'double m_TravelTimeFactor': Factor est�tico que describe el peso (importancia) del tiempo de viaje
     * de los veh�culos, en la funci�n objetivo.
     * - 'double m_SlackTimeFactor': Factor est�tico que describe el peso (importancia) de la duraci�n de 
     * los slacks, en la funci�n objetivo.
     * - 'double m_VQFactor': Factor est�tico que describe el peso (importancia) de la cantidad de veh�culos
     * de la soluci�n, en la funci�n objetivo.
     * - 'double m_ExcessRideTimeFactor': Factor est�tico que describe el peso (importancia) del exceso en el
     * tiempo de viaje de los clientes de la soluci�n, en la funci�n objetivo.
     * - 'double m_WaitTimeFactor': Factor est�tico que describe el peso (importancia) del tiempo de espera 
     * de los clientes de la soluci�n, en la funci�n objetivo.
     * - 'double m_RideTimeFactor': Factor est�tico que describe el peso (importancia) del tiempo de viaje 
     * de los clientes de la soluci�n, en la funci�n objetivo.
     * - 'double m_TravelTime': Tiempo de viaje de los veh�culos almacenado de la soluci�n.
     * - 'double m_SlackTime': Tiempo de duraci�n de los slacks en la ruta almacenado de la soluci�n.
     * - 'double m_VQ': Cantidad de veh�culos almacenado de la soluci�n.
     * - 'double m_ExcessRideTime': Exceso en el tiempo de viaje de los clientes almacenado de la soluci�n.
     * - 'double m_WaitTime': Tiempo de espera de los clientes almacenado de la soluci�n.
     * - 'double m_RideTime': Tiempo de viaje de los clientes almacenado de la soluci�n.
     * - 'double m_Result': Resultado de la evaluaci�n total de los par�metros de la soluci�n, de acuerdo
     * a la funci�n objetivo.
     */
    public class Evaluation
    {
        private static double m_TravelTimeFactor = 1;
        private static double m_SlackTimeFactor = 1;
        private static double m_VQFactor = 0;
        private static double m_ExcessRideTimeFactor = 0;
        private static double m_WaitTimeFactor = 0;
        private static double m_RideTimeFactor = 0;

        [ParameterTag( "Factor del tiempo de duraci�n de las rutas", "ttimefactor", 0.0, Double.MaxValue, 1 )]
        public static double TravelTimeFactor { get { return m_TravelTimeFactor; } set { m_TravelTimeFactor = value; } }

        [ParameterTag( "Factor del tiempo de slacks", "stimefactor", 0.0, Double.MaxValue, 1 )]
        public static double SlackTimeFactor { get { return m_SlackTimeFactor; } set { m_SlackTimeFactor = value; } }

        [ParameterTag( "Factor de la cantidad de veh�culos", "vqfactor", 0.0, Double.MaxValue, 1 )]
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
         *  Genera una nueva instancia de evaluaci�n acorde a un arreglo de rutas que se asignen.
         * 
         *  Par�metros:
         * - 'Route[] routes': Arreglo de rutas que se usar� como base para construir la evaluaci�n.
         */
        public Evaluation( Route[] routes )
        {
            for( int i = 0; i < routes.Length; i++ )
            {
                // Se descartan las rutas vac�as, considerandose como veh�culos no utilizados...
                if( routes[i] != null && !routes[i].IsVoidRoute )
                {
                    m_VQ++;

                    // Eventualmente, la ruta que se revisa en el momento puede no estar evaluada...
                    if( !routes[i].IsEvaluated )
                        routes[i].EvaluateRoute();

                    //  Se suman los resultados individuales de la ruta a los resultados generales de la
                    // soluci�n...
                    m_TravelTime += routes[i].TravelTime;
                    m_SlackTime += routes[i].SlackTime;
                    m_ExcessRideTime += routes[i].ExcessRideTime;
                    m_WaitTime += routes[i].WaitTime;
                    m_RideTime += routes[i].RideTime;
                }
            }

            // Finalmente, se calcula el resultado acorde a la funci�n objetivo.
            m_Result = m_TravelTime * m_TravelTimeFactor + m_SlackTime * m_SlackTimeFactor + m_VQ * m_VQFactor + m_ExcessRideTime * m_ExcessRideTimeFactor + m_WaitTime * m_WaitTimeFactor + m_RideTime * m_RideTimeFactor;
        }
    }
}
