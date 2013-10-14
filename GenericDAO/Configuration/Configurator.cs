using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

using System.Reflection;
using Castle.DynamicProxy;

using Extensions;
using Fasterflect;
using System.ComponentModel;
using System.Drawing;
using Support;
using Exceptions;
using Core;
using System.Data;

namespace Configuration {
    public abstract class Configurator {

        protected internal static IDictionary<Type,Type> proxyTypes=new Dictionary<Type,Type>();
        protected internal static IDictionary<Type,IEnumerable<PropertyInfo>> propertiesForTypes=new Dictionary<Type,IEnumerable<PropertyInfo>>();

        protected internal static ProxyGenerationOptions ProxygenerationOptions {
            get;
            set;
        }

        protected internal static ProxyGenerator ProxyGenerator {
            get;
            set;
        }


        protected internal static Type DbNullType {
            get;
            private set;
        }

        protected internal static IEnumerable<PropertyInfo> GetPropertiesFor<T>() {
            Type entityType=typeof(T);
            if(!propertiesForTypes.ContainsKey(entityType)) {
                propertiesForTypes[entityType]=entityType.GetProperties(BindingFlags.NonPublic|
                                                                          BindingFlags.Public|
                                                                          BindingFlags.Instance);
            }
            return propertiesForTypes[entityType];
        }

        protected internal static IEnumerable<PropertyInfo> GetWritablePropertiesFor<T>() {
            return GetPropertiesFor<T>().Where(prop => prop.CanWrite);
        }

        static Configurator() {
            ProxygenerationOptions=new ProxyGenerationOptions(new LazyLoadingProxyGenerationHook());
            ProxyGenerator=new ProxyGenerator();
            DbNullType=typeof(DBNull);
            TypeConverters=new Dictionary<object,TypeConverter>();
          
        }
        public Core.GenericDAO.ExceptionPolicy Policy {
            get;
            set;
        }


        protected internal bool configured;

        protected internal readonly List<Action<DbCommand,IEnumerable<string>>> initConfigs=new List<Action<DbCommand,IEnumerable<string>>>();


        /// <summary>
        /// Mapping between SP fields and object properties
        /// </summary>
        private Dictionary<string,PropertyInfo> FieldsToPropertiesMap {
            get;
            set;
        }

        /// <summary>
        /// Custom mapping for SP fields.
        /// 
        /// Set before the first SP invocation by configuration methods
        /// </summary>
        public Dictionary<string,PropertyInfo> CustomFieldsToPropertiesMap {
            get;
            set;
        }


        protected internal IEnumerable<string> FieldsUsed {
            get;
            set;
        }

        protected abstract IEnumerable<Configurator> GetRelatedConfigurators();

        public abstract IEnumerable<string> GetAllFieldsUsedBy(IDataReader reader);

        protected abstract IEnumerable<string> GetFieldNamesUsedBy(IDataReader reader);


        /// <summary>
        /// Initializes a new instance of the Configurator class.
        /// </summary>
        public Configurator() {
            CustomFieldsToPropertiesMap=new Dictionary<string,PropertyInfo>();
            Policy=Core.GenericDAO.ExceptionPolicy.None;
        }

        protected internal static Dictionary<object,TypeConverter> TypeConverters {
            get;
            set;
        }

        public static void AddConverter<TType,TConverterType>() {
            TypeDescriptor.AddAttributes(typeof(TType),new TypeConverterAttribute(typeof(TConverterType)));
        }

        private static TypeConverter GetConverter(Type obj) {
            if(!TypeConverters.ContainsKey(obj)) {
                TypeConverters[obj]=TypeDescriptor.GetConverter(obj);
            }
            return TypeConverters[obj];
        }

        private static TypeConverter GetConverter(PropertyInfo obj)
        {
            var propertyDescriptor=TypeDescriptor.GetProperties(obj.DeclaringType).Find(obj.Name,false);
            if (!TypeConverters.ContainsKey(obj))
            {
                TypeConverters[obj] = propertyDescriptor.Converter;
            }
            return TypeConverters[obj];
        }


