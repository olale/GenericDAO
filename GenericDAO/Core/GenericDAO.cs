using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Configuration;
using Extensions;
using Concurrent;
using System.Threading.Tasks;
using Support.NpgSql;
using Support;

using Exceptions;

namespace Core {
    public enum ReturnType {
        Static,
        Dynamic
    }

    public class GenericDAO {
        [Flags]
        public enum ExceptionPolicy {
            None=0x0,
            AbortOnFieldsUnused=0x1,
            AbortOnPropertiesUnset=0x2,
            AbortOnDuplicateFields=0x4
        }

        [Flags]
        public enum FetchRelatedObjectsPolicy {
            Off=0x0,
            // Scan for all objects retrieved in a single SP call as identified by the fields returned by the SP
            ScanFields=0x1,
            // Scan for all objects that can be retrieved through calls to SP:s available from attributes decorating object properties
            UsePrefetch=0x2,
            On=ScanFields|UsePrefetch
        }

        protected internal static IDictionary<Type,string> prefixMap=new Dictionary<Type,string>();
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
        /// <summary>
        /// Initializes the data converters and prefix mappings. Generated from domain files and SP names.
        /// 
        /// Converters may be needed if no conversion is possible
        /// </summary>
        static GenericDAO() {
            AddPrefix<SingleValueSPConfigurator.SingleValue>("");
            ProxygenerationOptions=new ProxyGenerationOptions(new LazyLoadingProxyGenerationHook());
            ProxyGenerator=new ProxyGenerator();
            DbNullType=typeof(DBNull);

        }


        public static void AddPrefix<TType>(string prefix) {
            AddPrefix<TType>(prefix,"_");
        }

        public static void AddPrefix<TType>(string prefix,string padding) {
            prefixMap[typeof(TType)]=String.Format("{0}{1}",prefix,padding);
        }


        protected internal static object StoredProcedureNamePrefixFor<T>() {
            if(prefixMap.ContainsKey(typeof(T))) {
                return prefixMap[typeof(T)];
            } else {
                throw new Exception(string.Format("Type {0} not mapped in prefix dictionary, please provide prefix for stored procedure names",typeof(T)));
            }
        }

        public static Type ToClrType(SqlDbType sqlType) {
            switch(sqlType) {
            case SqlDbType.BigInt:
            return typeof(long?);

            case SqlDbType.Binary:
            case SqlDbType.Image:
            case SqlDbType.Timestamp:
            case SqlDbType.VarBinary:
            return typeof(byte[]);

            case SqlDbType.Bit:
            return typeof(bool?);

            case SqlDbType.Char:
            case SqlDbType.NChar:
            case SqlDbType.NText:
            case SqlDbType.NVarChar:
            case SqlDbType.Text:
            case SqlDbType.VarChar:
            case SqlDbType.Xml:
            return typeof(string);

            case SqlDbType.DateTime:
            case SqlDbType.SmallDateTime:
            case SqlDbType.Date:
            case SqlDbType.Time:
            case SqlDbType.DateTime2:
            return typeof(DateTime?);

            case SqlDbType.Decimal:
            case SqlDbType.Money:
            case SqlDbType.SmallMoney:
            return typeof(decimal?);

            case SqlDbType.Float:
            return typeof(double?);

            case SqlDbType.Int:
            return typeof(int?);

            case SqlDbType.Real:
            return typeof(float?);

            case SqlDbType.UniqueIdentifier:
            return typeof(Guid?);

            case SqlDbType.SmallInt:
            return typeof(short?);

            case SqlDbType.TinyInt:
            return typeof(byte?);

            case SqlDbType.Variant:
            case SqlDbType.Udt:
            return typeof(object);

            case SqlDbType.Structured:
            return typeof(DataTable);

            case SqlDbType.DateTimeOffset:
            return typeof(DateTimeOffset?);

            default:
            throw new ArgumentOutOfRangeException(string.Format("sqlType: {0}",sqlType));
            }
        }

    }

