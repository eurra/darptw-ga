/* RouteGenome.cs
 * �ltima modificaci�n: 19/03/2008
 */

using System;
using System.Collections.Generic;
using System.Text;
using DARPTW_GA.GA_Base;
using DARPTW_GA.DARP;
using DARPTW_GA.Framework;
using DARPTW_GA.Framework.Routing;
using DARPTW_GA.Misc;

namespace DARPTW_GA.Framework.Genomes
{
    /* Clase 'RouteGenome'
     *  Representa un cromosoma basado en rutas. Se basa en guardar directa e internamente el
     * conjunto de rutas y mascaras asociadas que definen la soluci�n, adem�s de otro tipo de 
     * informaci�n relevante para el cromosoma. Define los operadores necesarios para su 
     * funcionamiento y otros metodos �tiles.
     * 
     *  Atributos:
     * - 'Evaluation m_Evaluation': Referencia al objeto que guarda la informaci�n de calidad del
     * cromosoma.
     * - 'Route[] m_Routes': Arreglo interno con las rutas asignadas al cromosoma. El �ndice del
     * arreglo indica el n�mero del veh�culo respectivo.
     * - 'ClientMask[] m_Masks': Arreglo interno con las m�scaras de clientes asociadas a las rutas
     * del cromosoma.
     * - 'Dictionary<int, int> m_ClientAsignation': Tabla que almacena la informaci�n relativa a
     * como estan asignados los clientes a las rutas en el cromosoma, con el fin de agilizar la
     * obtenci�n de informaci�n de estos datos. La llave corresponde al identificador de un cliente
     * y el valor es el veh�culo al que esta asignado dicho cliente.
     */
    public class RouteGenome : Genome
    {
        private Evaluation m_Evaluation;

        private Route[] m_Routes;
        private ClientMask[] m_Masks;
        private Dictionary<int, int> m_ClientAsignation;

        public double Fitness { get { return m_Evaluation.Result; } }
        public Evaluation Evaluation { get { return m_Evaluation; } }
        public int Count { get { return m_Masks.Length; } }

        /* Constructor
         *  Crea una nueva instancia de este tipo de cromosoma, en base a las rutas y sus mascaras que
         * lo compondr�n internamente. Junto con incluir estos datos en el cromosoma, se calcula la
         * calidad y se actualiza la tabla de asignaci�n de clientes.
         * 
         *  Par�metros:
         * - 'ClientMask[] masks': Arreglo que contiene las m�scaras a utilizar.
         * - 'Route[] routes': Arreglo que contiene las rutas a utilizar.
         */
        public RouteGenome( ClientMask[] masks, Route[] routes )
        {
            m_Masks = masks;
            m_Routes = routes;

            CalculateFitness();
            UpdateClientAsignation();
        }

        /* Constructor
         *  Crea una nueva instancia de este tipo de cromosoma, con una lista de rutas y m�scaras
         * vac�a. El tama�o de estas listas va acorde a los par�metros globales.
         */
        public RouteGenome()
        {
            m_Masks = new ClientMask[GlobalParams.VehiclesMaxNumber];
            m_Routes = new Route[GlobalParams.VehiclesMaxNumber];
        }

        /* M�todo 'UpdateClientAsignation'
         *  En base al arreglo de rutas interno del cromosoma construye la tabla de asignaci�n
         * de clientes a los veh�culos.
         */ 
        private void UpdateClientAsignation()
        {
            m_ClientAsignation = new Dictionary<int, int>( GlobalParams.ClientNumber );

            for( int i = 0; i < m_Routes.Length; i++ )
            {
                //  La tabla se construye en base a la lista de clientes de la ruta, por ello se
                // asume que la misma esta evaluada convenientemente.
                List<int> clients = m_Routes[i].ClientList;

                for( int j = 0; j < clients.Count; j++ )
                    m_ClientAsignation[clients[j]] = i;
            }
        }

        /* M�todo 'CalculateFitness'
         *  Calcula la calidad del cromosoma en base al arreglo de rutas interno y guarda la
         * informaci�n.
         */
        private void CalculateFitness()
        {
            m_Evaluation = new Evaluation( m_Routes );
        }

        /* M�todo 'GetMask'
         *  Obtiene la instancia de una m�scara en base al indice del veh�culo asociado a su ruta.
         * 
         *  Par�metros:
         * - 'int index': �ndice del veh�culo asociado a la m�scara a obtener.
         */
        public ClientMask GetMask( int index )
        {
            return m_Masks[index];
        }

