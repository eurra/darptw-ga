using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace DARPTW_GA
{
    [AttributeUsage( AttributeTargets.Property )]
    public class ParameterTagAttribute : Attribute
    {
        private string m_Name;
        private string m_Tag;
        private double m_MinValue;
        private double m_MaxValue;
        private double m_DefaultValue;

        public string Name { get { return m_Name; } }
        public string Tag { get { return m_Tag; } }
        public double MinValue { get { return m_MinValue; } }
        public double MaxValue { get { return m_MaxValue; } }
        public double DefaultValue { get { return m_DefaultValue; } }

        public ParameterTagAttribute( string name, string tag, double minValue, double maxValue, double defaultValue )
        {
            m_Name = name;
            m_Tag = tag;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_DefaultValue = defaultValue;
        }
    }

    public class ParameterEntry
    {
        private PropertyInfo m_Info;
        private ParameterTagAttribute m_Attribute;
        private double m_Value;

        public string Name { get { return m_Attribute.Name; } }
        public double MinValue { get { return m_Attribute.MinValue; } }
        public double MaxValue { get { return m_Attribute.MaxValue; } }
        public string Tag { get { return m_Attribute.Tag; } }

        public double Value
        {
            get { return m_Value; }

            set
            {
                if( value < m_Attribute.MinValue )
                    value = m_Attribute.MinValue;
                else if( value > m_Attribute.MaxValue )
                    value = m_Attribute.MaxValue;

                m_Value = value;
            }
        }

        public ParameterEntry( PropertyInfo info, ParameterTagAttribute attribute )
        {
            m_Info = info;
            m_Attribute = attribute;
            Value = m_Attribute.DefaultValue;
        }

        public void ApplyValue( object toSet )
        {
            object checkedValue;

            if( m_Info.PropertyType == typeof( int ) )
                checkedValue = (int)m_Value;
            else
                checkedValue = m_Value;

            m_Info.SetValue( toSet, checkedValue, null );
        }
    }

    public class ParameterConfig
    {
        private string m_Name;
        private Type m_BaseType;
        private Dictionary<string, ParameterEntry> m_Parameters = new Dictionary<string, ParameterEntry>();

        public string ConfigName { get { return m_Name; } }
        public Type BaseType { get { return m_BaseType; } }
        public int Count { get { return m_Parameters.Count; } }
        public Dictionary<string, ParameterEntry> ParametersTable { get { return m_Parameters; } }
        public Dictionary<string, ParameterEntry>.ValueCollection Entries { get { return m_Parameters.Values; } }
        public Dictionary<string, ParameterEntry>.KeyCollection Tags { get { return m_Parameters.Keys; } }

        public ParameterEntry this[string tag]
        {
            get
            {                
                ParameterEntry entry;
                m_Parameters.TryGetValue( tag, out entry );

                return entry;
            }
        }

        public ParameterConfig( string name, Type baseType )
        {
            m_Name = name;
            m_BaseType = baseType;

            PropertyInfo[] infos = m_BaseType.GetProperties();

            for( int i = 0; i < infos.Length; i++ )
            {
                object[] attrs = infos[i].GetCustomAttributes( typeof( ParameterTagAttribute ), false );

                if( attrs.Length > 0 )
                {
                    ParameterTagAttribute attr = (ParameterTagAttribute)attrs[0];

                    if( !m_Parameters.ContainsKey( attr.Tag ) )
                        m_Parameters[attr.Tag] = new ParameterEntry( infos[i], attr );
                }
            }
        }

        public void ApplyValues()
        {
            ApplyValues( null );
        }

        public void ApplyValues( object target )
        {
            foreach( ParameterEntry entry in m_Parameters.Values )
                entry.ApplyValue( target );
        }
    }
}