    public class GenericDAO<T>:GenericDAO where T:class,new() {


        #region Dispatch

        public static Dispatcher<T> Dispatch() {
            return new Dispatcher<T>(ReturnType.Static);
        }

        public static Dispatcher<T> Dispatch(ExceptionPolicy policy) {
            return Dispatch(policy,ReturnType.Static);
        }

        public static Dispatcher<T> Dispatch(ExceptionPolicy policy,ReturnType returnType) {
            return Dispatch(policy,returnType,Int32.MaxValue);
        }


        /// <summary>
        /// Return a dynamic object that can be used to dispatch calls to SP:s
        /// 
        /// </summary>
        /// <param name="returnType">indicates whether objects of type T or dynamic objects are requested</param>
        /// <param name="limit">indicates the number of results wanted</param>
        /// <param name="policy">indicates if exceptions should be thrown when creating a statically typed objects.</param>
        /// <returns></returns>
        public static Dispatcher<T> Dispatch(ExceptionPolicy policy,ReturnType returnType,int limit) {
            return new Dispatcher<T>(policy,returnType,limit);
        }

        public static Dispatcher<T> Dispatch(GenericDAO.FetchRelatedObjectsPolicy scanPolicy) {
            return new Dispatcher<T>(scanPolicy);
        }

        #endregion

        public static void AddPrefix(string prefix) {
            GenericDAO.AddPrefix<T>(prefix);
        }

        /// <summary>
        /// Configurations for SQL stored procedures, such as mapping between fields and properties, and inclusion of related objects
        /// </summary>
        protected readonly static Dictionary<string,SPConfigurator<T>> configurators=new Dictionary<string,SPConfigurator<T>>();

        protected internal static SPConfigurator<T> GetConfig(string storedProcedureName) {
            return Configure(storedProcedureName) as SPConfigurator<T>;
        }

        public static ISPConfigurator<T> Configure(string storedProcedureName) {
            if(!configurators.ContainsKey(storedProcedureName)) {
                SPConfigurator<T> c=new SPConfigurator<T>(storedProcedureName);
                configurators[storedProcedureName]=c;
            }
            return configurators[storedProcedureName];
        }

        /// <summary>
        /// Internal use only. Replace previous configuration by providing the new config for the named SP
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static ISPConfigurator<T> Configure(string storedProcedureName,SPConfigurator<T> c) {
            configurators[storedProcedureName]=c;
            return c;
        }

        /// <summary>
        /// Retrieve the SP prefix in the dictionary
        /// </summary>
        /// <returns></returns>
        internal static string StoredProcedureNamePrefix() {
            if(prefixMap.ContainsKey(typeof(T))) {
                return prefixMap[typeof(T)];
            } else {
                throw new Exception(string.Format("Type {0} not mapped in prefix dictionary, please provide prefix for stored procedure names",typeof(T)));
            }
        }

        /// <summary>
        /// Hide the default constructor so no objects can be created by mistake.
        /// </summary>
        protected GenericDAO() {

        }

        private static void AddArgument(DbCommand command,string name,object value) {
            command.Parameters.Add(new Npgsql.NpgsqlParameter(String.Format("@{0}",
                                                          name),
                                            value));
        }

        private static DbCommand BuildCommand(string procedureName,object entity=null) {
            var command=DBAccess.GetCommand(procedureName);
            // Add parameters from the values of the value type properties of entity
            if(entity!=null) {
                // Dynamic objects implement IDictionary<,>
                if(typeof(IDictionary<string,object>).IsAssignableFrom(entity.GetType())) {
                    foreach(var entry in (IDictionary<string,object>)entity) {
                        AddArgument(command,entry.Key,entry.Value);
                    }

                } else {
                    // Other objects have value type properties
                    foreach(PropertyInfo property in entity.GetPrimitiveTypeProperties(true)) {
                        AddArgument(command,property.Name,property.GetValue(entity,null));
                    }
                }
            }
            return command;
        }