        /* M�todo 'GetRoute'
         *  Obtiene la instancia de una ruta en base al indice del veh�culo asociado.
         * 
         *  Par�metros:
         * - 'int index': �ndice del veh�culo asociado a la ruta a obtener.
         */
        public Route GetRoute( int index )
        {
            return m_Routes[index];
        }

        /* M�todo 'GenerateRandomGenome'
         *  M�todo est�tico que retorna una instancia aleatoria de un cromosoma de este tipo,
         * con una estructura factible.
         */
        public static RouteGenome GenerateRandomGenome()
        {
            Route[] routes = null;
            ClientMask[] masks = null;

            SolutionGeneration.GenerateSolution( out masks, out routes );

            return new RouteGenome( masks, routes );
        }

        /* M�todo 'FillNullRoutes'
         *  M�todo est�tico que modifica una lista de rutas y m�scaras, descartando los elementos
         * nulos en ellas y reemplazandolos por instancias vacias.
         * 
         *  Par�metros:
         * - 'Route[] routes': Arreglo de rutas que ser� modificado.
         * - 'ClientMask[] masks': Arreglo de m�scaras asociado a las rutas que ser� modificado.
         */
        private static void FillNullRoutes( Route[] routes, ClientMask[] masks )
        {
            for( int i = 0; i < routes.Length; i++ )
            {
                if( routes[i] == null )
                {
                    routes[i] = new Route();
                    masks[i] = ClientMask.Empty;
                }
            }
        }

