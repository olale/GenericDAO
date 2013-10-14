using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using Attributes;
using System.Collections.Concurrent;
using Exceptions;
using Fasterflect;
using Core;

namespace Support {
    public class BaseInterceptor {

        private static ConcurrentDictionary<Type,PropertyInfo> KeyProperties {
            get;
            set;
        }

        protected internal static PropertyInfo GetKeyProperty<T>() {
            Type targetType=typeof(T);
            return GetKeyProperty(targetType);
        }

        protected internal static PropertyInfo GetKeyProperty(Type targetType) {
            if(!KeyProperties.ContainsKey(targetType)) {
                PropertyInfo[] targetTypeProperties=targetType.GetProperties();
                KeyProperties[targetType]=targetTypeProperties
                    .FirstOrDefault(prop => Attribute.IsDefined(prop,typeof(KeyAttribute)))
                    ??
                    targetTypeProperties
                    .First(prop => prop.Name=="id");
            }
            return KeyProperties[targetType];
        }

        static BaseInterceptor() {
            KeyProperties=new ConcurrentDictionary<Type,PropertyInfo>();
        }

        /// <summary>
        /// For a property "prop" that should be used for prefetch, extract the property in T that should act as a foreign key, 
        /// identifying the value(s) that relate to the object(s) for which we prefetch property values
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        protected internal static PropertyInfo GetForeignKeyProperty<T>(PropertyInfo prop) {
            PropertyInfo[] allProperties=typeof(T).GetProperties(BindingFlags.Instance|BindingFlags.Public);
            var foreignKeyAttribute=prop.Attribute<ForeignKeyAttribute>();
            PropertyInfo foreignKeyProperty=default(PropertyInfo);
            if(foreignKeyAttribute!=default(ForeignKeyAttribute)) {
                // Use the attribute by the name given from the ForeignKey attribute
                foreignKeyProperty=allProperties.FirstOrDefault(p => p.Name==foreignKeyAttribute.Name);
            } else {
                foreignKeyProperty=
                    // or use the convention PropertyName+Id (ignoring case)
                allProperties.FirstOrDefault(p => p.Name.Equals(prop.Name+"Id",StringComparison.CurrentCultureIgnoreCase))??
                    // As a last resort, try to use the primary key property, if there is one
                GetKeyProperty<T>();

            }
            if(foreignKeyProperty==default(PropertyInfo)) {
                throw new IllegalPropertyException(String.Format("No valid foreign key property found in {0} for {1}",typeof(T),prop.Name));
            }
            return foreignKeyProperty;
        }


        public static object GetDAOValue(Type returnType,
                                         string storedProcedureName,
                                         object parameterObject,
                                         string daoMethodName) {

            // Create the GenericDAO<T> type for the required return type
            var DAOType=typeof(GenericDAO<>).MakeGenericType(returnType);
            return DAOType.InvokeMember(daoMethodName,
                                          BindingFlags.InvokeMethod|
                                          BindingFlags.Static|
                                          BindingFlags.Public|
                                          BindingFlags.FlattenHierarchy,
                            null,
                            DAOType,
                            new object[]
                                    {
                                        storedProcedureName,
                                        parameterObject
                                    });

        }

        protected internal static object GetParameterObject(string keyParameter,object keyValue) {
            var parameterObject=new ExpandoObject();
            var parameters=parameterObject as IDictionary<string,object>;
            parameters[keyParameter]=keyValue;
            return parameterObject;
        }

        public static object GetDAOValue(Type returnType,
                                         string storedProcedureName,
                                         string keyParameter,
                                         object keyValue,
                                         string daoMethodName) {
            return GetDAOValue(returnType,
                       storedProcedureName,
                       GetParameterObject(keyParameter,keyValue),
                       daoMethodName);
        }

        public static object InvokeDAOGetCollection(Type returnType,
                                                       string storedProcedureName,
                                                       object parameterObject) {
            return GetDAOValue(returnType,storedProcedureName,parameterObject,"Get");
        }


        public static object InvokeDAOGetSingle(Type returnType,
                                                   string storedProcedureName,
                                                   object parameterObject) {
            return GetDAOValue(returnType,storedProcedureName,parameterObject,"First");
        }


    }
}

