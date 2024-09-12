/* RouteGeneration.cs
 * �ltima modificaci�n: 09/04/2008
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DARPTW_GA.DARP;
using DARPTW_GA.Misc;

namespace DARPTW_GA.Framework.Routing
{
    /* Clase 'RouteGeneration'
     *  Clase estatica que contiene un set de m�todos destinados a la generaci�n y modificaci�n de
     * rutas para el problema, en distintos contextos de factibilidad.
     */
    public static class RouteGeneration
    {
        /* M�todo 'TryInsertion'
         *  Evalua la inserci�n factible de un par de eventos, asumidos como recogida/entrega de un
         * cliente, en una ruta determinada, definiendo los puntos en la ruta donde ambos eventos se
         * insertar�n.
         * 
         *  Par�metros:
         * - 'Route baseRoute': La ruta en la cual se realizar� la inserci�n.
         * - 'Event pickup': Evento de recogida de se insertar�.
         * - 'Event delivery': Evento de entrega que se insertar�.
         * - 'Event pickupPos': Evento que marca la posici�n previa de inserci�n del evento de recogida
         * en la ruta.
         * - 'Event deliveryPos': Evento que marca la posici�n previa de inserci�n del evento de
         * entrega en la ruta.
         */
        public static bool TryInsertion( Route baseRoute, Event pickup, Event delivery, Event pickupPos, Event deliveryPos )
        {
            // Evento en la ruta que fue previamente evaluado...
            Event previousChecked = pickupPos;
            //  Evento en la ruta que se evalua actualmente. El el primer chequeo, se tratar� del nodo
            // de recogida a insertar...
            Event actualToCheck = pickup;


            // Evento que denota el final del nuevo segmento que se reemplazar� en la ruta...
            Event newSegmentStart = null;
            // Evento que denota el inicio del nuevo segmento que se reemplazar� en la ruta...
            Event newSegmentEnd = null;
            // Evento que denota el l�mite inferior del nuevo segmento...
            Event newSegmentLowBound = pickupPos;
            // Evento que denota el l�mite superor del nuevo segmento...
            Event newSegmentUpBound = null;

            // Booleano que indica si se eliminar� un slack en el l�mite inferior del nuevo segmento...
            bool demarkPrePickupSlack = false;
            // Booleano que indica si se insertar� un slack en el l�mite inferior del nuevo segmento...
            bool markPrePickupSlack = false;
            // Booleano que indica si los eventos de entrega y recogida a insertar estan seguidos...
            bool noMidInterval = ( pickupPos == deliveryPos );

            //  Marcas de tiempo usadas para ir evaluando la factibilidad del segmento a insertar...
            TimeSpan checkET = ( previousChecked.EType == EventType.StartDepot ? previousChecked.ET : previousChecked.BET );
            TimeSpan checkLT = ( previousChecked.EType == EventType.StartDepot ? previousChecked.LT : previousChecked.BLT );

            //  Booleano que indica si se estan evaluando eventos de la secci�n extra del segmento
            // a insertar...
            bool extraChecking = false;
            // Booleano que indica si la evaluaci�n se debe terminar (factiblemente)...
            bool stopChecking = false;

            //  Se realiza un ciclo dodne cada ejecuci�n evalua un evento en la ruta...
            while( !stopChecking )
            {
                // Chequeo b�sico de carga del veh�culo...
                if( !extraChecking && previousChecked.AcumulatedLoad + actualToCheck.LoadChange > GlobalParams.VehicleMaxLoad )
                    return false;

                //  En base a las distancias, se desplaza la ventana factible acumulada hacia adelante
                // del nodo previo en relaci�n al nodo actual...
                TimeSpan dist = TimeSpan.FromMinutes( Locations.DRTS[previousChecked.Location, actualToCheck.Location] );

                TimeSpan despET = checkET + previousChecked.ServiceTime + dist;
                TimeSpan despLT = checkLT + previousChecked.ServiceTime + dist;

                // Se intersecta el resultado con la ventana base del nodo actual...
                checkET = TimeComparer.Max( despET, actualToCheck.ET );
                checkLT = TimeComparer.Min( despLT, actualToCheck.LT );

                // Si se genera una intersecci�n factible...
                if( checkET <= checkLT )
                {
                    //  En caso de ser la primera evaluaci�n, y el limite inferior del segmento ten�a
                    // definido un slack, el mismo deber� eliminarse, es decir, se fusiona el bloque
                    // anterior con el actual...
                    if( previousChecked == pickupPos && pickupPos.HasSlack )
                        demarkPrePickupSlack = true;

                    // Si el nodo actual es el final de la ruta, se debe detener la evaluaci�n...
                    if( actualToCheck.EType == EventType.StopDepot )
                    {
                        actualToCheck.BET = checkET;
                        actualToCheck.BLT = checkLT;

                        stopChecking = true;
                    }
                    // En otro caso, se procesa el evento actual...
                    else
                    {
                        Event toAdd;

                        //  Si el evento actual es uno de los que se insertar�n, se agregan
                        // directamente al nuevo segmento...
                        if( actualToCheck == pickup || actualToCheck == delivery )
                            toAdd = actualToCheck;
                        //  En otro caso, se copia  a un nuevo evento (para no modificar la ruta
                        // original en caso de infactibilidaddes futuras...
                        else
                            toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );

                        // Se actualizan los datos del evento a insertar convenientemente...
                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET;
                        toAdd.BLT = checkLT;

                        // Se actualiza el segmento a insertar con el nuevo evento...
                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        // Ahora el nodo agregado pasa a ser el nodo previamente revisado...
                        previousChecked = toAdd;

                        // Se actualiza el nodo a revisar actualmente, seg�n distintos casos...
                        // Si el nodo actual es la recogida...
                        if( actualToCheck == pickup )
                        {
                            //  Si los dos eventos a insertar estan seguidos, el evento que sigue es
                            // la entrega, en otro caso, el que siga en la ruta...
                            if( noMidInterval )
                                actualToCheck = delivery;
                            else
                                actualToCheck = pickupPos.Next;
                        }
                        // Si es la posici�n de la entrega, sigue el evento de la entrega... 
                        else if( actualToCheck == deliveryPos )
                        {
                            actualToCheck = delivery;
                        }
                        //  Si es el evento de la entrega, sigue el evento posterior a la posici�n
                        // de la misma...
                        else if( actualToCheck == delivery )
                        {
                            actualToCheck = deliveryPos.Next;
                        }
                        // En otros casos, simplemente es el evento que sigue en la ruta...
                        else
                        {
                            actualToCheck = actualToCheck.Next;
                        }

                        //  Si el evento previo revisado, recien actualizado, es la entrega, estamos
                        // evaluando la secci�n adicional del segmento...
                        if( previousChecked == delivery )
                            extraChecking = true;
                    }

                }
                //  Si no se genera la intersecci�n factible, hay que determinar si se trata de un
                // slack generado. Esto se dar� si es que el extremo inferior de la ventana desplazada
                // es menor que el extremo superior de la ventana base del evento actual, y que el
                // el evento previo tenga una carga de pasajeros igual a 0...
                else if( despET < actualToCheck.LT && previousChecked.AcumulatedLoad == 0 )
                {
                    //  Si el evento previo es el l�mite inferior del segmento, se debe indicar que
                    // se insertar� un slack en dicha posici�n...
                    if( previousChecked == pickupPos )
                    {
                        if( !previousChecked.HasSlack )
                            markPrePickupSlack = true;
                    }
                    //  En otro caso, estamos dentro del segmento a insertar, por ende podemos asignar
                    // directamente el slack al evento previo sin modificar la ruta original...
                    else
                    {
                        previousChecked.HasSlack = true;
                    }

                    //  En este caso de generaci�n de slack, es posible que ya estemos evaluando la
                    // secci�n adicional del segmento a insertar. Esta secci�n termina justamente
                    // cuando la evaluaci�n del nodo actual nos lleva a generar un slack, ya sea si
                    // el mismo exist�a o no (generando una divisi�n de bloque en este �ltimo caso).
                    // Tambi�n termina si nos encontramos con el final de la ruta. Cuando ocurre esto,
                    // se debe terminar la evaluaci�n de la inserci�n (factiblemente)...
                    if( extraChecking && ( actualToCheck.Previous.HasSlack || actualToCheck.Previous.EType == EventType.StopDepot ) )
                    {
                        stopChecking = true;
                    }
                    //  En otro caso, se agrega convenientemente el nuevo evento al segmento, del mismo
                    // modo que en el caso de generaci�n de intersecci�n...
                    else
                    {
                        Event toAdd;

                        if( actualToCheck == pickup || actualToCheck == delivery )
                            toAdd = actualToCheck;
                        else
                            toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );

                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET = toAdd.ET;
                        toAdd.BLT = checkLT = toAdd.LT;

                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        //  Tanto el evento previo como el actual se actualizan de la misma forma que
                        // el caso de generaci�n de intersecci�n...
                        previousChecked = toAdd;

                        if( actualToCheck == pickup )
                        {
                            if( noMidInterval )
                                actualToCheck = delivery;
                            else
                                actualToCheck = pickupPos.Next;
                        }
                        else if( actualToCheck == deliveryPos )
                        {
                            actualToCheck = delivery;
                        }
                        else if( actualToCheck == delivery )
                        {
                            actualToCheck = deliveryPos.Next;
                        }
                        else
                        {
                            actualToCheck = actualToCheck.Next;
                        }

                        if( previousChecked == delivery )
                            extraChecking = true;
                    }
                }
                // Otro caso implica la infactibilidad de la inserci�n...
                else
                {
                    return false;
                }
            }

            //  A estas alturas, la inserci�n es factible, por lo que se debe realizar la actualizaci�n
            // de la ruta y lo datos de sus eventos...

            // Se asume el evento actual como el l�mite superior del segmento...
            newSegmentUpBound = actualToCheck;

            //  En caso de que el limite inferior del segmento no defina un slack, o si lo defina 
            // pero con la inserci�n el mismo debe eliminarse, es necesario realizar una actualizaci�n
            // extra de los eventos previos a este l�mite...
            bool doExtraUpdate = ( !pickupPos.HasSlack || demarkPrePickupSlack );

            //  Se actualiza la ruta con el nuevo segmento, modificando los enlaces entre eventos de
            // sus l�mites...
            newSegmentLowBound.Next = newSegmentStart;
            newSegmentStart.Previous = newSegmentLowBound;

            newSegmentUpBound.Previous = newSegmentEnd;
            newSegmentEnd.Next = newSegmentUpBound;

            // Se inserta o elimina el slack del l�mite inferior del segmento si corresponde...
            if( demarkPrePickupSlack )
                pickup.Previous.HasSlack = false;
            else if( markPrePickupSlack )
                pickup.Previous.HasSlack = true;

            // Se actualiza la ruta y se termina factiblemente la inserci�n.
            UpdateRouteRange( newSegmentLowBound, newSegmentUpBound, doExtraUpdate );

            return true;
        }

        /* M�todo 'TryDeletion'
         *  Evalua la eliminaci�n factible de un par de eventos, asumidos como recogida/entrega de un
         * cliente, en una ruta determinada, definiendo los puntos en la ruta donde ambos eventos 
         * existen en dicha ruta.
         * 
         *  Par�metros:
         * - 'Route baseRoute': La ruta en la cual se realizar� la eliminaci�n.
         * - 'Event pickup': Evento de recogida que se eliminar�.
         * - 'Event delivery': Evento de entrega que se eliminar�.
         */
        public static bool TryDeletion(Route baseRoute, Event pickup, Event delivery)
        {
            // Si hay s�lo un cliente en la ruta y corresponde al que se eliminar�, se evalua directamente...
            if (pickup.Next == delivery && pickup.Previous.EType == EventType.StartDepot && delivery.Next.EType == EventType.StopDepot)
            {
                baseRoute.First.Next = baseRoute.Last;
                baseRoute.Last.Previous = baseRoute.First;

                return true;
            }

            //  Las variables usadas son an�logas al caso de la inserci�n. Para el caso del evento previamente
            // revisado, se inicializa con el evento previo al de recogida que se eliminar�...
            Event previousChecked = pickup.Previous;
            Event actualToCheck = pickup.Next;

            Event newSegmentStart = null;
            Event newSegmentEnd = null;
            Event newSegmentLowBound = pickup.Previous;
            Event newSegmentUpBound = null;

            bool demarkPrePickupSlack = false;
            bool markPrePickupSlack = false;
                        
            TimeSpan checkET = (previousChecked.EType == EventType.StartDepot ? previousChecked.ET : previousChecked.BET);
            TimeSpan checkLT = (previousChecked.EType == EventType.StartDepot ? previousChecked.LT : previousChecked.BLT);
                        
            bool extraChecking = false;
            bool stopChecking = false;

            while( !stopChecking )
            {
                //  Si el evento actual es la entrega que se eliminar�, se avanza inmediatamente al evento
                // siguiente y se considera que se ingresa a la secci�n extra del segmento modificado...
                if( actualToCheck == delivery )
                {
                    actualToCheck = actualToCheck.Next;
                    extraChecking = true;
                    continue;
                }

                TimeSpan dist = TimeSpan.FromMinutes( Locations.DRTS[previousChecked.Location, actualToCheck.Location] );

                TimeSpan despET = checkET + previousChecked.ServiceTime + dist;
                TimeSpan despLT = checkLT + previousChecked.ServiceTime + dist;

                checkET = TimeComparer.Max( despET, actualToCheck.ET );
                checkLT = TimeComparer.Min( despLT, actualToCheck.LT );

                // Si se genera una intersecci�n factible...
                if( checkET <= checkLT )
                {
                    //  Se elimina el slack en caso que el evento anterior sea el previo al inicio del
                    // segmento modificado...
                    if( previousChecked == pickup.Previous && pickup.Previous.HasSlack )
                        demarkPrePickupSlack = true;

                    // La revisi�n se detiene si el evento actual es el dep�sito final...
                    if( actualToCheck.EType == EventType.StopDepot )
                    {
                        actualToCheck.BET = checkET;
                        actualToCheck.BLT = checkLT;

                        stopChecking = true;
                    }
                    else
                    {
                        // Se actualizan los datos del nuevo evento en el segmento...
                        Event toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );
                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET;
                        toAdd.BLT = checkLT;

                        // Se actualiza el segmento nuevo convenientemente...
                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        // Se actualiza la referencia a los eventos asignados...
                        previousChecked = toAdd;
                        actualToCheck = actualToCheck.Next;
                    }

                }
                //  Si no ocurre intersecci�n factible, se debe evaluar si se puede generar un slack, lo que
                // ocurrir� si el extremo inferior de la ventana desplazada es menor que el extremo superior
                // de la ventana a la que se desplaz�, adicional al hecho que la carga del veh�culo actual
                // sea 0...
                else if( despET < actualToCheck.LT && previousChecked.AcumulatedLoad == 0 )
                {
                    //  Se inserta el slack en caso que el evento anterior sea el previo al inicio del
                    // segmento modificado...
                    if( previousChecked == pickup.Previous )
                    {
                        if( !previousChecked.HasSlack )
                            markPrePickupSlack = true;
                    }
                    else
                    {
                        previousChecked.HasSlack = true;
                    }

                    //  Cuando se genera un slack, se debe evaluar si el mismo forma parte ya de la ruta
                    // original, y se esta evaluando la secci�n extra del segmento modificado. En este
                    // caso, se debe detener la revisi�n...
                    if( extraChecking && actualToCheck.Previous.HasSlack )
                    {
                        stopChecking = true;
                    }
                    else
                    {
                        // En otro caso, se procede de forma an�loga al caso de la intersecci�n...
                        Event toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );
                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET = toAdd.ET;
                        toAdd.BLT = checkLT = toAdd.LT;

                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        previousChecked = toAdd;
                        actualToCheck = actualToCheck.Next;
                    }
                }
                // En otro caso, la eliminaci�n es infactible...
                else
                {
                    return false;
                }
            }

            //  A estas alturas, la eliminaci�n de los eventos es factible, por ende se procede a actualizar
            // los datos que correspondan...

            newSegmentUpBound = actualToCheck;
                        
            //  Booleano que indicar� si se debe actualizar eventos previos en la ruta al inicio del nuevo
            // segmento...
            bool doExtraUpdate = ( !pickup.Previous.HasSlack || demarkPrePickupSlack );
            //  Booleano que indicar� si es necesario hacer una actualizaci�n del segmento generado. Como se
            // trata de una eliminaci�n, puede que este segmento no exista, por ende no es necesaria la 
            // actualizaci�n...
            bool updateRange = true;

            // Se actualiza la ruta con el nuevo segmento, en caso que corresponda...
            if( newSegmentStart == null )
            {
                newSegmentLowBound.Next = newSegmentUpBound;
                newSegmentUpBound.Previous = newSegmentLowBound;

                // En caso de que los eventos eliminados sean los unicos presentes en el bloque que se
                // realiz� dicha eliminaci�n, la actualizaci�n del segmento (inexistente) no es
                // necesaria...
                if( newSegmentLowBound.HasSlack && !demarkPrePickupSlack && delivery.HasSlack )
                    updateRange = false;
            }
            else
            {
                newSegmentLowBound.Next = newSegmentStart;
                newSegmentStart.Previous = newSegmentLowBound;

                newSegmentUpBound.Previous = newSegmentEnd;
                newSegmentEnd.Next = newSegmentUpBound;
            }

            // Se actualizan los slacks para los l�mites del segmento si corresponde...
            if( demarkPrePickupSlack )
                pickup.Previous.HasSlack = false;
            else if( markPrePickupSlack )
                pickup.Previous.HasSlack = true;

            // Por �ltimo se actualiza la ruta si corresponde y se retorna como resultado.
            if( updateRange )
                UpdateRouteRange( newSegmentLowBound, newSegmentUpBound, doExtraUpdate );

            return true;
        }

        /* M�todo 'TrySwap'
         *  Evalua el intercambio de orden factible entre un par de eventos continuos en una ruta, 
         * definiendo los puntos en la ruta donde ambos eventos existen en dicha ruta.
         * 
         *  Par�metros:
         * - 'Route baseRoute': La ruta en la cual se realizar� el intercambio.
         * - 'Event a': Primer evento que se intercambiar�.
         * - 'Event b': Segundo evento que se intercambiar�.
         */
        public static bool TrySwap( Route baseRoute, Event a, Event b )
        {
            //  Las variables usadas son an�logas al caso de la inserci�n. Para el caso del evento previamente
            // revisado, se inicializa con el evento previo al de recogida que se eliminar�...
            Event previousChecked = a.Previous;
            Event actualToCheck = b;

            Event newSegmentStart = null;
            Event newSegmentEnd = null;
            Event newSegmentLowBound = a.Previous;
            Event newSegmentUpBound = null;

            bool demarkPreASlack = false;
            bool markPreASlack = false;

            TimeSpan checkET = ( previousChecked.EType == EventType.StartDepot ? previousChecked.ET : previousChecked.BET );
            TimeSpan checkLT = ( previousChecked.EType == EventType.StartDepot ? previousChecked.LT : previousChecked.BLT );

            bool extraChecking = false;
            bool stopChecking = false;

            while( !stopChecking )
            {
                // Se debe evaluar que la carga del veh�culo sea correcta...
                if( !extraChecking && previousChecked.AcumulatedLoad + actualToCheck.LoadChange > GlobalParams.VehicleMaxLoad )
                    return false;

                TimeSpan dist = TimeSpan.FromMinutes( Locations.DRTS[previousChecked.Location, actualToCheck.Location] );

                TimeSpan despET = checkET + previousChecked.ServiceTime + dist;
                TimeSpan despLT = checkLT + previousChecked.ServiceTime + dist;

                checkET = TimeComparer.Max( despET, actualToCheck.ET );
                checkLT = TimeComparer.Min( despLT, actualToCheck.LT );

                // Si ocurre una intersecci�n factible...
                if( checkET <= checkLT )
                {
                    // Se elimina el slack si es que existe y se trata del primer evento revisado...
                    if( previousChecked == a.Previous && a.Previous.HasSlack )
                        demarkPreASlack = true;

                    // Si se trata del dep�sito de entrega, se debe detener la revisi�n...
                    if( actualToCheck.EType == EventType.StopDepot )
                    {
                        actualToCheck.BET = checkET;
                        actualToCheck.BLT = checkLT;

                        stopChecking = true;
                    }
                    else
                    {
                        // Se actualizan los datos del nuevo evento en el segmento a modificar...
                        Event toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );
                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET;
                        toAdd.BLT = checkLT;

                        // Se actualizan las referencias asociadas al segmento...
                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        previousChecked = toAdd;

                        //  Si el evento que se revisar� ahora corresponde al segundo que se intercambiar�,
                        // en el nuevo segmento el siguiente se debe considerar como el primero a 
                        // intercambiar...
                        if( actualToCheck == b )
                        {
                            actualToCheck = a;
                        }
                        //  An�logo a lo anterior, para el primer evento a intercambiar, se debe considerar 
                        // el segundo como el siguiente...
                        else if( actualToCheck == a )
                        {
                            actualToCheck = b.Next;
                            extraChecking = true;
                        }
                        else
                        {
                            actualToCheck = actualToCheck.Next;
                        }
                    }

                }
                //  Si no ocurre intersecci�n factible, se debe evaluar si se puede generar un slack, lo que
                // ocurrir� si el extremo inferior de la ventana desplazada es menor que el extremo superior
                // de la ventana a la que se desplaz�, adicional al hecho que la carga del veh�culo actual
                // sea 0...
                else if( despET < actualToCheck.LT && previousChecked.AcumulatedLoad == 0 )
                {
                    //  Se inserta el slack en caso que el evento anterior sea el previo al inicio del
                    // segmento modificado...
                    if( previousChecked == a.Previous )
                    {
                        if( !previousChecked.HasSlack )
                            markPreASlack = true;
                    }
                    else
                    {
                        previousChecked.HasSlack = true;
                    }

                    //  Cuando se genera un slack, se debe evaluar si el mismo forma parte ya de la ruta
                    // original, y se esta evaluando la secci�n extra del segmento modificado. En este
                    // caso, se debe detener la revisi�n...
                    if( extraChecking && actualToCheck.Previous.HasSlack )
                    {
                        stopChecking = true;
                    }
                    else
                    {
                        // En otro caso, se procede de forma an�loga al caso de la intersecci�n...
                        Event toAdd = new Event( Clients.Requests[actualToCheck.ServiceID] );
                        toAdd.AcumulatedLoad = previousChecked.AcumulatedLoad + toAdd.LoadChange;

                        toAdd.BET = checkET = toAdd.ET;
                        toAdd.BLT = checkLT = toAdd.LT;

                        if( newSegmentStart == null )
                        {
                            newSegmentStart = newSegmentEnd = toAdd;
                        }
                        else
                        {
                            newSegmentEnd = toAdd;

                            previousChecked.Next = toAdd;
                            toAdd.Previous = previousChecked;
                        }

                        previousChecked = toAdd;

                        if( actualToCheck == b )
                        {
                            actualToCheck = a;
                        }
                        else if( actualToCheck == a )
                        {
                            actualToCheck = b.Next;
                            extraChecking = true;
                        }
                        else
                        {
                            actualToCheck = actualToCheck.Next;
                        }
                    }
                }
                // En otro caso, el intercambio de los eventos no es factible...
                else
                {
                    return false;
                }
            }

            //  A estas alturas el intercambio de los eventos es factible, por ende se procede a actualizar
            // los datos de la ruta que correspondan...

            newSegmentUpBound = actualToCheck;

            //  Booleano que indicar� si se debe actualizar eventos previos en la ruta al inicio del nuevo
            // segmento...
            bool doExtraUpdate = ( !a.Previous.HasSlack || demarkPreASlack );

            // Se actualiza la ruta con el nuevo segmento...
            newSegmentLowBound.Next = newSegmentStart;
            newSegmentStart.Previous = newSegmentLowBound;

            newSegmentUpBound.Previous = newSegmentEnd;
            newSegmentEnd.Next = newSegmentUpBound;

            // Se actualizan los slacks al principio del segmento insertado, en caso que corresponda...
            if( demarkPreASlack )
                a.Previous.HasSlack = false;
            else if( markPreASlack )
                a.Previous.HasSlack = true;

            // Por �ltimo se actualiza la ruta y se retorna como resultado.
            UpdateRouteRange( newSegmentLowBound, newSegmentUpBound, doExtraUpdate );

            return true;
        }
        
        /* M�todo 'UpdateRouteRange'
         *  Actualiza los valores correspondientes al tiempo actual y a la ventana de factibilidad
         * de los eventos que forman parte de un segmento de ruta determinado. Se asume que este
         * segmento ha sido construido y/o modificado antes de forma que la ventana acumulada hacia
         * adelante esta definida de antemano. Cabe destacar que la actualizaci�n se realiza hacia
         * atras.
         * 
         *  Par�metros:
         * - 'Event lowBound': Evento anterior al inicio del segmento a actualizar, o su l�mite
         * inferior.
         * - 'Event upBound': Evento posterior al final del segmento a actualizar, o su l�mite superior.
         * - 'bool doExtraUpdate': Booleano que indica si se considerar� la actualizaci�n de eventos
         * anteriores al primero del segmento.
         */
        public static void UpdateRouteRange( Event lowBound, Event upBound, bool doExtraUpdate )
        {
            //  Si el l�mite superior del segmento es el final de la ruta, se actualizan directamente
            // sus valores. Cabe recordar que lo que sigue al segmento a modificar, ser� un slack o
            // el final de la ruta, por lo que en el primer caso no es necesaria ninguna actualizaci�n
            // al l�mite superior del segmento, dado que un slack divide la ruta en segmentos
            // independientes en lo que se refiere a sus valores de tiempos...
            if( upBound.EType == EventType.StopDepot )
            {
                upBound.FET = upBound.AT = upBound.BET;
                upBound.FLT = upBound.BLT;
            }
            
            //  Booleanos que indican si estamos actualizando eventos iguales o previos al limite
            // inferior del segmento, los qur no necesariamente deben pertenecer a un bloque distinto
            // al del segmento...
            bool beforeLowBound = false;
            bool extraUpdating = false;

            // Booleano que cambia su valor a true para indicar que se termine la actualizaci�n...
            bool stopUpdating = false;

            Event checking = upBound;

            // Desde el l�mite superior del segmento, se actualizan hacia atras los eventos...
            while( !stopUpdating )
            {
                Event toUpdate = checking.Previous;

                //  Si el evento actual es el l�mite inferior, actualizamos la variable que indica
                // que estamos actualizando antes del segmento...
                if( !beforeLowBound && checking == lowBound )
                    beforeLowBound = true;

                //  Si estamos actualizando eventos previos al l�mite inferior, hay que determinar si
                // se debe detener la actualizaci�n o no. Si se defini� por par�metros que la 
                // actualizaci�n no se hiciera bajo este l�mite, se detiene inmediatamente. En otro
                // caso, hay que discriminar si el evento a actualizar tiene un slack o no. El que
                // dicho slack exista implica la detenci�n de la actualizaci�n, pues ya se lleg� al
                // primer evento del bloque en el que esta el segmento que se insert� en la ruta...
                if( beforeLowBound && ( !doExtraUpdate || ( extraUpdating && toUpdate.HasSlack ) ) )
                {
                    stopUpdating = true;
                }
                else
                {
                    //  En caso de que al evento a actualizar le siga un slack (por ende se trata del
                    // �ltimo evento del segmento, y �ltimo en un bloque), se asignan directamente los
                    // valores que corresponden...
                    if( toUpdate.HasSlack )
                    {
                        toUpdate.FET = toUpdate.AT = toUpdate.BET;
                        toUpdate.FLT = toUpdate.BLT;
                    }
                    // En otro caso se calculan los valores acorde a las distancias de los nodos...
                    else
                    {
                        TimeSpan dist = toUpdate.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[toUpdate.Location, checking.Location] );

                        toUpdate.FET = toUpdate.AT = checking.FET - dist;
                        toUpdate.FLT = checking.FLT - dist;
                    }

                    // Si el evento actualizado es el inicio de la ruta, se detiene la actualizaci�n...
                    if( toUpdate.EType == EventType.StartDepot )
                    {
                        stopUpdating = true;
                    }
                    else
                    {                        
                        if( !extraUpdating && checking == lowBound )
                            extraUpdating = true;

                        // Se actualiza el evento previo.
                        checking = checking.Previous;
                    }
                }
            }
        }

        /* M�todo 'GenerateRouteByInsertion'
         *  M�todo gen�rico que en base a una m�scara de clientes determinada, genera una ruta usando
         * el mecanismo de inserci�n de clientes. La ruta que se retorna eventualmente es nula, al
         * tratarse de una m�scara infactible.
         * 
         *  Par�metros:
         * - 'ClientMask mask': La m�scara que se usar� como base para generar la ruta.
         */
        public static Route GenerateRouteByInsertion( ClientMask mask )
        {
            //  Para la generaci�n de la ruta, se requiere una lista con los clientes que estan
            // indicados en la m�scara, para ello se itera por la misma para obtener los clientes
            // correspondientes. La lista generada se ordena de forma aleatoria...
            List<Client> clientList = new List<Client>();

            for( int i = 0; i < mask.Length; i++ )
            {
                if( mask[i] )
                    clientList.Insert( RandomTool.GetInt( clientList.Count - 1 ), Clients.GetClient( i ) );
            }

            //  Se prueba la inserci�n de los clientes en una busqueda recursiva, y se retorna el
            // resultado.
            Route ret = DeepRouteSearchByClientInsertion( new Route(), clientList, new List<Client>() );

            return ret;
        }

        /* M�todo 'DeepRouteSearchByClientInsertion'
         *  Realiza una inserci�n recursiva de clientes en una ruta, en base al algoritmo de inserci�n
         * realizando chequeos de factibilidad adecuados. El algoritmo de este m�todo asegura que todas
         * las posibles combinaciones de orden de inserci�n de clientes son evaluadas.
         * 
         *  P�r�metros:
         * - 'Route buildRoute': Ruta en la cual se realizar�n las inserciones.
         * - 'List<Client> clientsRemaining': Lista de clientes que representan, en alg�n punto de la
         * ejecuci�n, cuantos clientes faltan por insertar para completar la ruta.
         * - 'List<Client> insertedOrder': Lista en la cual se guardar� el orden final en que los 
         * clientes fueron insertados, en caso de factibilidad en la construcci�n de la ruta.
         */
        public static Route DeepRouteSearchByClientInsertion( Route buildRoute, List<Client> clientsRemaining, List<Client> insertedOrder )
        {
            Client checking = null;
            Route res = null;

            //  La revisi�n de clientes se hace desde el �ltimo dentro de la lista de clientes que faltan
            // hasta el primero de la misma, siempre que la ruta no se haya encontrado hasta el momento...
            for( int i = clientsRemaining.Count - 1; i >= 0 && res == null; i-- )
            {
                //  Para la revisi�n del cliente actual, se clona la ruta de entrada en caso de
                // infactibilidades...
                checking = clientsRemaining[i];
                Route cloneRoute = (Route)buildRoute.Clone();

                // Se eval�a la inserci�n...
                if( CheckClientInsertion( cloneRoute, checking ) )
                {
                    //  De ser factible, se copia la lista de clientes restantes, (considerando que en un
                    // siguiente nivel de recursividad se modificar�) eliminando el insertado, adem�s se
                    // actualiza la lista de orden de inserci�n...
                    List<Client> nextList = new List<Client>( clientsRemaining );
                    nextList.RemoveAt( i );
                    insertedOrder.Add( clientsRemaining[i] );
                    
                    //  Si aun quedan clientes pendientes para insertar, se entra recursivamente a esta
                    // misma funci�n, en caso contrario se retorna la ruta resultado...
                    if( nextList.Count > 0 )
                        res = DeepRouteSearchByClientInsertion( cloneRoute, nextList, insertedOrder );
                    else
                        return cloneRoute;

                    //  Si la inserci�n en la �ltima ejecuci�n recursiva no fue factible, se elimina el
                    // cliente considerado de ella...
                    if( res == null )
                        insertedOrder.Remove( clientsRemaining[i] );
                }
            }

            //  En estas instancias, res es null, se retorna para denotar que en el nivel actual de
            // recursividad, cualquier combinaci�n de inserciones no es factible.
            return res;
        }

        /* M�todo 'CheckClientInsertion'
         *  M�todo que evalua la factibilidad de inserci�n de un cliente en una ruta determinada. Retorna
         * un booleano de acuerdo al resultado
         * 
         *  Par�metros:
         * - 'Route buildRoute': Ruta en la que se evaluar� la inserci�n.
         * - 'Client cl': Cliente que ser� insertado si el proceso es factible.
         */
        public static bool CheckClientInsertion( Route buildRoute, Client cl )
        {
            // Se revisan las solicitudes de servicio asociadas al cliente y sus respectivos identificadores...
            Event pickup = new Event( cl.UpRequest );
            Event delivery = new Event( cl.DownRequest );

            int pickupServiceID = pickup.ServiceID;
            int deliveryServiceID = delivery.ServiceID;

            // Si la ruta esta vac�a, se insertan inmediatamente los eventos y se retorna...
            if( buildRoute.IsVoidRoute )            
                return TryInsertion( buildRoute, pickup, delivery, buildRoute.First, buildRoute.First );            

            // Se busca en la ruta las posiciones posibles de inserci�n de los eventos de recogida y entrega.
            //  Los puntos se agrupan en pares de listas. Cada par tiene una lista para puntos de insercion
            // de la recogida y otra lista para la entrega.
            //  Que exista m�s de un par de listas, significa que los puntos posibles de inserci�n, ya sea para
            // el evento de recogida o el evento de entrega, estan divididos por "slack absoluto", es decir
            // slack que existir� independiente de los sucesos anteriores al evento que genera el slack,
            // esto implica que el LT del evento previo al slack siempre estar� antes que el ET del evento
            // posterior al slack, incluso si se ha desplazado hacia este �ltimo.
            //  Cuando un par s�lo tiene una de las listas de puntos de inserci�n, ya sea para la recogida o para
            // la entrega, no es v�lida...            
            List<List<Event>[]> insertionPoints = new List<List<Event>[]>();
            
            // Booleano que indica si en el momento de la revisi�n de los rangos, se esta en ellos... 
            bool inInsertionRange = false;
            // Booleano que indica cuando la busqueda de rangos se debe detener...
            bool stopSearch = false;

            // Lista de eventos del rango activo...
            List<Event> activeList = null;
            // Lista que va recolectando los eventos que preceden a un slack absoluto...
            List<Event> absoluteSlackEvents = new List<Event>();

            Event checking = buildRoute.First.Next;

            // En el primer loop se colectan los rangos de inserci�n del evento de recogida...
            while( !stopSearch )
            {
                //  Primero se eval�a cuando debe empezar el rango de inserci�n. Esto ocurrir� si el evento
                // actual es el deposito final, o si el mismo no esta en la lista de precedencia del evento
                // de recogida...
                if( !inInsertionRange )
                {
                    if( checking.EType == EventType.StopDepot || Clients.PrecedenceTable[pickupServiceID].BinarySearch( checking.ServiceID ) < 0 )
                        inInsertionRange = true; 
                }
                
                //  Si se esta en el rango de inserci�n, se procede a evaluar los eventos del mismo...
                if( inInsertionRange )
                {
                    // Se define la lista de inserci�n activa en caso que corresponda...
                    if( activeList == null )
                    {
                        activeList = new List<Event>();
                        insertionPoints.Add( new List<Event>[] { activeList, null } );
                    }                    

                    // Se agrega a la lista el evento previo...
                    activeList.Add( checking.Previous );

                    // Si el evento agregado tiene un slack, se debe evaluar si este es absoluto...
                    if( checking.Previous.HasSlack )
                    {
                        TimeSpan dist = checking.Previous.ServiceTime + TimeSpan.FromMinutes( Locations.DRTS[checking.Previous.Location, checking.Location] );

                        if( checking.Previous.LT + dist < checking.ET )
                        {
                            //  Si el slack es absoluto, se agrega el evento que lo precede a la lista que
                            // corresponde, adem�s de crear una nueva lista activa...
                            absoluteSlackEvents.Add( checking.Previous );
                            activeList = new List<Event>();
                            activeList.Add( checking.Previous );
                            insertionPoints.Add( new List<Event>[] { activeList, null } );
                        }
                    }
                }

                //  Se evalua si terminar con la revisi�n. Esto ocurrir� si el evento actual es el dep�sito
                // final o si la lista de precedencia preliminar del evento actual contiene al evento de
                // recogida...
                if( !stopSearch )
                {
                    if( checking.EType == EventType.StopDepot || Clients.PrecedenceTable[checking.ServiceID].BinarySearch( pickupServiceID ) >= 0 )
                        stopSearch = true;
                    else
                        checking = checking.Next;
                }
            }
            
            // Chequeo de sanidad: Si la lista de rangos de inserci�n esta vac�a, la inserci�n es infactible...
            if( insertionPoints.Count == 0 )
                return false;

            //  En el segundo loop se colectan los rangos de inserci�n del evento de entrega, de forma an�loga
            // al evento de recogida. En este caso, como ya se recolectaron los eventos previos a los slacks
            // absolutos, el rango obtenido se divide en base a ellos. Adem�s, como ya se tiene la divisi�n
            // del rango de inserci�n del evento de recogida y por consiguiente, la estructura que los almacena,
            // los rangos para el evento de entrega se almacenan en esas mismas estructuras...
            inInsertionRange = false;
            stopSearch = false;

            activeList = null;
            //  Entero que indicar� a que par de rangos de los ya definidos, deber� agregarse el que se
            //  recolecte para elevento de entrega...
            int pairIndex = 0;

            //  El primer evento para el rango del evento de entrega no puede estar antes del primero del evento
            // de recogida...
            checking = insertionPoints[0][0][0].Next;

            while( !stopSearch )
            { 
                if( !inInsertionRange )
                {
                    if( checking.EType == EventType.StopDepot || Clients.PrecedenceTable[deliveryServiceID].BinarySearch( checking.ServiceID ) < 0 )
                        inInsertionRange = true;
                }

                if( inInsertionRange )
                {
                    if( activeList == null )
                    {
                        activeList = new List<Event>();
                        // Se asigna la lista activa al segundo elemento del par que corresponda...
                        insertionPoints[pairIndex][1] = activeList;
                    }

                    activeList.Add( checking.Previous );
                }

                if( checking.Previous.HasSlack && absoluteSlackEvents.Contains( checking.Previous ) )
                {
                    //  Si nos topamos con un slack absoluto, se cambia autom�ticamente la lista activa y se
                    // aumenta el �ndice para la inserci�n del nuevo rango...
                    activeList = null;
                    pairIndex++;

                    if( pairIndex >= insertionPoints.Count )
                        stopSearch = true;
                }

                if( !stopSearch )
                {
                    if( checking.EType == EventType.StopDepot || Clients.PrecedenceTable[checking.ServiceID].BinarySearch( deliveryServiceID ) >= 0 )
                        stopSearch = true;
                    else
                        checking = checking.Next;
                }
            }

            //  Con ambos rangos ya divididos, se procede a filtrar los inv�lidos, estos son, los que o s�lo
            // tienen rango para el evento de entrega, o s�lo un rango para el evento de recogida...
            List<List<Event>[]> finalInsertionPoints = new List<List<Event>[]>( insertionPoints.Count );

            foreach( List<Event>[] pair in insertionPoints )
            {
                if( pair[1] != null )
                    finalInsertionPoints.Insert( RandomTool.GetInt( finalInsertionPoints.Count ), pair );
            }

            //  Chequeo de sanidad final: Si al realizar el filtrado no obtenemos rangos, la inserci�n es
            // infactible...
            if( finalInsertionPoints.Count == 0 )
                return false;

            //  Con la lista de rangos definitiva, se prueban combinaciones aleatorias de inserci�n de los
            // eventos de entrega y recogida...
            while( finalInsertionPoints.Count > 0 )
            {                
                // Los rangos de inserci�n se sacan de la lista construida antes desde el final al principio...
                List<Event>[] actualPair = finalInsertionPoints[finalInsertionPoints.Count - 1];
                finalInsertionPoints.RemoveAt( finalInsertionPoints.Count - 1 );

                List<Event> pickupInsertionPoints = actualPair[0];
                List<Event> deliveryInsertionPoints = actualPair[1];

                while( pickupInsertionPoints.Count > 0 )
                { 
                    int indexPickup = RandomTool.GetInt( pickupInsertionPoints.Count - 1 );

                    Event pickupPos = pickupInsertionPoints[indexPickup];
                    pickupInsertionPoints.RemoveAt( indexPickup );

                    //  Para cada evento de recogida, escogidos de forma aleatoria, se evalua la inserci�n con
                    // otro evento de entrega tambi�n seleccionado de forma aleatoria...
                    foreach( Event deliveryPos in deliveryInsertionPoints )
                    {
                        //  No es posible seleccionar un punto de inserci�n para el evento de recogida,
                        // posterior al punto de inserci�n para la entrega, por ende dichas combinaciones
                        // de inserci�n se filtran.
                        if( deliveryPos.AT < pickupPos.AT )
                            continue;

                        // Finalmente, se prueba la inserci�n de los eventos seleccionados. Para el primer
                        // intento factible, se retorna con la respectiva ruta construida...
                        if( TryInsertion( buildRoute, pickup, delivery, pickupPos, deliveryPos ) )
                            return true;
                    }
                }
            }

            //  En estas instancias, se probo con todas las posibles combinaciones de eventos de entrega y 
            // recogida, por lo tanto toda inserci�n es infactible.
            return false;
        }

        /* M�todo 'CheckClientDeletion'
         *  M�todo que evalua la factibilidad de eliminaci�n de un cliente en una ruta determinada. Retorna
         * un booleano de acuerdo al resultado
         * 
         *  Par�metros:
         * - 'Route buildRoute': Ruta en la que se evaluar� la eliminaci�n.
         * - 'Client cl': Cliente que ser� insertado si el proceso es factible.
         */
        public static bool CheckClientDeletion( Route buildRoute, Client cl )
        {
            // Se descarta de antemano una ruta vac�a...
            if( buildRoute.IsVoidRoute )
                return false;

            int pickupServiceID = cl.UpRequest.ID;
            int deliveryServiceID = cl.DownRequest.ID;

            Event checking = buildRoute.First;
            Event pickup = null;
            Event delivery = null;

            // Se busca en la ruta las posiciones de los eventos de recogida y entrega a eliminar...
            while( checking != null )
            {
                if( pickup == null && checking.ServiceID == pickupServiceID )
                {
                    pickup = checking;
                }
                else if( checking.ServiceID == deliveryServiceID )
                {
                    delivery = checking;
                    break;
                }

                checking = checking.Next;
            }

            // Chequeo de sanidad, si es que no se encontro alguno de los eventos asociados...
            if( pickup == null || delivery == null )
                return false;

            // Se retorna el resultado del intento de eliminaci�n del cliente.
            return TryDeletion( buildRoute, pickup, delivery );
        }
        
    }
        
}