        /* M�todo 'TryCrossover'
         *  Evalua la recombinaci�n entre un par de cromosomas especificados. Retorna el nuevo
         * cromosoma en caso de que sea exitosa la recombinaci�n, o null en caso contrario.
         * 
         *  Par�metros:
         * - 'RouteGenome mainParent': Cromosoma padre que ejerce el rol de principal en la operaci�n.
         * - 'RouteGenome secParent': Cromosoma padre que ejerce el rol de secundario en la operaci�n.
         */
        public static RouteGenome TryCrossover( RouteGenome mainParent, RouteGenome secParent )
        {
            // Arreglos que contendran tanto las m�scaras como las rutas del nuevo hijo...
            ClientMask[] offspringMasks = new ClientMask[GlobalParams.VehiclesMaxNumber];
            Route[] offspringRoutes = new Route[GlobalParams.VehiclesMaxNumber];

            // Esta m�scara se ira modificando a medida que se insertan clientes al hijo...
            ClientMask offspringClients = ClientMask.Empty;

            //  Este selector entrega indices de ruta de forma aleatoria para la asignaci�n de las
            // mismas al hijo...
            RandomUniqueSelector unassignedRoutesSelector = new RandomUniqueSelector( offspringRoutes.Length - 1 );

            //  Etapa 1: se donan alrededor de un tercio de las rutas del padre principal al hijo...            
            ClientMask fullMask = ClientMask.Full;
                        
            int routesToDonate = Math.Max( offspringRoutes.Length / 3, 1 );
            int routesDonated = 0;
            RandomUniqueSelector mainParentRouteSelector = new RandomUniqueSelector( mainParent.Count - 1 );

            // Se van seleccionando de forma aleatoria rutas del padre...
            while( !mainParentRouteSelector.IsCompleted )
            {
                int routeSelectedIndex = mainParentRouteSelector.Next();
                Route routeSelected;

                // Se omiten las rutas vac�as en la selecci�n...
                if( ( routeSelected = mainParent.GetRoute( routeSelectedIndex ) ).IsVoidRoute )
                    continue;

                ClientMask maskSelected = mainParent.GetMask( routeSelectedIndex );

                // Se asignan tanto la ruta como la m�scara correspondiente al hijo, clonandolas...
                int indexToInsert = unassignedRoutesSelector.Next();

                offspringRoutes[indexToInsert] = (Route)routeSelected.Clone();
                offspringMasks[indexToInsert] = (ClientMask)maskSelected.Clone();

                //  Se evalua si se han considerado ya todos los clientes en la soluci�n temporal. Si
                // es as�, se retorna el hijo como resultado...
                offspringClients = offspringClients.OR( maskSelected );

                if( offspringClients.IsSame( fullMask ) )
                {
                    FillNullRoutes( offspringRoutes, offspringMasks );
                    return new RouteGenome( offspringMasks, offspringRoutes );
                }

                routesDonated++;

                //  Si se ha donado el limite de rutas del padre principal, se continua a la siguiente
                // etapa...
                if( routesDonated == routesToDonate )
                    break;
            }

            //  Etapa 2: Se seleccionan rutas del padre secundario, se filtran y donan al hijo acorde a
            // su cantidad de clientes: una ruta con m�s clientes ser� priorizada sobre otra con menos.
            // Este proceso se har� mientras queden rutas disponibles en el hijo...
            if( !unassignedRoutesSelector.IsCompleted )
            {
                // Estas estructuras guardar�n las rutas procesadas del padre secundario...
                List<Route> processedRoutes = new List<Route>( secParent.Count );
                Dictionary<Route, ClientMask> processedMasks = new Dictionary<Route, ClientMask>( secParent.Count );

                // Se revisan una por una las rutas del padre secundario...
                for( int i = 0; i < secParent.Count; i++ )
                {
                    Route routeToCheck = secParent.GetRoute( i );

                    // Se omiten las rutas vacias...
                    if( routeToCheck.IsVoidRoute )
                        continue;

                    // Se obtiene una copia tanto de la ruta seleccionada como de su m�scara...
                    Route routeToProcess = (Route)routeToCheck.Clone();
                    ClientMask maskToProcess = (ClientMask)secParent.GetMask( i ).Clone();

                    //  Se opera con la m�scara para ver que clientes se deben eliminar para poder
                    // insertar la ruta en el hijo...
                    ClientMask ANDResult = maskToProcess.AND( offspringClients );
                    List<int> clientsToDelete = ANDResult.GetClientList();

                    if( clientsToDelete.Count > 0 )
                    {
                        bool allDeleted = true;

                        // Se evalua uno por uno los clientes que deber�an eliminarse de la ruta...
                        for( int j = 0; j < clientsToDelete.Count; j++ )
                        {
                            int toDeleteID = clientsToDelete[j];

                            // Se evalua la eliminaci�n del cliente en la ruta...
                            if( !RouteGeneration.CheckClientDeletion( routeToProcess, Clients.GetClient( toDeleteID ) ) )
                            {
                                allDeleted = false;
                                break;
                            }
                            else
                            {
                                //  Si la eliminaci�n es positiva, se aprovecha de modificar la m�scara
                                // de la ruta...
                                maskToProcess[toDeleteID] = false;
                            }
                        }

                        // Si al menos un cliente no se pudo eliminar, se omite la ruta...
                        if( !allDeleted )
                            continue;
                    }

                    // A estas alturas, el filtrado de la ruta fue correcto...

                    // Se evalua la ruta filtrada para la consistencia de datos...
                    routeToProcess.EvaluateRoute();

                    //  Se inserta la ruta en el arreglo de rutas del hijo. La inserci�n se realiza
                    // ordenadamente, dejando en los primeros �ndices a las rutas con las clientes...
                    int indexToInsert;

                    if( ( indexToInsert = processedRoutes.BinarySearch( routeToProcess, RouteComparer.Instance ) ) < 0 )                    
                        processedRoutes.Insert( ~indexToInsert, routeToProcess );                    
                    else                    
                        processedRoutes.Insert( indexToInsert, routeToProcess );

                    // Tambi�n se inserta la mascara en el indice correspondiente...
                    processedMasks[routeToProcess] = maskToProcess;
                }

                // Si no hay rutas correctamente filtradas, no es posible realizar la recombinaci�n...
                if( processedRoutes.Count == 0 )
                    return null;

                int indexOfRouteSelected = processedRoutes.Count - 1;

                //  Se van asignando las rutas con m�s clientes de las filtradas, a posiciones
                // aleatorias de los veh�culos...
                while( !unassignedRoutesSelector.IsCompleted && indexOfRouteSelected >= 0 )
                {
                    int indexToInsert = unassignedRoutesSelector.Next();
                    offspringRoutes[indexToInsert] = processedRoutes[indexOfRouteSelected];
                    offspringMasks[indexToInsert] = processedMasks[offspringRoutes[indexToInsert]];

                    //  En caso de que al asignar las una ruta, se complete la cantidad de clientes
                    // en el cromosoma hijo, el mismo se retorna como resultado...
                    offspringClients = offspringClients.OR( offspringMasks[indexToInsert] );

                    if( offspringClients.IsSame( fullMask ) )
                    {
                        FillNullRoutes( offspringRoutes, offspringMasks );
                        return new RouteGenome( offspringMasks, offspringRoutes );
                    }

                    indexOfRouteSelected--;
                }
            }

            //  Etapa 3: Se insertan los clientes que falten para completar la soluci�n que representa
            // el cromosoma hijo...

            // Se llenan los espacios de veh�culos vacios con rutas y m�scaras vacias...
            FillNullRoutes( offspringRoutes, offspringMasks );

            // Se genera una lista con los clientes que faltan por considerar en la soluci�n...
            ClientMask remainingClientsMask = offspringClients.NOT();
            List<int> clientsRemaining = remainingClientsMask.GetClientList();

            //  Esta lista guarda los indices de las rutas modificadas para que al final se actualizen
            // sus datos convenientemente.
            List<int> routesToUpdate = new List<int>( offspringRoutes.Length );

            // Se revisa cada cliente en la lista...
            foreach( int toInsert in clientsRemaining )
            {
                RandomUniqueSelector routeIndexSelector = new RandomUniqueSelector( offspringRoutes.Length - 1 );

                // Se revisan una a una las rutas, de forma aleatoria, para la posible inserci�n...
                while( !routeIndexSelector.IsCompleted )
                {
                    int routeIndex = routeIndexSelector.Next();
                    Route routeSelected = offspringRoutes[routeIndex];

                    // Se verifican clientes incompatibles para evitar una inserci�n infactible...
                    List<int> incompatibleClients;
                    Clients.CyclicDependenceTable.TryGetValue( toInsert, out incompatibleClients );

                    bool incompatibleDestRoute = false;

                    if( incompatibleClients != null )
                    {
                        foreach( int incompatibleClient in incompatibleClients )
                        {
                            //  Antes de ver la lista de clientes de una ruta, es necesario
                            // actualizar sus datos para que sean consistentes...
                            if( routesToUpdate.Contains( routeIndex ) )
                            {
                                routeSelected.EvaluateRoute();
                                routesToUpdate.Remove( routeIndex );
                            }
                            
                            if( routeSelected.ClientList.BinarySearch( incompatibleClient ) > 0 )
                            {
                                incompatibleDestRoute = true;
                                break;
                            }
                        }
                    }

                    //  Si la ruta tiene clientes incompatibles con el que se quiere insertar,
                    // se omite..
                    if( incompatibleDestRoute )
                        continue;

                    // Se evalua la inserci�n del cliente en la ruta seleccionada...
                    if( RouteGeneration.CheckClientInsertion( routeSelected, Clients.GetClient( toInsert ) ) )
                    {
                        offspringMasks[routeIndex][toInsert] = true;

                        if( !routesToUpdate.Contains( routeIndex ) )
                            routesToUpdate.Add( routeIndex );

                        break;
                    }
                    //  Si el cliente no pudo ser insertado en ninguna ruta, es imposible realizar la
                    // recombinaci�n...
                    else if( routeIndexSelector.IsCompleted )
                    {
                        return null;
                    }
                }
            }

            //  Con los clientes insertados, se actualizan los datos de las rutas, y se retorna el
            // nuevo cromosoma hijo...
            for( int i = 0; i < routesToUpdate.Count; i++ )
                offspringRoutes[routesToUpdate[i]].EvaluateRoute();

            return new RouteGenome( offspringMasks, offspringRoutes );
        }

