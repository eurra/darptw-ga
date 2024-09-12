using System;
using System.Collections.Generic;
using System.Text;

namespace DARPTW_GA.Misc
{
    [AttributeUsage( AttributeTargets.Method )]
    public class OperatorAttribute : Attribute
    {
        private string m_Name;

        public OperatorAttribute( string name )
        {
            m_Name = name;
        }

        public string Name { get { return m_Name; } }
    }
}