        internal static string StoredProcedureFullName(string storedProcedureName) {
            return string.Format("{0}{1}",StoredProcedureNamePrefix(),storedProcedureName);
        }

        /// <summary>
        /// If no SP name is given, we assume the convention <PREFIX>_<OPERATION><TYPE>, where <TYPE> is the name of the generic type argument to GenericDAO.
        /// </summary>
        /// <returns></returns>
        /// <param name="procedureType"></param>
        private static string StoredProcedureFullNameForOperation(string procedureType) {
            return StoredProcedureFullName(string.Format("{0}{1}",procedureType,typeof(T).Name));
        }

        private static string GetSaveMethodName() {
            return StoredProcedureFullNameForOperation("Save");
        }

        private static string GetDeleteMethodName()
        {
            return StoredProcedureFullNameForOperation("Delete");
        }

        protected internal static object CreateDynamicObject(IDataReader reader) {
            var entity=new ExpandoObject();
            var dict=(IDictionary<string,object>)entity;
            for(int i=0;i<reader.FieldCount;i++) {
                dict[reader.GetName(i)]=reader.GetValue(i);
            }
            return entity;
        }


        private static ICollection<T> GetEntities(string procedureName,object parameterObject,
                                                  int limit) {
            int recordsRead=0;
            var config=GetConfig(procedureName);
            IList<T> entities=new ConcurrentList<T>();
            try {
                using(DbCommand command=BuildCommand(procedureName,parameterObject)) {
                    using(IDataReader reader=command.ExecuteReader()) {
                        var fieldNames=reader.Names();
                        config.Init(command,fieldNames);
                        var allFieldsUsed=config.GetAllFieldsUsedBy(reader);
                        if((config.Policy&
                             GenericDAO.ExceptionPolicy.AbortOnFieldsUnused)!=0&&
                            !fieldNames.IsSubSetOf(allFieldsUsed)) {
                            // Abort if some SQL result fields are unused
                            throw new FieldsUnusedFromQueryException(fieldNames.Except(allFieldsUsed));
                        }
                        // Add a mapping from the SP to the properties it can set on the current object
                        while(reader.Read()&&++recordsRead<limit) {
                            entities.Add(config.InjectFrom(reader));
                        }
                    }
                }
                entities=config.Process(entities);
            } finally {
                DBAccess.CloseConnection();
            }
            return entities;
        }

        private static IEnumerable<object> GetObjectEnumeratorFromReader(IDataReader reader,int limit) {
            int recordsRead=0;
            while(reader.Read()&&++recordsRead<limit) {
                yield return CreateDynamicObject(reader);
            }
            yield break;
        }

        private static ICollection<object> GetDynamicEntities(string procedureName,object parameterObject,int limit) {
            var returnObjects=new ConcurrentList<object>();
            try {
                using(var command=BuildCommand(procedureName,parameterObject)) {
                    using(IDataReader reader=command.ExecuteReader()) {
                        var objects=GetObjectEnumeratorFromReader(reader,limit);
                        Parallel.ForEach(objects,
                                         (obj) => {
                                             returnObjects.Add(obj);
                                         });
                    }
                }
            } finally {
                DBAccess.CloseConnection();
            }
            return returnObjects;
        }

        /// <summary>
        /// Call this from an overloaded Get-method to invoke the SP denoted by the caller
        /// </summary>
        /// <param name="parameterObject"></param>
        /// <returns></returns>
        protected static ICollection<T> GetInternal(object parameterObject) {
            return Get(StoredProcedureFullName(new StackTrace().GetFrame(1).GetMethod().Name),
                               parameterObject);
        }