        /* M�todo 'DoCrossover'
         *  Operador de recombinaci�n del modelo basado en rutas. Realiza la recombinaci�n en base
         * a un arreglo de cromosomas padres. Se realizan dos recombinaciones intercambiando los roles
         * de principal y secundario para los padres. Se realiza un n�mero limitado de intentos para
         * cada hijo, en caso de que no se obtenga factibilidad, se usa uno de los padres como 
         * resultado.
         * 
         *  Par�metros:
         * - 'Genome[] parents': Arreglo de los cromosomas padres usados en la recombinaci�n.
         */
        [Operator( "Recombinaci�n de rutas." )]
        public static Genome[] DoCrossover( Genome[] parents )
        {
            Genome[] childs = new Genome[2];
            int checks = 0;

            do
            {
                childs[0] = TryCrossover( (RouteGenome)parents[0], (RouteGenome)parents[1] );
                checks++;
            }
            while( childs[0] == null && checks < 20 );

            checks = 0;

            do
            {
                childs[1] = TryCrossover( (RouteGenome)parents[1], (RouteGenome)parents[0] );
                checks++;
            }
            while( childs[1] == null && checks < 20 );

            if( childs[0] == null )
                childs[0] = parents[0];

            if( childs[1] == null )
                childs[1] = parents[1];

            return childs;
        }

