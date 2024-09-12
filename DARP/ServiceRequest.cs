/* ServiceRequest.cs
 * �ltima modificaci�n: 13/03/2008
 */

using System;
using System.Runtime.Serialization;
using DARPTW_GA.Misc;

namespace DARPTW_GA.DARP
{
    /* Clase 'ServiceRequest'
     *  Corresponde a una representaci�n de una solicitud de servicio que realiza un cliente para ser
     * transportado a una cierta locaci�n, con una ventana de tiempo asociada. La misma puede ser
     * tanto de recogida como de entrega.
     * 
     *  Atributos:
     * - 'int m_ID': Identificador asociado a esta solicitud.
     * - 'int m_ClientID': Identificador del cliente asociado a esta solicitud.
     * - 'TimeSpan m_ET': Extremo inferior de la ventana de tiempo base asociada a esta solicitud.
     * - 'TimeSpan m_LT': Extremo superior de la ventana de tiempo base asociada a esta solicitud.
     * - 'int m_LocationID': Identificador de la locaci�n asociada a esta solicitud.
     * - 'TimeSpan m_ServiceTime': Tiempo de servicio asignado a esta solicitud.
     * - 'int m_LoadChange': Variaci�n de carga en la que incurrir� el veh�culo que sirva esta
     * solicitud. Ser� positiva para recogidas y negativa para entregas.
     */
    [Serializable()]
    public class ServiceRequest
    {
        private int m_ID;
        private int m_ClientID = -1;
        private TimeSpan m_ET;
        private TimeSpan m_LT;
        private int m_LocationID;
        private TimeSpan m_ServiceTime;
        private int m_LoadChange;        

        public int ID { get { return m_ID; } }
        public int ClientID { get { return m_ClientID; } set { m_ClientID = value; } }
        public TimeSpan ET { get { return m_ET; } }
        public TimeSpan LT { get { return m_LT; } }
        public int LocationID { get { return m_LocationID; } }
        public TimeSpan ServiceTime { get { return m_ServiceTime; } }
        public int LoadChange { get { return m_LoadChange; } }

        /* Propiedad 'IsValidTimeWindow' (s�lo lectura)
         *  Retorna un booleano indicando si la ventana base asociada a esta solicitud es v�lida (true)
         * o no (false).
         */ 
        public bool IsValidTimeWindow { get { return ( ( LT - ET ).TotalMinutes < 1440 ); } }

        /* Constructor
         *  Crea una nueva solicitud de servicio en donde los datos de la locaci�n se entregan en base
         * a las coordenadas de la misma.
         * 
         *  Par�metros:
         * - 'int id': Identificador que se asignar� a la solicitud.
         * - 'int et': Tiempo (min.) del extremo inferior de la ventana que se asignar� a la solicitud.
         * - 'int lt': Tiempo (min.) del extremo superior de la ventana que se asignar� a la solicitud.
         * - 'double locx': Coordenada x de la locaci�n que se asignar� a la solicitud.
         * - 'double locy': Coordenada y de la locaci�n que se asignar� a la solicitud.
         * - 'int stime': Tiempo de servicio (min.) que se asignar� a la solicitud.
         * - 'int lchange': Diferencia de carga que se asignar� a la solicitud.
         */
        public ServiceRequest( int id, int et, int lt, double locx, double locy, int stime, int lchange )
            : this( id, et, lt, Locations.GetNodeID( locx, locy ), stime, lchange )
        {
        }

        /* Constructor
         *  Crea una nueva solicitud de servicio en donde los datos de la locaci�n se entregan en base
         * a su identificador.
         * 
         *  Par�metros:
         * - 'int id': Identificador que se asignar� a la solicitud.
         * - 'int et': Tiempo (min.) del extremo inferior de la ventana que se asignar� a la solicitud.
         * - 'int loc': Identificador de la locaci�n que se asignar� a la solicitud.
         * - 'double locx': Coordenada x de la locaci�n que se asignar� a la solicitud.
         * - 'int stime': Tiempo de servicio (min.) que se asignar� a la solicitud.
         * - 'int lchange': Diferencia de carga que se asignar� a la solicitud.
         */
        public ServiceRequest( int id, int et, int lt, int loc, int stime, int lchange )
        {
            m_ID = id;
            m_ET = TimeSpan.FromMinutes( et );
            m_LT = TimeSpan.FromMinutes( lt );
            m_LocationID = loc;
            m_ServiceTime = TimeSpan.FromMinutes( stime );
            m_LoadChange = lchange;
        }

        /* M�todo 'UpdateTimesFromDelivery'
         *  Cuando el cliente asociado a esta solicitud es "de entrada", s�lo especifica su ventana
         * de entrega, por lo que la de recogida no esta definida. Este m�todo calcula esta �ltima
         * ventana en base a los datos globales del problema y a la ventana de entrega que se
         * especifique.
         * 
         *  Par�metros:
         * - 'ServiceRequest delivery': Servicio de entrega en base al que se calcular� la ventana.
         */
        public void UpdateTimesFromDelivery( ServiceRequest delivery )
        {  
            m_ET = TimeComparer.Max( TimeSpan.Zero, delivery.ET - GlobalParams.MRT - m_ServiceTime );
            m_LT = TimeComparer.Min( delivery.LT - TimeSpan.FromMinutes( Locations.DRTS[delivery.LocationID, m_LocationID] ) - m_ServiceTime, TimeSpan.FromMinutes( GlobalParams.PlanningHorizonLenght ) );
        }

        /* M�todo 'UpdateTimesFromPickup'
         *  Cuando el cliente asociado a esta solicitud es "de salida", s�lo especifica su ventana
         * de recogida, por lo que la de entrega no esta definida. Este m�todo calcula esta �ltima
         * ventana en base a los datos globales del problema y a la ventana de recogida que se
         * especifique.
         * 
         *  Par�metros:
         * - 'ServiceRequest pickup': Servicio de recogida en base al que se calcular� la ventana.
         */
        public void UpdateTimesFromPickup( ServiceRequest pickup )
        {
            m_ET = TimeComparer.Max( TimeSpan.Zero, pickup.ET + pickup.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[pickup.LocationID, m_LocationID] ) );
            m_LT = TimeComparer.Min( pickup.LT + pickup.ServiceTime + GlobalParams.MRT, TimeSpan.FromMinutes( GlobalParams.PlanningHorizonLenght ) );
        }

        /* M�todo 'ToString' (sobrecargado de 'Object')
         *  Retorna una representaci�n en string de la solicitud actual, con el formato "<id_cliente>+"
         * para una solicitud de recogida o "<id_cliente>-" para una solicitud de entrega.
         */
        public override string ToString()
        {
            return ( m_ClientID == -1 ? "N/A" : m_ClientID.ToString() + ( m_LoadChange < 0 ? "-" : "+" ) );
        }
    }
}
