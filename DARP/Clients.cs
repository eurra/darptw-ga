/* Client.cs
 * Última modificación: 12/03/2008
 */

using System;
using System.Collections.Generic;
using DARPTW_GA.Misc;

namespace DARPTW_GA.DARP
{
    /* Clase 'Clients'
     *  Clase estática que contiene estructuras, métodos y otras subclases enfocadas a la
     * administración de la información de los clientes, obtenida de los datos de entrada utilizados.
     * Contiene 4 elementos principales:
     * - Una lista con los clientes del problema (lista de clase 'Client'). Se asume que para cada
     * objeto cliente en esta lista, su índice en ella corresponderá a su identificador, después que
     * la misma lista haya sido ordenada. Este identificador es usado en esta implementación sin
     * ninguna referencia a los datos de entrada.
     * - Una lista con las solicitudes de servicio de los clientes (lista de clase 'ServiceRequest').
     * Se asume que para cada objeto de solicitud de servicio en esta lista, su índice corresponderá
     * a su identificador, después que la misma lista haya sido ordenada. Como los identificadores de
     * las solicitudes de servicio son obtenidos de los datos de entrada, lo anterior es válido sólo
     * si en los datos de entrada estan todos los identificadores en un rango determinado.
     * - Un diccionario que representa la tabla de precedencia, en donde la llave corresponde al
     * identificador de una solicitud de servicio, y el valor es una lista con los identificadores de
     * las solicitudes de servicio que estan antes de la indicada en la llave.
     * - Un diccionario que representa la tabla de incopatibilidad de clientes, donde la llave
     * corresponde al identificador de un cliente y el valor es una lista con los identificadores de
     * los clientes que no son compatibles al del indicado en la llave.
     * 
     *  Atributos:
     * - 'List<Client> m_Clients': Lista estática que contiene los objetos de los clientes.
     * - 'List<ServiceRequest> m_Requests': Lista estática que contiene los objetos de las solicitudes
     * de servicio.
     * - 'Dictionary<int, List<int>> m_PrecedenceTable': Diccionario estático que representa la tabla
     * de precedencia preliminar.
     * - 'Dictionary<int, List<int>> m_ClientIncompatibilityTable': Diccionario estático que representa
     * la tabla de incompatibilidad de clientes.
     */
    public static class Clients
    {
        private static List<Client> m_Clients = new List<Client>();
        private static List<ServiceRequest> m_Requests = new List<ServiceRequest>(); 
        private static Dictionary<int, List<int>> m_PrecedenceTable = new Dictionary<int,List<int>>();
        private static Dictionary<int, List<int>> m_ClientIncompatibilityTable = new Dictionary<int,List<int>>();
        
        public static Dictionary<int, List<int>> PrecedenceTable { get { return m_PrecedenceTable; } }
        public static Dictionary<int, List<int>> CyclicDependenceTable { get { return m_ClientIncompatibilityTable; } }
        public static List<ServiceRequest> Requests { get { return m_Requests; } }

        /* Método 'AddClient'
         *  Agrega un nuevo cliente a la lista, en base a un par de solicitudes de servicio asumidas
         * como la recogida y la entrega del mismo. Estas mismas solicitudes se agregan a su lista
         * respectiva también.
         * 
         *  Parámetros:
         * - 'ServiceRequest pReq': La solicitud de servicio de recogida (pickup) del cliente.
         * - 'ServiceRequest dReq': la solicitud de servicio de entrega (delivery) del cliente.
         */
        public static void AddClient( ServiceRequest pReq, ServiceRequest dReq )
        {
            if( pReq == null || dReq == null )
                return;

            m_Clients.Add( new Client( pReq, dReq ) );

            m_Requests.Add( pReq );
            m_Requests.Add( dReq );
        }

        /* Método 'GetClient'
         *  Obtiene la instancia de un objeto cliente dentro de la lista de ellos, en base al índice
         * en esta lista (por ende su identificador, después de que la lista sea ordenada).
         * 
         *  Parámetros:
         * - 'int index': Índice del cliente a obtener de la lista.
         */
        public static Client GetClient( int index )
        {
            if( index < 0 || index > m_Clients.Count - 1 )
                return null;

            return m_Clients[index];
        }

        /* Método 'AddRequest'
         *  Agrega una nueva solicitud de servicio a la lista.
         * 
         *  Parámetros:
         * - 'ServiceRequest sr': La solicitud de servicio de recogida que se agregará.
         */
        public static void AddRequest( ServiceRequest sr )
        {
            if( sr == null )
                return;

            m_Requests.Add( sr );
        }

