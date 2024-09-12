/* Event.cs
 * Última modificación: 20/03/2008
 */

using System;
using System.Runtime.Serialization;

namespace DARPTW_GA.DARP
{
    /* Enum 'EventType'
     *  Describe los tipos de eventos que se pueden definir:
     * - 'Pickup': Evento de recogida.
     * - 'Delivery': Evento de entrega.
     * - 'StartDepot': Inicio del recorrido del vehículo.
     * - 'StopDepot': Fin del recorrido del vehículo.
     */
    public enum EventType
    {
        Pickup,
        Delivery,
        StartDepot,
        StopDepot
    }

    /* Clase 'Event'
     *  Representa un evento en el contexto de la ruta que realiza un vehículo dentro de la
     * programación que se le asigna. Encapsula una solicitud de servicio desde la que se obtienen
     * datos generales, como los extremos de la ventana base asociada al evento. Adicionalmente
     * contiene elementos y propiedades útiles para describir la programación del evento dentro 
     * de una ruta.
     *  Como una ruta se implementa a modo de lista doblemente enlazada, de forma individual un 
     * evento se considera como parte de ella, por lo que contiene referencias a un eventual evento
     * posterior y uno anterior.
     * 
     *  Atributos:
     * - 'Event m_Next': Referencia al siguiente evento en el contexto de una ruta.
     * - 'Event m_Previous': Referencia al evento previo en el contexto de una ruta.
     * - 'EventType m_Etype': Tipo del evento.
     * - 'TimeSpan m_AT': Tiempo actual asignado al evento en el contexto de una ruta.
     * - 'TimeSpan m_FET': Tiempo del extremo inferior de la ventana factible relativa al evento.
     * - 'TimeSpan m_FLT': Tiempo del extremo superior de la ventana factible relativa al evento.
     * - 'TimeSpan m_BET': Tiempo del extremo inferior de la ventana acumulada hacia adelante.
     * - 'TimeSpan m_BLT': Tiempo del extremo superior de la ventana acumulada hacia adelante.
     * - 'bool m_HasSlack': Booleano que indica si posterior a este evento en una ruta, existe un 
     * slack. Esto permite prescindir de otro objeto de evento que cumpla el rol de slack.
     * - 'int m_InternalLocation': Entero que se utiliza cuando no existe una solicitud de
     * servicio asociada a este evento, esto para los casos de inicio y fin de la ruta (depots).
     * - 'int m_acumLoad': Entero que describe el estado de la carga de pasajeros en el evento,
     * dentro del contexto de la ruta.
     * - 'ServiceRequest m_Request': Referencia a la solicitud de servicio asociada a este evento.
     */
    public class Event : ICloneable
    {
        private Event m_Next;
        private Event m_Previous;
        
        private EventType m_Etype;

        private TimeSpan m_AT;
        private TimeSpan m_FET;
        private TimeSpan m_FLT;
        private TimeSpan m_BET;
        private TimeSpan m_BLT;

        private bool m_HasSlack;
       
        private int m_InternalLocation;
        private int m_acumLoad;
        private ServiceRequest m_Request;

        public Event Next { get { return m_Next; } set { m_Next = value; } }
        public Event Previous { get { return m_Previous; } set { m_Previous = value; } }

        public EventType EType { get { return m_Etype; } }
        
        public TimeSpan AT{ get { return m_AT; } set { m_AT = value; } }
        public TimeSpan FET{ get { return m_FET; } set { m_FET = value; } }
        public TimeSpan FLT{ get { return m_FLT; } set { m_FLT = value; } }
        public TimeSpan BET{ get { return m_BET; } set { m_BET = value; } }
        public TimeSpan BLT{ get { return m_BLT; } set { m_BLT = value; } }

        public bool HasSlack{ get { return m_HasSlack; } set { m_HasSlack = value; } }

        public int AcumulatedLoad { get { return m_acumLoad; } set { m_acumLoad = value; } }

        /* Propiedad 'Location' (sólo lectura)
         *  Retorna un entero indicando el id de la locación asociada a este evento. Cuando se
         * trata de eventos de recogida y entrega, se usa la información de la locación de la
         * solicitud de servicio asociada, en caso de tratarse de un depósito, se usa la variable
         * 'm_InternalLocation'.
         */
        public int Location
        {
            get
            {
                if( m_Etype == EventType.Delivery || m_Etype == EventType.Pickup )
                    return ( m_Request != null ? m_Request.LocationID : Locations.DefaultID );
                else
                    return m_InternalLocation;
            }
        }

