/* SolutionGeneration.cs
 * Última modificación: 11/04/2008
 */

using System;
using System.Collections.Generic;
using System.Text;
using DARPTW_GA.DARP;
using DARPTW_GA.Misc;

namespace DARPTW_GA.Framework.Routing
{
    /* Clase 'SolutionGeneration'
     *  Clase est'atica que contiene un set de métodos destinados a la generación de soluciones completas
     * para el problema (set de rutas con una programación definida para cada vehículo en la solución)
     * en distintos contextos de factibilidad.
     */
    public static class SolutionGeneration
    {
        /* Método 'TryGenerateBaseFactibleSolution'
         *  Evalua la generación factible de una solución base, esto es, aquella que contiene sólo los
         * clientes que pueden entrar en conflicto por no poder compartir un vehículo (clientes incompatibles).
         * Puede resultar tanto una generación positiva como negativa.
         * 
         *  Parámetros:
         * - 'List<int> clientsRemaining': Como en este método se genera una solución base, en esta referencia
         * se almacenará una lista para identificar los clientes que faltan para completar la solución.
         * - 'ClientMask[] masks': Referencia a la cual se asignarán las máscaras de la solución base.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignarán las rutas de la solución base.
         */
        public static bool TryGenerateBaseFactibleSolution( out List<int> clientsRemaining, out ClientMask[] masks, out Route[] generatedRoutes )
        {
            // Máscara que mostrará a los clientes que faltan por insertar en la solución...
            ClientMask clientsRemainingMask = ClientMask.Full;  
            // Diccionario que guardará la tabla que describe la asignación de clientes a vehículos...
            Dictionary<int, int> clientAsignation = new Dictionary<int, int>( GlobalParams.ClientNumber );

            clientsRemaining = null;
            generatedRoutes = new Route[GlobalParams.VehiclesMaxNumber];
            masks = new ClientMask[GlobalParams.VehiclesMaxNumber];

            // Se inicializan tanto las máscaras como las rutas del resultado...
            for( int i = 0; i < masks.Length; i++ )
            {
                masks[i] = new ClientMask( GlobalParams.ClientNumber );
                generatedRoutes[i] = new Route();
            }

            //  Ciclo principal. Se basa en utilizar un selector de vehículos para insertar a cada cliente.
            // Para evitar colocar clientes incompatibles en un mismos vehículo, antes de empezar a usar el
            // selector, se asumen como ya seleccionados los vehículos donde ya hay insertados clientes que
            // son incompatibles con el actual...
            foreach( int client in Clients.CyclicDependenceTable.Keys )
            {
                List<int> incompatibleClients = Clients.CyclicDependenceTable[client];
                RandomUniqueSelector vehicleSelector = new RandomUniqueSelector( generatedRoutes.Length - 1 );

                foreach( int incompatibleClient in incompatibleClients )
                {
                    //  Para cada cliente incompatible en la lista del actual, se revisa si esta insertado
                    // ya en un vehículo...
                    if( clientAsignation.ContainsKey( incompatibleClient ) )
                    {
                        int incompatibleRoute = clientAsignation[incompatibleClient];
 
                        //  Si se detecta un cliente incompatible asignado, se asume su vehículo como no
                        // asignable...
                        if( vehicleSelector[incompatibleRoute] == true )
                            vehicleSelector.ClearValue( incompatibleRoute );
                    }
                }
                
                // Asumiendo que el cliente actual no ha sido insertado, se procede a intentar una inserción...
                bool inserted = false;

                while( !inserted && !vehicleSelector.IsCompleted )
                {
                    // Se elige una ruta aleatoria...
                    int routeSelected = vehicleSelector.Next();

                    ClientMask maskToCheck = masks[routeSelected];

                    // Se intenta la inserción...
                    if( RouteGeneration.CheckClientInsertion( generatedRoutes[routeSelected], Clients.GetClient( client ) ) )
                    {
                        inserted = true;
                        maskToCheck[client] = true;
                    }
                }

                //  A estas alturas, si el cliente no ha sido insertado y se probó con todos los vehículos
                // posibles (excluyendo los de los clientes incompatibles), la generación de la solución 
                // base se vuelve infactible...
                if( !inserted )                
                    return false;

                // Se actualiza la máscara de clientes restantes con la inserción del actual...
                clientsRemainingMask[client] = false;
            }

            //  A estas alturas, la generación fue factible. Finalmente se genera la lista de clientes
            // restantes en base a la máscara usada.
            clientsRemaining = clientsRemainingMask.GetClientList();

            return true;
        }

        /* Método 'TryGenerateSolution'
         *  Evalua la generación factible de una solución completa. Puede resultar tanto una generación
         * positiva como negativa.
         * 
         *  Parámetros:
         * - 'ClientMask[] masks': Referencia a la cual se asignarán las máscaras de la solución completa.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignarán las rutas de la solución completa.
         */
        public static bool TryGenerateSolution( out ClientMask[] masks, out Route[] routes )
        {
            List<int> clientsRemaining = null;
            routes = null;
            masks = null;

            //  Se intenta en primera instancia el generar la solución base. Si el resultado es negativo,
            // se retorna un resultado negativo también...
            if( !TryGenerateBaseFactibleSolution( out clientsRemaining, out masks, out routes ) )
                return false;            
            
            // Con las rutas base, se realizan inserciones para cada uno de los clientes que faltan...
            while( clientsRemaining.Count > 0 )
            {
                // Se selecciona aleatoriamente uno de los clientes restantes...
                int indexOfToInsert = RandomTool.GetInt( clientsRemaining.Count - 1 );
                int toInsert = clientsRemaining[indexOfToInsert];
                RandomUniqueSelector routeSelector = new RandomUniqueSelector( masks.Length - 1 );

                //  Se prueba de forma aleatoria la inserción factible del cliente en las rutas de la
                // solución...
                while( !routeSelector.IsCompleted )
                {
                    int routeIndex = routeSelector.Next();

                    // Revisión de que la ruta seleccionada no sea nula...
                    if( routes[routeIndex] == null )
                        routes[routeIndex] = new Route();

                    // Se prueba la inserción...
                    if( RouteGeneration.CheckClientInsertion( routes[routeIndex], Clients.GetClient( toInsert ) ) )
                    {
                        masks[routeIndex][toInsert] = true;
                        clientsRemaining.RemoveAt( indexOfToInsert );
                        break;
                    }
                    //  Si la inserción falla y no quedan rutas por insertar, se retorna inmediatamente el
                    // resultado negativo...
                    else if( routeSelector.IsCompleted )
                    {                        
                        return false;
                    }
                }
            }

            //  A estas alturas, la generación de la solución completa fue positiva, por ende se evalúan las
            // rutas generadas.
            for( int i = 0; i < routes.Length; i++ )
                routes[i].EvaluateRoute();

            return true;
        }

        /* Método 'GenerateSolution'
         *  Realiza la generación de una solución completa. Se realizan intentos iterativos hasta generar
         * una solución factible.
         * 
         *  Parámetros:
         * - 'ClientMask[] masks': Referencia a la cual se asignarán las máscaras de la solución generada.
         * - 'Route[] generatedRoutes': Referencia a la cual se asignarán las rutas de la solución generada.
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