        public static object GetValue(object sqlValue, Type t)
        {
            return GetValue(sqlValue, t, GetConverter(t));
        }

        public static object GetValue(object sqlValue, PropertyInfo propertyInfo)
        {
            return GetValue(sqlValue, propertyInfo.PropertyType, GetConverter(propertyInfo));
        }

        /// <summary>
        /// Convert a value using a TypeConverter, Enum.Parse or Convert.ChangeType depending on the target type
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        protected internal static object GetValue(object sqlValue, Type propertyType, TypeConverter converter) {
            Type sqlType=sqlValue.GetType();
            object value;
            if(sqlType==DbNullType) {
                // DBNulls are interpreted without conversion
                value=null;
            } else if(propertyType.IsAssignableFrom(sqlType)) {
                // as are values that can be directly assigned to the property
                value=sqlValue;
            } else if(converter.CanConvertFrom(sqlType)) {
                // Use an existing type converter if one exists
                value=converter.ConvertFrom(sqlValue);
            } else if(propertyType.IsEnum) {
                // Or try to parse the string representation of the SQL value if the target property is an Enum
                value=Enum.Parse(propertyType,sqlValue.ToString());
            } else {
                try {
                    // As a last possibility, use Convert to convert to primitive IConvertible classes
                    value=Convert.ChangeType(sqlValue,propertyType);
                } catch {
                    // If all else fails, return null
                    value=null;
                }
            }
            return value;
        }

        protected internal void InitFieldsToPropertiesMap<T>(IDataReader reader,
                                                             IEnumerable<string> allowedFields=null) {
            var properties=GetWritablePropertiesFor<T>();
            var fieldNames=allowedFields??reader.Names();
            var propertyNames=properties.Select(prop => prop.Name);
            var uniquePropertyNames=propertyNames.Distinct();
            if(!uniquePropertyNames.SequenceEqual(propertyNames)) {
                // Abort if there a multiple properties with the same name
                throw new NonUniquePropertyNameException(GetDescription(),propertyNames.Except(uniquePropertyNames));
            }
            FieldsToPropertiesMap=properties.Where(prop => fieldNames.Select(f => f.ToLower()).Contains(prop.Name.ToLower())).ToDictionary(prop => prop.Name);

            if((Policy&
                 Core.GenericDAO.ExceptionPolicy.AbortOnPropertiesUnset)!=0&&
                !propertyNames.IsSubSetOf(fieldNames)) {
                // Abort if properties not set in Entity
                throw new PropertyNotSetByQueryException(propertyNames.Except(fieldNames));
            }
            foreach(var fieldAndProp in CustomFieldsToPropertiesMap)
                FieldsToPropertiesMap[fieldAndProp.Key]=fieldAndProp.Value;
        }

        protected internal Dictionary<string,PropertyInfo> GetFieldsToPropertiesMap<T>(IDataReader reader,
                                                                                        IEnumerable<string> allowedFields=null) {
            if(FieldsToPropertiesMap==null) {
                InitFieldsToPropertiesMap<T>(reader,
                                             allowedFields);
            } else if(allowedFields!=null) {
                return FieldsToPropertiesMap.Where(x => allowedFields.Contains(x.Key)).ToDictionary(x => x.Key,x => x.Value);
            }
            return FieldsToPropertiesMap;
        }

        protected internal T CreateObject<T>(IDataReader reader,
                                             IEnumerable<string> allowedFields=null) where T:class, new() {

            T proxy=ProxyGenerator.CreateClassProxy<T>(ProxygenerationOptions,
                                                         new LazyLoadingInterceptor<T>());

            foreach(var fieldAndProperty in GetFieldsToPropertiesMap<T>(reader,
                                                         allowedFields)) {
                var value=GetValue(reader[fieldAndProperty.Key],
                                     fieldAndProperty.Value);
                if(value!=null) {
                    GetSetter(fieldAndProperty.Value)(proxy,
                                                      value);
                }
            }

            return proxy;
        }

        protected abstract MemberSetter GetSetter(PropertyInfo propertyInfo);
        internal abstract string GetDescription();
    }
}

