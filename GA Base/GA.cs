/* GA.cs
 * �ltima modificaci�n: 25/04/2008
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using dotnetCHARTING.WinForms;
using DARPTW_GA.Misc;
using DARPTW_GA.DARP;
using DARPTW_GA.Framework;

namespace DARPTW_GA.GA_Base
{
    /* Delegado 'InitialPoblationLauncher'
     *  Define un tipo de m�todo en el que se implementar� un generador de p�blaci�n inicial, retornando
     * una lista de cromosomas que representa dicha poblaci�n.
     * 
     * Par�metros:
     * - 'int pobSize': Tama�o de la poblaci�n a generar.
     */
    public delegate List<Genome> InitialPoblationLauncher( int pobSize );

    /* Delegado 'SelectionLauncher'
     *  Define un tipo de m�todo en el que se implementar� un operador de selecci�n, retornando un
     * individual seleccionado de la poblaci�n correspondiente.
     * 
     * Par�metros:
     * - 'List<Genome> poblation': Poblaci�n en base a la cual se realizar� la selecci�n.
     */
    public delegate Genome SelectionLauncher( List<Genome> poblation );

    /* Delegado 'CrossoverLauncher'
     *  Define un tipo de m�todo en el que se implementar� un operador de recombinaci�n, retornando un
     * arreglo de cromosomas (normalmente de largo 2) resultante de la recombinaci�n.
     * 
     * Par�metros:
     * - 'Genome[] parents': Arreglo que contiene a los cromosomas padres usados en la recombinaci�n
     * (normalmente de largo 2).
     */
    public delegate Genome[] CrossoverLauncher( Genome[] parents );

    /* Delegado 'MutationLauncher'
     *  Define un tipo de m�todo en el que se implementar� un operador de mutaci�n.
     * 
     * Par�metros:
     * - 'Genome toMutate': Cromosoma que ser� sometido a la recombinaci�n.
     * - 'double chance': Probabilidad de mutaci�n usada.
     */
    public delegate void MutationLauncher( Genome toMutate, double chance );

    /* Clase 'GA'
     *  Implementa un objeto que permite realizar la ejecuci�n del algoritmo genetico en base a distintos
     * par�metros configurados, relacionados con los modelos implementados y los par�metros del problema.
     * Tambi�n guarda atributos propios del algoritmo gen�tico.
     * 
     * Atributos:
     * - 'double m_routeMutationRate': Ratio de mutaci�n en rutas configurado en el GA.
     * - 'double m_clusterMutationRate': Ratio de mutaci�n en clusters configurado en el GA.
     * - 'double m_crossoverRate': Ratio de recombinaci�n configurado en el GA.
     * - 'double m_populationSize': Tama�o de la poblaci�n configurada en el GA.
     * - 'double m_generationMax': N�mero de generaciones m�ximas configurada en el GA.
     * - 'List<Genome> m_thisGeneration': Lista de cromosomas que representa la poblaci�n actual que maneja
     * el GA.
     * - 'InitialPoblationLauncher m_initialPoblationGenerator': Delegado que referencia al m�todo usado
     * para generar la poblaci�n inicial.
     * - 'SelectionLauncher m_selectionMethod': Delegado que referencia al m�todo usado como operador de 
     * selecci�n.
     * - 'CrossoverLauncher m_crossoverMethod': Delegado que referencia al m�todo usado como operador de
     * recombinaci�n;
     * - 'MutationLauncher m_routeMutationMethod': Delegado que referencia al m�todo usado como operador
     * de mutaci�n en rutas;
     * - 'MutationLauncher m_clusterMutationMethod': Delegado que referencia al m�todo usado como operador
     * de mutaci�n en cl�sters;
     * - 'string m_OuputFolder': Ruta del directorio de salida que se usar� para entregar los resultados
     * que arroje el GA.
     * - 'StreamWriter m_InfoWriter': Buffer de salida para el archivo que entregar� informaci�n respecto
     * a la ejecuci�n del GA.
     * - 'StreamWriter m_SolutionsWriter': Buffer de salida para el archivo que entregar� informaci�n de
     * las soluciones.
     * - 'Dictionary<int, double> m_Graph_BestPerGeneration': Tabla que almacena la informaci�n para la
     * generaci�n del gr�fico de convergencia, respecto a los mejores resultados por generaci�n.
     * - 'Dictionary<int, double> m_Graph_MeanPerGeneration': Tabla que almacena la informaci�n para la
     * generaci�n del gr�fico de convergencia, respecto a los resultados promedio por generaci�n.
     * - 'Dictionary<int, double> m_Graph_BestSolutions': Tabla que almacena la informaci�n para la
     * generaci�n del gr�fico de convergencia, respecto a los mejores resultados en ejecuci�n.
     * - 'int m_Graph_BestPerGenerationDensity': Valor que indica cada cuantas generaciones se
     * graficar� un punto en la linea de convergencia de los mejores resultados por generaci�n.
     * - 'int m_Graph_MeanPerGenerationDensity': Valor que indica cada cuantas generaciones se
     * graficar� un punto en la linea de convergencia de los resultados promedio por generaci�n.
     * - 'int[] m_Cursor_GenerationPos': Arreglo de enteros, usado como un par, para guardar la informaci�n
     * de la posici�n del cursor donde se escribir� el nro. de generaci�n mientras se ejecuta el GA.
     * - 'int[] m_Cursor_BestPerGenerationPos': Arreglo de enteros, usado como un par, para guardar la
     * informaci�n de la posici�n del cursor donde se escribir� el valor del mejor individuo por generaci�n
     * mientras se ejecuta el GA.
     * - 'int[] m_Cursor_MeanPerGenerationPos': Arreglo de enteros, usado como un par, para guardar la
     * informaci�n de la posici�n del cursor donde se escribir� el valor promedio de los individuos por
     * generaci�n mientras se ejecuta el GA.
     * - 'int[] m_Cursor_BestSolutionPos': Arreglo de enteros, usado como un par, para guardar la
     * informaci�n de la posici�n del cursor donde se escribir� el valor del mejor individuo encontrado
     * hasta el momento mientras se ejecuta el GA.
     * - 'm_Cursor_EndProcess': Arreglo de enteros, usado como un par, para guardar la informaci�n de la
     * posici�n del cursor donde se escribir� el mensaje de t�rmino cuando el usuario finalize manualmente
     * la ejecuci�n del GA.
     * - 'double m_Statistics_BestValuePerGeneration': Variable que guarda el valor del mejor individuo
     * por generaci�n.
     * - 'Genome m_Statistics_BestPerGeneration': Variable que guarda la referencia del individuo con el
     * mejor valor por generaci�n.
     * - 'double m_Statistics_MeanPerGeneration': Variable que guarda el valor promedio de los individuos
     * por generaci�n.
     * - 'double m_Statistics_BestSolutionValue': Variable que guarda el valor del mejor individuo encontrado
     * hasta el momento en la ejecuci�n del GA.
     * - 'Evaluation m_Statistics_BestSolutionInfo': Variable que guarda la referencia al objeto que tiene la
     * informaci�n de evaluaci�n del mejor individuo encontrado hasta el momento en la ejecuci�n del GA.
     */
    public class GA
    {
        private double m_routeMutationRate = 0.1;
        private double m_clusterMutationRate = 0.01;
        private double m_crossoverRate = 0.7;
        private int m_populationSize = 100;
        private int m_generationMax = 1000;

        private List<Genome> m_thisGeneration;

        [ParameterTag( "Tama�o de la poblaci�n", "pobsize", 2, Int32.MaxValue, 100 )]
        public int PopulationSize
        {
            get { return m_populationSize; }
            set { m_populationSize = value; }
        }

        [ParameterTag( "N�mero m�ximo de generaciones", "gencount", 1, Int32.MaxValue, 1000 )]
        public int Generations
        {
            get { return m_generationMax; }
            set { m_generationMax = value; }
        }

        [ParameterTag( "Probabilidad de recombinaci�n", "crossrate", 0.0, 1.0, 0.7 )]
        public double CrossoverRate
        {
            get { return m_crossoverRate; }
            set { m_crossoverRate = value; }
        }

        [ParameterTag( "Probabilidad de mutaci�n en rutas", "mut2rate", 0.0, 1.0, 0.1 )]
        public double RouteMutationRate
        {
            get { return m_routeMutationRate; }
            set { m_routeMutationRate = value; }
        }

        [ParameterTag( "Probabilidad de mutaci�n en clusters", "mut1rate", 0.0, 1.0, 0.01 )]
        public double ClusterMutationRate
        {
            get { return m_clusterMutationRate; }
            set { m_clusterMutationRate = value; }
        }

        private InitialPoblationLauncher m_initialPoblationGenerator;
        private SelectionLauncher m_selectionMethod;
        private CrossoverLauncher m_crossoverMethod;
        private MutationLauncher m_routeMutationMethod;
        private MutationLauncher m_clusterMutationMethod;

        public InitialPoblationLauncher InitialPoblationGenerator
        {
            get { return m_initialPoblationGenerator; }
            set { m_initialPoblationGenerator = value; }
        }

        public SelectionLauncher SelectionMethod
        {
            get { return m_selectionMethod; }
            set { m_selectionMethod = value; }
        }

        public CrossoverLauncher CrossoverMethod
        {
            get { return m_crossoverMethod; }
            set { m_crossoverMethod = value; }
        }

        public MutationLauncher RouteMutationMethod
        {
            get { return m_routeMutationMethod; }
            set { m_routeMutationMethod = value; }
        }

        public MutationLauncher ClusterMutationMethod
        {
            get { return m_clusterMutationMethod; }
            set { m_clusterMutationMethod = value; }
        }

        /* Constructor
         *  Instancia un nuevo objeto GA para ser usado con sus par�metros por defecto.
         */
        public GA()
        {
        }

        /* M�todo 'GenerateParameterInfoString'
         *  Genera un string con informaci�n relevante a los par�metros usados en la ejecuci�n del GA,
         * adem�s de sus operadores y las caracteristicas de los datos de entrada asociados al problema.
         */
        private string GenerateParameterInfoString()
        {
            string ret = "";

            ret += "INFORMACION DE PARAMETROS CONFIGURADOS\r\n";

            ParameterConfig gaParams = Program.GAParams;
            ParameterConfig selectionParams = Program.SelectionParams;
            ret += "\r\n1) " + gaParams.ConfigName + "\r\n";

            foreach( string tag in gaParams.Tags )
            {
                ParameterEntry pEntry = gaParams[tag];
                ret += String.Format( "{0}: {1}\r\n", pEntry.Name, pEntry.Value );
            }

            foreach( string tag in selectionParams.Tags )
            {
                ParameterEntry pEntry = selectionParams[tag];
                ret += String.Format( "{0}: {1}\r\n", pEntry.Name, pEntry.Value );
            }

            ParameterConfig darpParams = Program.DARPParams;
            ret += "\r\n1) " + darpParams.ConfigName + "\r\n";

            foreach( string tag in darpParams.Tags )
            {
                ParameterEntry pEntry = darpParams[tag];
                ret += String.Format( "{0}: {1}\r\n", pEntry.Name, pEntry.Value );
            }

            ret += "\r\n3) OPERADORES UTILIZADOS\r\n";

            string[] opNames = new string[5];
            object[] check;

            if( m_crossoverMethod != null && ( check = m_crossoverMethod.Method.GetCustomAttributes( typeof( OperatorAttribute ), false ) ).Length > 0 )
                opNames[0] = ( (OperatorAttribute)check[0] ).Name;

            if( m_routeMutationMethod != null && ( check = m_routeMutationMethod.Method.GetCustomAttributes( typeof( OperatorAttribute ), false ) ).Length > 0 )
                opNames[1] = ( (OperatorAttribute)check[0] ).Name;

            if( m_clusterMutationMethod != null && ( check = m_clusterMutationMethod.Method.GetCustomAttributes( typeof( OperatorAttribute ), false ) ).Length > 0 )
                opNames[2] = ( (OperatorAttribute)check[0] ).Name;

            if( m_initialPoblationGenerator != null && ( check = m_initialPoblationGenerator.Method.GetCustomAttributes( typeof( OperatorAttribute ), false ) ).Length > 0 )
                opNames[3] = ( (OperatorAttribute)check[0] ).Name;

            if( m_selectionMethod != null && ( check = m_selectionMethod.Method.GetCustomAttributes( typeof( OperatorAttribute ), false ) ).Length > 0 )
                opNames[4] = ( (OperatorAttribute)check[0] ).Name;

            ret += String.Format( "Operador recombinaci�n: {0}\r\n", opNames[0] != null ? opNames[0] : "NO DEFINIDO" );
            ret += String.Format( "Operador mutaci�n en rutas: {0}\r\n", opNames[1] != null ? opNames[1] : "NO DEFINIDO" );
            ret += String.Format( "Operador mutaci�n en clusters: {0}\r\n", opNames[2] != null ? opNames[2] : "NO DEFINIDO" );
            ret += String.Format( "Generador de poblaci�n inicial: {0}\r\n", opNames[3] != null ? opNames[3] : "NO DEFINIDO" );
            ret += String.Format( "Operador de selecci�n: {0}\r\n", opNames[4] != null ? opNames[4] : "NO DEFINIDO" );

            ret += "\r\n4) CARACTERISTICAS DE LOS DATOS DE ENTRADA\r\n";

            ret += String.Format( "Cantidad de clientes: {0}\r\n", GlobalParams.ClientNumber );
            ret += String.Format( "Cantidad m�xima de veh�culos: {0}\r\n", GlobalParams.VehiclesMaxNumber );
            ret += String.Format( "Carga m�xima de veh�culos: {0}\r\n", GlobalParams.VehicleMaxLoad );
            ret += String.Format( "Tiempo m�ximo de transporte de pasajeros: {0}", GlobalParams.MRT );

            return ret;
        }

        /* M�todo 'GenerateSolutionDetailsString'
         *  Genera un string con informaci�n relevante a una soluci�n, que puede ser la mejor de una
         * generaci�n, lo que incluye su evaluaci�n, rutas, m�scaras y tiempos calculados.
         * 
         * Par�metros:
         * - 'int genNumber': N�mero de generaci�n de la evaluaci�n.
         * - 'Genome solution': Cromosoma del que se sacar� la informaci�n.
         */
        private string GenerateSolutionDetailsString( int genNumber, Genome solution )
        {
            string ret = "***************************\r\n";

            ret += String.Format( "\r\nGENERACION {0} de {1}\r\n", genNumber, m_generationMax );
            ret += "Detalles de soluci�n:\r\n\r\n";

            ret += String.Format( "Evaluaci�n / Calidad: {0}\r\n\r\n", solution.Fitness );

            ret += "Rutas:\r\n";
            ret += solution.GetStringRoutes() + "\r\n";
            ret += "Clusters:\r\n";
            ret += solution.GetStringMasks() + "\r\n";

            ret += "Tiempos:\r\n";
            ret += String.Format( "Travel Time: {0}\r\n", solution.Evaluation.TravelTime );
            ret += String.Format( "Slack Time: {0}\r\n", solution.Evaluation.SlackTime );
            ret += String.Format( "Vehicle Quantity: {0}\r\n", solution.Evaluation.VQ );
            ret += String.Format( "Excess Ride Time: {0}\r\n", solution.Evaluation.ExcessRideTime );
            ret += String.Format( "Ride Time: {0}\r\n", solution.Evaluation.RideTime );
            ret += String.Format( "Wait Time: {0}\r\n\r\n", solution.Evaluation.WaitTime );

            ret += "***************************";

            return ret;
        }

        private string m_OuputFolder;
        private StreamWriter m_InfoWriter;
        private StreamWriter m_SolutionsWriter;

        private Dictionary<int, double> m_Graph_BestPerGeneration;
        private Dictionary<int, double> m_Graph_MeanPerGeneration;
        private Dictionary<int, double> m_Graph_BestSolutions;

        private int m_Graph_BestPerGenerationDensity = 1;
        private int m_Graph_MeanPerGenerationDensity = 1;

        public string OuputFolder
        {
            get { return m_OuputFolder; }
            set { m_OuputFolder = value; }
        }

        public int Graph_BestPerGenerationDensity
        {
            get { return m_Graph_BestPerGenerationDensity; }

            set
            {
                if( value < 1 )
                    value = 1;
                else if( value > m_generationMax )
                    value = m_generationMax;

                m_Graph_BestPerGenerationDensity = value;
            }
        }

        public int Graph_MeanPerGenerationDensity
        {
            get { return m_Graph_MeanPerGenerationDensity; }

            set
            {
                if( value < 1 )
                    value = 1;
                else if( value > m_generationMax )
                    value = m_generationMax;

                m_Graph_MeanPerGenerationDensity = value;
            }
        }

        /* M�todo 'CheckOuputFolder'
         *  Realiza un checkeo previo de la carpeta configurada para las salidas del GA. En caso de que
         * el directorio no exista, se usar� uno por defecto en la carpeta de la aplicaci�n.
         */
        private void CheckOuputFolder()
        {
            if( m_OuputFolder == null || !Directory.Exists( m_OuputFolder ) )
            {
                m_OuputFolder = "GAResults/" + DateTime.Now.ToFileTime().ToString();
                Directory.CreateDirectory( m_OuputFolder );
            }
        }

        /* M�todo 'InitLogs'
         *  Inicializa los archivos de salida del GA, en base a la ruta de salida configurada.
         */
        private void InitLogs()
        {
            Console.Clear();
            
            m_InfoWriter = File.CreateText( m_OuputFolder + "/garun-info.txt" );
            m_SolutionsWriter = File.CreateText( m_OuputFolder + "/garun-solutions.txt" );
            m_InfoWriter.AutoFlush = true;
            m_SolutionsWriter.AutoFlush = true;
            
            m_SolutionsWriter.WriteLine( "INFORMACION DE MEJORES SOLUCIONES ENCONTRADAS\r\n" );
        }

        /* M�todo 'CloseLogs'
         *  Cierra los buffer de los archivos de salida usados en el GA.
         */
        private void CloseLogs()
        {
            if( m_InfoWriter != null )
            {
                m_InfoWriter.Close();
                m_InfoWriter = null;
            }

            if( m_SolutionsWriter != null )
            {
                m_SolutionsWriter.Close();
                m_SolutionsWriter = null;
            }
        }

        /* M�todo 'LogLine'
         *  Escribe un texto con salto de linea en el archivo de salida de informaci�n activo del GA.
         * 
         * Par�metros:
         * - 'string input': String con formato que se escribir� en el archivo.
         * - 'object[] args': lista de entradas que se usan como par�metros del string formateado que se
         * escribir� en el archivo.
         */
        private void LogLine( string input, params object[] args )
        {
            LogLine( true, input, args );
        }

        /* M�todo 'LogLine'
         *  Escribe un texto con salto de linea en consola, con la opci�n que tambi�n sea impresa en el archivo
         * de salida de informaci�n activo del GA.
         * 
         * Par�metros:
         * - 'bool toFile': Booleano que indica si la salida tambi�n se imprimir� en el archivo (true).
         * - 'string input': String con formato que se escribir� en el archivo.
         * - 'object[] args': lista de entradas que se usan como par�metros del string formateado que se
         * imprimir�.
         */
        private void LogLine( bool toFile, string input, params object[] args )
        {
            Console.WriteLine( input, args );

            if( toFile && m_InfoWriter != null )
                m_InfoWriter.WriteLine( input, args );
        }

        /* M�todo 'Log'
         *  Escribe un texto sin salto de linea en el archivo de salida de informaci�n activo del GA.
         * 
         * Par�metros:
         * - 'string input': String con formato que se escribir� en el archivo.
         * - 'object[] args': lista de entradas que se usan como par�metros del string formateado que se
         * escribir� en el archivo.
         */
        private void Log( string input, params object[] args )
        {
            Log( true, input, args );
        }

        /* M�todo 'Log'
         *  Escribe un texto sin salto de linea en consola, con la opci�n que tambi�n sea impresa en el archivo
         * de salida de informaci�n activo del GA.
         * 
         * Par�metros:
         * - 'bool toFile': Booleano que indica si la salida tambi�n se imprimir� en el archivo (true).
         * - 'string input': String con formato que se escribir� en el archivo.
         * - 'object[] args': lista de entradas que se usan como par�metros del string formateado que se
         * imprimir�.
         */
        private void Log( bool toFile, string input, params object[] args )
        {
            Console.Write( input, args );

            if( toFile && m_InfoWriter != null )
                m_InfoWriter.Write( input, args );
        }

        private int[] m_Cursor_GenerationPos;
        private int[] m_Cursor_BestPerGenerationPos;
        private int[] m_Cursor_MeanPerGenerationPos;
        private int[] m_Cursor_BestSolutionPos;
        private int[] m_Cursor_EndProcess;

        private double m_Statistics_BestValuePerGeneration;
        private Genome m_Statistics_BestPerGeneration;
        private double m_Statistics_MeanPerGeneration;
        private double m_Statistics_BestSolutionValue;
        private Evaluation m_Statistics_BestSolutionInfo;

        /* M�todo 'InitStatistics'
         *  Inicializa los valores de las distintas variables usadas para manejar estad�sticas mientras se
         * ejecuta el GA, mostrando avisos por consola correspondientemente.
         */
        private void InitStatistics()
        {
            Console.Write( "Generaci�n: " );
            m_Cursor_GenerationPos = new int[] { Console.CursorLeft, Console.CursorTop };

            Console.Write( "\r\nMejor Evaluaci�n Actual: " );
            m_Cursor_BestPerGenerationPos = new int[] { Console.CursorLeft, Console.CursorTop };

            Console.Write( "\r\nPromedio Actual: " );
            m_Cursor_MeanPerGenerationPos = new int[] { Console.CursorLeft, Console.CursorTop };

            Console.Write( "\r\n\r\nMejor resultado hasta ahora: " );
            m_Cursor_BestSolutionPos = new int[] { Console.CursorLeft, Console.CursorTop };

            m_Cursor_EndProcess = new int[] { 0, m_Cursor_BestSolutionPos[1] + 1 };

            m_Statistics_BestSolutionValue = Double.MaxValue;

            m_Graph_BestPerGeneration = new Dictionary<int, double>( m_generationMax );
            m_Graph_MeanPerGeneration = new Dictionary<int, double>( m_generationMax );
            m_Graph_BestSolutions = new Dictionary<int, double>( (int)( m_generationMax * 0.1 ) );
        }

        /* M�todo 'UpdateGenerationStatistics'
         *  Actualiza las variables que almacena informaci�n estadistica de la ejecuci�n del GA, acorde a la
         * situaci�n de la generaci�n actual. Adicionalmente, escribe la informaci�n actualizada en consola y
         * actualiza los datos de las variables asociadas al gr�fico de convergencia.
         * 
         * Par�metros:
         * - 'int generation': N�mero de la generaci�n actual.
         */
        private void UpdateGenerationStatistics( int generation )
        {
            // Primero se actualizan los valores de las variables estad�sticas gen�ricas...
            double fitnessSum = 0;
            m_Statistics_BestValuePerGeneration = Double.MaxValue;

            for( int i = 0; i < m_thisGeneration.Count; i++ )
            {
                double thisFitness = m_thisGeneration[i].Fitness;
                fitnessSum += thisFitness;

                if( thisFitness < m_Statistics_BestValuePerGeneration )
                {
                    m_Statistics_BestPerGeneration = m_thisGeneration[i];
                    m_Statistics_BestValuePerGeneration = thisFitness;
                }
            }

            m_Statistics_MeanPerGeneration = fitnessSum / m_thisGeneration.Count;

            // Se escribe en pantalla los datos actualizados correspondientes...
            Console.SetCursorPosition( m_Cursor_GenerationPos[0], m_Cursor_GenerationPos[1] );
            Console.Write( generation );
            
            Console.SetCursorPosition( m_Cursor_BestPerGenerationPos[0], m_Cursor_BestPerGenerationPos[1] );
            Console.Write( m_Statistics_BestValuePerGeneration + "         " );

            Console.SetCursorPosition( m_Cursor_MeanPerGenerationPos[0], m_Cursor_MeanPerGenerationPos[1] );
            Console.Write( m_Statistics_MeanPerGeneration + "         " );

            // Se guardan los datos de las variables asociadas a la generaci�n del gr�fico de convergencia...
            if( m_Graph_BestPerGenerationDensity == 1 || ( generation % m_Graph_BestPerGenerationDensity ) == 0 )            
                m_Graph_BestPerGeneration[generation] = m_Statistics_BestValuePerGeneration;

            if( m_Graph_MeanPerGenerationDensity == 1 || ( generation % m_Graph_MeanPerGenerationDensity ) == 0 )
                m_Graph_MeanPerGeneration[generation] = m_Statistics_MeanPerGeneration;            

            // Por �ltimo, se actualizan variables rezagadas de los grupos anteriores...
            if( m_Statistics_BestSolutionValue > m_Statistics_BestValuePerGeneration )
            {
                m_Statistics_BestSolutionValue = m_Statistics_BestValuePerGeneration;
                m_Statistics_BestSolutionInfo = m_Statistics_BestPerGeneration.Evaluation;
                m_Graph_BestSolutions[generation] = m_Statistics_BestSolutionValue;

                Console.SetCursorPosition( m_Cursor_BestSolutionPos[0], m_Cursor_BestSolutionPos[1] );
                Console.Write( m_Statistics_BestSolutionValue + "         " );

                // Esto incluye la actualizaci�n del archivo que tiene informaci�n de las mejores soluciones.
                if( m_SolutionsWriter != null )
                    m_SolutionsWriter.WriteLine( GenerateSolutionDetailsString( generation, m_Statistics_BestPerGeneration ) );
            }
        }

        /* M�todo 'Go'
         *  Ejecuta la instancia actual del GA, en donde la informaci�n de la mejor soluci�n encontrada, no ser�
         * almacenada directamente.
         */
        public void Go()
        {
            Evaluation dummy;
            Go( out dummy );
        }

        /* M�todo 'Go'
         *  Ejecuta la instancia actual del GA, en donde se puede especificar la instancia del objeto de
         * evaluaci�n donde se guardar� la informaci�n de la mejor soluci�n.
         * 
         * Par�metros:
         * - 'Evaluation bestEvaluation': Referencia de salida al objeto que guardar� la informaci�n de
         * evaluaci�n del mejor individuo encontrado.
         */
        public void Go( out Evaluation bestEvaluation )
        {
            bestEvaluation = null;
            
            // Chequeos previos...
            if( m_initialPoblationGenerator == null || m_selectionMethod == null )
            {
                Console.WriteLine( "Algunos operadores necesarios para la ejecuci�n del GA no estan definidos." );
                return;
            }

            // Inicializaci�n general, se parte por carpetas y archivos de salida...
            CheckOuputFolder();
            InitLogs();

            LogLine( "LOG DE EJECUCION DE GA:\r\n" );
            LogLine( GenerateParameterInfoString() + "\r\n" );            

            DateTime start = DateTime.Now;
            LogLine( "({0}) Comienzo de ejecuci�n del algoritmo.", start );

            // Se genera la poblaci�n inicial...
            DateTime initMarker = DateTime.Now;
            LogLine( "({0}) Comienza la generaci�n de la poblaci�n inicial.", initMarker );

            m_thisGeneration = InitialPoblationGenerator( m_populationSize );

            DateTime endMarker = DateTime.Now;
            LogLine( "({0}) Termina la generaci�n de la poblaci�n inicial. Demora: {1}", endMarker, endMarker - initMarker );
            LogLine( "({0}) Se inicia el avance de generaciones.", DateTime.Now );
            Console.WriteLine( "Presionar la tecla \"q\" para detener.\r\n" );

            //  Se inicializan los archivos de estadisticas, incluyendo actualizaci�n de archivos con la primera
            // poblaci�n...
            InitStatistics();
            UpdateGenerationStatistics( 0 );

            int lastGeneration = m_generationMax;
            bool forceStopped = false;

            // Se ejecuta el bucle principal del GA...
            for( int i = 1; i <= m_generationMax; i++ )
            { 
                //  Se genera la siguiente generaci�n (incluye la ejecuci�n de los diversos operadores) y se
                // actualizan las distintas variables de ejecuci�n acorde a ella...
                CreateNextGeneration();
                UpdateGenerationStatistics( i );

                // En caso de que el usuario haya detenido la ejecuci�n manualmente, se finaliza el proceso...
                if( Console.KeyAvailable && Console.ReadKey( true ).KeyChar == 'q' )
                {
                    Console.SetCursorPosition( m_Cursor_EndProcess[0], m_Cursor_EndProcess[1] );
                    Console.WriteLine();
                    LogLine( "({0}) Se detiene el proceso por el usuario, en la generaci�n {1}.", DateTime.Now, i );
                    
                    lastGeneration = i;
                    forceStopped = true;

                    break;
                }                
            }

            DateTime end = DateTime.Now;
            TimeSpan time = end - start;

            // Se actualizan distintos valores e imprimen valores en pantalla y archivos, post-ejecuci�n del GA...
            bestEvaluation = m_Statistics_BestSolutionInfo;
            bestEvaluation.Time = time;

            if( !forceStopped )
            {
                Console.SetCursorPosition( m_Cursor_EndProcess[0], m_Cursor_EndProcess[1] );
                Console.WriteLine();
            }            

            LogLine( "({0}) Fin de ejecuci�n del algoritmo. Demora total: {1}", end, time );

            if( m_InfoWriter != null )
                m_InfoWriter.Write( "\r\nValor de mejor soluci�n: {0}", m_Statistics_BestSolutionValue );            

            // Se cierran los archivos con informaci�n de la ejecuci�n...
            CloseLogs();
             
            // Finalmente se genera el gr�fico de convergencia...
            LogLine( "({0}) Iniciando generaci�n de gr�fico de convergencia.", DateTime.Now );
            GenerateGraph( lastGeneration );
            LogLine( "({0}) Generaci�n de gr�fico de convergencia completada.", DateTime.Now );
            LogLine( "({0}) Fin de ejecuci�n de algoritmo.", DateTime.Now );
        }

        /* M�todo 'GenerateGraph'
         *  Genera un gr�fico de convergencia del GA en base a los valores guardados en las variables
         * estad�sticas del objeto, usando la librer�a dotnetCHARTING.
         * 
         * Par�metros:
         * - 'int lastGeneration': N�mero de la �ltima generaci�n ejecutada por el GA.
         */
        private void GenerateGraph( int lastGeneration )
        {
            // Se crea el objeto que genera el gr�tifo, y se configuran sus par�metros generales...
            Chart toGen = new Chart();

            toGen.Type = ChartType.Combo;
            toGen.Size = new Size( 800, 600 );

            toGen.XAxis.ScaleRange.ValueHigh = lastGeneration;
            toGen.XAxis.Label = new Label( "Generaci�n" );
            toGen.YAxis.ScaleRange.ValueLow = m_Statistics_BestSolutionValue - 100.0;
            toGen.YAxis.Interval = 100;
            toGen.YAxis.Label = new Label( "Evaluaci�n / Fitness" );

            toGen.TempDirectory = m_OuputFolder;
            toGen.Title = "Ejecuci�n de DARPTW GA";            
            toGen.FileName = "ga-chart";
            toGen.DefaultSeries.Type = SeriesType.Line;
            toGen.TitleBox.Position = TitleBoxPosition.FullWithLegend;
            toGen.DefaultElement.Marker.Visible = false;
            toGen.YAxis.Markers.Add( new AxisMarker( "Mejor evaluaci�n", new Line( Color.Red, 2 ), m_Statistics_BestSolutionValue ) );
                        
            // Se generan las series que se mostrar�n en el gr�fico...
            SeriesCollection SC = new SeriesCollection();

            // Serie 1: mejores soluciones por generaci�n...
            Series bestPerGenerationSerie = new Series();
            bestPerGenerationSerie.Name = "Mejores por generaci�n";
            bestPerGenerationSerie.DefaultElement.Color = Color.FromArgb( 0, 150, 0 );

            foreach( int generation in m_Graph_BestPerGeneration.Keys )
            {
                Element e = new Element();

                e.YValue = m_Graph_BestPerGeneration[generation];
                e.XValue = generation;

                bestPerGenerationSerie.Elements.Add( e );
            }

            // Serie 2: promedio de soluciones por generaci�n...
            Series meanPerGenerationSerie = new Series();
            meanPerGenerationSerie.Name = "Medias por generaci�n";
            meanPerGenerationSerie.DefaultElement.Color = Color.FromArgb( 150, 0, 0 );

            foreach( int generation in m_Graph_MeanPerGeneration.Keys )
            {
                Element e = new Element();

                e.YValue = m_Graph_MeanPerGeneration[generation];
                e.XValue = generation;

                meanPerGenerationSerie.Elements.Add( e );
            }

            // Serie 3: mejores soluciones...
            Series bestSolutionsSerie = new Series();
            bestSolutionsSerie.Name = "Mejores en ejecuci�n";
            bestSolutionsSerie.DefaultElement.Color = Color.FromArgb( 255, 128, 0 );

            foreach( int generation in m_Graph_BestSolutions.Keys )
            {
                Element e = new Element();

                e.YValue = m_Graph_BestSolutions[generation];
                e.XValue = generation;

                bestSolutionsSerie.Elements.Add( e );
            }

            SC.Add( bestPerGenerationSerie );
            SC.Add( meanPerGenerationSerie );
            SC.Add( bestSolutionsSerie );
            
            toGen.SeriesCollection.Add( SC );

            // Por �ltimo, se genera la imagen...
            toGen.ImageFormat = dotnetCHARTING.WinForms.ImageFormat.Png;
            toGen.FileManager.SaveImage();
        }

        /* M�todo 'CreateNextGeneration'
         *  Genera una futura generaci�n en base a los individuos de la generaci�n actual.
         */
        private void CreateNextGeneration()
        {
            List<Genome> intermediateGeneration = new List<Genome>( m_populationSize );

            // Etapa 1: Poblaci�n intermedia via selecci�n...
            for( int i = 0; i < m_populationSize; i++ )
                intermediateGeneration.Add( SelectionMethod( m_thisGeneration ) );

            // Etapa 2: Poblaci�n final via crossover y mutaci�n...
            List<Genome> nextGeneration = new List<Genome>( m_populationSize );

            for( int i = 0; i < m_populationSize; i += 2 )
            {
                Genome[] parents = new Genome[2];

                //  Se seleccionan dos individuos de la poblaci�n aleatoriamente, para ser usados como padres
                // de los nuevos individuos...
                parents[0] = intermediateGeneration[RandomTool.GetInt( intermediateGeneration.Count - 1 )];
                parents[1] = intermediateGeneration[RandomTool.GetInt( intermediateGeneration.Count - 1 )];

                Genome[] childs;

                //  Si se cumple con la probabilidad de recombinaci�n, se usa el operador, si no se pasan
                // directamente los padres a la pr�xima generaci�n...
                if( m_crossoverMethod != null && m_crossoverRate > RandomTool.GetDouble() )
                    childs = CrossoverMethod( parents );
                else
                    childs = parents;

                // Los resultantes son sometidos a mutaci�n por clusters...
                if( m_clusterMutationRate > 0 && m_clusterMutationMethod != null )
                {
                    ClusterMutationMethod( childs[0], m_clusterMutationRate );
                    ClusterMutationMethod( childs[1], m_clusterMutationRate );
                }

                // Los resultantes son sometidos a mutaci�n por rutas...
                if( m_routeMutationRate > 0 && m_routeMutationMethod != null )
                {
                    RouteMutationMethod( childs[0], m_routeMutationRate );                    
                     RouteMutationMethod( childs[1], m_routeMutationRate );
                }                

                // Finalmente se insertan los individuos en la nueva poblaci�n...
                nextGeneration.Add( childs[0] );
                nextGeneration.Add( childs[1] );
            }

            // Con la nueva poblaci�n construida, se reemplaza la misma por la anterior.
            m_thisGeneration = nextGeneration;
        }
    }
}
