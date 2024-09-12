/* GlobalParams.cs
 * Última modificación: 13/03/2008
 */

using System;

namespace DARPTW_GA.DARP
{
    /* Clase 'GlobalParams'
     *  Clase estática que guarda información global y crítica para la ejecución del algoritmo,
     * relativa a los datos de entrada usados en el momento.
     * 
     *  Atributos:
     * - 'int m_VehiclesMaxNumber': Número máximo de vehículos a utilizar para la instancia.
     * - 'int m_ClientNumber': Número de clientes considerados en la instancia.
     * - 'int m_VehicleMaxLoad': Carga máxima de un vehículo que especifica esta instancia.
     * - 'int m_PlanningHorizonLenght': Duración máxima (en min) de la planificación total, que 
     * especifica esta instancia.
     * - 'TimeSpan m_MRT': Tiempo máximo de viaje especificado por la instancia.
     */
    public static class GlobalParams
    {
        private static int m_VehiclesMaxNumber;
        private static int m_ClientNumber;        
        private static int m_VehicleMaxLoad;
        private static int m_PlanningHorizonLenght;
        private static TimeSpan m_MRT = TimeSpan.Zero;

        public static int VehiclesMaxNumber
        {
            get { return m_VehiclesMaxNumber; }
            set { m_VehiclesMaxNumber = value; }
        }

        public static int ClientNumber
        {
            get { return m_ClientNumber; }
            set { m_ClientNumber = value; }
        }

        public static int VehicleMaxLoad
        {
            get { return m_VehicleMaxLoad; }
            set { m_VehicleMaxLoad = value; }
        }

        public static int PlanningHorizonLenght
        {
            get { return m_PlanningHorizonLenght; }
            set { m_PlanningHorizonLenght = value; }
        }

        public static TimeSpan MRT
        {
            get { return m_MRT; }
            set { m_MRT = value; }
        }

        /* Método 'ResetParams'
         *  Reinicializa todos los valores globales de esta clase.
         */ 
        public static void ResetParams()
        {
            m_VehiclesMaxNumber = 0;
            m_ClientNumber = 0;        
            m_VehicleMaxLoad = 0;
            m_PlanningHorizonLenght = 0;
            m_MRT = TimeSpan.Zero;
        }
    }
}