        /* M�todo 'ClusterMutation'
         *  Operador de mutaci�n de clusters del modelo basao en rutas. Evalua todas las posibilidades
         * de mutaci�n de cada uno de los clientes en la soluci�n, de forma aleatoria. No asegura que 
         * la mutaci�n efectivamente ocurra.
         * 
         *  Par�metros:
         * - 'Genome genome': Cromosoma que mutar�.
         * - 'double chance': Probabilidad (0.0-1.0) de que un gen en el cromosoma mute.
         */
        [Operator( "Mutaci�n de Clusters para genotipo de rutas." )]
        public static void ClusterMutation( Genome genome, double chance )
        {
            RouteGenome toMutate = (RouteGenome)genome;

            Route[] routes = toMutate.m_Routes;
            ClientMask[] masks = toMutate.m_Masks;
            Dictionary<int, int> clientAsignation = toMutate.m_ClientAsignation;

            //  Esta tabla guarda los clientes cambiados en la mutaci�n para que despues la tabla de
            // asignaci�n de clientes sea actualizada convenientemente...
            Dictionary<int, int> clientsChanged = new Dictionary<int, int>( (int)( clientAsignation.Count * 0.1 ) );            

            bool mutated = false;

            // Se evalua cada uno de los clientes de la soluci�n...
            foreach( int clientToMoveID in clientAsignation.Keys )
            {
                // Si la probabilidad para el cliente no se satisface, se omite...
                if( chance < RandomTool.GetDouble() )
                    continue;

                int indexOrigRoute = clientAsignation[clientToMoveID];
                Route origRoute = routes[indexOrigRoute];

                RandomUniqueSelector destRouteSelector = new RandomUniqueSelector( routes.Length - 1 );

                // Para el cliente, se escoge una ruta aleatoria...
                while( !destRouteSelector.IsCompleted )
                {
                    int indexDestRoute = 0;
                    Route destRoute = null;
                                        
                    while( destRoute == null && !destRouteSelector.IsCompleted )
                    {
                        indexDestRoute = destRouteSelector.Next();

                        // Se procura seleccionar una ruta distinta a la de origen...
                        if( indexOrigRoute == indexDestRoute )
                            destRoute = null;
                        else
                            destRoute = routes[indexDestRoute];
                    }

                    // Si no se pudo seleccionar ruta, la mutaci�n para este cliente es infactible...
                    if( destRoute == null )
                        break;

                    //  Chequeo de dependencia ciclica. Se revisa si la nueva ruta a la que ser�
                    // asignado el cliente a modificar, contiene clientes incompatibles respecto a �l.
                    // De ser asi no se podr� insertar al cliente en dicha ruta.
                    List<int> incompatibleClients;
                    Clients.CyclicDependenceTable.TryGetValue( clientToMoveID, out incompatibleClients );

                    bool incompatibleDestRoute = false;

                    if( incompatibleClients != null )
                    {
                        foreach( int incompatibleClient in incompatibleClients )
                        {
                            if( destRoute.ClientList.BinarySearch( incompatibleClient ) > 0 )
                            {
                                incompatibleDestRoute = true;
                                break;
                            }
                        }
                    }

                    if( incompatibleDestRoute )
                        continue;
 
                    Route origRouteClone = (Route)origRoute.Clone();
                    Client clientToMove = Clients.GetClient( clientToMoveID );

                    //  Se evalua la eliminaci�n del cliente desde la ruta origen (usando una ruta
                    // clonada) y la inserci�n del mismo en la ruta destino...
                    if( RouteGeneration.CheckClientDeletion( origRouteClone, clientToMove ) && RouteGeneration.CheckClientInsertion( destRoute, clientToMove ) )
                    {
                        //  De ser todo factible, se asigna la ruta clonada (origen) a la posici�n que 
                        // corresponde. La ruta destina ya esta modificada asi que no se considera..
                        routes[indexOrigRoute] = origRouteClone;

                        // Se modifican las m�scaras de ambas rutas de forma conveniente..
                        masks[indexOrigRoute][clientToMoveID] = false;
                        masks[indexDestRoute][clientToMoveID] = true;

                        // Se evaluan las rutas para su consistencia en posteriores procesos...
                        origRouteClone.EvaluateRoute();
                        destRoute.EvaluateRoute();

                        // Se agrega el cliente movido a la tabla de clientes cambiados...
                        clientsChanged[clientToMoveID] = indexDestRoute;

                        if( !mutated )
                            mutated = true;

                        break;
                    }
                }
            }

            //  Si ocurri� la mutaci�n, se recalcula la calidad del cromosoma y se actualiza la tabla
            // de clientes cambiados...
            if( mutated )
            {
                toMutate.CalculateFitness();

                foreach( int clientChanged in clientsChanged.Keys )
                    clientAsignation[clientChanged] = clientsChanged[clientChanged];
            }
        }

