using System;
using System.IO;
using System.Collections.Generic;
using DARPTW_GA.DARP;
using DARPTW_GA.Misc;
using DARPTW_GA.Framework;

namespace DARPTW_GA.Parser
{
    public static class DARPReader
    {
        private static readonly char[] charSeparator = new char[] { ' ' };

        private static string[] ProcessLine( string orig )
        {
            if( orig == null )
                return new string[] { "" };
            
            return ( orig.Trim() ).Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );
        }

        private static void SetGlobalParams( string[] tempArr )
        { 
            GlobalParams.VehiclesMaxNumber = Int32.Parse( tempArr[0] );
            //GlobalParams.ClientNumber = Int32.Parse( tempArr[1] );
            //GlobalParams.PlanningHorizonLenght = Int32.Parse( tempArr[2] );
            GlobalParams.PlanningHorizonLenght = 1440;
            GlobalParams.VehicleMaxLoad = Int32.Parse( tempArr[3] );            
            GlobalParams.MRT = TimeSpan.FromMinutes( Int32.Parse( tempArr[4] ) );
        }

        private static ServiceRequest GetServiceRequest( string[] tempArr )
        {
            int id = Int32.Parse( tempArr[0] );
            double locx = Double.Parse( tempArr[1].Replace( '.', ',' ) );
            double locy = Double.Parse( tempArr[2].Replace( '.', ',' ) );
            int stime = Int32.Parse( tempArr[3] );
            int load = Int32.Parse( tempArr[4] ); 
            int et = Int32.Parse( tempArr[5] );
            int lt = Int32.Parse( tempArr[6] );

            return new ServiceRequest( id, et, lt, locx, locy, stime, load );
        }

        /*private static void GenerateNewClient( ServiceRequest pReq, ServiceRequest dReq )
        { 
            if( !pReq.IsValidTimeWindow )
                pReq.UpdateTimesFromDelivery( dReq );
            else
                dReq.UpdateTimesFromPickup( pReq );

            Clients.AddClient( new Client( pReq, dReq ) );
        }*/

        public static void ParseStream( StreamReader re )
        {
            GlobalParams.ResetParams();
            Clients.ClearInfo();
            Locations.ClearInfo();

            SetGlobalParams( ProcessLine( re.ReadLine() ) );

            // Lee info de depot
            Clients.AddRequest( GetServiceRequest( ProcessLine( re.ReadLine() ) ) );

            // Lee info de solicitudes
            List<ServiceRequest> preliminarRequests = new List<ServiceRequest>();
            int requestNumber = Int32.MaxValue;
            int clientNumber = 0;

            for( int i = 0; i < requestNumber; i++ )
            {
                ServiceRequest sr = GetServiceRequest( ProcessLine( re.ReadLine() ) );
                preliminarRequests.Add( sr );

                if( requestNumber == Int32.MaxValue && sr.LoadChange < 0 )
                {                    
                    clientNumber = i;
                    requestNumber = i * 2;                    
                }
            }

            Locations.UpdateDRTs();
            GlobalParams.ClientNumber = clientNumber;

            for( int i = 0; i < clientNumber; i++ )
            {
                ServiceRequest pReq = preliminarRequests[i];
                ServiceRequest dReq = preliminarRequests[i + clientNumber];

                if( !pReq.IsValidTimeWindow )
                    pReq.UpdateTimesFromDelivery( dReq );
                else
                    dReq.UpdateTimesFromPickup( pReq );

                Clients.AddClient( pReq, dReq );
            }
            
            Clients.SortInfo();
            Clients.UpdatePrecedenceTable();
            Clients.UpdateIncompatibilityTable();
        }
    }
}
