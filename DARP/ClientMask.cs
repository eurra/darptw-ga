/* ClientMask.cs
 * Última modificación: 12/03/2008
 */

using System;
using System.Collections.Generic;
using System.Text;
using DARPTW_GA.Misc;

namespace DARPTW_GA.DARP
{
    /* Clase 'ClientMask'
     *  Implementación de un arreglo de bits con diversos operadores, que representa una máscara 
     * de asignación de pasajeros a un vehículo. La máscara contendra un '1' en una determinada
     * posición, para indicar que el vehículo respectivo esta transportando al cliente asociado
     * a esa posición, y contendrá un '0' si no transporta a dicho cliente. Esta representación
     * se basa en estructuras de datos livianas en terminos de memoria (bytes), con el fin de su 
     * utilización en gran número dentro de la ejecución.
     *  Esta implementado con un arreglo interno de bytes, en donde cada uno de ellos tiene puede
     * representar un máximo de 8 pasajeros, de modo que la cantidad de bytes en este arreglo
     * definirá la cantidad de clientes que pueden ser representados en la máscara. Para un
     * número que no sea múltiplo de 8, se utiliza un valor que indica la cantidad de clientes 
     * que son representados en la máscara.
     *  El largo de la máscara esta limitado a 255 elementos. Para casos con más pasajeros, se
     * requiere una modificación del atributo que indica la cantidad de pasajeros en la máscara.
     * 
     *  Atributos:
     * - 'byte[] bytePos': Arreglo estático de bytes, que se utiliza para buscar elementos en la
     * máscara, por los distintos operadores del objeto.
     * - 'byte[] m_Values': Arreglo de bytes que representa los valores actuales de la máscara.
     * - 'byte m_Length': Byte que indica la cantidad de pasajeros representada en la máscara.
     */
    public class ClientMask : ICloneable
    {
        private static readonly byte[] bytePos = new byte[]{ 1, 2, 4, 8, 16, 32, 64, 128 };
        
        private byte[] m_Values;
        private byte m_Length;

        public int Length { get { return m_Length; } }


        /* Propiedad 'Empty' (sólo lectura)
         *  Retorna un nuevo objeto ClientMask, sólo con valores 0 y del largo definido en la
         * variable global 'GlobalParams.ClientNumber'
         */
        public static ClientMask Empty
        {
            get
            {
                return new ClientMask( GlobalParams.ClientNumber );
            }
        }

        /* Propiedad 'Full' (sólo lectura)
         *  Retorna un nuevo objeto ClientMask, sólo con valores 1 y del largo definido en la 
         * variable global 'GlobalParams.ClientNumber'. Cabe destacar que si este largo no es
         * múltiplo de 8, los bits de la máscara que sobran quedan en 0.
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

        /* Propiedad índice (lectura/escritura)
         *  Al ser leído, retorna 'true' si el valor de la máscara en la posición indicada es 1, y 
         * retorna 'false' si dicho valor es '0'.
         *  Al ser asignado con 'true', dejará en '1' el valor del elemento en la posición indicada, 
         * en cambio si es asignado con 'false', dicho valor quedará con '0'.
         * 
         *  Parámetros:
         * - 'int index': Posición indicada de la máscarapara la lectura/escritura del valor.
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
         *  Crea una máscara de largo definido, inicializando todos sus valores en 0.
         * 
         *  Parámetros:
         * - 'int length': Largo de la máscara a crear.
         */
        public ClientMask( int length )
        {
            m_Values = new byte[(int)Math.Ceiling( (double)length / 8.0 )];
            m_Length = (byte)length;
        }

        /* Constructor
         *  Crea una máscara de largo definido, asignando de forma aleatoria un 1 a un número
         * determinado de sus valores.
         * 
         *  Parámetros:
         * - 'int length': Largo de la máscara a crear.
         * - 'int maxSel': Cantidad de elementos a los que se asignará un 1.
         */
        public ClientMask( int length, int maxSel ) : this( length )
        {
            RandomizeValues( maxSel );
        }

        /* Constructor
         *  Crea una máscara de largo definido, asignando un '1' a los clientes de las posicíones
         * especificadas.
         * 
         *  Parámetros:
         * - 'int length': Largo de la máscara a crear.
         * - 'int[] clients': Arreglo que define las posiciones de los clientes que seran asignados
         * en la máscara.
         */
        public ClientMask( int length, int[] clients ) : this( length ) 
        {
            for( int i = 0; i < clients.Length; i++ )
                this[clients[i]] = true;
        }

