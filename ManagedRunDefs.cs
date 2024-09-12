using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using dotnetCHARTING.WinForms;
using DARPTW_GA;
using DARPTW_GA.GA_Base;
using DARPTW_GA.Misc;
using DARPTW_GA.Framework;

namespace DARPTW_GA
{
    public class ManagedRunParameter
    {
        private ParameterEntry m_Entry;
        private List<double> m_Values;
        private int m_SelectedIndex;

        public ParameterEntry Entry { get { return m_Entry; } }
        public double ActualEntryValue { get { return m_Entry.Value; } }
        public int ValuesCount { get { return m_Values.Count; } }
        public int SelectedIndex { get { return m_SelectedIndex; } }

        public ManagedRunParameter( ParameterEntry entry )
        {
            m_Entry = entry;
            m_Values = new List<double>();
        }

        public void AddValue( double value )
        {
            if( !m_Values.Contains( value ) )
                m_Values.Add( value );
        }

        public double GetValue( int index )
        {
            return m_Values[index];
        }

        public void ApplyValueToEntry( double value )
        {
            int index;

            if( ( index = m_Values.IndexOf( value ) ) < 0 )
                return;

            ApplyValueToEntry( index );
        }

        public void ApplyValueToEntry( int index )
        {
            m_Entry.Value = m_Values[index];
            m_SelectedIndex = index;
        }
    }

    public class ManagedRunLauncher
    {
        private List<int> m_Models;
        private List<string> m_Instances;
        private List<ManagedRunParameter> m_Parameters;
        private string m_BaseOuputFolder;
        private int[][] m_ParameterConfigurations;

        private int m_Repetitions = 1;
        private int m_ActiveParamsConfig;

        public int Repetitions
        {
            get { return m_Repetitions; }

            set
            {
                if( value < 1 )
                    value = 1;

                m_Repetitions = value;
            }
        }

        public string BaseOuputFolder
        {
            get { return m_BaseOuputFolder; }
            set { m_BaseOuputFolder = value; }
        }

        public ManagedRunLauncher()
        {
            m_Models = new List<int>();
            m_Instances = new List<string>();
            m_Parameters = new List<ManagedRunParameter>();
        }

        public void AddModel( int model )
        {
            if( !m_Models.Contains( model ) )
                m_Models.Add( model );
        }

        public void AddInstance( string instance )
        {
            if( !m_Instances.Contains( instance ) && File.Exists( instance ) )
                m_Instances.Add( instance );
        }

        private bool SearchForEntry( ParameterEntry entry, out ManagedRunParameter mParam )
        {
            mParam = null;

            for( int i = 0; i < m_Parameters.Count; i++ )
            {
                if( m_Parameters[i].Entry == entry )
                {
                    mParam = m_Parameters[i];
                    return true;
                }
            }

            return false;
        }

        public void AddParameterValue( ParameterEntry entry, double value )
        {
            ManagedRunParameter mParam;

            if( !SearchForEntry( entry, out mParam ) )
            {
                mParam = new ManagedRunParameter( entry );
                m_Parameters.Add( mParam );
            }

            mParam.AddValue( value );
        }

        private void CheckOuputFolder()
        {
            if( m_BaseOuputFolder == null || !Directory.Exists( m_BaseOuputFolder ) )
            {
                m_BaseOuputFolder = "ManagedRuns/" + DateTime.Now.ToFileTime().ToString();
                Directory.CreateDirectory( m_BaseOuputFolder );
            }
        }

        private string ProcessActualExecFolder()
        {
            string buildFolder = m_BaseOuputFolder;
            buildFolder += "/" + Program.GetActiveModel().Name;
            buildFolder += "/" + Program.ActualDataFile;
            buildFolder += "/test" + ( m_ActiveParamsConfig + 1 );

            if( !Directory.Exists( buildFolder ) )
                Directory.CreateDirectory( buildFolder );

            return buildFolder;
        }

