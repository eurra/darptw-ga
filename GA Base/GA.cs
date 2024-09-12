/* GA.cs
 * Última modificación: 25/04/2008
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
     *  Define un tipo de método en el que se implementará un generador de póblación inicial, retornando
     * una lista de cromosomas que representa dicha población.
     * 
     * Parámetros:
     * - 'int pobSize': Tamaño de la población a generar.
     */
    public delegate List<Genome> InitialPoblationLauncher( int pobSize );

    /* Delegado 'SelectionLauncher'
     *  Define un tipo de método en el que se implementará un operador de selección, retornando un
     * individual seleccionado de la población correspondiente.
     * 
     * Parámetros:
     * - 'List<Genome> poblation': Población en base a la cual se realizará la selección.
     */
    public delegate Genome SelectionLauncher( List<Genome> poblation );

    /* Delegado 'CrossoverLauncher'
     *  Define un tipo de método en el que se implementará un operador de recombinación, retornando un
     * arreglo de cromosomas (normalmente de largo 2) resultante de la recombinación.
     * 
     * Parámetros:
     * - 'Genome[] parents': Arreglo que contiene a los cromosomas padres usados en la recombinación
     * (normalmente de largo 2).
     */
    public delegate Genome[] CrossoverLauncher( Genome[] parents );

    /* Delegado 'MutationLauncher'
     *  Define un tipo de método en el que se implementará un operador de mutación.
     * 
     * Parámetros:
     * - 'Genome toMutate': Cromosoma que será sometido a la recombinación.
     * - 'double chance': Probabilidad de mutación usada.
     */
    public delegate void MutationLauncher( Genome toMutate, double chance );

    /* Clase 'GA'
     *  Implementa un objeto que permite realizar la ejecución del algoritmo genetico en base a distintos
     * parámetros configurados, relacionados con los modelos implementados y los parámetros del problema.
     * También guarda atributos propios del algoritmo genético.
     * 
     * Atributos:
     * - 'double m_routeMutationRate': Ratio de mutación en rutas configurado en el GA.
     * - 'double m_clusterMutationRate': Ratio de mutación en clusters configurado en el GA.
     * - 'double m_crossoverRate': Ratio de recombinación configurado en el GA.
     * - 'double m_populationSize': Tamaño de la población configurada en el GA.
     * - 'double m_generationMax': Número de generaciones máximas configurada en el GA.
     * - 'List<Genome> m_thisGeneration': Lista de cromosomas que representa la población actual que maneja
     * el GA.
     * - 'InitialPoblationLauncher m_initialPoblationGenerator': Delegado que referencia al método usado
     * para generar la población inicial.
     * - 'SelectionLauncher m_selectionMethod': Delegado que referencia al método usado como operador de 
     * selección.
     * - 'CrossoverLauncher m_crossoverMethod': Delegado que referencia al método usado como operador de
     * recombinación;
     * - 'MutationLauncher m_routeMutationMethod': Delegado que referencia al método usado como operador
     * de mutación en rutas;
     * - 'MutationLauncher m_clusterMutationMethod': Delegado que referencia al método usado como operador
     * de mutación en clústers;
     * - 'string m_OuputFolder': Ruta del directorio de salida que se usará para entregar los resultados
     * que arroje el GA.
     * - 'StreamWriter m_InfoWriter': Buffer de salida para el archivo que entregará información respecto
     * a la ejecución del GA.
     * - 'StreamWriter m_SolutionsWriter': Buffer de salida para el archivo que entregará información de
     * las soluciones.
     * - 'Dictionary<int, double> m_Graph_BestPerGeneration': Tabla que almacena la información para la
     * generación del gráfico de convergencia, respecto a los mejores resultados por generación.
     * - 'Dictionary<int, double> m_Graph_MeanPerGeneration': Tabla que almacena la información para la
     * generación del gráfico de convergencia, respecto a los resultados promedio por generación.
     * - 'Dictionary<int, double> m_Graph_BestSolutions': Tabla que almacena la información para la
     * generación del gráfico de convergencia, respecto a los mejores resultados en ejecución.
     * - 'int m_Graph_BestPerGenerationDensity': Valor que indica cada cuantas generaciones se
     * graficará un punto en la linea de convergencia de los mejores resultados por generación.
     * - 'int m_Graph_MeanPerGenerationDensity': Valor que indica cada cuantas generaciones se
     * graficará un punto en la linea de convergencia de los resultados promedio por generación.
     * - 'int[] m_Cursor_GenerationPos': Arreglo de enteros, usado como un par, para guardar la información
     * de la posición del cursor donde se escribirá el nro. de generación mientras se ejecuta el GA.
     * - 'int[] m_Cursor_BestPerGenerationPos': Arreglo de enteros, usado como un par, para guardar la
     * información de la posición del cursor donde se escribirá el valor del mejor individuo por generación
     * mientras se ejecuta el GA.
     * - 'int[] m_Cursor_MeanPerGenerationPos': Arreglo de enteros, usado como un par, para guardar la
     * información de la posición del cursor donde se escribirá el valor promedio de los individuos por
     * generación mientras se ejecuta el GA.
     * - 'int[] m_Cursor_BestSolutionPos': Arreglo de enteros, usado como un par, para guardar la
     * información de la posición del cursor donde se escribirá el valor del mejor individuo encontrado
     * hasta el momento mientras se ejecuta el GA.
     * - 'm_Cursor_EndProcess': Arreglo de enteros, usado como un par, para guardar la información de la
     * posición del cursor donde se escribirá el mensaje de término cuando el usuario finalize manualmente
     * la ejecución del GA.
     * - 'double m_Statistics_BestValuePerGeneration': Variable que guarda el valor del mejor individuo
     * por generación.
     * - 'Genome m_Statistics_BestPerGeneration': Variable que guarda la referencia del individuo con el
     * mejor valor por generación.
     * - 'double m_Statistics_MeanPerGeneration': Variable que guarda el valor promedio de los individuos
     * por generación.
     * - 'double m_Statistics_BestSolutionValue': Variable que guarda el valor del mejor individuo encontrado
     * hasta el momento en la ejecución del GA.
     * - 'Evaluation m_Statistics_BestSolutionInfo': Variable que guarda la referencia al objeto que tiene la
     * información de evaluación del mejor individuo encontrado hasta el momento en la ejecución del GA.
     */
    public class GA
    {
        private double m_routeMutationRate = 0.1;
        private double m_clusterMutationRate = 0.01;
        private double m_crossoverRate = 0.7;
        private int m_populationSize = 100;
        private int m_generationMax = 1000;

        private List<Genome> m_thisGeneration;

        [ParameterTag( "Tamaño de la población", "pobsize", 2, Int32.MaxValue, 100 )]
        public int PopulationSize
        {
            get { return m_populationSize; }
            set { m_populationSize = value; }
        }

        [ParameterTag( "Número máximo de generaciones", "gencount", 1, Int32.MaxValue, 1000 )]
        public int Generations
        {
            get { return m_generationMax; }
            set { m_generationMax = value; }
        }

        [ParameterTag( "Probabilidad de recombinación", "crossrate", 0.0, 1.0, 0.7 )]
        public double CrossoverRate
        {
            get { return m_crossoverRate; }
            set { m_crossoverRate = value; }
        }

        [ParameterTag( "Probabilidad de mutación en rutas", "mut2rate", 0.0, 1.0, 0.1 )]
        public double RouteMutationRate
        {
            get { return m_routeMutationRate; }
            set { m_routeMutationRate = value; }
        }

        [ParameterTag( "Probabilidad de mutación en clusters", "mut1rate", 0.0, 1.0, 0.01 )]
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
         *  Instancia un nuevo objeto GA para ser usado con sus parámetros por defecto.
         */
        public GA()
        {
        }

        /* Método 'GenerateParameterInfoString'
         *  Genera un string con información relevante a los parámetros usados en la ejecución del GA,
         * además de sus operadores y las caracteristicas de los datos de entrada asociados al problema.
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

            ret += String.Format( "Operador recombinación: {0}\r\n", opNames[0] != null ? opNames[0] : "NO DEFINIDO" );
            ret += String.Format( "Operador mutación en rutas: {0}\r\n", opNames[1] != null ? opNames[1] : "NO DEFINIDO" );
            ret += String.Format( "Operador mutación en clusters: {0}\r\n", opNames[2] != null ? opNames[2] : "NO DEFINIDO" );
            ret += String.Format( "Generador de población inicial: {0}\r\n", opNames[3] != null ? opNames[3] : "NO DEFINIDO" );
            ret += String.Format( "Operador de selección: {0}\r\n", opNames[4] != null ? opNames[4] : "NO DEFINIDO" );

            ret += "\r\n4) CARACTERISTICAS DE LOS DATOS DE ENTRADA\r\n";

            ret += String.Format( "Cantidad de clientes: {0}\r\n", GlobalParams.ClientNumber );
            ret += String.Format( "Cantidad máxima de vehículos: {0}\r\n", GlobalParams.VehiclesMaxNumber );
            ret += String.Format( "Carga máxima de vehículos: {0}\r\n", GlobalParams.VehicleMaxLoad );
            ret += String.Format( "Tiempo máximo de transporte de pasajeros: {0}", GlobalParams.MRT );

            return ret;
        }

        /* Método 'GenerateSolutionDetailsString'
         *  Genera un string con información relevante a una solución, que puede ser la mejor de una
         * generación, lo que incluye su evaluación, rutas, máscaras y tiempos calculados.
         * 
         * Parámetros:
         * - 'int genNumber': Número de generación de la evaluación.
         * - 'Genome solution': Cromosoma del que se sacará la información.
         */
        private string GenerateSolutionDetailsString( int genNumber, Genome solution )
        {
            string ret = "***************************\r\n";

            ret += String.Format( "\r\nGENERACION {0} de {1}\r\n", genNumber, m_generationMax );
            ret += "Detalles de solución:\r\n\r\n";

            ret += String.Format( "Evaluación / Calidad: {0}\r\n\r\n", solution.Fitness );

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

        /* Método 'CheckOuputFolder'
         *  Realiza un checkeo previo de la carpeta configurada para las salidas del GA. En caso de que
         * el directorio no exista, se usará uno por defecto en la carpeta de la aplicación.
         */
        private void CheckOuputFolder()
        {
            if( m_OuputFolder == null || !Directory.Exists( m_OuputFolder ) )
            {
                m_OuputFolder = "GAResults/" + DateTime.Now.ToFileTime().ToString();
                Directory.CreateDirectory( m_OuputFolder );
            }
        }

        /* Método 'InitLogs'
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

        /* Método 'CloseLogs'
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

        /* Método 'LogLine'
         *  Escribe un texto con salto de linea en el archivo de salida de información activo del GA.
         * 
         * Parámetros:
         * - 'string input': String con formato que se escribirá en el archivo.
         * - 'object[] args': lista de entradas que se usan como parámetros del string formateado que se
         * escribirá en el archivo.
         */
        private void LogLine( string input, params object[] args )
        {
            LogLine( true, input, args );
        }

        /* Método 'LogLine'
         *  Escribe un texto con salto de linea en consola, con la opción que también sea impresa en el archivo
         * de salida de información activo del GA.
         * 
         * Parámetros:
         * - 'bool toFile': Booleano que indica si la salida también se imprimirá en el archivo (true).
         * - 'string input': String con formato que se escribirá en el archivo.
         * - 'object[] args': lista de entradas que se usan como parámetros del string formateado que se
         * imprimirá.
         */
        private void LogLine( bool toFile, string input, params object[] args )
        {
            Console.WriteLine( input, args );

            if( toFile && m_InfoWriter != null )
                m_InfoWriter.WriteLine( input, args );
        }

        /* Método 'Log'
         *  Escribe un texto sin salto de linea en el archivo de salida de información activo del GA.
         * 
         * Parámetros:
         * - 'string input': String con formato que se escribirá en el archivo.
         * - 'object[] args': lista de entradas que se usan como parámetros del string formateado que se
         * escribirá en el archivo.
         */
        private void Log( string input, params object[] args )
        {
            Log( true, input, args );
        }

        /* Método 'Log'
         *  Escribe un texto sin salto de linea en consola, con la opción que también sea impresa en el archivo
         * de salida de información activo del GA.
         * 
         * Parámetros:
         * - 'bool toFile': Booleano que indica si la salida también se imprimirá en el archivo (true).
         * - 'string input': String con formato que se escribirá en el archivo.
         * - 'object[] args': lista de entradas que se usan como parámetros del string formateado que se
         * imprimirá.
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

        /* Método 'InitStatistics'
         *  Inicializa los valores de las distintas variables usadas para manejar estadísticas mientras se
         * ejecuta el GA, mostrando avisos por consola correspondientemente.
         */
        private void InitStatistics()
        {
            Console.Write( "Generación: " );
            m_Cursor_GenerationPos = new int[] { Console.CursorLeft, Console.CursorTop };

            Console.Write( "\r\nMejor Evaluación Actual: " );
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

        /* Método 'UpdateGenerationStatistics'
         *  Actualiza las variables que almacena información estadistica de la ejecución del GA, acorde a la
         * situación de la generación actual. Adicionalmente, escribe la información actualizada en consola y
         * actualiza los datos de las variables asociadas al gráfico de convergencia.
         * 
         * Parámetros:
         * - 'int generation': Número de la generación actual.
         */
        private void UpdateGenerationStatistics( int generation )
        {
            // Primero se actualizan los valores de las variables estadísticas genéricas...
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

            // Se guardan los datos de las variables asociadas a la generación del gráfico de convergencia...
            if( m_Graph_BestPerGenerationDensity == 1 || ( generation % m_Graph_BestPerGenerationDensity ) == 0 )            
                m_Graph_BestPerGeneration[generation] = m_Statistics_BestValuePerGeneration;

            if( m_Graph_MeanPerGenerationDensity == 1 || ( generation % m_Graph_MeanPerGenerationDensity ) == 0 )
                m_Graph_MeanPerGeneration[generation] = m_Statistics_MeanPerGeneration;            

            // Por último, se actualizan variables rezagadas de los grupos anteriores...
            if( m_Statistics_BestSolutionValue > m_Statistics_BestValuePerGeneration )
            {
                m_Statistics_BestSolutionValue = m_Statistics_BestValuePerGeneration;
                m_Statistics_BestSolutionInfo = m_Statistics_BestPerGeneration.Evaluation;
                m_Graph_BestSolutions[generation] = m_Statistics_BestSolutionValue;

                Console.SetCursorPosition( m_Cursor_BestSolutionPos[0], m_Cursor_BestSolutionPos[1] );
                Console.Write( m_Statistics_BestSolutionValue + "         " );

                // Esto incluye la actualización del archivo que tiene información de las mejores soluciones.
                if( m_SolutionsWriter != null )
                    m_SolutionsWriter.WriteLine( GenerateSolutionDetailsString( generation, m_Statistics_BestPerGeneration ) );
            }
        }

        /* Método 'Go'
         *  Ejecuta la instancia actual del GA, en donde la información de la mejor solución encontrada, no será
         * almacenada directamente.
         */
        public void Go()
        {
            Evaluation dummy;
            Go( out dummy );
        }

        /* Método 'Go'
         *  Ejecuta la instancia actual del GA, en donde se puede especificar la instancia del objeto de
         * evaluación donde se guardará la información de la mejor solución.
         * 
         * Parámetros:
         * - 'Evaluation bestEvaluation': Referencia de salida al objeto que guardará la información de
         * evaluación del mejor individuo encontrado.
         */
        public void Go( out Evaluation bestEvaluation )
        {
            bestEvaluation = null;
            
            // Chequeos previos...
            if( m_initialPoblationGenerator == null || m_selectionMethod == null )
            {
                Console.WriteLine( "Algunos operadores necesarios para la ejecución del GA no estan definidos." );
                return;
            }

            // Inicialización general, se parte por carpetas y archivos de salida...
            CheckOuputFolder();
            InitLogs();

            LogLine( "LOG DE EJECUCION DE GA:\r\n" );
            LogLine( GenerateParameterInfoString() + "\r\n" );            

            DateTime start = DateTime.Now;
            LogLine( "({0}) Comienzo de ejecución del algoritmo.", start );

            // Se genera la población inicial...
            DateTime initMarker = DateTime.Now;
            LogLine( "({0}) Comienza la generación de la población inicial.", initMarker );

            m_thisGeneration = InitialPoblationGenerator( m_populationSize );

            DateTime endMarker = DateTime.Now;
            LogLine( "({0}) Termina la generación de la población inicial. Demora: {1}", endMarker, endMarker - initMarker );
            LogLine( "({0}) Se inicia el avance de generaciones.", DateTime.Now );
            Console.WriteLine( "Presionar la tecla \"q\" para detener.\r\n" );

            //  Se inicializan los archivos de estadisticas, incluyendo actualización de archivos con la primera
            // población...
            InitStatistics();
            UpdateGenerationStatistics( 0 );

            int lastGeneration = m_generationMax;
            bool forceStopped = false;

            // Se ejecuta el bucle principal del GA...
            for( int i = 1; i <= m_generationMax; i++ )
            { 
                //  Se genera la siguiente generación (incluye la ejecución de los diversos operadores) y se
                // actualizan las distintas variables de ejecución acorde a ella...
                CreateNextGeneration();
                UpdateGenerationStatistics( i );

                // En caso de que el usuario haya detenido la ejecución manualmente, se finaliza el proceso...
                if( Console.KeyAvailable && Console.ReadKey( true ).KeyChar == 'q' )
                {
                    Console.SetCursorPosition( m_Cursor_EndProcess[0], m_Cursor_EndProcess[1] );
                    Console.WriteLine();
                    LogLine( "({0}) Se detiene el proceso por el usuario, en la generación {1}.", DateTime.Now, i );
                    
                    lastGeneration = i;
                    forceStopped = true;

                    break;
                }                
            }

            DateTime end = DateTime.Now;
            TimeSpan time = end - start;

            // Se actualizan distintos valores e imprimen valores en pantalla y archivos, post-ejecución del GA...
            bestEvaluation = m_Statistics_BestSolutionInfo;
            bestEvaluation.Time = time;

            if( !forceStopped )
            {
                Console.SetCursorPosition( m_Cursor_EndProcess[0], m_Cursor_EndProcess[1] );
                Console.WriteLine();
            }            

            LogLine( "({0}) Fin de ejecución del algoritmo. Demora total: {1}", end, time );

            if( m_InfoWriter != null )
                m_InfoWriter.Write( "\r\nValor de mejor solución: {0}", m_Statistics_BestSolutionValue );            

            // Se cierran los archivos con información de la ejecución...
            CloseLogs();
             
            // Finalmente se genera el gráfico de convergencia...
            LogLine( "({0}) Iniciando generación de gráfico de convergencia.", DateTime.Now );
            GenerateGraph( lastGeneration );
            LogLine( "({0}) Generación de gráfico de convergencia completada.", DateTime.Now );
            LogLine( "({0}) Fin de ejecución de algoritmo.", DateTime.Now );
        }

        /* Método 'GenerateGraph'
         *  Genera un gráfico de convergencia del GA en base a los valores guardados en las variables
         * estadísticas del objeto, usando la librería dotnetCHARTING.
         * 
         * Parámetros:
         * - 'int lastGeneration': Número de la última generación ejecutada por el GA.
         */
        private void GenerateGraph( int lastGeneration )
        {
            // Se crea el objeto que genera el grátifo, y se configuran sus parámetros generales...
            Chart toGen = new Chart();

            toGen.Type = ChartType.Combo;
            toGen.Size = new Size( 800, 600 );

            toGen.XAxis.ScaleRange.ValueHigh = lastGeneration;
            toGen.XAxis.Label = new Label( "Generación" );
            toGen.YAxis.ScaleRange.ValueLow = m_Statistics_BestSolutionValue - 100.0;
            toGen.YAxis.Interval = 100;
            toGen.YAxis.Label = new Label( "Evaluación / Fitness" );

            toGen.TempDirectory = m_OuputFolder;
            toGen.Title = "Ejecución de DARPTW GA";            
            toGen.FileName = "ga-chart";
            toGen.DefaultSeries.Type = SeriesType.Line;
            toGen.TitleBox.Position = TitleBoxPosition.FullWithLegend;
            toGen.DefaultElement.Marker.Visible = false;
            toGen.YAxis.Markers.Add( new AxisMarker( "Mejor evaluación", new Line( Color.Red, 2 ), m_Statistics_BestSolutionValue ) );
                        
            // Se generan las series que se mostrarán en el gráfico...
            SeriesCollection SC = new SeriesCollection();

            // Serie 1: mejores soluciones por generación...
            Series bestPerGenerationSerie = new Series();
            bestPerGenerationSerie.Name = "Mejores por generación";
            bestPerGenerationSerie.DefaultElement.Color = Color.FromArgb( 0, 150, 0 );

            foreach( int generation in m_Graph_BestPerGeneration.Keys )
            {
                Element e = new Element();

                e.YValue = m_Graph_BestPerGeneration[generation];
                e.XValue = generation;

                bestPerGenerationSerie.Elements.Add( e );
            }

            // Serie 2: promedio de soluciones por generación...
            Series meanPerGenerationSerie = new Series();
            meanPerGenerationSerie.Name = "Medias por generación";
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
            bestSolutionsSerie.Name = "Mejores en ejecución";
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

            // Por último, se genera la imagen...
            toGen.ImageFormat = dotnetCHARTING.WinForms.ImageFormat.Png;
            toGen.FileManager.SaveImage();
        }

        /* Método 'CreateNextGeneration'
         *  Genera una futura generación en base a los individuos de la generación actual.
         */
        private void CreateNextGeneration()
        {
            List<Genome> intermediateGeneration = new List<Genome>( m_populationSize );

            // Etapa 1: Población intermedia via selección...
            for( int i = 0; i < m_populationSize; i++ )
                intermediateGeneration.Add( SelectionMethod( m_thisGeneration ) );

            // Etapa 2: Población final via crossover y mutación...
            List<Genome> nextGeneration = new List<Genome>( m_populationSize );

            for( int i = 0; i < m_populationSize; i += 2 )
            {
                Genome[] parents = new Genome[2];

                //  Se seleccionan dos individuos de la población aleatoriamente, para ser usados como padres
                // de los nuevos individuos...
                parents[0] = intermediateGeneration[RandomTool.GetInt( intermediateGeneration.Count - 1 )];
                parents[1] = intermediateGeneration[RandomTool.GetInt( intermediateGeneration.Count - 1 )];

                Genome[] childs;

                //  Si se cumple con la probabilidad de recombinación, se usa el operador, si no se pasan
                // directamente los padres a la próxima generación...
                if( m_crossoverMethod != null && m_crossoverRate > RandomTool.GetDouble() )
                    childs = CrossoverMethod( parents );
                else
                    childs = parents;

                // Los resultantes son sometidos a mutación por clusters...
                if( m_clusterMutationRate > 0 && m_clusterMutationMethod != null )
                {
                    ClusterMutationMethod( childs[0], m_clusterMutationRate );
                    ClusterMutationMethod( childs[1], m_clusterMutationRate );
                }

                // Los resultantes son sometidos a mutación por rutas...
                if( m_routeMutationRate > 0 && m_routeMutationMethod != null )
                {
                    RouteMutationMethod( childs[0], m_routeMutationRate );                    
                     RouteMutationMethod( childs[1], m_routeMutationRate );
                }                

                // Finalmente se insertan los individuos en la nueva población...
                nextGeneration.Add( childs[0] );
                nextGeneration.Add( childs[1] );
            }

            // Con la nueva población construida, se reemplaza la misma por la anterior.
            m_thisGeneration = nextGeneration;
        }
    }
}
