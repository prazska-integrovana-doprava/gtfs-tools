using CsvSerializer.Attributes;
using System;
using System.Reflection;

namespace CsvSerializer
{
    /// <summary>
    /// Dvojice MemberInfo z reflection + přiřazený atribut
    /// </summary>
    internal class MemberAndAttribute
    {
        public CsvFieldAttribute Attribute { get; set; }
        public MemberInfo MemberInfo { get; set; }

        public Type FieldType
        {
            get
            {
                Type fieldType;
                if (MemberInfo.MemberType == MemberTypes.Field)
                {
                    fieldType = ((FieldInfo)MemberInfo).FieldType;
                }
                else if (MemberInfo.MemberType == MemberTypes.Property)
                {
                    fieldType = ((PropertyInfo)MemberInfo).PropertyType;
                }
                else
                {
                    throw new Exception("Unknown type");
                }

                return fieldType;
            }
        }
    }

}