        /* Método 'GetRequest'
         *  Obtiene la instancia de un objeto de solicitud de servicio dentro de la lista de ellos,
         * en base al índice en esta lista (por ende su indentificador, si los datos de entrada 
         * contienen todos los identificadores en un rango determinado y la lista ha sido ordenada).
         * 
         *  Parámetros:
         * - 'int index': Índice de la solicitud de servicio a obtener de la lista.
         */
        public static ServiceRequest GetRequest( int index )
        {
            if( index < 0 || index > m_Requests.Count - 1 )
                return null;

            return m_Requests[index];
        }

        /* Método 'ClearInfo'
         *  Reinicializa todas las listas de esta clase, dejándolas vacias.
         */ 
        public static void ClearInfo()
        {
            m_Clients.Clear();
            m_Requests.Clear();
            m_PrecedenceTable.Clear();
            m_ClientIncompatibilityTable.Clear();
        }

        /* Método 'SortInfo'
         *  Ordena la listas de clientes y solicitudes de servicio acorde a los comparadores
         * implementados en esta clase. 
         */
        public static void SortInfo()
        {
            m_Clients.Sort( ClientComparer.Instance );
            m_Requests.Sort( ServiceRequestComparer.Instance );

            //  Como el orden de los clientes en la lista hace referencia a sus identificadores,
            // cuando la misma ya se ha ordenado, es posible asignar el identificador de cada uno
            // de ellos a sus solicitudes de servicio respectivas (de recogida y entrega).
            for( int i = 0; i < m_Clients.Count; i++ )
            {
                m_Clients[i].UpRequest.ClientID = i;
                m_Clients[i].DownRequest.ClientID = i;
            }
        }

        /* Clase 'ServiceRequestComparer' (implementada desde IComparer<>)
         *  Clase útil para el ordenamiento de la lista de solicitudes de servicio.
         * 
         *  Atributos:
         * - 'ServiceRequestComparer Instance': Objeto estático, usado como instancia general 
         * de esta misma clase.
         */
        private class ServiceRequestComparer : IComparer<ServiceRequest>
        {
            public static readonly ServiceRequestComparer Instance = new ServiceRequestComparer();
            
            public ServiceRequestComparer()
            {
            }

            //  El criterio para comparar las solicitudes de servicio se basa en el indicador de
            // las mismas.
            public int Compare( ServiceRequest x, ServiceRequest y )
            {
                if( x.ID > y.ID )
                    return 1;
                else if( x.ID == y.ID )
                    return 0;
                else
                    return -1;
            }
        }

        /* Clase 'ClientComparer' (implementada desde IComparer<>)
         *  Clase útil para el ordenamiento de la lista de clientes.
         * 
         *  Atributos:
         * - 'ClientComparer Instance': Objeto estático, usado como instancia general de esta  
         * misma clase.
         */
        private class ClientComparer : IComparer<Client>
        {
            public static readonly ClientComparer Instance = new ClientComparer();
            
            public ClientComparer()
            {
            }

            //  El criterio para comparar los clientes, se basa en sus solicitudes de servicio de
            // recogida, en particular en el ET de estas.
            public int Compare( Client x, Client y )
            {
                if( x.UpRequest.ET > y.UpRequest.ET )
                    return 1;
                else if( x.UpRequest.ET == y.UpRequest.ET )
                    return 0;
                else
                    return -1;
            }
        }
        