        private void GenerateTestInfo()
        {
            StreamWriter writer = File.CreateText( m_BaseOuputFolder + "/test-info.txt" );

            writer.WriteLine( "INFORMACION DE EJECUCION ADMINISTRADA\r\n" );

            writer.WriteLine( "I) MODELOS A UTILIZAR:" );

            for( int i = 0; i < m_Models.Count; i++ )
                writer.WriteLine( "* {0}", Program.GetModel(m_Models[i]).Name );

            writer.WriteLine( "\r\nII) DATOS DE ENTRADA A UTILIZAR:" );

            for( int i = 0; i < m_Instances.Count; i++ )
                writer.WriteLine( "* {0}", m_Instances[i] );

            writer.WriteLine( "\r\nIII) ESPECIFICACION DE EJECUCIONES:" );

            for( int i = 0; i < m_ParameterConfigurations.Length; i++ )
            {
                writer.WriteLine( "\r\nTest {0}:", i + 1 );
                int[] config = m_ParameterConfigurations[i];

                for( int j = 0; j < m_Parameters.Count; j++ )
                    writer.WriteLine( "* {0}: {1}", m_Parameters[j].Entry.Name, m_Parameters[j].GetValue( config[j] ) );
            }

            writer.Close();
        }

        public string GenerateActualParamsInfo()
        {
            string ret = "";

            for( int j = 0; j < m_Parameters.Count; j++ )
                ret += String.Format( "{0}: {1}\r\n", m_Parameters[j].Entry.Name, m_Parameters[j].Entry.Value );

            return ret;
        }

        private void GenerateParameterConfigurations()
        {
            int configCount = 1;

            for( int i = 0; i < m_Parameters.Count; i++ )
                configCount *= m_Parameters[i].ValuesCount;

            m_ParameterConfigurations = new int[configCount][];
            int configIndex = 0;

            SetParamValues( 0, new int[m_Parameters.Count], ref configIndex );
        }

        private void SetParamValues( int actualParamIndex, int[] tempValuesConfig, ref int configIndex )
        { 
            int valuesCount = m_Parameters[actualParamIndex].ValuesCount;

            for( int i = 0; i < valuesCount; i++ )
            {
                tempValuesConfig[actualParamIndex] = i;

                if( actualParamIndex == m_Parameters.Count - 1 )
                {
                    int[] configToSet = m_ParameterConfigurations[configIndex] = (int[])tempValuesConfig.Clone();
                    configIndex++;
                }
                else
                {
                    SetParamValues( actualParamIndex + 1, tempValuesConfig, ref configIndex );
                }
            }
        }

        private void Log( string input )
        {
            if( m_LogStream != null )
                m_LogStream.Write( input );
        }

        private void Log( string input, params object[] args )
        {
            if( m_LogStream != null )
                m_LogStream.Write( input, args );
        }

        private void LogLine( string input )
        {
            if( m_LogStream != null )
                m_LogStream.WriteLine( input );
        }

        private void LogLine( string input, params object[] args )
        {
            if( m_LogStream != null )
                m_LogStream.WriteLine( input, args );
        }

        private void InitLogger()
        {
            m_LogStream = File.CreateText( m_BaseOuputFolder + "/log.txt" );
            m_LogStream.AutoFlush = true;
        }

        private void FinishExecution()
        {
            if( m_LogStream != null )
                m_LogStream.Close();

            if( m_ExportableStream != null )
                m_ExportableStream.Close();

            m_ParameterConfigurations = null;
        }

        private StreamWriter m_LogStream;
        private StreamWriter m_ResultsStream;
        private StreamWriter m_ExportableStream;

