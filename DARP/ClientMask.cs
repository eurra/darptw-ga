/* ClientMask.cs
 * �ltima modificaci�n: 12/03/2008
 */

using System;
using System.Collections.Generic;
using System.Text;
using DARPTW_GA.Misc;

namespace DARPTW_GA.DARP
{
    /* Clase 'ClientMask'
     *  Implementaci�n de un arreglo de bits con diversos operadores, que representa una m�scara 
     * de asignaci�n de pasajeros a un veh�culo. La m�scara contendra un '1' en una determinada
     * posici�n, para indicar que el veh�culo respectivo esta transportando al cliente asociado
     * a esa posici�n, y contendr� un '0' si no transporta a dicho cliente. Esta representaci�n
     * se basa en estructuras de datos livianas en terminos de memoria (bytes), con el fin de su 
     * utilizaci�n en gran n�mero dentro de la ejecuci�n.
     *  Esta implementado con un arreglo interno de bytes, en donde cada uno de ellos tiene puede
     * representar un m�ximo de 8 pasajeros, de modo que la cantidad de bytes en este arreglo
     * definir� la cantidad de clientes que pueden ser representados en la m�scara. Para un
     * n�mero que no sea m�ltiplo de 8, se utiliza un valor que indica la cantidad de clientes 
     * que son representados en la m�scara.
     *  El largo de la m�scara esta limitado a 255 elementos. Para casos con m�s pasajeros, se
     * requiere una modificaci�n del atributo que indica la cantidad de pasajeros en la m�scara.
     * 
     *  Atributos:
     * - 'byte[] bytePos': Arreglo est�tico de bytes, que se utiliza para buscar elementos en la
     * m�scara, por los distintos operadores del objeto.
     * - 'byte[] m_Values': Arreglo de bytes que representa los valores actuales de la m�scara.
     * - 'byte m_Length': Byte que indica la cantidad de pasajeros representada en la m�scara.
     */
    public class ClientMask : ICloneable
    {
        private static readonly byte[] bytePos = new byte[]{ 1, 2, 4, 8, 16, 32, 64, 128 };
        
        private byte[] m_Values;
        private byte m_Length;

        public int Length { get { return m_Length; } }


        /* Propiedad 'Empty' (s�lo lectura)
         *  Retorna un nuevo objeto ClientMask, s�lo con valores 0 y del largo definido en la
         * variable global 'GlobalParams.ClientNumber'
         */
        public static ClientMask Empty
        {
            get
            {
                return new ClientMask( GlobalParams.ClientNumber );
            }
        }

        /* Propiedad 'Full' (s�lo lectura)
         *  Retorna un nuevo objeto ClientMask, s�lo con valores 1 y del largo definido en la 
         * variable global 'GlobalParams.ClientNumber'. Cabe destacar que si este largo no es
         * m�ltiplo de 8, los bits de la m�scara que sobran quedan en 0.
         */
        public static ClientMask Full
        {
            get
            {
                ClientMask ret = Empty;
                byte[] retArray = ret.m_Values;

                bool checkLastDif = ( 8 * retArray.Length > ret.Length );

                for( int i = 0; i < retArray.Length; i++ )                
                    retArray[i] = 255;

                ret.ClearUnusedBits();

                return ret;
            }
        }

        /* Propiedad �ndice (lectura/escritura)
         *  Al ser le�do, retorna 'true' si el valor de la m�scara en la posici�n indicada es 1, y 
         * retorna 'false' si dicho valor es '0'.
         *  Al ser asignado con 'true', dejar� en '1' el valor del elemento en la posici�n indicada, 
         * en cambio si es asignado con 'false', dicho valor quedar� con '0'.
         * 
         *  Par�metros:
         * - 'int index': Posici�n indicada de la m�scarapara la lectura/escritura del valor.
         */
        public bool this[int index]
        {
            get
            {
                int valsIndex = index / 8;

                byte val = m_Values[valsIndex];
                byte check = bytePos[index - 8 * valsIndex];

                return ( ( val & check ) == check );
            }

            set
            {
                int valsIndex = index / 8;

                byte check = bytePos[index - 8 * valsIndex];

                if( value )                    
                    m_Values[index / 8] |= check;                    
                else                    
                    m_Values[index / 8] ^= check;                    
            }
        }

