using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using DARPTW_GA.Framework;
using DARPTW_GA.Framework.Genomes;
using DARPTW_GA.Framework.Routing;
using DARPTW_GA.Misc;
using DARPTW_GA.GA_Base;
using DARPTW_GA.Parser;
using DARPTW_GA.DARP;
using DARPTW_GA;

namespace DARPTW_GA
{    
    public delegate void CommandHandler( string args );

    public class ModelCoreConfig
    {
        private string m_Name;
        private Type m_genomeType;
        private InitialPoblationLauncher m_initialPoblationMethod;
        private CrossoverLauncher m_crossoverMethod;
        private MutationLauncher m_routeMutationMethod;
        private MutationLauncher m_clusterMutationMethod;
        private SelectionLauncher m_selectionMethod;

        public string Name { get { return m_Name; } }
        public Type GenomeType { get { return m_genomeType; } }
        public InitialPoblationLauncher InitialPoblationMethod { get { return m_initialPoblationMethod; } }
        public CrossoverLauncher CrossoverMethod { get { return m_crossoverMethod; } }
        public MutationLauncher RouteMutationMethod { get { return m_routeMutationMethod; } }
        public MutationLauncher ClusterMutationMethod { get { return m_clusterMutationMethod; } }
        public SelectionLauncher SelectionMethod { get { return m_selectionMethod; } }

        public ModelCoreConfig( string name, Type genomeType, InitialPoblationLauncher initialPoblationMethod, CrossoverLauncher crossoverMethod, MutationLauncher routeMutationMethod, MutationLauncher clusterMutationMethod, SelectionLauncher selectionMethod )
        {
            m_Name = name;
            m_genomeType = genomeType;
            m_initialPoblationMethod = initialPoblationMethod;
            m_crossoverMethod = crossoverMethod;
            m_routeMutationMethod = routeMutationMethod;
            m_clusterMutationMethod = clusterMutationMethod;
            m_selectionMethod = selectionMethod;
        }
    }
    
    public class Program
    {
        private const string m_Version = "1.0";

        [AttributeUsage(AttributeTargets.Method)] 
        private class CommandInfoAttribute : Attribute
        {
            private string m_Usage;
            private string m_Description;

            public string Usage { get { return m_Usage; } }
            public string Description { get { return m_Description; } }

            public CommandInfoAttribute( string usage, string description )
            {
                m_Usage = usage;
                m_Description = description;
            }
        }

        private class CommandDefinition
        {            
            private CommandHandler m_Handler;
            private string m_Word;
            private bool m_Logable;

            public CommandHandler Handler { get { return m_Handler; } }
            public string Word { get { return m_Word; } }
            public bool Logable { get { return m_Logable; } }

            public CommandDefinition( string word, CommandHandler handler, bool logable )
            {
                m_Word = word;
                m_Handler = handler;
                m_Logable = logable;
            }
        }

        private static readonly ModelCoreConfig[] ModelCoreOptions = new ModelCoreConfig[]
        {
            new ModelCoreConfig( "Modelo LLGA", typeof(LLGAGenome), InitialPoblationMethods.LLGAGeneration, LLGAGenome.DoCrossover, LLGAGenome.RouteMutation, LLGAGenome.ClusterMutation, TournamentSelection.Run ),
            new ModelCoreConfig( "Modelo basado en Rutas", typeof(RouteGenome), InitialPoblationMethods.RouteGeneration, RouteGenome.DoCrossover, RouteGenome.RouteMutation, RouteGenome.ClusterMutation, TournamentSelection.Run )
        };

        public static ModelCoreConfig GetActiveModel()
        {
            return ModelCoreOptions[m_ModelSelected];
        }

        public static ModelCoreConfig GetModel( int index )
        {
            return ModelCoreOptions[index];
        }

        private static int m_ModelSelected;

        public static int ModelSelected
        {
            get { return m_ModelSelected; }

            set
            {
                if( value < 0 )
                    value = 0;
                else if( value > ModelCoreOptions.Length - 1 )
                    value = ModelCoreOptions.Length - 1;

                m_ModelSelected = value;
            }
        }

        private static Dictionary<string, CommandDefinition> m_CommandTable;        

        private static void GenerateCommandsTable()
        {
            m_CommandTable = new Dictionary<string, CommandDefinition>( m_CommandDefinitions.Length );
            
            for( int i = 0; i < m_CommandDefinitions.Length; i++ )
            {
                CommandDefinition def = m_CommandDefinitions[i];

                if( def.Word != null && !m_CommandTable.ContainsKey( def.Word ) && def.Handler != null )
                    m_CommandTable[def.Word] = def;                
            }
        }

