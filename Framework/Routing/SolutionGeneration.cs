/* SolutionGeneration.cs
 * �ltima modificaci�n: 11/04/2008
 */

using System;
using System.Collections.Generic;
using System.Text;
using DARPTW_GA.DARP;
using DARPTW_GA.Misc;

namespace DARPTW_GA.Framework.Routing
{
    /* Clase 'SolutionGeneration'
     *  Clase est'atica que contiene un set de m�todos destinados a la generaci�n de soluciones completas
     * para el problema (set de rutas con una programaci�n definida para cada veh�culo en la soluci�n)
     * en distintos contextos de factibilidad.
     */
    public static class SolutionGeneration
    {
        /* M�todo 'TryGenerateBaseFactibleSolution'
         *  Evalua la generaci�n factible de una soluci�n base, esto es, aquella que contiene s�lo los
         * clientes que pueden entrar en conflicto por no poder compartir un veh�culo (clientes incompatibles).
         * Puede resultar tanto una generaci�n positiva como negativa.
         * 
         *  Par�metros:
         * - 'List<int> clientsRemaining': Como en este m�todo se genera una soluci�n base, en esta referencia
         * se almacenar� una lista para identificar los clientes que faltan para completar la soluci�n.
         * - 'ClientMask[] masks': Referencia a la cual se asignar�n las m�scaras de la soluci�n base.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignar�n las rutas de la soluci�n base.
         */
        public static bool TryGenerateBaseFactibleSolution( out List<int> clientsRemaining, out ClientMask[] masks, out Route[] generatedRoutes )
        {
            // M�scara que mostrar� a los clientes que faltan por insertar en la soluci�n...
            ClientMask clientsRemainingMask = ClientMask.Full;  
            // Diccionario que guardar� la tabla que describe la asignaci�n de clientes a veh�culos...
            Dictionary<int, int> clientAsignation = new Dictionary<int, int>( GlobalParams.ClientNumber );

            clientsRemaining = null;
            generatedRoutes = new Route[GlobalParams.VehiclesMaxNumber];
            masks = new ClientMask[GlobalParams.VehiclesMaxNumber];

            // Se inicializan tanto las m�scaras como las rutas del resultado...
            for( int i = 0; i < masks.Length; i++ )
            {
                masks[i] = new ClientMask( GlobalParams.ClientNumber );
                generatedRoutes[i] = new Route();
            }

            //  Ciclo principal. Se basa en utilizar un selector de veh�culos para insertar a cada cliente.
            // Para evitar colocar clientes incompatibles en un mismos veh�culo, antes de empezar a usar el
            // selector, se asumen como ya seleccionados los veh�culos donde ya hay insertados clientes que
            // son incompatibles con el actual...
            foreach( int client in Clients.CyclicDependenceTable.Keys )
            {
                List<int> incompatibleClients = Clients.CyclicDependenceTable[client];
                RandomUniqueSelector vehicleSelector = new RandomUniqueSelector( generatedRoutes.Length - 1 );

                foreach( int incompatibleClient in incompatibleClients )
                {
                    //  Para cada cliente incompatible en la lista del actual, se revisa si esta insertado
                    // ya en un veh�culo...
                    if( clientAsignation.ContainsKey( incompatibleClient ) )
                    {
                        int incompatibleRoute = clientAsignation[incompatibleClient];
 
                        //  Si se detecta un cliente incompatible asignado, se asume su veh�culo como no
                        // asignable...
                        if( vehicleSelector[incompatibleRoute] == true )
                            vehicleSelector.ClearValue( incompatibleRoute );
                    }
                }
                
                // Asumiendo que el cliente actual no ha sido insertado, se procede a intentar una inserci�n...
                bool inserted = false;

                while( !inserted && !vehicleSelector.IsCompleted )
                {
                    // Se elige una ruta aleatoria...
                    int routeSelected = vehicleSelector.Next();

                    ClientMask maskToCheck = masks[routeSelected];

                    // Se intenta la inserci�n...
                    if( RouteGeneration.CheckClientInsertion( generatedRoutes[routeSelected], Clients.GetClient( client ) ) )
                    {
                        inserted = true;
                        maskToCheck[client] = true;
                    }
                }

                //  A estas alturas, si el cliente no ha sido insertado y se prob� con todos los veh�culos
                // posibles (excluyendo los de los clientes incompatibles), la generaci�n de la soluci�n 
                // base se vuelve infactible...
                if( !inserted )                
                    return false;

                // Se actualiza la m�scara de clientes restantes con la inserci�n del actual...
                clientsRemainingMask[client] = false;
            }

            //  A estas alturas, la generaci�n fue factible. Finalmente se genera la lista de clientes
            // restantes en base a la m�scara usada.
            clientsRemaining = clientsRemainingMask.GetClientList();

            return true;
        }