        /* Constructor
         *  Crea una m�scara de largo definido, inicializando todos sus valores en 0.
         * 
         *  Par�metros:
         * - 'int length': Largo de la m�scara a crear.
         */
        public ClientMask( int length )
        {
            m_Values = new byte[(int)Math.Ceiling( (double)length / 8.0 )];
            m_Length = (byte)length;
        }

        /* Constructor
         *  Crea una m�scara de largo definido, asignando de forma aleatoria un 1 a un n�mero
         * determinado de sus valores.
         * 
         *  Par�metros:
         * - 'int length': Largo de la m�scara a crear.
         * - 'int maxSel': Cantidad de elementos a los que se asignar� un 1.
         */
        public ClientMask( int length, int maxSel ) : this( length )
        {
            RandomizeValues( maxSel );
        }

        /* Constructor
         *  Crea una m�scara de largo definido, asignando un '1' a los clientes de las posic�ones
         * especificadas.
         * 
         *  Par�metros:
         * - 'int length': Largo de la m�scara a crear.
         * - 'int[] clients': Arreglo que define las posiciones de los clientes que seran asignados
         * en la m�scara.
         */
        public ClientMask( int length, int[] clients ) : this( length ) 
        {
            for( int i = 0; i < clients.Length; i++ )
                this[clients[i]] = true;
        }

        /* Constructor
         *  Crea una m�scara en base al un string. Se asume que este string s�lo contiene
         * caracteres '1' y '0', cualquier otro caracter es interpretado como '0'.
         * 
         *  Par�metros:
         * - 'string mask': String que ser� usado para construir la m�scara.
         */
        public ClientMask( string mask ) : this( mask.Length )
        {
            char[] values = mask.ToCharArray();

            for( int i = 0; i < values.Length; i++ )
            {
                if( values[i] == '1' )                
                    this[i] = true;                
            }
        }

        /* M�todo 'Clone' (implementado de 'ICloneable')
         *  Crea una copia de esta instancia de m�scara. Retorna la copia como tipo object.
         */ 
        public object Clone()
        {
            ClientMask cloned = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                cloned.m_Values[i] = m_Values[i];

            return cloned;
        }

        /* M�todo 'IsSame'
         *  Compara los valores de esta m�scara con los de otra. Retorna 'true' si esta m�scara es
         * identica a la otra m�scara o si ambas m�scaras corresponden a la misma referencia.
         * Retorna 'false' si las m�scaras tienen valores distintos, si la otra m�scara es null o 
         * si ambas m�scaras tienen largo distinto.
         * 
         *  Par�metros:
         * - 'ClientMask other': M�scara que se comparar�.
         */
        public bool IsSame( ClientMask other )
        {
            if( other == null )
            {
                return false;
            }
            else if( other == this )
            {
                return true;
            }            
            else if( Length != other.Length )
            {
                return false;
            }                
            else
            {
                for( int i = m_Values.Length - 1; i >= 0; i-- )
                {
                    if( m_Values[i] != other.m_Values[i] )                        
                        return false;                        
                }

                return true;
            }           
        }

        /* M�todo 'GetClientList'
         *  Retorna un objeto List<int> que representa una lista con los enteros que indican 
         * las posiciones de los clientes asignados en esta m�scara.
         */
        public List<int> GetClientList()
        {
            List<int> ret = new List<int>( GlobalParams.ClientNumber / GlobalParams.VehiclesMaxNumber );

            for( int i = 0; i < m_Values.Length; i++ )
            {
                if( m_Values[i] > 0 )
                {
                    int checkIndex = 8 * i;
                    byte checkByte = m_Values[i];

                    bool checkUnused = ( ( m_Values.Length * 8 > m_Length ) && ( i == m_Values.Length - 1 ) );

                    for( int j = 0; j < 8 && ( !checkUnused || checkIndex < m_Length ); j++ )
                    {
                        if( ( checkByte & 1 ) == 1 )
                            ret.Add( checkIndex );

                        checkByte = (byte)( checkByte >> 1 );
                        checkIndex++;
                    }
                }
            }

            return ret;
        }

        /* M�todo 'ResetValues'
         *  Asigna '0' a todos los valores de la m�scara, dej�ndola vac�a.
         */
        public void ResetValues()
        {
            for( int i = 0; i < m_Values.Length; i++ )
                m_Values[i] = 0;
        }

