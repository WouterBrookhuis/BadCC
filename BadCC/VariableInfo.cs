using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    public class TypeInfo
    {
        public static readonly TypeInfo Int = new TypeInfo(TypeSpec.Int, false);
        public static readonly TypeInfo IntPtr = new TypeInfo(TypeSpec.Int, true);

        public enum TypeSpec
        {
            Void,
            Int,
        }

        /// <summary>
        /// The basic type of this info
        /// </summary>
        public TypeSpec Type { get; private set; }
        /// <summary>
        /// If this type is a pointer type
        /// </summary>
        public bool IsPointer { get; private set; }
        /// <summary>
        /// The type that this is a pointer or array of
        /// </summary>
        public TypeInfo ElementInfo { get; private set; }
        /// <summary>
        /// True if this is an array of some basic type
        /// </summary>
        public bool IsArray => !IsPointer && ElementInfo != null;
        /// <summary>
        /// True if this is a basic type (TypeSpec)
        /// </summary>
        public bool IsBasicType => ElementInfo == null;

        public TypeInfo(TypeSpec type, bool isPointer)
        {
            Type = type;
            IsPointer = isPointer;
        }

        public TypeInfo(TypeInfo elementInfo, bool isPointer)
        {
            IsPointer = isPointer;
            ElementInfo = elementInfo;
        }

        public override string ToString()
        {
            if(!IsBasicType)
            {
                if(IsArray)
                {
                    return "[" + ElementInfo.ToString() + "]";
                }
                else if(IsPointer)
                {
                    return "*" + ElementInfo.ToString();
                }
                else
                {
                    return "ILLEGAL TYPE";
                }
            }
            else
            {
                if(IsArray)
                {
                    return "[" + Type.ToString() + "]";
                }
                else if(IsPointer)
                {
                    return "*" + Type.ToString();
                }
                else
                {
                    return Type.ToString();
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is TypeInfo info &&
                   Type == info.Type &&
                   IsPointer == info.IsPointer &&
                   EqualityComparer<TypeInfo>.Default.Equals(ElementInfo, info.ElementInfo) &&
                   IsArray == info.IsArray;
        }

        public override int GetHashCode()
        {
            var hashCode = -1911918688;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + IsPointer.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeInfo>.Default.GetHashCode(ElementInfo);
            hashCode = hashCode * -1521134295 + IsArray.GetHashCode();
            return hashCode;
        }
    }

    class VariableInfo
    {
        public string Name { get; private set; }
        public TypeInfo Type { get; private set; }

        public VariableInfo(string name, TypeInfo type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return "{Name=" + Name + ", Type=" + Type.ToString() + "}";
        }
    }
}