        /* Constructor
         *  Crea una máscara en base al un string. Se asume que este string sólo contiene
         * caracteres '1' y '0', cualquier otro caracter es interpretado como '0'.
         * 
         *  Parámetros:
         * - 'string mask': String que será usado para construir la máscara.
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

        /* Método 'Clone' (implementado de 'ICloneable')
         *  Crea una copia de esta instancia de máscara. Retorna la copia como tipo object.
         */ 
        public object Clone()
        {
            ClientMask cloned = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                cloned.m_Values[i] = m_Values[i];

            return cloned;
        }

        /* Método 'IsSame'
         *  Compara los valores de esta máscara con los de otra. Retorna 'true' si esta máscara es
         * identica a la otra máscara o si ambas máscaras corresponden a la misma referencia.
         * Retorna 'false' si las máscaras tienen valores distintos, si la otra máscara es null o 
         * si ambas máscaras tienen largo distinto.
         * 
         *  Parámetros:
         * - 'ClientMask other': Máscara que se comparará.
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

        /* Método 'GetClientList'
         *  Retorna un objeto List<int> que representa una lista con los enteros que indican 
         * las posiciones de los clientes asignados en esta máscara.
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

        /* Método 'ResetValues'
         *  Asigna '0' a todos los valores de la máscara, dejándola vacía.
         */
        public void ResetValues()
        {
            for( int i = 0; i < m_Values.Length; i++ )
                m_Values[i] = 0;
        }

        /* Método 'RandomizeValues'
         *  Asigna de forma aleatoria un '1' a una cantidad determinada de elementos en la máscara.
         * Esta asignación es independiente del valor previo de las posiciones seleccionadas.
         * 
         *  Parámetros:
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

        /* Método 'OR'
         *  Retorna una nueva máscara, despues de aplicar una operación "OR" ('ó' lógico) a esta
         * máscara respecto a otra. Ejemplo:
         * 
         *  esta máscara:       11010110100
         *  otra máscara:       00100000101
         *                      -----------
         *  resultado OR:       11110110101
         * 
         *  Parámetros:
         * - 'ClientMask toCompare': La otra máscara que se usará para obtener el resultado.
         */
        public ClientMask OR( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] | toCompare.m_Values[i] );

            return ret;            
        }

        /* Método 'AND'
         *  Retorna una nueva máscara, despues de aplicar una operación "AND" ('y' lógico) a esta
         * máscara respecto a otra. Ejemplo:
         * 
         *  esta máscara:       11010110100
         *  otra máscara:       00110010101
         *                      -----------
         *  resultado AND:      00010010100
         * 
         *  Parámetros:
         * - 'ClientMask toCompare': La otra máscara que se usará para obtener el resultado.
         */
        public ClientMask AND( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] & toCompare.m_Values[i] );

            return ret;
        }

        /* Método 'XOR'
         *  Retorna una nueva máscara, despues de aplicar una operación "XOR" ('ó exclusivo' lógico)
         * a esta máscara respecto a otra. Ejemplo:
         * 
         *  esta máscara:       11010110100
         *  otra máscara:       00110010101
         *                      -----------
         *  resultado XOR:      11100100001
         * 
         *  Parámetros:
         * - 'ClientMask toCompare': La otra máscara que se usará para obtener el resultado.
         */
        public ClientMask XOR( ClientMask toCompare )
        {
            ClientMask ret = new ClientMask( this.Length );

            for( int i = 0; i < m_Values.Length; i++ )
                ret.m_Values[i] = (byte)( m_Values[i] ^ toCompare.m_Values[i] );

            return ret;
        }

        /* Método 'NOT'
         *  Retorna una nueva máscara, despues de aplicar una operación "NOT" (negación lógica)
         * a esta máscara. Ejemplo:
         * 
         *  esta máscara:       11010110100
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

        /* Método 'ClearUnusedBits'
         *  Asigna un valor '0' a todos los bits de la máscara que no se utilizan. Estos son los
         * que sobran en el último byte del arreglo interno, cuando el largo de la máscara no es
         * múltiplo de 8.
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

        /* Método 'ToString' (sobrecargado de 'Object')
         *  Retorna una representación en string de esta máscara, con la forma '100101...'.
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