        //  Las siguientes propiedades retornan sus valores desde el objeto asociado a la solicitud
        // de servicio asociada a este evento, si es que existe. Para mayores detalles, consultar
        // ServiceRequest.cs
        public int ServiceID { get { return ( m_Request == null ? -1 : m_Request.ID ); } }
        public int ClientID { get { return ( m_Request == null ? -1 : m_Request.ClientID ); } }
        public TimeSpan ET { get { return ( m_Request == null ? TimeSpan.Zero : m_Request.ET ); } }
        public TimeSpan LT { get { return ( m_Request == null ? TimeSpan.FromMinutes( GlobalParams.PlanningHorizonLenght ) : m_Request.LT ); } }
        public TimeSpan ServiceTime { get { return ( m_Request == null ? TimeSpan.Zero : m_Request.ServiceTime ); } }
        public int LoadChange{ get{ return ( m_Request == null ? 0 : m_Request.LoadChange ); } }


        /* Constructor
         *  Crea un nuevo evento basandose en una solicitud de servicio base. Esta enfocado para
         * instanciar eventos de recogida y de entrega.
         * 
         *  Parámetros:
         * - 'ServiceRequest request': El objeto de la solicitud de servicio usada.
         */
        public Event( ServiceRequest request )
        {
            m_Request = request;

            if( LoadChange < 0 )
                m_Etype = EventType.Delivery;
            else
                m_Etype = EventType.Pickup;
        }

        /* Constructor
         *  Crea un nuevo evento basandose en las coordenadas de la locación del nodo asociado al
         * evento. Esta enfocado para instanciar eventos de partida y de fin de la ruta (depots).
         * 
         *  Parámetros:
         * - 'double locx': Coordenada x de la locación utilizada.
         * - 'double locy': Coordenada y de la locación utilizada.
         * - 'bool start': Booleano que indica si el evento a crear es el inicio de la ruta (true)
         *  o el final de ella (false).
         */
        public Event( double locx, double locy, bool start )
            : this( Locations.GetNodeID( locx, locy ), start )
        {
        }

        /* Constructor
         *  Crea un nuevo evento basandose en el identificador de la locación del nodo asociado al
         * evento. Esta enfocado para instanciar eventos de partida y de fin de la ruta (depots).
         * 
         *  Parámetros:
         * - 'int location': Identificador de la locación utilizada.
         * - 'bool start': Booleano que indica si el evento a crear es el inicio de la ruta (true)
         *  o el final de ella (false).
         */
        public Event( int location, bool start )
        {
            m_InternalLocation = location;

            if( start )
                m_Etype = EventType.StartDepot;
            else
                m_Etype = EventType.StopDepot;
        }

        /* Método 'Clone' (implementado de ICloneable)
         *  Crea una copia de este evento y la retorna como tipo object. Como el único objeto interno
         * que guarda esta clase es la solicitud de servicio, la misma al ser una referencia global no
         * es necesario copiarla también (por ello se usa 'MemberwiseClone').
         */
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /* Propiedad ChainedEventsForward (sólo lectura)
         *  Retorna un string que describe la sucesión de eventos posterior a éste, incluyéndolo.
         * tiene utilidades de debugging y funciona de forma recursiva. El string retornado es del
         * estilo "<evento_actual> <evento_siguiente_1> <evento_siguiente 2> ...".
         */ 
        public string ChainedEventsForward { get { return ToString() + ( Next != null ? " " + Next.ChainedEventsForward : "" ); } }

        /* Propiedad ChainedEventsBackwards (sólo lectura)
         *  Retorna un string que describe la sucesión de eventos posterior a éste, incluyéndolo.
         * tiene utilidades de debugging y funciona de forma recursiva. El string retornado es del
         * estilo "... <evento_anterior_2> <evento_anterior_1> <evento_actual>".
         */
        public string ChainedEventsBackwards { get { return ( Previous != null ? Previous.ChainedEventsBackwards + " " : "" ) + ToString(); } }

        /* Método 'ToString' (sobrecargado de 'Object')
         *  Retorna una representación de string del evento actual.
         */ 
        public override string ToString()
        {
            if( m_Etype == EventType.StartDepot )
                return "<D0>";
            else if( m_Etype == EventType.StopDepot )
                return "<DN>";
            else          
                return m_Request.ToString();
        }
    }    
}