        public void Go()
        {
            CheckOuputFolder();
            InitLogger();

            LogLine( "({0}) Inicio de ejecución administrada.", DateTime.Now );
            LogLine( "({0}) Configurando instancias de ejecución de los parámetros.", DateTime.Now );
            GenerateParameterConfigurations();

            LogLine( "({0}) Generando archivo de información de la ejecución.", DateTime.Now );
            GenerateTestInfo();

            LogLine( "({0}) Inicialización lista. Se comienza con la ejecución de los tests.", DateTime.Now );
            
            for( int i = 0; i < m_Models.Count; i++ )
            {
                Program.ModelSelected = m_Models[i];
                LogLine( "({0}) Cargado modelo: {1}.", DateTime.Now, Program.GetActiveModel().Name );

                for( int j = 0; j < m_Instances.Count; j++ )
                {
                    Program.LoadDataFile( m_Instances[j] );
                    LogLine( "({0}) Cargado set de datos: {1}.", DateTime.Now, Program.ActualDataFile );

                    string instanceFolder = m_BaseOuputFolder + "/" + Program.GetActiveModel().Name + "/" + Program.ActualDataFile;

                    if( !Directory.Exists( instanceFolder ) )
                        Directory.CreateDirectory( instanceFolder );

                    ManagedRunGraphSet graphSet = new ManagedRunGraphSet( m_Parameters, instanceFolder );

                    m_ResultsStream = File.CreateText( instanceFolder + "/results-info.txt" );
                    m_ResultsStream.AutoFlush = true;
                    m_ResultsStream.WriteLine( "************************************************" );
                    m_ResultsStream.WriteLine( "* RESULTADOS EJECUCION" );
                    m_ResultsStream.WriteLine( "* Modelo: {0}", Program.GetActiveModel().Name );
                    m_ResultsStream.WriteLine( "* Set de datos: {0}", Program.ActualDataFile );
                    m_ResultsStream.WriteLine( "************************************************\r\n" );
                    m_ResultsStream.WriteLine( "Test-Rep\tEval\tTTime\tSTime\tVQ\tERTime\tWTime\tRTime\tCPU\r\n" );

                    double[] evalSums = new double[7];
                    int[] evalBestTest = new int[7];
                    int[] evalBestRep = new int[7];
                    double[] evalBests = new double[] { Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, 0.0 };

                    m_ExportableStream = File.CreateText( instanceFolder + "/results-exp.txt" );
                    m_ExportableStream.AutoFlush = true;

                    for( int k = 0; k < m_ParameterConfigurations.Length; k++ )
                    {
                        m_ActiveParamsConfig = k;
                        LogLine( "({0}) Comienza ejecución de test {1} con {2} repeticiones", DateTime.Now, k + 1, m_Repetitions );

                        RunActualConfig( graphSet, evalSums, evalBests, evalBestTest, evalBestRep );
                    }

                    int totalTests = m_ParameterConfigurations.Length * m_Repetitions;

                    m_ResultsStream.WriteLine( "\r\n******************" );
                    m_ResultsStream.WriteLine( "Síntesis Resultados" );
                    m_ResultsStream.WriteLine( "******************\r\n" );
                    m_ResultsStream.WriteLine( "Dato\tTest-Rep\tBest\tAvg\r\n" );

                    m_ResultsStream.WriteLine( "Eval\t{0:0.00}\t{1:0.00}\t{2:0.00}\t(CPU : {3:0.00})", "test " + ( evalBestTest[0] + 1 ) + "-" + evalBestRep[0], evalBests[0], evalSums[0] / totalTests, evalBests[7] );
                    m_ResultsStream.WriteLine( "TTime\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[1] + 1 ) + "-" + evalBestRep[1], evalBests[1], evalSums[1] / totalTests );
                    m_ResultsStream.WriteLine( "STime\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[2] + 1 ) + "-" + evalBestRep[2], evalBests[2], evalSums[2] / totalTests );
                    m_ResultsStream.WriteLine( "VQ\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[3] + 1 ) + "-" + evalBestRep[3], evalBests[3], evalSums[3] / totalTests );
                    m_ResultsStream.WriteLine( "ERTime\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[4] + 1 ) + "-" + evalBestRep[4], evalBests[4], evalSums[4] / totalTests );
                    m_ResultsStream.WriteLine( "WTime\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[5] + 1 ) + "-" + evalBestRep[5], evalBests[5], evalSums[5] / totalTests );
                    m_ResultsStream.Write( "RTime\t{0:0.00}\t{1:0.00}\t{2:0.00}", "test " + ( evalBestTest[6] + 1 ) + "-" + evalBestRep[6], evalBests[6], evalSums[6] / totalTests );

                    m_ResultsStream.Close();
                    m_ResultsStream = null;
                    m_ExportableStream.Close();
                    m_ExportableStream = null;

                    for( int k = 0; k < graphSet.SingleCount; k++ )
                    {
                        LogLine( "({0}) Generación de gráfico simple de parametros {1}", DateTime.Now, k + 1 );
                        graphSet.GenerateSingleGraph( k, evalBests[0] );
                    }
                    
                    for( int k = 0; k < graphSet.DualCount; k++ )
                    {
                        LogLine( "({0}) Generación de gráfico comparativo de parametros {1}", DateTime.Now, k + 1 );
                        graphSet.GenerateDualGraph( k, evalBests[0] );
                    }                    
                }
            }

            LogLine( "({0}) Finalizada ejecución de tests.", DateTime.Now );
            FinishExecution();
        }

        private void RunActualConfig( ManagedRunGraphSet graphSet, double[] evalSums, double[] evalBests, int[] evalBestTest, int[] evalBestRep )
        {
            string actualFolder = ProcessActualExecFolder();

            int[] config = m_ParameterConfigurations[m_ActiveParamsConfig];

            for( int i = 0; i < m_Parameters.Count; i++ )
                m_Parameters[i].ApplyValueToEntry( config[i] );

            string paramsInfo = GenerateActualParamsInfo();

            double thisEvalSum = 0.0;

            for( int i = 0; i < m_Repetitions; i++ )
            {
                string finalPath = actualFolder;
                
                if( m_Repetitions > 1 )
                {
                    finalPath += "/" + DateTime.Now.ToFileTime().ToString();
                    Directory.CreateDirectory( finalPath );
                }

                Evaluation eval;                
                Program.Run( finalPath, out eval );

                thisEvalSum += eval.Result;
               
                evalSums[0] += eval.Result;
                evalSums[1] += eval.TravelTime;
                evalSums[2] += eval.SlackTime;
                evalSums[3] += eval.VQ;
                evalSums[4] += eval.ExcessRideTime;
                evalSums[5] += eval.WaitTime;
                evalSums[6] += eval.RideTime;
                
                if( evalBests[0] > eval.Result )
                {
                    evalBests[0] = eval.Result;
                    evalBestTest[0] = m_ActiveParamsConfig;
                    evalBestRep[0] = i;

                    evalBests[7] = eval.Time.TotalMinutes;
                }

                if( evalBests[1] > eval.TravelTime )
                {
                    evalBests[1] = eval.TravelTime;
                    evalBestTest[1] = m_ActiveParamsConfig;
                    evalBestRep[1] = i;
                }

                if( evalBests[2] > eval.SlackTime )
                {
                    evalBests[2] = eval.SlackTime;
                    evalBestTest[2] = m_ActiveParamsConfig;
                    evalBestRep[2] = i;
                }

                if( evalBests[3] > eval.VQ )
                {
                    evalBests[3] = eval.VQ;
                    evalBestTest[3] = m_ActiveParamsConfig;
                    evalBestRep[3] = i;
                }

                if( evalBests[4] > eval.ExcessRideTime )
                {
                    evalBests[4] = eval.ExcessRideTime;
                    evalBestTest[4] = m_ActiveParamsConfig;
                    evalBestRep[4] = i;
                }

                if( evalBests[5] > eval.WaitTime )
                {
                    evalBests[5] = eval.WaitTime;
                    evalBestTest[5] = m_ActiveParamsConfig;
                    evalBestRep[5] = i;
                }

                if( evalBests[6] > eval.RideTime )
                {
                    evalBests[6] = eval.RideTime;
                    evalBestTest[6] = m_ActiveParamsConfig;
                    evalBestRep[6] = i;
                }

                m_ResultsStream.WriteLine( "{0:0.00}\t{1:0.00}\t{2:0.00}\t{3:0.00}\t{4}\t{5:0.00}\t{6:0.00}\t{7:0.00}\t{8:0.00}", "test " + ( m_ActiveParamsConfig + 1 ) + "-" + i, eval.Result, eval.TravelTime, eval.SlackTime, eval.VQ, eval.ExcessRideTime, eval.WaitTime, eval.RideTime, eval.Time.TotalMinutes );
            }

            double evalProm = thisEvalSum / m_Repetitions;

            if( graphSet.DualCount > 0 )
                graphSet.ProcessResult( m_Parameters, evalProm );

            WriteToExportable( evalProm );
        }

        private void WriteToExportable( double eval )
        {
            if( m_ExportableStream == null )
                return;

            // Se asume orden de parametros: pobsize - gencount - crossrate - mut1rate - mut2rate - tournsize
            m_ExportableStream.WriteLine( "{0} {1} {2} {3} {4} {5} {6}", m_Parameters[0].ActualEntryValue, m_Parameters[1].ActualEntryValue, m_Parameters[2].ActualEntryValue, m_Parameters[3].ActualEntryValue, m_Parameters[4].ActualEntryValue, m_Parameters[5].ActualEntryValue, eval );
        }
    }

    public class ManagedRunGraphSet
    {
        private DualParamGraphBuilder[] dualBuilders;
        private SingleParamGraphBuilder[] singleBuilders;
        public int DualCount { get { return dualBuilders.Length; } }
        public int SingleCount { get { return singleBuilders.Length; } }

        public ManagedRunGraphSet( List<ManagedRunParameter> paramSet, string ouputFolder )
        {
            List<ManagedRunParameter> filtredSet = FilterParameterList( paramSet );
            int filtredSetCount = filtredSet.Count;

            if( filtredSetCount > 1 )
            {
                dualBuilders = new DualParamGraphBuilder[(int)( Math.Pow( filtredSetCount, 2.0 ) - ( ( filtredSetCount * ( filtredSetCount + 1.0 ) ) / 2.0 ) )];
                int actualindex = 0;

                for( int i = 0; i < filtredSetCount - 1; i++ )
                {
                    for( int j = i + 1; j < filtredSetCount; j++ )
                    {
                        dualBuilders[actualindex] = new DualParamGraphBuilder( filtredSet[i], filtredSet[j], ouputFolder );
                        actualindex++;
                    }
                }

                singleBuilders = new SingleParamGraphBuilder[filtredSetCount];

                for( int i = 0; i < filtredSetCount; i++ )                
                    singleBuilders[i] = new SingleParamGraphBuilder( filtredSet[i], ouputFolder );                
            }
            else
            {
                dualBuilders = new DualParamGraphBuilder[0];
                singleBuilders = new SingleParamGraphBuilder[0];
            }
        }

        public void ProcessResult( List<ManagedRunParameter> paramSet, double value )
        {
            List<ManagedRunParameter> filtredSet = FilterParameterList( paramSet );
            int actualindex = 0;

            for( int i = 0; i < filtredSet.Count - 1; i++ )
            {
                for( int j = i + 1; j < filtredSet.Count; j++ )
                {
                    dualBuilders[actualindex].SumValue( value );
                    actualindex++;
                }
            }

            for( int i = 0; i < filtredSet.Count; i++ )
                singleBuilders[i].SumValue( value );
        }

        public void GenerateDualGraph( int builderIndex, double bestValue )
        {
            GenerateDualGraph( null, builderIndex, bestValue );
        }

        public void GenerateDualGraph( string setName, int builderIndex, double bestValue )
        {
            dualBuilders[builderIndex].GenerateGraph( setName, builderIndex, bestValue );
        }

        public void GenerateSingleGraph( int builderIndex, double bestValue )
        {
            GenerateSingleGraph( null, builderIndex, bestValue );
        }

        public void GenerateSingleGraph( string setName, int builderIndex, double bestValue )
        {
            singleBuilders[builderIndex].GenerateGraph( setName, builderIndex, bestValue );
        }

        private List<ManagedRunParameter> FilterParameterList( List<ManagedRunParameter> baseList )
        {
            List<ManagedRunParameter> filtredList = new List<ManagedRunParameter>( baseList.Count );

            for( int i = 0; i < baseList.Count; i++ )
            {
                if( baseList[i].ValuesCount > 1 )
                    filtredList.Add( baseList[i] );
            }

            return filtredList;
        }
    }

    public class SingleParamGraphBuilder
    {
        private ManagedRunParameter m_Param;
        private double[] m_Evaluations;
        private int[] m_SumCounts;
        private string m_OuputFolder;

        public SingleParamGraphBuilder( ManagedRunParameter param ) : this( param, null )
        {
        }

        public SingleParamGraphBuilder( ManagedRunParameter param, string ouputFolder )
        {
            m_Param = param;
            m_OuputFolder = ouputFolder;

            m_Evaluations = new double[m_Param.ValuesCount];
            m_SumCounts = new int[m_Param.ValuesCount];
        }

        public void SumValue( double value )
        {
            int index = m_Param.SelectedIndex;

            SumValue( value, index );
        }

        public void SumValue( double value, int index )
        {
            m_Evaluations[index] += value;
            m_SumCounts[index]++;
        }

        private double[] CalculateMeans()
        {
            int count = m_Param.ValuesCount;

            double[] means = new double[count];

            for( int i = 0; i < count; i++ )
                means[i] = m_Evaluations[i] / m_SumCounts[i];

            return means;
        }

        public static readonly KnownColor[] GraphColors = new KnownColor[]
        {
            KnownColor.Gold,
            KnownColor.Green,            
            KnownColor.Orange,
            KnownColor.Blue,
            KnownColor.Black,
            KnownColor.Brown,
            KnownColor.Aqua,
            KnownColor.Fuchsia,            
            KnownColor.Gray,            
            KnownColor.Magenta,            
            KnownColor.Pink,
            KnownColor.Purple,
            KnownColor.Red,
            KnownColor.White,
            KnownColor.Yellow
        };

        public void GenerateGraph( int testNumber, double bestValue )
        {
            GenerateGraph( null, testNumber, bestValue );
        }

        public void GenerateGraph( string setName, int testNumber, double bestValue )
        {
            double[] means = CalculateMeans();

            Chart toGen = new Chart();

            toGen.Type = ChartType.Combo;
            toGen.Use3D = true;
            toGen.Size = new Size( 800, 600 );

            //toGen.XAxis.Label = new Label( "Valor " + m_Param.Entry.Name );
            toGen.YAxis.ScaleRange.ValueLow = bestValue - 200.0;
            toGen.YAxis.Interval = 100;
            toGen.YAxis.Label = new Label( "Evaluación / Fitness" );

            toGen.LegendBox.Template = "%Name%icon";

            toGen.TempDirectory = m_OuputFolder;
            toGen.Title = String.Format( "Comparación {0}\nset {1}\n{2}", m_Param.Entry.Name, ( setName == null ? Program.ActualDataFile : setName ), Program.GetActiveModel().Name );
            toGen.FileName = "graph-singlecomp-" + testNumber.ToString(); ;
            toGen.DefaultSeries.Type = SeriesType.Bar;
            //toGen.TitleBox.Position = TitleBoxPosition.FullWithLegend;
            toGen.DefaultElement.Marker.Visible = false;
            toGen.ShadingEffect = true;

            SeriesCollection SC = new SeriesCollection();
            RandomUniqueSelector colorSelector = new RandomUniqueSelector( GraphColors.Length - 1 );

            for( int i = 0; i < means.Length; i++ )
            {
                Series s = new Series();
                s.Name = m_Param.GetValue( i ).ToString();

                if( colorSelector.IsCompleted )
                    s.DefaultElement.Color = Color.FromArgb( RandomTool.GetInt( 255 ), RandomTool.GetInt( 255 ), RandomTool.GetInt( 255 ) );
                else
                    s.DefaultElement.Color = Color.FromKnownColor( GraphColors[colorSelector.Next()] );

                Element e = new Element();
                e.YValue = means[i];

                s.Elements.Add( e );
                SC.Add( s );
            }
                        
            toGen.SeriesCollection.Add( SC );

            toGen.ImageFormat = dotnetCHARTING.WinForms.ImageFormat.Png;
            toGen.FileManager.SaveImage();
        }
    }

    public class DualParamGraphBuilder
    {
        private ManagedRunParameter m_MainParam;
        private ManagedRunParameter m_SecondaryParam;
        private double[,] m_Evaluations;
        private int[,] m_SumCounts;
        private string m_OuputFolder;

        public DualParamGraphBuilder( ManagedRunParameter mainParam, ManagedRunParameter secondaryParam ) : this( mainParam, secondaryParam, null )
        {
        }

        public DualParamGraphBuilder( ManagedRunParameter mainParam, ManagedRunParameter secondaryParam, string ouputFolder )
        {
            m_MainParam = mainParam;
            m_SecondaryParam = secondaryParam;
            m_OuputFolder = ouputFolder;

            m_Evaluations = new double[m_MainParam.ValuesCount, m_SecondaryParam.ValuesCount];
            m_SumCounts = new int[m_MainParam.ValuesCount, m_SecondaryParam.ValuesCount];
        }

        public void SumValue( double value )
        {
            int mainIndex = m_MainParam.SelectedIndex;
            int secondaryIndex = m_SecondaryParam.SelectedIndex;

            SumValue( value, mainIndex, secondaryIndex );
        }

        public void SumValue( double value, int mainIndex, int secondaryIndex )
        {
            m_Evaluations[mainIndex, secondaryIndex] += value;
            m_SumCounts[mainIndex, secondaryIndex]++;
        }

        private double[,] CalculateMeans()
        {
            int mainCount = m_MainParam.ValuesCount;
            int secondaryCount = m_SecondaryParam.ValuesCount;

            double[,] means = new double[mainCount, secondaryCount];

            for( int i = 0; i < mainCount; i++ )            
                for( int j = 0; j < secondaryCount; j++ )                
                    means[i, j] = m_Evaluations[i, j] / m_SumCounts[i, j];

            return means;
        }

        public void GenerateGraph( int testNumber, double bestValue )
        {
            GenerateGraph( null, testNumber, bestValue );
        }

        public void GenerateGraph( string setName, int testNumber, double bestValue )
        {
            double[,] means = CalculateMeans();

            Chart toGen = new Chart();

            toGen.Type = ChartType.Combo;
            toGen.Use3D = true;
            toGen.Size = new Size( 800, 600 );
            
            toGen.XAxis.Label = new Label( "Valor " + m_MainParam.Entry.Name );
            toGen.YAxis.ScaleRange.ValueLow = bestValue - 200.0;
            toGen.YAxis.Interval = 100;
            toGen.YAxis.Label = new Label( "Evaluación / Fitness" );

            toGen.LegendBox.Template = "%Name%icon";

            toGen.TempDirectory = m_OuputFolder;
            toGen.Title = String.Format( "Comparación {0} - {1}\nset {2}\n{3}", m_MainParam.Entry.Name, m_SecondaryParam.Entry.Name, ( setName == null ? Program.ActualDataFile : setName ), Program.GetActiveModel().Name );
            toGen.FileName = "graph-dualcomp-" + testNumber.ToString(); ;
            toGen.DefaultSeries.Type = SeriesType.Bar;
            //toGen.TitleBox.Position = TitleBoxPosition.FullWithLegend;
            toGen.DefaultElement.Marker.Visible = false;
            toGen.ShadingEffect = true;

            SeriesCollection SC = new SeriesCollection();
            RandomUniqueSelector colorSelector = new RandomUniqueSelector( SingleParamGraphBuilder.GraphColors.Length - 1 );

            for( int i = 0; i < means.GetUpperBound( 1 ) + 1; i++ )
            {
                Series s = new Series();
                s.Name = m_SecondaryParam.GetValue( i ).ToString();

                if( colorSelector.IsCompleted )
                    s.DefaultElement.Color = Color.FromArgb( RandomTool.GetInt( 255 ), RandomTool.GetInt( 255 ), RandomTool.GetInt( 255 ) );
                else
                    s.DefaultElement.Color = Color.FromKnownColor( SingleParamGraphBuilder.GraphColors[colorSelector.Next()] );

                for( int j = 0; j < means.GetUpperBound( 0 ) + 1; j++ )
                {
                    Element e = new Element();

                    e.YValue = means[j, i];
                    e.Name = m_MainParam.GetValue( j ).ToString();

                    s.Elements.Add( e );
                }

                SC.Add( s );
            }

            toGen.SeriesCollection.Add( SC );

            toGen.ImageFormat = dotnetCHARTING.WinForms.ImageFormat.Png;
            toGen.FileManager.SaveImage();
        }
    }
}
