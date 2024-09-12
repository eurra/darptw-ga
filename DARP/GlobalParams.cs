/* GlobalParams.cs
 * �ltima modificaci�n: 13/03/2008
 */

using System;

namespace DARPTW_GA.DARP
{
    /* Clase 'GlobalParams'
     *  Clase est�tica que guarda informaci�n global y cr�tica para la ejecuci�n del algoritmo,
     * relativa a los datos de entrada usados en el momento.
     * 
     *  Atributos:
     * - 'int m_VehiclesMaxNumber': N�mero m�ximo de veh�culos a utilizar para la instancia.
     * - 'int m_ClientNumber': N�mero de clientes considerados en la instancia.
     * - 'int m_VehicleMaxLoad': Carga m�xima de un veh�culo que especifica esta instancia.
     * - 'int m_PlanningHorizonLenght': Duraci�n m�xima (en min) de la planificaci�n total, que 
     * especifica esta instancia.
     * - 'TimeSpan m_MRT': Tiempo m�ximo de viaje especificado por la instancia.
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

        /* M�todo 'ResetParams'
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