        /* M�todo 'RouteMutation'
         *  Operador de mutaci�n de rutas del modelo basado en rutas. Evalua todas las posibilidades
         * de mutaci�n de cada una de las rutas internas del cromosoma, de forma aleatoria. No asegura
         * que la mutaci�n efectivamente ocurra.
         * 
         *  Par�metros:
         * - 'Genome toMutate': Cromosoma que mutar�.
         * - 'double chance': Probabilidad (0.0-1.0) de que un gen en el cromosoma mute.
         */
        [Operator( "Mutaci�n de Rutas para genotipo de rutas." )]
        public static void RouteMutation( Genome toMutate, double chance )
        {
            RouteGenome genome = (RouteGenome)toMutate;
            bool mutated = false;

            RandomUniqueSelector routeSelector = new RandomUniqueSelector( genome.Count - 1 );

            // Se revisan una por una las rutas del cromosoma...
            while( !routeSelector.IsCompleted )
            {              
                Route routeToCheck = genome.GetRoute( routeSelector.Next() );

                //  Si la ruta no tiene puntos intercambiables o no se cumple la probabilidad, se sigue
                // con la siguiente ruta...
                if( routeToCheck.SwapablePointsCount == 0 || chance < RandomTool.GetDouble() )
                    continue;

                RandomUniqueSelector swapPointsSelector = new RandomUniqueSelector( routeToCheck.SwapablePointsCount - 1 );

                // Se revisan uno a uno los posibles puntos de intercambio de la ruta...
                while( !swapPointsSelector.IsCompleted )
                {
                    Event toSwap = routeToCheck.SwapablePoints[swapPointsSelector.Next()];

                    // Se evalua si es factible el intercambio en el punto seleccionado...
                    if( RouteGeneration.TrySwap( routeToCheck, toSwap, toSwap.Next ) )
                    {
                        // De ser factible, se reevalua la ruta y si sigue con la pr�xima...
                        routeToCheck.EvaluateRoute();

                        if( !mutated )
                            mutated = true;

                        break;
                    }
                }
            }

            // Si se gener� una mutaci�n, se recalcula la calidad del cromosoma.
            if( mutated )            
                genome.CalculateFitness();
        }

        /* M�todo 'GetStringRoutes'
         *  Retorna una representaci�n en string de las rutas del cromosoma, con el formato:
         * "1: <evento_1_ruta_1> <evento_2_ruta_1>... <evento_n_ruta_1>
         *  2: <evento_1_ruta_2> <evento_2_ruta_2>... <evento_n_ruta_2>
         *  ...
         *  m: <evento_1_ruta_m> <evento_2_ruta_m>... <evento_n_ruta_m>"
         */
        public string GetStringRoutes()
        {
            if( m_Routes == null )
                return "(VACIO)";
            
            string ret = "";

            for( int i = 0; i < m_Routes.Length; i++ )
            {
                ret += i + ": ";
                
                if( m_Routes[i] == null )
                    ret += "(NULL)\r\n";
                else
                    ret += m_Routes[i] + "\r\n";
            }

            return ret;
        }

        /* M�todo 'GetStringMasks'
         *  Retorna una representaci�n en string de las m�scaras del cromosoma, con el formato:
         * "1: <mascara_ruta_1>
         *  2: <mascara_ruta_2>
         *  ...
         *  m: <mascara_ruta_m>"
         */
        public string GetStringMasks()
        {
            if( m_Masks == null )
                return "(VACIO)";

            string ret = "";

            for( int i = 0; i < m_Masks.Length; i++ )
            {
                ret += i + ": ";

                if( m_Masks[i] == null )
                    ret += "(NULL)\r\n";
                else
                    ret += m_Masks[i] + "\r\n";
            }

            return ret;
        }
    }
}
