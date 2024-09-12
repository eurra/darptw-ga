/* Route.cs
 * Última modificación: 13/03/2008
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DARPTW_GA.Misc;
using DARPTW_GA.Framework;

namespace DARPTW_GA.DARP
{
    /* Clase 'RouteComparer' (implementada desde IComparer<>)
     *  Clase útil para la priorización/ordenamiento de rutas.
     * 
     *  Atributos:
     * - 'RouteComparer Instance': Objeto estático, usado como instancia general de esta misma clase. 
     */
    public class RouteComparer : IComparer<Route>
    {
        public static readonly RouteComparer Instance = new RouteComparer();

        public RouteComparer()
        {
        }

        // La comparación de las rutas se hace en base a el número de clientes que contienen.
        public int Compare( Route x, Route y )
        {
            if( x.ClientCount > y.ClientCount )            
                return 1;            
            else if( x.ClientCount < y.ClientCount )            
                return -1;            
            else
                return 0;
            
        }
    }

    /* Clase 'Route'
     *  Corresponde a la implementación de una lista doblemente enlazada, que representa una sucesión
     * de eventos asociada al recorrido que realizará un vehículo para transportar pasajeros. Esta
     * clase agrupa de forma ordenada los objetos 'Event' que describen los eventos respectivos. 
     * Adicionalmente guarda información general de la ruta, como son tiempos, clientes contenidos,
     * entre otros. Parte de esta información debe ser generada cuando la ruta ya esta construida,
     * en base a parámetros generales que hayan sido especificados para el problema.
     * 
     *  Atributos:
     * - 'Event m_First': Referencia al primer evento en la sucesión de la ruta.
     * - 'Event m_Last': Referencia al último evento en la sucesión de la ruta.
     * - 'double m_TravelTime': Tiempo de viaje total del vehículo en la ruta.
     * - 'double m_SlackTime': Tiempo total de slacks en la ruta.
     * - 'double m_ExcessRideTime': Exceso de tiempo total de los clientes en la ruta.
     * - 'double m_WaitTime': Tiempo de espera total de los clientes en la ruta.
     * - 'double m_RideTime': Tiempo de viaje total de todos los clientes en la ruta.
     * - 'List<int> m_Clients': Lista de clientes asociados a la ruta.
     * - 'List<Event> m_SwapablePoints': Lista con las referencias a los eventos que pueden
     * ser evaluados para un posible intercambio de posición respecto a su evento siguiente.
     * - 'bool m_IsEvaluated': Booleano que indica si la ruta esta en condiciones de ser evaluada
     * (valor false).
     */
    public class Route : ICloneable
    {
        private Event m_First;
        private Event m_Last;
        
        private double m_TravelTime;
        private double m_SlackTime;
        private double m_ExcessRideTime;
        private double m_WaitTime;
        private double m_RideTime;

        private List<int> m_Clients;
        private List<Event> m_SwapablePoints;

        private bool m_IsEvaluated;

        public Event First { get { return m_First; } set { m_First = value; } }
        public Event Last { get { return m_Last; } set { m_Last = value; } }

        public double TravelTime { get { return m_TravelTime; } }
        public double SlackTime { get { return m_SlackTime; } }
        public double WaitTime { get { return m_WaitTime; } }
        public double ExcessRideTime { get { return m_ExcessRideTime; } }
        public double RideTime { get { return m_RideTime; } }

        public List<int> ClientList { get { return ( m_Clients != null ? m_Clients : new List<int>() ); } }
        public int ClientCount { get { return ( m_Clients != null ? m_Clients.Count : 0 ); } }

        public List<Event> SwapablePoints { get { return ( m_SwapablePoints != null ? m_SwapablePoints : new List<Event>() ); } }
        public int SwapablePointsCount { get { return ( m_SwapablePoints != null ? m_SwapablePoints.Count : 0 ); } }
                
        public bool IsEvaluated { get { return m_IsEvaluated; } }

        /* Propiedad 'IsVoidRoute' (sólo lectura)
         *  Retorna un booleando indicando si la ruta esta vacía (true) o no (false). La ruta se 
         * considera vacía si es que no contiene ningún evento o si sólo contiene los eventos de
         * partida y fin de la ruta (depósitos).
         */ 
        public bool IsVoidRoute
        {
            get
            {
                if( First == null || Last == null || ( First.Next == Last && Last.Previous == First ) )
                    return true;

                return false;
            }
        }

        /* Constructor
         *  Crea una instancia vacía de ruta, sólo con los eventos de salida y llegada al depósito.
         */ 
        public Route() : this( true )
        {
        }

        /* Constructor
         *  Crea una instancia vacía de ruta, dodne se puede indicar si es que deben crear o no los
         * eventos de salida y llegada al depósito.
         * 
         *  Parámetros:
         * - 'bool init': Booleano que especifica si se crean los eventos de inicio/fin de la ruta
         * (true) o no (false).
         */
        public Route( bool init )
        {
            if( init )            
                AddDepots();                
        }

        /* Constructor
         *  Crea una instancia de ruta, en donde se especifica un conjunto de eventos que se usarán
         * como parte de ella y además se puede especificar si los eventos en este segmento serán
         * simplemente asignados por su referencia, o copiados a la nueva ruta.
         * 
         *  Parámetros:
         * - 'Event routeStart': Referencia al primer evento del conjunto de eventos a utilizar.
         * - 'Event routeEnd': Referencia al último evento del conjunto de eventos a utilizar.
         * - 'bool copyEvents': Booleano que indica si se deben copiar los eventos del conjunto
         * especificado (true) o no (false). En el primer caso, adicionalmente se generará la lista
         * de puntos de intercambio.
         */
        public Route( Event routeStart, Event routeEnd, bool copyEvents )
        {
            if( !copyEvents )
            {
                First = routeStart;
                Last = routeEnd;
            }
            else
            {
                m_SwapablePoints = new List<Event>();
                
                First = (Event)routeStart.Clone();
                Event checking = routeStart.Next;
                Event lastAdded = First;

                while( checking != null )
                {                                        
                    Event toAdd = (Event)checking.Clone();

                    lastAdded.Next = toAdd;
                    toAdd.Previous = lastAdded;

                    if( lastAdded.ServiceID != -1 && toAdd.ServiceID != -1 && Clients.PrecedenceTable[toAdd.ServiceID].BinarySearch( lastAdded.ServiceID ) < 0 )
                        m_SwapablePoints.Add( lastAdded );

                    lastAdded = toAdd;
                    checking = checking.Next;
                }

                Last = lastAdded;
            }
        }        

        /* Método 'Clone' (implementado de ICloneable)
         *  Retorna una copia de esta ruta, como tipo object.
         */ 
        public object Clone()
        {
            Route cloned = new Route( First, Last, true );

            cloned.m_TravelTime = m_TravelTime;
            cloned.m_SlackTime = m_SlackTime;
            cloned.m_ExcessRideTime = m_ExcessRideTime;
            cloned.m_WaitTime = m_WaitTime;
            cloned.m_RideTime = m_RideTime;

            cloned.m_IsEvaluated = m_IsEvaluated;
            cloned.m_Clients = new List<int>( m_Clients );

            return cloned;
        }

        /* Método 'AddFirst'
         *  Inserta un evento especificado al principio de la ruta.
         * 
         *  Parámetros:
         * - 'Event toAdd': Evento a insertar.
         */
        public void AddFirst( Event toAdd )
        {
            if( First == null )
            {
                First = toAdd;

                if( Last == null )
                    Last = toAdd;
            }
            else
            {
                First.Previous = toAdd;
                toAdd.Next = First;

                First = toAdd;
            }
        }

        /* Método 'AddLast'
         *  Inserta un evento especificado al final de la ruta.
         * 
         *  Parámetros:
         * - 'Event toAdd': Evento a insertar.
         */
        public void AddLast( Event toAdd )
        {
            if( Last == null )
            {
                Last = toAdd;

                if( First == null )
                    First = toAdd;
            }
            else
            {
                Last.Next = toAdd;
                toAdd.Previous = Last;

                Last = toAdd;
            }
        }

        /* Método 'AddDepots'
         *  Inserta los eventos de inicio y final de la ruta (depots) en caso de que no existan.
         */
        private void AddDepots()
        {
            if( First == null || First.EType != EventType.StartDepot )
                AddFirst( new Event( Locations.DepotID, true ) );

            if( Last.EType != EventType.StopDepot )
                AddLast( new Event( Locations.DepotID, false ) );

            First.AcumulatedLoad = 0;
            Last.AcumulatedLoad = 0;
        }

        /* Método 'GetRandomSwapPoint'
         *  Retorna la instancia aleatoria de un evento marcado para ser evaluado en un intercambio.
         */
        public Event GetRandomSwapPoint()
        {
            if( m_SwapablePoints == null || m_SwapablePoints.Count == 0 )
                return null;

            return m_SwapablePoints[RandomTool.GetInt( m_SwapablePoints.Count - 1 )];
        }

        /* Método 'EvaluateRoute'
         *  Genera toda la información de tiempos, clientes y otros relativa a la ruta. Este método
         * esta pensado para ser usado cuando la ruta ya esta construída.
         */
        public void EvaluateRoute()
        {
            m_Clients = new List<int>( GlobalParams.ClientNumber / GlobalParams.VehiclesMaxNumber );
            m_SwapablePoints = new List<Event>();
            
            Event actual = First.Next;

            TimeSpan travelTime = TimeSpan.Zero;
            TimeSpan slackTime = TimeSpan.Zero;
            TimeSpan waitTime = TimeSpan.Zero;
            TimeSpan excessRideTime = TimeSpan.Zero;
            TimeSpan rideTime = TimeSpan.Zero;

            //  Esta tabla guarda los tiempos actuales de recogida de los clientes, para que se puedan
            // calcular los tiempos de sus viajes y el exceso en ellos cuando sean entregados a sus
            // destinos respectivos.
            Dictionary<int, TimeSpan> pickups = new Dictionary<int, TimeSpan>();

            // Se recorre cada evento para ir sumando los valores que correspondan...
            while( actual != null )
            {
                // Se suma el tiempo de viaje desde el evento anterior al actual...
                travelTime += actual.AT - actual.Previous.AT;

                // Si este no es el último evento, se pueden seguir evaluando cosas...
                if( actual.EType != EventType.StopDepot )
                {                    
                    Event next = actual.Next;
                    
                    // Si al evento actual le sigue un slack, se calcula el tiempo asociado...
                    if( actual.HasSlack )
                        slackTime += next.AT - ( actual.AT + actual.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[actual.Location, next.Location] ) );                   
                    
                    // Se suma el tiempo de espera del cliente...
                    waitTime += actual.AT - actual.ET;

                    //  Para evaluar el tiempo de viaje del cliente y el exceso en él, se debe evaluar
                    // si el evento es entrega, que es donde se pueden hacer los cálculos.
                    if( actual.EType == EventType.Pickup )
                    {
                        pickups[actual.ClientID] = actual.AT;
                    }
                    else if( actual.EType == EventType.Delivery )
                    {
                        int toCheckID = actual.ClientID;
                        Client toCheck = Clients.GetClient( toCheckID );
                        
                        int index = m_Clients.BinarySearch( toCheckID );
                        m_Clients.Insert( ~index, toCheckID );

                        TimeSpan directRideTime = toCheck.UpRequest.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[toCheck.UpRequest.LocationID, toCheck.DownRequest.LocationID] );
                        TimeSpan realRideTime = actual.AT - pickups[toCheckID];

                        // Se suma el tiempo de viaje efectivo del cliente...
                        rideTime += realRideTime;
                        // Se suma el exceso del tiempo de viaje...
                        excessRideTime += ( realRideTime - directRideTime );

                        pickups.Remove( actual.ClientID );
                    }

                    // Se revisa si en este punto es posible evaluar un intercambio...
                    if( next.ServiceID != -1 && Clients.PrecedenceTable[next.ServiceID].BinarySearch( actual.ServiceID ) < 0 )
                        m_SwapablePoints.Add( actual );
                }

                actual = actual.Next;
            }

            // Finalmente se asignan a las variables internas los datos calculados.
            m_TravelTime = travelTime.TotalMinutes;
            m_SlackTime = slackTime.TotalMinutes;
            m_WaitTime = waitTime.TotalMinutes;
            m_ExcessRideTime = excessRideTime.TotalMinutes;
            m_RideTime = rideTime.TotalMinutes;

            m_IsEvaluated = true;
        }

        /* Método 'ToString' (sobrecargado de 'Object')
         *  Retorna una representación en string de la ruta actual, en el formato:
         * "<evento_1> <evento_2> <SLK> <evento_3> ... <evento_n>", donde "<SLK>" es un eventual
         * tiempo de slack entre eventos.
         */
        public override string ToString()
        {
            Event node = First;
            string res = "";

            while( node != null )
            {
                res += node.ToString() + ( node.Next != null ? ( node.HasSlack ? " <SLK> " : " " ) : "" );
                node = node.Next;
            }

            return res;
        }
    }
}