        /* M�todo 'RandomizeValues'
         *  Asigna de forma aleatoria un '1' a una cantidad determinada de elementos en la m�scara.
         * Esta asignaci�n es independiente del valor previo de las posiciones seleccionadas.
         * 
         *  Par�metros:
         * - 'int maxOnes': Cantidad de posiciones a las que seran asignadas de forma aleatoria un 1.
         */
        public void RandomizeValues( int maxOnes )
        {
            List<int> check = new List<int>( m_Length );            

            for( int i = 0; i < m_Length; i++ )
                check.Add( i );

            for( int i = 0; i < maxOnes; i++ )
            {
                int sel = check[RandomTool.GetInt( check.Count - 1 )];
                this[sel] = true;
                check.Remove( sel );
            }
        }

        /* M�todo 'OR'
         *  Retorna una nueva m�scara, despues de aplicar una operaci�n "OR" ('�' l�gico) a esta
         * m�scara respecto a otra. Ejemplo:
         * 
         *  esta m�scara:       11010110100
         *  otra m�scara:       00100000101
         *                      -----------
         *  resultado OR:       11110110101
         * 
         *  Par�metros:
         * - 'ClientMask toCompare': La otra m�scara que se usar� para obtener el resultado.
         */
        public ClientMask OR( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] | toCompare.m_Values[i] );

            return ret;            
        }

        /* M�todo 'AND'
         *  Retorna una nueva m�scara, despues de aplicar una operaci�n "AND" ('y' l�gico) a esta
         * m�scara respecto a otra. Ejemplo:
         * 
         *  esta m�scara:       11010110100
         *  otra m�scara:       00110010101
         *                      -----------
         *  resultado AND:      00010010100
         * 
         *  Par�metros:
         * - 'ClientMask toCompare': La otra m�scara que se usar� para obtener el resultado.
         */
        public ClientMask AND( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] & toCompare.m_Values[i] );

            return ret;
        }

        /* M�todo 'XOR'
         *  Retorna una nueva m�scara, despues de aplicar una operaci�n "XOR" ('� exclusivo' l�gico)
         * a esta m�scara respecto a otra. Ejemplo:
         * 
         *  esta m�scara:       11010110100
         *  otra m�scara:       00110010101
         *                      -----------
         *  resultado XOR:      11100100001
         * 
         *  Par�metros:
         * - 'ClientMask toCompare': La otra m�scara que se usar� para obtener el resultado.
         */
        public ClientMask XOR( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] ^ toCompare.m_Values[i] );

            return ret;
        }

        /* M�todo 'NOT'
         *  Retorna una nueva m�scara, despues de aplicar una operaci�n "NOT" (negaci�n l�gica)
         * a esta m�scara. Ejemplo:
         * 
         *  esta m�scara:       11010110100
         *                      -----------
         *  resultado NOT:      00101001011
         */
        public ClientMask NOT()
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( ~m_Values[i] );

            ret.ClearUnusedBits();

            return ret;
        }

        /* M�todo 'ClearUnusedBits'
         *  Asigna un valor '0' a todos los bits de la m�scara que no se utilizan. Estos son los
         * que sobran en el �ltimo byte del arreglo interno, cuando el largo de la m�scara no es
         * m�ltiplo de 8.
         */
        private void ClearUnusedBits()
        {
            if( m_Length < m_Values.Length * 8 )
            {
                int validBits = 8 - ( m_Values.Length * 8 - m_Length );
                byte mask = (byte)( Math.Pow( 2, validBits ) - 1 );

                m_Values[m_Values.Length - 1] &= mask;
            }
        }

        /* M�todo 'ToString' (sobrecargado de 'Object')
         *  Retorna una representaci�n en string de esta m�scara, con la forma '100101...'.
         */
        public override string ToString()
        {
            string res = "";
            int pos = 0;

            for( int i = 0; i < m_Values.Length; i++ )
            {
                for( int j = 0; j < bytePos.Length && pos < m_Length; j++ )
                {
                    res += ( ( m_Values[i] & bytePos[j] ) == bytePos[j] ? "1" : "0" );
                    pos++;
                }
            }

            return res;
        }
    }
}