        private static ParameterConfig m_GAParams;
        private static ParameterConfig m_DARPParams;
        private static ParameterConfig m_SelectionParams;

        public static ParameterConfig GAParams { get { return m_GAParams; } }
        public static ParameterConfig DARPParams { get { return m_DARPParams; } }
        public static ParameterConfig SelectionParams { get { return m_SelectionParams; } }

        private static void Initialize()
        {
            GenerateCommandsTable();
            m_GAParams = new ParameterConfig( "PARAMETROS DEL GA", typeof( GA ) );
            m_DARPParams = new ParameterConfig( "PARAMETROS DE FUNCION DARP", typeof( Evaluation ) );
            m_SelectionParams = new ParameterConfig( "PARAMETROS DEL OPERADOR DE SELECCION", ModelCoreOptions[m_ModelSelected].SelectionMethod.Method.DeclaringType );
        }

        static void Main( string[] args )
        {
            Console.Clear();
            Console.WriteLine( "******************************************************************************" );
            Console.WriteLine( "********** Dial-a-Ride Problem with Time Windows Genetic Algorithm  **********" );
            Console.WriteLine( "******************************************************************************\n" );

            Console.WriteLine( "Version: {0}\n", m_Version );

            Console.WriteLine( "Ingrese \"help\" para ver los comandos disponibles.\n" );

            Initialize();

            if( File.Exists( "autoexec.txt" ) )
            {
                Console.WriteLine( "Ejecutando archivo de comandos por defecto..." );
                ProcessCommand( "batch", "autoexec.txt" );
            }
            else
            {
                Console.WriteLine( "Archivo de comandos por defecto no encontrado." );
            }
            
            DoMainLoop();
        } 

        private static readonly char[] charSeparator = new char[] { ' ' };
        private static bool m_OpenFile = false;
        private static string m_OuputFolder;
        private static StreamWriter m_ActiveLog;
        private static bool m_TextOuput = false;

        private static string[] GetCommandArgs( string args )
        {
            return args.Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );
        }
        
        private static void ProcessInput( string input, out string command, out string args )
        {
            command = null;
            args = null;

            string[] processedInput = input.ToLower().Trim().Split( charSeparator, 2, StringSplitOptions.RemoveEmptyEntries );

            if( processedInput.Length > 0 )
            {
                command = processedInput[0];

                if( processedInput.Length > 1 )
                    args = processedInput[1];
            }
        }

        private static void PrintLine( string input, params object[] args )
        {
            Console.WriteLine( input, args );

            if( m_ActiveLog != null )
                m_ActiveLog.WriteLine( input, args );
        }

        private static void Print( string input, params object[] args )
        {
            Console.Write( input, args );

            if( m_ActiveLog != null )
                m_ActiveLog.Write( input, args );
        }

        private static void ProcessCommand( string command, string args )
        {
            if( !m_CommandTable.ContainsKey( command ) )
            {
                Console.WriteLine( "El comando especificado no es válido." );
            }
            else
            {
                CommandDefinition def = m_CommandTable[command];
                bool log = ( m_TextOuput && def.Logable );

                if( log )
                {
                    string logPath = "";

                    if( m_OuputFolder != null && Directory.Exists( m_OuputFolder ) )
                        logPath += m_OuputFolder + "/";

                    m_ActiveLog = File.CreateText( logPath + command + "-log.txt" );
                }
                
                m_CommandTable[command].Handler( args );

                if( log )
                {                    
                    m_ActiveLog.Close();
                    m_ActiveLog = null;
                }
            }
        }

        private static bool CloseApp = false;

        private static void DoMainLoop()
        {  
            do
            {
                Console.Write( "\n> " );

                string command, args;
                ProcessInput( Console.ReadLine(), out command, out args );

                if( command != null )
                    ProcessCommand( command, args );
            }
            while( !CloseApp );
        }

        #region Definición de comandos

        private static readonly CommandDefinition[] m_CommandDefinitions = new CommandDefinition[]
            {
                new CommandDefinition( "exit", Exit_Command, false ),
                new CommandDefinition( "help", Help_Command, true ),
                new CommandDefinition( "open", Open_Command, false ),
                new CommandDefinition( "batch", Batch_Command, false ),
                new CommandDefinition( "fileouput", FileOuput_Command, false ),
                new CommandDefinition( "setouputfolder", SetOuputFolder_Command, false ),
                new CommandDefinition( "modelconfig", ModelConfig_Command, false ),
                new CommandDefinition( "gaconfig", GAConfig_Command, false ),
                new CommandDefinition( "darpconfig", DARPConfig_Command, false ),
                new CommandDefinition( "selectionconfig", SelectionConfig_Command, false ),
                new CommandDefinition( "printclientinfo", PrintClientInfo_Command, true ),
                new CommandDefinition( "precedenceinfo", PrecedenceInfo_Command, true ),
                new CommandDefinition( "drtinfo", DRTInfo_Command, true ),
                new CommandDefinition( "testroutemod", TestRouteMod_Command, false ),
                new CommandDefinition( "testsolutiongen", TestSolutionGen_Command, true ),
                new CommandDefinition( "run", Run_Command, false ),
                new CommandDefinition( "execmanagedrun", ExecManagedRun_Command, false ),
                new CommandDefinition( "genparamsgraph", GenParamsGraph_Command, false )
            };

