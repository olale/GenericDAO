using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Extensions {
    static class TypeExtensions {
        static Dictionary<Type,List<Type>> dict=new Dictionary<Type,List<Type>>() {
        { typeof(decimal), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char) } },
        { typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
        { typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
        { typeof(ulong), new List<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(char) } },
        { typeof(long), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char) } },
        { typeof(uint), new List<Type> { typeof(byte), typeof(ushort), typeof(char) } },
        { typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char) } },
        { typeof(ushort), new List<Type> { typeof(byte), typeof(char) } },
        { typeof(short), new List<Type> { typeof(byte) } }
    };
        public static bool IsCastableFrom(this Type to,Type from) {
            if(to.IsAssignableFrom(from)) {
                return true;
            }
            if(dict.ContainsKey(to)&&dict[to].Contains(from)) {
                return true;
            }
            bool castable=from.GetMethods(BindingFlags.Public|BindingFlags.Static)
                            .Any(
                                m => m.ReturnType==to&&
                                m.Name=="op_Implicit"||
                                m.Name=="op_Explicit"
                            );
            return castable;
        }

        public static bool IsCollection(this Type returnType) {
            return returnType.IsGenericType&&
                   typeof(ICollection<>).IsAssignableFrom(returnType.GetGenericTypeDefinition());
        }

        public static Type GetInnerType(this Type returnType) {
            return IsCollection(returnType)?
            returnType.GetGenericArguments()[0]:
            returnType;
        }

        public static bool IsSimpleValue(this Type type) {
            return type.IsPrimitive||type==typeof(string)||type==typeof(DateTime)||type==typeof(Decimal)||type.IsEnum||(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(Nullable<>));
        }
        
        private static readonly Dictionary<Type,object> defaultValues=new Dictionary<Type,object> { { typeof(string),null } };

        public static object DefaultValue(this Type type) {
            return defaultValues.ContainsKey(type)?defaultValues[type]:Activator.CreateInstance(type);
        }


    } 
}