        /* M�todo 'TryGenerateSolution'
         *  Evalua la generaci�n factible de una soluci�n completa. Puede resultar tanto una generaci�n
         * positiva como negativa.
         * 
         *  Par�metros:
         * - 'ClientMask[] masks': Referencia a la cual se asignar�n las m�scaras de la soluci�n completa.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignar�n las rutas de la soluci�n completa.
         */
        public static bool TryGenerateSolution( out ClientMask[] masks, out Route[] routes )
        {
            List<int> clientsRemaining = null;
            routes = null;
            masks = null;

            //  Se intenta en primera instancia el generar la soluci�n base. Si el resultado es negativo,
            // se retorna un resultado negativo tambi�n...
            if( !TryGenerateBaseFactibleSolution( out clientsRemaining, out masks, out routes ) )
                return false;            
            
            // Con las rutas base, se realizan inserciones para cada uno de los clientes que faltan...
            while( clientsRemaining.Count > 0 )
            {
                // Se selecciona aleatoriamente uno de los clientes restantes...
                int indexOfToInsert = RandomTool.GetInt( clientsRemaining.Count - 1 );
                int toInsert = clientsRemaining[indexOfToInsert];
                RandomUniqueSelector routeSelector = new RandomUniqueSelector( masks.Length - 1 );

                //  Se prueba de forma aleatoria la inserci�n factible del cliente en las rutas de la
                // soluci�n...
                while( !routeSelector.IsCompleted )
                {
                    int routeIndex = routeSelector.Next();

                    // Revisi�n de que la ruta seleccionada no sea nula...
                    if( routes[routeIndex] == null )
                        routes[routeIndex] = new Route();

                    // Se prueba la inserci�n...
                    if( RouteGeneration.CheckClientInsertion( routes[routeIndex], Clients.GetClient( toInsert ) ) )
                    {
                        masks[routeIndex][toInsert] = true;
                        clientsRemaining.RemoveAt( indexOfToInsert );
                        break;
                    }
                    //  Si la inserci�n falla y no quedan rutas por insertar, se retorna inmediatamente el
                    // resultado negativo...
                    else if( routeSelector.IsCompleted )
                    {                        
                        return false;
                    }
                }
            }

            //  A estas alturas, la generaci�n de la soluci�n completa fue positiva, por ende se eval�an las
            // rutas generadas.
            for( int i = 0; i < routes.Length; i++ )
                routes[i].EvaluateRoute();

            return true;
        }

        /* M�todo 'GenerateSolution'
         *  Realiza la generaci�n de una soluci�n completa. Se realizan intentos iterativos hasta generar
         * una soluci�n factible.
         * 
         *  Par�metros:
         * - 'ClientMask[] masks': Referencia a la cual se asignar�n las m�scaras de la soluci�n generada.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignar�n las rutas de la soluci�n generada.
         */
        public static void GenerateSolution( out ClientMask[] masks, out Route[] routes )
        {
            masks = null;
            routes = null;

            bool check = false;

            do
            {
                check = TryGenerateSolution( out masks, out routes );
            }
            while( !check );
        }
    }
}