        internal static object ApplyInternal(string procedureName,
                                             object parameterObject=null,
                                             Type returnType=null) {
            object result=returnType!=null?Activator.CreateInstance(returnType):null;
            bool isValueType=returnType!=null&&
                               returnType.IsValueType;
            try {
                using(DbCommand command=BuildCommand(procedureName,
                                                         parameterObject)) {
                    if(isValueType) {
                        result=Convert.ChangeType(command.ExecuteScalar(),returnType);
                    } else {
                        command.ExecuteNonQuery();
                    }
                }
            } finally {
                DBAccess.CloseConnection();
            }
            return result;
        }

        /// <summary>
        /// Call this method from an overloaded method to invoke the SP denoted by the method in the caller
        /// </summary>
        /// <param name="parameterObject"></param>
        protected static object ApplyInternal(object parameterObject=null,Type returnType=null) {
            return ApplyInternal(StoredProcedureFullName(new StackTrace().GetFrame(1).GetMethod().Name),
                          parameterObject,returnType);
        }

        public static void Apply(string procedureName, object parameterObject = null)
        {
            ApplyInternal(procedureName, parameterObject);
        }
        
        public static R Apply<R>(string procedureName, object parameterObject=null) {
            return (R)ApplyInternal(procedureName,parameterObject,typeof(R));
        }


        /// <summary>
        /// Saves the given object using an SP named PREFIX_SaveT with suitable values for PREFIX and T
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="extraParam"></param>
        public static void Save(T obj,object extraParam=null) {
            string saveMethodName=GetSaveMethodName();
            ApplyInternal(saveMethodName,GetConfig(saveMethodName).CreateParameterObject(obj,extraParam));
        }

        /// <summary>
        /// Deletes the given object using an SP named PREFIX_DeleteT with suitable values for PREFIX and T
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="extraParam"></param>
        public static void Delete(T obj, object extraParam = null)
        {
            string methodName = GetDeleteMethodName();
            ApplyInternal(methodName, GetConfig(methodName).CreateParameterObject(obj, extraParam));
        }


        #region public Get-methods

        public static ICollection<T> Get() {
			return Get(StoredProcedureFullNameForOperation ("Get"));
        }

        public static ICollection<T> Get(string storedProcedureName) {
            return Get(storedProcedureName,
                       null);
        }


        public static ICollection<T> Get(string storedProcedureName,
                                         object parameterObject) {
            return Get(storedProcedureName,
                       parameterObject,
                       Int32.MaxValue);
        }


        /// <summary>
        /// Unless specified, we assume that there is a Stored Procedure called <code>PREFIX</code>_Get<code>T</code>
        /// where T is the type parameter for this class. 
        /// 
        /// All parameter names are taken from the names of the value type properties of T, 
        /// unless there is a mapper in this class in which case it is used.
        /// </summary>
        /// <param name="storedProcedureName">The SP to invoke. Inferred from the type parameter T by default</param>
        /// <param name="parameterObject">The object that contains all parameters and values necessary for the SP as properties</param>
        /// <returns></returns>
        public static ICollection<T> Get(string storedProcedureName,
                                         object parameterObject,
            int limit) {
            string procedureName=storedProcedureName??StoredProcedureFullNameForOperation("Get");

            return GetEntities(procedureName,parameterObject,
                                                          limit);

        }

        public static T First(string storedProcedureName,
                      object parameterObject) {
            return Get(storedProcedureName, parameterObject).FirstOrDefault();
        }
        #endregion

        #region dynamic Get-methods
        public static ICollection<object> GetObjects(string storedProcedureName) {
            return GetObjects(storedProcedureName,
                              null);
        }
        public static ICollection<object> GetObjects(string storedProcedureName,
                                 object parameterObject) {
            return GetObjects(storedProcedureName,
                              parameterObject,
                              Int32.MaxValue);
        }

        public static ICollection<object> GetObjects(string storedProcedureName,
                                 object parameterObject,int limit) {
            string procedureName=storedProcedureName??StoredProcedureFullNameForOperation("Get");
            return GetDynamicEntities(procedureName,parameterObject,limit);
        }
        #endregion


    }
}