        [CommandInfo( "exit",
            "Termina la ejecución de la aplicación.")]
        private static void Exit_Command( string args )
        {
            Environment.Exit( 0 );
        }

        [CommandInfo( "help",
            "Entrega ayuda acerca de los comandos disponibles.")]
        private static void Help_Command( string args )
        {
            PrintLine( "Comandos disponibles en esta versión:" );

            foreach( CommandDefinition def in m_CommandTable.Values )
            {
                string usage = "No definido.";
                string description = "No definido.";
                
                object[] attrs = def.Handler.Method.GetCustomAttributes( typeof( CommandInfoAttribute ), false );

                if( attrs.Length > 0 )
                {
                    CommandInfoAttribute info = (CommandInfoAttribute)attrs[0];

                    usage = info.Usage;
                    description = info.Description;
                }

                PrintLine( "\r\n\"{0}\" (Uso: {1})", def.Word.ToUpper(), usage );
                PrintLine( "\"{0}\"", description );
            }
        }

        [CommandInfo( "open <archivo de datos>",
            "Abre y procesa un archivo de datos de entrada." )]
        private static void Open_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Se requiere el nombre del archivo de entrada." );
                return;
            }

            if( !LoadDataFile( args ) )
            {
                PrintLine( "El archivo de entrada especificado no existe." );
                return;
            }

            PrintLine( "Archivo abierto exitosamente." );
            m_OpenFile = true;            
        }

        private static string m_ActualDataFile;
        public static string ActualDataFile { get { return m_ActualDataFile; } }

        public static bool LoadDataFile( string path )
        {
            StreamReader reader = null;

            try
            {
                reader = File.OpenText( path );
            }
            catch
            {
                return false;
            }
            
            DARPReader.ParseStream( reader );
            reader.Close();

            m_ActualDataFile = Path.GetFileNameWithoutExtension( path );

            return true;
        }

        [CommandInfo( "batch <archivo de comandos>",
            "Abre un archivo de comandos y ejecuta su contenido." )]
        private static void Batch_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Se requiere el nombre del archivo de comandos." );
                return;
            }

            StreamReader reader = null;

            try
            {
                reader = File.OpenText( args );
            }
            catch
            {
                PrintLine( "El archivo de comandos especificado no existe." );
                return;
            }

            string buffer = null;

            while( ( buffer = reader.ReadLine() ) != null )
            {
                string command, commandArgs;
                ProcessInput( buffer, out command, out commandArgs );

                if( command != null )
                    ProcessCommand( command, commandArgs );
            }

            reader.Close();            
        }

        [CommandInfo( "fileouput",
            "Configura si para algunos comandos, se generará una salida en archivo de texto." )]
        private static void FileOuput_Command( string args )
        {
            Print( "Generación de logs de comandos " );

            if( m_TextOuput )
            {
                PrintLine( "DESACTIVADA." );
                m_TextOuput = false;
            }
            else
            {
                PrintLine( "ACTIVADA." );
                m_TextOuput = true;
            }
        }

        [CommandInfo( "setouputfolder <ruta de carpeta>",
            "Configura una carpeta del disco para contener la salidas del programa." )]
        private static void SetOuputFolder_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Se requiere una ruta para la carpeta destino." );
                return;
            }

            if( !Directory.Exists( args ) )
            {
                PrintLine( "La carpeta seleccionada no existe." );
            }
            else
            {
                m_OuputFolder = args;
                PrintLine( "Nueva carpeta de salida: {0}", m_OuputFolder );                
            }
        }

        [CommandInfo( "modelconfig [id de modelo]",
            "Permite ver los modelos que se pueden utilizar o seleccionar uno." )]
        private static void ModelConfig_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Opciones disponibles:" );

                for( int i = 0; i < ModelCoreOptions.Length; i++ )                
                    PrintLine( "Opcion {0}: {1}" + ( m_ModelSelected == i ? " (Seleccionado)" : "" ), i, ModelCoreOptions[i].Name );                
            }
            else
            {
                string[] input = args.Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );
                
                int opt = -1;

                try
                {
                    opt = Int32.Parse( input[0] );
                }
                catch
                {
                    PrintLine( "Se esperaba un número de opción" );
                    return;
                }

                if( opt < 0 || opt > ModelCoreOptions.Length - 1 )
                {
                    PrintLine( "El número de opción especificado no es válido." );
                    return;
                }

                m_ModelSelected = opt;
                PrintLine( "Seleccinado Modelo: " + ModelCoreOptions[opt].Name );
            }
        }

        [CommandInfo( "gaconfig [<TAG del parámetro a configurar> <valor del párametro>]",
            "Permite ver la configuración actual de los parámetros del GA o cambiar sus valores. La ejecución del comando sin parámetros muestra los TAGs de los parámetros." )]
        private static void GAConfig_Command( string args )
        {
            if( m_GAParams.Count == 0 )
            {
                PrintLine( "No hay parámetros disponibles para configurar." );
                return;
            }
            
            if( args != null )
            {
                string[] input = args.Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                if( input.Length != 2 )
                {
                    PrintLine( "Se debe especificar el TAG del parámetro a configurar y su valor. Ejecute este comando sin parámetros para ver los TAGs disponibles." );
                    return;
                }

                ParameterEntry pEntry = m_GAParams[input[0]];

                if( pEntry == null )
                {
                    PrintLine( "El TAG especificado no esta definido. Ejecute este comando sin parámetros para ver los TAGs disponibles." );
                    return;
                }

                double toSet = 0.0;

                try
                {
                    toSet = Double.Parse( input[1] );
                }
                catch
                {
                    PrintLine( "El valor del parámetro especificado es inválido." );
                    return;
                }

                pEntry.Value = toSet;

                PrintLine( "Nuevo valor configurado: \"{0}\" -> \"{1}\"", pEntry.Name, toSet );
            }
            else
            {
                PrintLine( "{0}:\r\n", m_GAParams.ConfigName );

                foreach( string tag in m_GAParams.Tags )
                {
                    ParameterEntry pEntry = m_GAParams[tag];
                    PrintLine( "[TAG {0}] {1}: {2}", tag, pEntry.Name, pEntry.Value );
                }
            }
        }

        [CommandInfo( "darpconfig [<TAG de parámetro a configurar> <valor del párametro>]",
            "Permite ver la configuración actual de los parámetros de la función DARP o cambiar sus valores. La ejecución del comando sin parámetros muestra los TAGs de los parámetros." )]
        private static void DARPConfig_Command( string args )
        {
            if( m_DARPParams.Count == 0 )
            {
                PrintLine( "No hay parámetros disponibles para configurar." );
                return;
            }

            if( args != null )
            {
                string[] input = args.Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                if( input.Length != 2 )
                {
                    PrintLine( "Se debe especificar el TAG del parámetro a configurar y su valor." );
                    return;
                }

                ParameterEntry pEntry = m_DARPParams[input[0]];

                if( pEntry == null )
                {
                    PrintLine( "El TAG especificado no esta definido. Ejecute este comando sin parámetros para ver los TAGs disponibles." );
                    return;
                }

                double toSet = 0.0;

                try
                {
                    toSet = Double.Parse( input[1] );
                }
                catch
                {
                    PrintLine( "El valor del parámetro especificado es inválido." );
                    return;
                }

                pEntry.Value = toSet;

                PrintLine( "Nuevo valor configurado: \"{0}\" -> \"{1}\"", pEntry.Name, toSet );
            }
            else
            {
                PrintLine( "{0}:\r\n", m_DARPParams.ConfigName );

                foreach( string tag in m_DARPParams.Tags )
                {
                    ParameterEntry pEntry = m_DARPParams[tag];
                    PrintLine( "[TAG {0}] {1}: {2}", tag, pEntry.Name, pEntry.Value );
                }
            }
        }

        [CommandInfo( "selectionconfig [<TAG de parámetro a configurar> <valor del párametro>]",
            "Permite ver la configuración actual de los parámetros del operador de selección o cambiar sus valores. La ejecución del comando sin parámetros muestra los TAGs de los parámetros." )]
        private static void SelectionConfig_Command( string args )
        {
            if( m_SelectionParams.Count == 0 )
            {
                PrintLine( "No hay parámetros disponibles para configurar." );
                return;
            }

            if( args != null )
            {
                string[] input = args.Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                if( input.Length != 2 )
                {
                    PrintLine( "Se debe especificar el TAG del parámetro a configurar y su valor." );
                    return;
                }

                ParameterEntry pEntry = m_SelectionParams[input[0]];

                if( pEntry == null )
                {
                    PrintLine( "El TAG especificado no esta definido. Ejecute este comando sin parámetros para ver los TAGs disponibles." );
                    return;
                }

                double toSet = 0.0;

                try
                {
                    toSet = Double.Parse( input[1] );
                }
                catch
                {
                    PrintLine( "El valor del parámetro especificado es inválido." );
                    return;
                }

                pEntry.Value = toSet;

                PrintLine( "Nuevo valor configurado: \"{0}\" -> \"{1}\"", pEntry.Name, toSet );
            }
            else
            {
                PrintLine( "{0}:\r\n", m_SelectionParams.ConfigName );

                foreach( string tag in m_SelectionParams.Tags )
                {
                    ParameterEntry pEntry = m_SelectionParams[tag];
                    PrintLine( "[TAG {0}] {1}: {2}", tag, pEntry.Name, pEntry.Value );
                }
            }
        }

        [CommandInfo( "printclientinfo",
            "Muestra información respecto a los clientes de los datos de entrada cargados." )]
        private static void PrintClientInfo_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }

            PrintLine( "*** INFORMACION DE CLIENTES ***" );

            for( int i = 0; i < GlobalParams.ClientNumber; i++ )
            {
                Client cl = Clients.GetClient( i );
                ServiceRequest srp = cl.UpRequest;
                ServiceRequest srd = cl.DownRequest;

                PrintLine( "\r\nCliente {0}:", i );
                PrintLine( "Eventos {0} - {1}", srp, srd );
                PrintLine( "ept: {0}, lpt: {1}", srp.ET, srp.LT );
                PrintLine( "edt: {0}, ldt: {1}", srd.ET, srd.LT );
                PrintLine( "service time: {0}:", srp.ServiceTime );
                PrintLine( "load change: {0}", srp.LoadChange );
            }
        }

        [CommandInfo( "precedenceinfo",
            "Muestra información respecto a las información de precedencia que se ha obtenido de los datos de entrada." )]
        private static void PrecedenceInfo_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }        

            PrintLine( "*** INFORMACION DE LISTA DE PRECEDENCIA PRELIMINAR ***" );

            foreach( int n in Clients.PrecedenceTable.Keys )
            {
                Print( "\r\nEvento {0} =>", Clients.Requests[n] );

                foreach( int m in Clients.PrecedenceTable[n] )
                    Print( " {0}", Clients.Requests[m].ToString() );

                Print( "\r\n" );
            }

            PrintLine( "\r\n*** INFORMACION DE DEPENDENCIA CICLICA ENTRE CLIENTES ***" );

            foreach( int n in Clients.CyclicDependenceTable.Keys )
            {
                Print( "\r\nCliente {0} =>", n );

                foreach( int m in Clients.CyclicDependenceTable[n] )
                    Print( " {0}",  m );

                Print( "\r\n" );
            }
        }

        [CommandInfo( "drtinfo",
            "Muestra información respecto a las locaciones obtenidas de los datos de entrada." )]
        private static void DRTInfo_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }
            
            PrintLine( "*** INFORMACION DE RED DE LOCACIONES ***\r\n" );
            PrintLine( "Nodos:" );

            for( int i = 0; i < Locations.Count; i++ )
                PrintLine( "\r\nNodo {0}: x={1}, y={2}", i, Locations.GetXPos( i ), Locations.GetYPos( i ) );

            PrintLine( "\r\nDistancias:" );

            for( int i = 0; i < Locations.DRTS.GetLength( 0 ); i++ )
            {
                for( int j = i; j < Locations.DRTS.GetLength( 1 ); j++ )
                {
                    PrintLine( "\r\nNodo {0} a Nodo {1}: d={2}", i, j, Locations.DRTS[i, j] );
                }
            }
        }

        [CommandInfo( "testroutemod",
            "Activa el modo de testeo de modificación de rutas." )]
        private static void TestRouteMod_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }
            
            PrintLine( "Test de inserción/eliminación de clientes...\n\r" );

            PrintLine( "Escriba \"ins <id_cliente>\" para insertar el cliente especificado." );
            PrintLine( "Escriba \"del <id_cliente>\" para eliminar el cliente especificado." );
            PrintLine( "Escriba \"end\" para terminar.\n\r" );

            List<Client> remainingClients = new List<Client>( GlobalParams.ClientNumber );
            List<Client> insertedClients = new List<Client>( GlobalParams.ClientNumber );

            for( int i = 0; i < GlobalParams.ClientNumber; i++ )
                remainingClients.Add( Clients.GetClient( i ) );

            string[] input;
            Route buildRoute = new Route();
            bool exit = false;

            do
            {
                Print( "\n(testroutemod) > " );
                input = Console.ReadLine().ToLower().Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                if( input.Length < 1 )
                    continue;

                if( input[0] == "end" )
                {
                    PrintLine( "Testing terminado por el usuario." );
                    exit = true;
                }
                else if( input[0] == "del" )
                {
                    if( input.Length != 2 )
                    {
                        PrintLine( "Uso: del <client_id>" );
                    }
                    else
                    {
                        int clientID;

                        try
                        {
                            clientID = Int32.Parse( input[1] );
                        }
                        catch
                        {
                            PrintLine( "El ID del cliente especificado tiene formato inválido" );
                            continue;
                        }

                        if( clientID < 0 || clientID >= GlobalParams.ClientNumber )
                        {
                            PrintLine( "El ID del cliente especificado es inválido" );
                            continue;
                        }

                        Client toDelete = Clients.GetClient( clientID );

                        if( !insertedClients.Contains( toDelete ) )
                        {
                            PrintLine( "El cliente señalado no ha sido insertado en la ruta." );
                            continue;
                        }

                        Print( "Eliminando cliente " + toDelete + "... " );

                        if( !RouteGeneration.CheckClientDeletion( buildRoute, toDelete ) )
                        {
                            PrintLine( "NO FACTIBLE." );
                            PrintLine( "Ruta actual: " + buildRoute.ToString() );
                        }
                        else
                        {
                            insertedClients.Remove( toDelete );
                            remainingClients.Add( toDelete );

                            PrintLine( "OK, {0} clientes en ruta.", insertedClients.Count );
                            PrintLine( "Ruta actual: " + buildRoute.ToString() );
                        }

                    }
                }
                else if( input[0] == "ins" )
                {
                    if( input.Length != 2 )
                    {
                        PrintLine( "Uso: ins <client_id>" );
                    }
                    else
                    {
                        int clientID;

                        try
                        {
                            clientID = Int32.Parse( input[1] );
                        }
                        catch
                        {
                            PrintLine( "El ID del cliente especificado tiene formato inválido" );
                            continue;
                        }

                        if( clientID < 0 || clientID >= GlobalParams.ClientNumber )
                        {
                            PrintLine( "El ID del cliente especificado es inválido" );
                            continue;
                        }

                        Client toAdd = Clients.GetClient( clientID );

                        if( insertedClients.Contains( toAdd ) )
                        {
                            PrintLine( "El cliente señalado ya ha sido insertado en la ruta." );
                            continue;
                        }

                        Print( "Insertando cliente " + toAdd + "... " );

                        if( !RouteGeneration.CheckClientInsertion( buildRoute, toAdd ) )
                        {
                            PrintLine( "NO FACTIBLE." );
                            PrintLine( "Ruta actual: " + buildRoute.ToString() );
                        }
                        else
                        {
                            insertedClients.Add( toAdd );
                            remainingClients.Remove( toAdd );

                            PrintLine( "OK, {0} clientes en ruta.", insertedClients.Count );
                            PrintLine( "Ruta actual: " + buildRoute.ToString() );
                        }

                    }
                }
                else
                {
                    PrintLine( "El comando ingresado es inválido.." );
                }
            }
            while( !exit );
        }

        [CommandInfo( "testsolutiongen",
            "Realiza un test de generación de una solución aleatoria." )]
        private static void TestSolutionGen_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }

            Route[] routes = null;
            ClientMask[] masks = null;

            PrintLine( "*** TEST DE GENERACIÓN DE SOLUCION ALEATORIA FACTIBLE ***\r\n" );

            DateTime start = DateTime.Now;
            SolutionGeneration.GenerateSolution( out masks, out routes );
            DateTime end = DateTime.Now;

            PrintLine( "Datos de solución encontrada:\r\n" );
            PrintLine( "Demora: {0}\r\n", end - start );

            PrintLine( "Clusters:\r\n" );

            for( int i = 0; i < masks.Length; i++ )
                PrintLine( "{0}: {1}", i, masks[i] );

            PrintLine( "\r\nRutas:\r\n" );

            for( int i = 0; i < routes.Length; i++ )
                PrintLine( "{0} ({1} Clientes): {2}", i, routes[i].ClientCount, routes[i] );
        }

        [CommandInfo( "run",
            "Ejecuta la instancia del Algoritmo Genético configurada actualmente." )]
        private static void Run_Command( string args )
        {
            if( !m_OpenFile )
            {
                PrintLine( "No hay archivo abierto para procesar, utilice el comando OPEN para procesar un archivo de entradas." );
                return;
            }

            Run();
        }

        private static void Run()
        {
            string ouputFolder = null;
            
            if( m_OuputFolder != null )
            {
                ouputFolder = m_OuputFolder + "/GAResults/" + DateTime.Now.ToFileTime().ToString();

                if( !Directory.Exists( ouputFolder ) )
                    Directory.CreateDirectory( ouputFolder );
            }

            Run( ouputFolder );
        }

        public static void Run( string baseOuputFolder )
        {
            Evaluation dummy;
            Run( baseOuputFolder, out dummy );
        }

        public static void Run( string baseOuputFolder, out Evaluation eval )
        {
            GA test = new GA();

            test.CrossoverMethod = ModelCoreOptions[m_ModelSelected].CrossoverMethod;
            test.InitialPoblationGenerator = ModelCoreOptions[m_ModelSelected].InitialPoblationMethod;
            test.SelectionMethod = ModelCoreOptions[m_ModelSelected].SelectionMethod;
            test.RouteMutationMethod = ModelCoreOptions[m_ModelSelected].RouteMutationMethod;
            test.ClusterMutationMethod = ModelCoreOptions[m_ModelSelected].ClusterMutationMethod;

            if( baseOuputFolder == null || !Directory.Exists( baseOuputFolder ) )
            {
                baseOuputFolder = "GAResults/" + DateTime.Now.ToFileTime().ToString();

                if( !Directory.Exists( baseOuputFolder ) )
                    Directory.CreateDirectory( baseOuputFolder );
            }

            test.OuputFolder = baseOuputFolder;

            m_GAParams.ApplyValues( test );
            m_DARPParams.ApplyValues();
            m_SelectionParams.ApplyValues();

            test.Go( out eval );
        }

        /* Formato de archivo de parámetros:
         * 
         * linea 1: <id modelo 1> <id modelo 2> ... <id modelo N>
         * linea 2: <archivo de datos 1> <archivo de datos 2> ... <archivo de datos N>
         * linea 3: <nro repeticiones por test>
         * linea 4 - linea N: < <tipo de parámetro> <tag de parámetro asociado> <valor 1 parámetro> <valor 2 parámetro> ... <valor N parámetro> >
         * 
         * tipos de parámetro: definido según el comando especificado que lo configure.
         * tag de parémetro: definido según el ParameterTagAttribute que lo configure.
         */

        [CommandInfo( "execmanagedrun <archivo de parámetros>",
            "Realiza un conjunto de ejecuciones del Algoritmo Genético en base a los parámetros del archivo proporcionado." )]
        private static void ExecManagedRun_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Se requiere el nombre del archivo de parámetros." );
                return;
            }

            StreamReader reader = null;

            try
            {
                reader = File.OpenText( args );
            }
            catch
            {
                PrintLine( "El archivo de parámetros especificado no existe." );
                return;
            }

            ManagedRunLauncher mRun = new ManagedRunLauncher();

            string[] input = reader.ReadLine().Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

            if( input.Length == 0 )
            {
                PrintLine( "Error: No se especificaron modelos." );
                reader.Close();
                return;
            }

            for( int i = 0; i < input.Length; i++ )
            {
                int model = 0;

                try
                {
                    model = Int32.Parse( input[i] );
                }
                catch
                {
                    PrintLine( "Error: Formato de modelo {0} inválido (se esperaba valor entero).", i + 1 );
                    reader.Close();
                    return;
                }

                if( model < 0 || model > ModelCoreOptions.Length - 1 )
                {
                    PrintLine( "Error: Modelo {0} no definido.", i + 1 );
                    reader.Close();
                    return;
                }

                mRun.AddModel( model );
            }

            input = reader.ReadLine().Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

            if( input.Length == 0 )
            {
                PrintLine( "Error: No se especificaron archivos de datos." );
                reader.Close();
                return;
            }

            for( int i = 0; i < input.Length; i++ )
            {
                if( !File.Exists( input[i] ) )
                {
                    PrintLine( "Error: Archivos de datos {0} no existe.", i + 1 );
                    reader.Close();
                    return;
                }

                mRun.AddInstance( input[i] );
            }

            input = reader.ReadLine().Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

            if( input.Length != 1 )
            {
                PrintLine( "Error: No se especificó cantidad de repeticiones." );
                reader.Close();
                return;
            }

            int reps = 0;

            try
            {
                reps = Int32.Parse( input[0] );
            }
            catch
            {
                PrintLine( "Error: Formato inválido en cantidad de repeticiones (se esperaba valor entero)." );
                reader.Close();
                return;
            }

            mRun.Repetitions = reps; ;

            string buffer;
            int line = 4;

            while( ( buffer = reader.ReadLine() ) != null )
            {
                input = buffer.Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                if( input.Length < 3 )
                {
                    PrintLine( "Error: Especificación de parámetros inválida en linea {0}.", line );
                    reader.Close();
                    return;
                }

                ParameterEntry entry = null;

                switch( input[0].ToLower() )
                {
                    case "gaconfig":
                    {
                        entry = m_GAParams[input[1].ToLower()];                        
                        break;
                    }
                    case "selectionconfig":
                    {
                        entry = m_SelectionParams[input[1].ToLower()];
                        break;
                    }
                    default:
                    {
                        PrintLine( "Error: Tipo de parámetro inválido en linea {0}.", line );
                        reader.Close();
                        return;
                    } 
                }

                if( entry == null )
                {
                    PrintLine( "Error: Tag de parámetro inválido en linea {0}.", line );
                    reader.Close();
                    return;
                }

                for( int i = 2; i < input.Length; i++ )
                {
                    double value = 0.0;

                    try
                    {
                        value = Double.Parse( input[i] );
                    }
                    catch
                    {
                        PrintLine( "Error: Valor de parámetro {0} inválido en linea {1}.", i - 1, line );
                        reader.Close();
                        return;
                    }

                    mRun.AddParameterValue( entry, value );
                }

                line++;
            }

            if( m_OuputFolder != null )
            {
                string ouputFolder = m_OuputFolder + "/ManagedRuns/" + DateTime.Now.ToFileTime().ToString();

                if( !Directory.Exists( ouputFolder ) )
                    Directory.CreateDirectory( ouputFolder );

                mRun.BaseOuputFolder = ouputFolder;
            }

            mRun.Go();
        }

        [CommandInfo( "genparamsgraph <archivo de datos>",
            "Genera un conjunto de graficos de comparación de parámetros en base a los datos archivo proporcionado." )]
        private static void GenParamsGraph_Command( string args )
        {
            if( args == null )
            {
                PrintLine( "Se requiere el nombre del archivo de datos." );
                return;
            }

            StreamReader reader = null;

            try
            {
                reader = File.OpenText( args );
            }
            catch
            {
                PrintLine( "El archivo de datos especificado no existe." );
                return;
            }
            
            List<ManagedRunParameter> paramSet = new List<ManagedRunParameter>();

            paramSet.Add( new ManagedRunParameter( m_GAParams["pobsize"] ) );
            paramSet.Add( new ManagedRunParameter( m_GAParams["gencount"] ) );
            paramSet.Add( new ManagedRunParameter( m_GAParams["crossrate"] ) );
            paramSet.Add( new ManagedRunParameter( m_GAParams["mut1rate"] ) );
            paramSet.Add( new ManagedRunParameter( m_GAParams["mut2rate"] ) );
            paramSet.Add( new ManagedRunParameter( m_SelectionParams["tournsize"] ) );

            for( int i = 0; i < 6; i++ )
            {
                string[] input = reader.ReadLine().Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                for( int j = 0; j < input.Length; j++ )
                    paramSet[i].AddValue( Double.Parse( input[j] ) );
            }

            string ouputFolder;

            if( m_OuputFolder == null || !Directory.Exists( m_OuputFolder ) )
                ouputFolder = "";
            else            
                ouputFolder = m_OuputFolder + "/";

            ouputFolder += "GraphSets/" + DateTime.Now.ToFileTime().ToString();
            Directory.CreateDirectory( ouputFolder );

            ManagedRunGraphSet graphSet = new ManagedRunGraphSet( paramSet, ouputFolder );

            int line = 1;
            string buffer;
            double bestValue = Double.MaxValue;

            while( ( buffer = reader.ReadLine() ) != null )
            {
                string[] input = buffer.Trim().Split( charSeparator, StringSplitOptions.RemoveEmptyEntries );

                try
                {
                    for( int i = 0; i < paramSet.Count; i++ )                    
                        paramSet[i].ApplyValueToEntry( Double.Parse( input[i] ) );

                    double value = Double.Parse( input[6] );

                    if( value < bestValue )
                        bestValue = value;

                    graphSet.ProcessResult( paramSet, value );
                }
                catch
                {                    
                    PrintLine( "Error de lectura en el archivo (linea {0}).", line );
                    reader.Close();
                    return;                    
                }

                line++;
            }

            reader.Close();

            for( int i = 0; i < graphSet.DualCount; i++ )
                graphSet.GenerateDualGraph( i, bestValue );

            for( int i = 0; i < graphSet.SingleCount; i++ )
                graphSet.GenerateSingleGraph( i, bestValue );
        }

        #endregion
    }
}