        /* Método 'UpdatePrecedenceTable'
         *  Este método esta pensado para ser llamado después que la lista de solicitudes de servicio,
         * y por ende, la de los clientes, hayan sido construidas, además de la tabla de distancias 
         * entre las distintas locaciones asociadas a los datos de entrada.
         *  En base a estos datos, se construye la tabla de precedencia preliminar asociada a los datos
         * de entrada obtenidos.          
         */
        public static void UpdatePrecedenceTable()
        {            
            //  Se recorre la lista de clientes, y por cada una de sus solicitudes de servicio
            // (recogida y entrega), se ingresa una nueva entrada en la tabla, con una lista vacía.
            // De este modo la tabla queda inicializada para que se pueda ingresar la información 
            // de precedencia...
            for( int i = 0; i < m_Clients.Count; i++ )
            {               
                m_PrecedenceTable[m_Clients[i].UpRequest.ID] = new List<int>();
                m_PrecedenceTable[m_Clients[i].DownRequest.ID] = new List<int>();
            }

            //  Se recorre en dos ciclos anidados la lista de solicitudes de servicio. Cabe
            // destacar que nunca se considera el primer elemento, que corresponde al depósito.
            //  Desde el primer ciclo se obtiene una solicitud como 'la de origen'...
            for( int i = 1; i < m_Requests.Count ; i++ )
            {                
                ServiceRequest orig = m_Requests[i];

                for( int j = 1; j < m_Requests.Count ; j++ )
                { 
                    if( j != i )
                    {                        
                        //  Desde el segundo ciclo se obtiene otra solicitud distinta a la de origen,
                        // como 'la de destino'. A continuación se evaluan ciertas condiciones, en
                        // donde se busca la imposibilidad de arribar a cualquier tiempo desde el nodo
                        // del origen, al nodo del destino...
                        ServiceRequest dest = m_Requests[j];

                        //  Si la solicitud de origen es entrega y la de destino corresponde a su
                        // respectiva recogida, se agrega el destino a la lista de precedencia del
                        // origen y se continua...
                        if( orig.LoadChange < 0 && orig.ID - m_Clients.Count == dest.ID )
                        {
                            m_PrecedenceTable[orig.ID].Add( dest.ID );
                            continue;
                        }

                        //  Si el extremo inferior de la ventana del origen es mayor o igual al extremo
                        // superior de la ventana del destino, se agrega el destino a la lista de
                        // precedencia del origen y se continua...
                        if( orig.ET >= dest.LT )
                        {
                            m_PrecedenceTable[orig.ID].Add( dest.ID );
                            continue;
                        }
                        
                        //  Por último, si al realizar el recorrido directo desde el nodo del origen
                        // hasta el nodo destino, el tiempo mínimo al que se arriba es mayor o igual
                        // al extremo superior de la ventana del destino, se agrega éste a la lista de
                        // precedencia del origen...
                        TimeSpan despET = orig.ET + orig.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[orig.LocationID, dest.LocationID] );

                        if( despET >= dest.LT )                        
                            m_PrecedenceTable[orig.ID].Add( dest.ID );                       
                    }
                }

                // Al final, lista de precedencia del origen seleccionado se ordena.
                m_PrecedenceTable[orig.ID].Sort();
            }
        }

        /* Método 'UpdateIncompatibilityTable'
         *  Este método esta pensado para ser llamado después que la tabla de precedencia preliminar
         * se haya construido. En base a ella, se construye la tabla de clientes incompatibles.       
         */
        public static void UpdateIncompatibilityTable()
        {
            //  Se recorre la lista de solicitudes de servicio, asumiendo cada elemento de ella como
            // 'el origen' y obteniendo su respectiva lista de precedencia...
            for( int i = 1; i < m_Requests.Count - 1; i++ )
            {
                List<int> origPrecList = m_PrecedenceTable[i];
                List<int> origClientCyclicList = null;

                //  Se revisa cada elemento en la lista de precedencia del origen, asumiendo cada
                // elemento de ella como 'el destino' y obteniendo su respectiva lista de
                // precedencia...
                for( int j = 0; j < origPrecList.Count; j++ )
                {
                    List<int> destPrecList = m_PrecedenceTable[origPrecList[j]];

                    //  Si se encuentra que el origen está contenido en la lista de precedencia del
                    // destino, los clientes de ambas solicitudes son incompatibles...
                    if( destPrecList.BinarySearch( i ) >= 0 )
                    {
                        //  Primero se evalúa si existe la lista de incompatibilidad del cliente de la
                        // solicitud de origen. Si no es así, se crea....
                        if( origClientCyclicList == null && !m_ClientIncompatibilityTable.TryGetValue( m_Requests[i].ClientID, out origClientCyclicList ) )
                        {
                            origClientCyclicList = new List<int>();
                            m_ClientIncompatibilityTable[m_Requests[i].ClientID] = origClientCyclicList;
                        }

                        //  Si el cliente de la solicitud de destino no esta en la lista de
                        // incompatibilidad del cliente de la solicitud de origen, se agrega.
                        if( !origClientCyclicList.Contains( m_Requests[origPrecList[j]].ClientID ) )
                            origClientCyclicList.Add( m_Requests[origPrecList[j]].ClientID );
                    }
                }
            }
        }
    }
}