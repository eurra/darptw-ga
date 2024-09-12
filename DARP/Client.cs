/* Client.cs
 * Última modificación: 10/03/2008
 */

using System;
using System.Collections.Generic;

namespace DARPTW_GA.DARP
{
    /* Clase 'Client'
     *  Encapsula la información relacionada con un cliente, que se obtiene de los datos de entrada.
     * 
     *  Atributos:
     * - 'ServiceRequest m_UpRequest': Objeto que guarda la informació relacionada con la 
     * recogida/pickup del cliente.
     * - 'ServiceRequest m_DownRequest': Objeto que guarda la información relacionada con la
     * entrega/delivery del cliente.
     */
    public class Client
    {
        private ServiceRequest m_UpRequest;
        private ServiceRequest m_DownRequest;

        public ServiceRequest UpRequest
        {
            get { return m_UpRequest; }        
        }

        public ServiceRequest DownRequest
        {
            get { return m_DownRequest; }
        }

        /* Constructor
         *  Crea una nueva instancia de Client, en base un par de solicitudes de recogida/entrega.
         * 
         *  Parámetros:
         * - 'ServiceRequest upRequest': Objeto que representa la solicitud de recogida/pickup.
         * - 'ServiceRequest downRequest': Objeto que representa la solicitud de entrega/delivery.
         */
        public Client( ServiceRequest upRequest, ServiceRequest downRequest )
        {
            m_UpRequest = upRequest;
            m_DownRequest = downRequest;
        }

        /* Método 'ToString' (sobrecargado de 'Object')
         *  Retorna una representación en string de este cliente, con la forma
         * '<string pickup>/<string delivery>'.
         */
        public override string ToString()
        {
            return m_UpRequest.ToString() + "/" + m_DownRequest.ToString();
        }
    }
}
