/* LLGAGenome.cs
 * �ltima modificaci�n: 13/03/2008
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using DARPTW_GA.GA_Base;
using DARPTW_GA.DARP;
using DARPTW_GA.Framework;
using DARPTW_GA.Framework.Routing;
using DARPTW_GA.Misc;

namespace DARPTW_GA.Framework.Genomes
{
    /* Clase 'LLGAGen'
     *  Representa un gen Cliente/Veh�culo para el tipo de cromosoma LLGA.
     * 
     *  Atributos:
     * - 'int m_Vehicle': Veh�culo asignado al gen.
     * - 'int m_Client': Identificador del cliente asignado al gen.
     */
    public class LLGAGen : ICloneable
    {
        private int m_Vehicle;
        private int m_Client;

        public int Vehicle{ get { return m_Vehicle; } set { m_Vehicle = value; } }
        public int Client { get { return m_Client; } }

        /* Constructor
         *  Crea un nuevo gen en base a un veh�culo y cliente.
         * 
         *  Par�metros:
         * - 'int vehicle': Veh�culo que se asignar�.
         * - 'int client': Id del cliente que se asignar�.
         */
        public LLGAGen( int vehicle, int client )
        {
            m_Vehicle = vehicle;
            m_Client = client;
        }

        /* M�todo 'Clone'
         *  Crea una copia del gen actual y la retorna como tipo 'Object'.
         */ 
        public object Clone()
        {
            return new LLGAGen( m_Vehicle, m_Client );
        }

        /* M�todo 'ToString' (sobrecargado de 'Object')
         *  Retorna una representaci�n en string del gen actual, con el formato
         * "(<id_cliente> <nr_vehiculo>)".
         */
        public override string ToString()
        {
            return String.Format( "({0} {1})", m_Client, m_Vehicle );
        }
    }

    /* Clase 'LLGAGenome'
     *  Representa un cromosoma LLGA, compuesto por genes de tipo 'LLGAGen'. Contiene una
     * representaci�n interna de las rutas y sus m�scaras asociadas, y una representaci�n externa de
     * los genes donde se especifican los clusters asignados. Adicionalmente contiene todos los
     * operadores relativos a la representaci�n.
     * 
     *  Atributos:
     * - 'Evaluation m_Evaluation': Referencia al objeto que guarda la informaci�n de calidad del
     * cromosoma.
     * - 'Route[] m_Routes': Arreglo interno con las rutas asignadas al cromosoma. El �ndice del
     * arreglo indica el n�mero del veh�culo respectivo.
     * - 'ClientMask[] m_Masks': Arreglo interno con las m�scaras de clientes asociadas a las rutas
     * del cromosoma.
     * - 'List<LLGAGen> m_Genes': Lista de los genes que componen este cromosoma.
     * - 'Dictionary<int, int> m_GenIndex': Tabla que permite obtener directamente la asignaci�n de
     * los clientes a los veh�culos. La llave corresponde a el identificador del cliente y el valor a
     * su veh�culo asignado.
     */
    public class LLGAGenome : Genome
    {
        private Evaluation m_Evaluation;

        private Route[] m_Routes;
        private ClientMask[] m_Masks;

        private List<LLGAGen> m_Genes;
        private Dictionary<int, int> m_GenIndex;

        public int ClientCount { get { return m_Genes.Count; } }
        public double Fitness { get { return m_Evaluation.Result; } }
        public Evaluation Evaluation { get { return m_Evaluation; } }

        /* Constructor
         *  Instancia un nuevo cromosoma sin datos de entrada, usado de forma interna en la clase.
         */ 
        private LLGAGenome()
        {
        }

        /* Constructor
         *  Instancia un nuevo cromosoma en base a las rutas y sus m�scaras, generando en base a
         * ellas los genes con un ordenamiento aleatorio y calculando la calidad correspondiente.
         * 
         *  Par�metros:
         * - 'ClientMask[] masks': Arreglo con las m�scaras que se usar�n en el cromosoma.
         * - 'Route[] routes': Arreglo con las rutas que se usar�n en el cromosoma.
         */
        public LLGAGenome( ClientMask[] masks, Route[] routes )
        {
            m_Routes = routes;
            m_Masks = masks;

            GenerateGenes();
            CalculateFitness();
        }

        /* M�todo 'GenerateRandomGenome'
         *  M�todo est�tico que retorna una instancia aleatoria de un cromosoma de este tipo,
         * con una estructura factible.
         */ 
        public static LLGAGenome GenerateRandomGenome()
        {
            Route[] routes = null;
            ClientMask[] masks = null;

            SolutionGeneration.GenerateSolution( out masks, out routes );

            return new LLGAGenome( masks, routes );
        }

        /* M�todo 'GenerateGenes'
         *  M�todo que genera la lista de genes con un ordenamiento aleatorio, en base al arreglo
         * interno de rutas. Tambi�n se genera la tabla de asignaci�n de clientes.
         */
        private void GenerateGenes()
        {
            m_Genes = new List<LLGAGen>( GlobalParams.ClientNumber );
            m_GenIndex = new Dictionary<int, int>( GlobalParams.ClientNumber );

            // Este ciclo genera la lista de genes...
            for( int i = 0; i < m_Routes.Length; i++ )
            {
                List<int> clientList = m_Routes[i].ClientList;

                for( int j = 0; j < clientList.Count; j++ )
                {
                    int posToInsert = RandomTool.GetInt( m_Genes.Count - 1 );
                    m_Genes.Insert( posToInsert, new LLGAGen( i, clientList[j] ) );                    
                }
            }

            // Con los genes generados, se puede generar la tabla de asignaci�n de clientes.
            for( int i = 0; i < m_Genes.Count; i++ )
                m_GenIndex[m_Genes[i].Client] = i;
        }

        /* M�todo 'CalculateFitness'
         *  Genera la evaluaci�n de la calidad del cromosoma en base al arreglo interno de rutas.
         */
        private void CalculateFitness()
        {
            m_Evaluation = new Evaluation( m_Routes );
        }

        /* M�todo 'GetRandomSegmentBounds'
         *  Asigna un par de �ndices aleatorios de la lista de cromosomas a modo de segmento.
         * 
         *  Par�metros:
         * - 'int start': Referencia al indice de partida del segmento que se asignar�.
         * - 'int end': Referencia al indice de termino del segmento que se asignar�.
         */
        public void GetRandomSegmentBounds( out int start, out int end )
        {
            end = RandomTool.GetInt( m_Genes.Count - 1 );
            start = RandomTool.GetInt( end );
        }

        /* M�todo 'GetRandomPoint'
         *  Retorna un indice aleatorio de la lista de cromosomas, a modo de punto de inserci�n.
         */
        public int GetRandomPoint()
        {
            return RandomTool.GetInt( m_Genes.Count - 1 );
        }

        /* M�todo 'TryDonation'
         *  Evalua la recombinaci�n entre este cromosoma y otro especificado, considerando un punto de
         * inserci�n en el primero y un segmento a donar en el segundo. Retorna por referencia el
         * nuevo cromosoma en caso de que sea exitosa la recombinaci�n (null en otro caso) y un
         * booleano indicando esto �ltimo.
         * 
         *  Par�metros:
         * - 'LLGAGenome donor': Referencia al cromosoma del que se obtendr� el segmento a donar.
         * - 'int startSegment': �ndice de inicio del segmento a donar.
         * - 'int endSegment': �ndice de fin del segmento a donar.
         * - 'int insPoint': �ndice del punto de inserci�n a utilizar.
         * - 'LLGAGenome newGenome': Referencia de salida para el eventual nuevo cromosoma resultante.
         */
        public bool TryDonation( LLGAGenome donor, int startSegment, int endSegment, int insPoint, out LLGAGenome newGenome )
        {
            newGenome = null;
            
            // Lista de genes del cromosoma donante...
            List<LLGAGen> donorGenes = donor.m_Genes;

            //  Tabla que guarda las rutas que ser�n reemplazadas en el cromosoma actual tras el
            // proceso de donaci�n de genes. Es necesario guardarlas aparte y no modificar las que
            // existen en el mismo cromosoma, en caso de que en un determinado punto de la verificaci�n
            // de la donaci�n, se obtenga infactibilidad de la misma.
            // La llave corresponde a la posici�n a reemplazar y el valor es la ruta por la cual se
            // reemplazar�...
            Dictionary<int, Route> routesToReplace = new Dictionary<int, Route>( m_Routes.Length );

            //  Tabla que guarda las m�scaras que ser�n reemplazadas en el cromosoma actual, de la
            // misma forma que las rutas...
            Dictionary<int, ClientMask> masksToReplace = new Dictionary<int, ClientMask>( m_Routes.Length );

            // Lista con los genes del segmento que se insertar� en el cromosoma actual..
            List<LLGAGen> newSegment = new List<LLGAGen>( endSegment - startSegment );

            // Lista con los clientes que podr� contener el segmento a donar...
            List<int> clientsInSegment = new List<int>( endSegment - startSegment );

            // Se evalua uno por uno los genes que se donar�n...
            for( int i = startSegment; i <= endSegment; i++ )
            {
                // Gen a evaluar...
                LLGAGen donorGen = donor.m_Genes[i];

                // id del cliente que cambiar�...
                int clientToMoveID = donorGen.Client;

                // Ruta que actualmente tiene el cliente a cambiar en el cromosoma...
                int oldRouteIndex = m_Genes[m_GenIndex[clientToMoveID]].Vehicle;

                // Ruta en la que deber� quedar el cliente tras la donaci�n...
                int newRouteIndex = donorGen.Vehicle;

                //  Si la ruta origen es igual a la de destino, s�lo se copia el gen en el nuevo
                // segmento y se continua con otro gen...
                if( oldRouteIndex == newRouteIndex )
                {
                    newSegment.Add( (LLGAGen)donorGen.Clone() );
                    clientsInSegment.Add( clientToMoveID );

                    continue;
                }

                //  Se obtiene la instancia de ruta a la que se mover� el cliente del gen tras la
                // donaci�n. Es necesario verificar si esta ruta ya ha sido modificada en los ciclos
                // anteriores, en ese caso su posici�n existir� en la tabla respectiva. Si no es as�
                // ser� necesario clonar la ruta antes de modificar la misma, para no hacer cambios
                // en la original del cromosoma recipiente...
                Route destRoute;
                bool cloneDestRoute = false;
                
                if( routesToReplace.ContainsKey( newRouteIndex ) )
                {
                    destRoute = routesToReplace[newRouteIndex];
                }
                else
                {
                    destRoute = m_Routes[newRouteIndex];
                    cloneDestRoute = true;
                }           

                //  Chequeo de dependencia ciclica. Se evalua si en la ruta a la que se mover� el
                // cliente, existen otros clientes incompatibles, para descartar infactibilidades
                // de antemano...
                List<int> incompatibleClients;
                Clients.CyclicDependenceTable.TryGetValue( clientToMoveID, out incompatibleClients );
                
                if( incompatibleClients != null )
                {
                    foreach( int incompatibleClient in incompatibleClients )
                    {
                        if( destRoute.ClientList.BinarySearch( incompatibleClient ) > 0 )
                            return false;
                    }
                }

                //  Se obtiene la instancia de ruta desde la que se mover� el cliente del gen tras la
                // donaci�n, evaluando la necesidad de clonaci�n de la misma tal como se hizo con la
                // ruta destino.
                Route origRoute;
                bool cloneOrigRoute = false;

                if( routesToReplace.ContainsKey( oldRouteIndex ) )
                {
                    origRoute = routesToReplace[oldRouteIndex];
                }
                else
                {
                    origRoute = m_Routes[oldRouteIndex];
                    cloneOrigRoute = true;
                }  

                // Se clonan las rutas si corresponde...
                Route routeToRemove = ( cloneOrigRoute ? (Route)origRoute.Clone() : origRoute );
                Route routeToInsert = ( cloneDestRoute ? (Route)destRoute.Clone() : destRoute ); 

                Client clientToMove = Clients.GetClient( clientToMoveID );

                //  Se evalua tanto la eliminaci�n del cliente desde la ruta origen, como la inserci�n
                // del mismo en la ruta destino...
                if( RouteGeneration.CheckClientDeletion( routeToRemove, clientToMove ) && RouteGeneration.CheckClientInsertion( routeToInsert, clientToMove ) )
                {
                    //  Si en este ciclo se clon� la ruta origen, se debe hacer lo mismo con su
                    // m�scara asociada, sumado a la modificaci�n correspondiente del cliente
                    // cambiado...
                    if( cloneOrigRoute )
                    {
                        routesToReplace[oldRouteIndex] = routeToRemove;

                        ClientMask maskToRemove = (ClientMask)m_Masks[oldRouteIndex].Clone();
                        maskToRemove[clientToMoveID] = false;

                        masksToReplace[oldRouteIndex] = maskToRemove;
                    }
                    //  Si no se clon�, la m�scara modificada ya existe y se puede actualizar
                    // directamente..
                    else
                    {
                        masksToReplace[oldRouteIndex][clientToMoveID] = false;
                    }

                    // Se hace exactamente lo mismo con la ruta destino...
                    if( cloneDestRoute )
                    {
                        routesToReplace[newRouteIndex] = routeToInsert;

                        ClientMask maskToInsert = (ClientMask)m_Masks[newRouteIndex].Clone();
                        maskToInsert[clientToMoveID] = true;

                        masksToReplace[newRouteIndex] = maskToInsert;
                    }
                    else
                    {
                        masksToReplace[newRouteIndex][clientToMoveID] = true;
                    }

                    //  Se re-evaluan las rutas modificadas, para que sus datos esten actualizados y
                    // sean consistentens para futuras modificaciones...
                    routeToRemove.EvaluateRoute();
                    routeToInsert.EvaluateRoute();

                    // Se actualiza la informaci�n del nuevo segmento...
                    newSegment.Add( (LLGAGen)donorGen.Clone() );
                    clientsInSegment.Add( clientToMoveID );
                }
                //  Si el checkeo de inserci�n/eliminaci�n resulta negativo, es imposible realizar la
                // donaci�n...
                else
                {
                    return false;
                }
            }

            // En estas instancias, la donaci�n es factible, entonces se genera el nuevo cromosoma...

            //  Se genera la nueva estructura interna del cromosoma, revisando cada ruta del cromosoma
            // recipiente...
            Route[] newRoutes = new Route[m_Routes.Length];
            ClientMask[] newMasks = new ClientMask[m_Masks.Length];
            
            for( int i = 0; i < m_Routes.Length; i++ )
            {
                Route routeToAdd;
                ClientMask maskToAdd;

                //  Si el indice de la ruta actual esta en la tabla de rutas a reemplazar, se asigna
                // directamente a la estructura interna del nuevo cromosoma junto a su m�scara...
                if( routesToReplace.ContainsKey( i ) )
                {
                    routeToAdd = routesToReplace[i];
                    maskToAdd = masksToReplace[i];
                }
                //  En caso contrario, se clona la ruta (con su m�scara)y se asigna. Esto no se realiza
                // antes para no ejecutar clonaciones innecesarias...
                else
                {
                    routeToAdd = (Route)m_Routes[i].Clone();
                    maskToAdd = (ClientMask)m_Masks[i].Clone();
                }

                newRoutes[i] = routeToAdd;
                newMasks[i] = maskToAdd;
            }

            //  Se genera el nuevo set de genes del cromosoma, recorriendo el que existe actualmente
            // y haciendo los cambios seg�n corresponda, lo que incluye la actualizaci�n de la tabla
            // de clientes/veh�culos...
            clientsInSegment.Sort();
            List<LLGAGen> newGenes = new List<LLGAGen>( m_Genes.Count );
            Dictionary<int, int> newGenIndex = new Dictionary<int, int>( m_Genes.Count );
            int actualIndex = 0;

            for( int i = 0; i < m_Genes.Count; i++ )
            {
                //  Si la posici�n del gen actual corresponde al punto de inserci�n, se agregan de
                // corrido todos los genes del nuevo segmento...
                if( i == insPoint )
                {
                    for( int j = 0; j < newSegment.Count; j++ )
                    {
                        newGenes.Add( newSegment[j] );
                        newGenIndex[newSegment[j].Client] = actualIndex;
                        actualIndex++;
                    }
                }
                
                //  Si el id del cliente del gen actual esta en la lista de los clientes en el nuevo
                // segmento, se trata de un gen que ser� (o fue) reemplazado por el que esta contenido
                // en el mismo, por ello no se considera en el nuevo set...
                if( clientsInSegment.BinarySearch( m_Genes[i].Client ) < 0 )
                {
                    newGenes.Add( (LLGAGen)m_Genes[i].Clone() );
                    newGenIndex[m_Genes[i].Client] = actualIndex;
                    actualIndex++;                        
                }                
            }

            // Se crea el nuevo cromosoma y se evalua.
            newGenome = new LLGAGenome();

            newGenome.m_Genes = newGenes;
            newGenome.m_GenIndex = newGenIndex;
            newGenome.m_Masks = newMasks;
            newGenome.m_Routes = newRoutes;

            newGenome.CalculateFitness();
            
            return true;
        }

        /* M�todo 'DoCrossover'
         *  Operador de recombinaci�n del LLGA. Realiza la recombinaci�n en base a un arreglo de
         * cromosomas padres, de los que se obtiene un segmento de donaci�n y el punto de inserci�n.
         * Se realizan dos recombinaci�n intercambiando los roles de donante y recipiente para los
         * padres. Se realizan intentos de crossover hasta obtener resultados factibles.
         * 
         *  Par�metros:
         * - 'Genome[] parents': Arreglo de los cromosomas padres usados en la recombinaci�n.
         */
        [Operator( "Recombinaci�n LLGA" )]
        public static Genome[] DoCrossover( Genome[] parents )
        {
            LLGAGenome childA = null;
            LLGAGenome childB = null;

            LLGAGenome parentA = (LLGAGenome)parents[0];
            LLGAGenome parentB = (LLGAGenome)parents[1];

            bool crossed = false;

            do
            {
                int startSegment = 0;
                int endSegment = 0;
                parentB.GetRandomSegmentBounds( out startSegment, out endSegment );

                crossed = parentA.TryDonation( parentB, startSegment, endSegment, parentA.GetRandomPoint(), out childA );
            }
            while( !crossed );

            crossed = false;

            do
            {
                int startSegment = 0;
                int endSegment = 0;
                parentA.GetRandomSegmentBounds( out startSegment, out endSegment );

                crossed = parentB.TryDonation( parentA, startSegment, endSegment, parentB.GetRandomPoint(), out childB );
            }
            while( !crossed );

            return new Genome[] { childA, childB };
        }

        /* M�todo 'ClusterMutation'
         *  Operador de mutaci�n de clusters del LLGA. Evalua todas las posibilidades de mutaci�n
         * de cada uno de los genes del cromosoma, de forma aleatoria. No asegura que la mutaci�n
         * efectivamente ocurra.
         * 
         *  Par�metros:
         * - 'Genome genome': Cromosoma que mutar�.
         * - 'double chance': Probabilidad (0.0-1.0) de que un gen en el cromosoma mute.
         */
        [Operator( "Mutaci�n de Clusters LLGA" )]
        public static void ClusterMutation( Genome genome, double chance )
        {
            LLGAGenome toMutate = (LLGAGenome)genome;

            Route[] routes = toMutate.m_Routes;
            ClientMask[] masks = toMutate.m_Masks;
            List<LLGAGen> genes = toMutate.m_Genes;

            bool mutated = false;

            // Se evalua cada gen en el cromosoma...
            for( int i = 0; i < genes.Count; i++ )
            {
                // Si no se cumple la probabilidad, se contin�a...
                if( chance < RandomTool.GetDouble() )
                    continue;

                // �ndice e instancia de la ruta origen del cliente en el gen...
                int indexOrigRoute = genes[i].Vehicle;
                Route origRoute = routes[indexOrigRoute];

                RandomUniqueSelector destRouteSelector = new RandomUniqueSelector( routes.Length - 1 );

                // Se revisa cada ruta como posible destino...
                while( !destRouteSelector.IsCompleted )
                {  
                    int indexDestRoute = 0;
                    Route destRoute = null;

                    // Se selecciona cualquier ruta aleatoriamente, menos la de origen...
                    while( destRoute == null && !destRouteSelector.IsCompleted )
                    {
                        indexDestRoute = destRouteSelector.Next();

                        if( indexOrigRoute == indexDestRoute )
                            destRoute = null;
                        else
                            destRoute = routes[indexDestRoute];
                    }

                    // Al ser null, ya se revisaron todas las rutas posibles para insertar al cliente...
                    if( destRoute == null )
                        break;

                    //  Chequeo de dependencia ciclica. Se revisa si la nueva ruta a la que ser�
                    // asignado el cliente a modificar, contiene clientes incompatibles respecto a �l.
                    // De ser asi no se podr� insertar al cliente en dicha ruta.
                    int clientToMoveID = genes[i].Client;

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
                        // corresponde. La ruta destina ya esta modificada asi que no se considera...
                        routes[indexOrigRoute] = origRouteClone;

                        // Se modifican las m�scaras de ambas rutas de forma conveniente...
                        masks[indexOrigRoute][clientToMoveID] = false;
                        masks[indexDestRoute][clientToMoveID] = true;

                        // Se evaluan las rutas para su consistencia en posteriores procesos...
                        origRouteClone.EvaluateRoute();
                        destRoute.EvaluateRoute();
                        
                        // Finalmente se modifica el gen con el nuevo veh�culo asignado...
                        genes[i].Vehicle = indexDestRoute;

                        if( !mutated )
                            mutated = true;

                        break;
                    }              
                }
            }

            // Si ocurri� la mutaci�n, se recalcula la calidad del cromosoma.
            if( mutated )
                toMutate.CalculateFitness();
        }

        /* M�todo 'RouteMutation'
         *  Operador de mutaci�n de rutas del LLGA. Evalua todas las posibilidades de mutaci�n
         * de cada una de las rutas internas del cromosoma, de forma aleatoria. No asegura que la
         * mutaci�n efectivamente ocurra.
         * 
         *  Par�metros:
         * - 'Genome toMutate': Cromosoma que mutar�.
         * - 'double chance': Probabilidad (0.0-1.0) de que un gen en el cromosoma mute.
         */
        [Operator( "Mutaci�n de Rutas LLGA" )]
        public static void RouteMutation( Genome toMutate, double chance )
        {
            LLGAGenome genome = (LLGAGenome)toMutate;
                        
            RandomUniqueSelector routeSelector = new RandomUniqueSelector( genome.m_Routes.Length - 1 );
            bool mutated = false;

            // Se revisan una por una las rutas del cromosoma...
            while( !routeSelector.IsCompleted )
            {
                Route routeToCheck = genome.m_Routes[routeSelector.Next()];

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

        /* M�todo 'GetStringGenes'
         *  Retorna una representaci�n en string del cromosoma, con el formato:
         * "(<id_cliente_1> <nr_vehiculo>)... (<id_cliente_n> <nr_vehiculo>)"
         */
        public string GetStringGenes()
        {
            string res = "";

            for( int i = 0; i < m_Genes.Count; i++ )
                res += m_Genes[i] + " ";

            return res;
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