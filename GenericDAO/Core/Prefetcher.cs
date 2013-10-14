using System;
using System.Collections.Generic;
using System.Linq;

using Extensions;
using System.Reflection;
using Fasterflect;
using System.Dynamic;
using Attributes;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Concurrent;
using System.Collections;
using Exceptions;
using Support;
using Core;
using Configuration;

namespace Core {
    public class Prefetcher<T>:BaseInterceptor where T:class,new() {

        protected internal static readonly IDictionary<string,MemberGetter> foreignKeyGetters=new ConcurrentDictionary<string,MemberGetter>();

        private static MemberGetter InitForeignKeyGetter(PropertyInfo prop) {
            PropertyInfo foreignKeyProperty=GetForeignKeyProperty<T>(prop);
            var foreignKeyGetter=foreignKeyProperty.DelegateForGetPropertyValue();
            return foreignKeyGetter;
        }

        private static MemberGetter GetForeignKeyGetter(PropertyInfo prop) {
            if(!foreignKeyGetters.ContainsKey(prop.Name)) {
                foreignKeyGetters[prop.Name]=InitForeignKeyGetter(prop);
            }
            return foreignKeyGetters[prop.Name];
        }

        public static string GetIdString(IEnumerable<T> objects,PropertyInfo prop) {
            MemberGetter foreignKeyGetter=GetForeignKeyGetter(prop);
            // Create a string of the ForeignKey values, comma-separated
            Func<string,object,string> concatStrings=
                (str,x) => string.IsNullOrEmpty(str)?
                x.ToString():
                string.Format("{0},{1}",str,x);
            return objects
                .Select(obj => foreignKeyGetter(obj))
                .Distinct()
                .Aggregate("",concatStrings);
        }

        /// <summary>
        /// Add an object as parameter with the ID:s of each individual object in the collection (unless UseIdList is set to false in the Prefetch attribute), 
        /// and possibly additional parameters
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        internal static IDictionary<string,object> GetParamObjectForPrefetch(IEnumerable<T> objects,PropertyInfo prop) {
            var paramObject=new ExpandoObject() as IDictionary<string,object>;
            if(prop.Attribute<SPAttribute>().UseIdList) {
                // Retrieve the foreign key indices from the objects
                paramObject["IdList"]=GetIdString(objects,prop);
            }
            var paramAttributes=prop.Attributes<ParamAttribute>();
            if(paramAttributes!=default(ParamAttribute)&&paramAttributes.Any()) {
                // Add parameters from the paramAttributes collection
                foreach(var param in paramAttributes) {
                    paramObject[param.Name]=param.Value;
                }
            }
            return paramObject;
        }

        private readonly static ConcurrentDictionary<Type,Type> relatedObjectsListTypes=new ConcurrentDictionary<Type,Type>();

        private static Type GetRelatedObjectListType(Type relatedObjectType) {
            if(!relatedObjectsListTypes.ContainsKey(relatedObjectType)) {
                relatedObjectsListTypes[relatedObjectType]=typeof(ConcurrentList<>).MakeGenericType(relatedObjectType);
            }
            return relatedObjectsListTypes[relatedObjectType];
        }

        /// <summary>
        /// For a collection of objects, group the returned related objects into groups based on the foreign key property of the related objects. 
        /// This is used to assign objects to collections of other objects later
        /// </summary>
        /// <param name="relatedObjects"></param>
        /// <param name="foreignKeyPropInRelatedClass"></param>
        /// <returns></returns>
        private static IDictionary<object,IList> GetGroupedCollection(Type relatedPropertyType,IEnumerable relatedObjects,Func<object,object> getKey) {
            var groupedCollection=new ConcurrentDictionary<object,IList>();
            foreach(var relatedObject in relatedObjects) {
                var keyValue=getKey(relatedObject);
                if(!groupedCollection.ContainsKey(keyValue)) {
                    groupedCollection[keyValue]=(IList)Activator.CreateInstance(GetRelatedObjectListType(relatedPropertyType));
                }
                groupedCollection[keyValue].Add(relatedObject);
            }
            return groupedCollection;
        }

        private static Action<T> GetSimpleValueUpdater(IEnumerable<T> objects,PropertyInfo prop) {
            var prefetchSPAttribute=prop.Attribute<SPAttribute>();
            var paramObject=GetParamObjectForPrefetch(objects,prop);
            var relatedValueSetter=SPConfigurator<T>.GetStaticSetter(prop);
            var getKey=GetForeignKeyGetter(prop);
            // Target we can obtain as SingleValue object and convert using Configurator.GetValue
            GenericDAO<SingleValueSPConfigurator.SingleValue>.Configure(prefetchSPAttribute.Name,new SingleValueSPConfigurator(prefetchSPAttribute.Name,prop.PropertyType));
            var relatedSingleValues=GenericDAO<SingleValueSPConfigurator.SingleValue>.Get(prefetchSPAttribute.Name,paramObject);
            return (obj) => {
                var relatedValueObjects=relatedSingleValues.Where(val => val.MasterId.Equals(getKey(obj))).ToList();
                if(relatedValueObjects.Count()>1) {
                    throw new MultipleValuesForPropertyException(typeof(T),prop.Name,relatedValueObjects,obj);
                }
                var relatedValueObject=relatedValueObjects.FirstOrDefault();
                // Only set the property if the current object is matched by the returned results
                if(relatedValueObject!=default(SingleValueSPConfigurator.SingleValue)&&relatedValueObject.Value!=null) {
                    relatedValueSetter(obj,relatedValueObject.Value);
                }
            };
        }
        private static Action<T> GetCompositeObjectsUpdater(IEnumerable<T> objects,PropertyInfo prop) {
            var propertyType=prop.PropertyType;
            var prefetchSPAttribute=prop.Attribute<SPAttribute>();
            var getKey=propertyType.IsCollection()?
                // Is the property type is a collection, then use the key property of class T
BaseInterceptor.GetKeyProperty<T>().DelegateForGetPropertyValue():
                // Otherwise, we assume that the key is the foreign key
GetForeignKeyGetter(prop);
            var propType=propertyType.GetInnerType();
            var relatedObjects=BaseInterceptor.InvokeDAOGetCollection(propType,prefetchSPAttribute.Name,GetParamObjectForPrefetch(objects,
                                   prop)) as ICollection;

            // The key in class T to group objects on should have the same name as a property in class propType if we access a collection, otherwise it should be the key property
            PropertyInfo foreignKeyPropInRelatedClass=default(PropertyInfo);
            if(propertyType.IsCollection()) {
				var foreignKey = prop.Attribute<ForeignKeyAttribute> ();

                if(foreignKey==default(Attribute)) {
                    throw new AttributeMissingException(string.Format("Missing ForeignKey attribute for property {0} in class {1}",
                                             prop.Name,
                                             typeof(T).Name));
                }

				string foreignKeyName=foreignKey.Name;
                foreignKeyPropInRelatedClass=propType.GetProperty(foreignKeyName);
                if(foreignKeyPropInRelatedClass==default(PropertyInfo)) {
                    // No key property found so throw an exception
                    throw new PropertyMissingException(propType,foreignKeyName);
                }
            } else {
                foreignKeyPropInRelatedClass=BaseInterceptor.GetKeyProperty(propType);
                if(foreignKeyPropInRelatedClass==default(PropertyInfo)) {
                    // No key property found so throw an exception
                    throw new PropertyMissingException(propType);
                }
            }
            var relatedObjectKeyGetter = foreignKeyPropInRelatedClass.DelegateForGetPropertyValue();
            return GetUpdater(prop, x=>relatedObjectKeyGetter(x), x=> getKey(x), relatedObjects);
        }


        private static Action<T> GetCompositeObjectsUpdaterFromClassMapping(IEnumerable<T> objects,PropertyInfo prop) {
            var propertyType=prop.PropertyType;
            var mapByAttribute=prop.Attribute<MapByAttribute>();
            var classMapping=Activator.CreateInstance(mapByAttribute.Mapper,mapByAttribute.StoredProcedureName) as ClassMapping;

            var propType=propertyType.GetInnerType();
            var relatedObjects=BaseInterceptor.InvokeDAOGetCollection(propType,classMapping.StoredProcedureName,classMapping.GetParamObject(objects.Cast<object>())) as ICollection;
            return GetUpdater(prop,x => classMapping.GetKeyForRelatedObject(x),x => classMapping.GetKeyForMainObject(x),relatedObjects);
        }

        private static Action<T> GetUpdater(PropertyInfo prop,Func<object,object> relatedObjectKeyGetter,Func<T,object> keyGetter,ICollection relatedObjects) {
            var propertyType=prop.PropertyType;
            var propType=propertyType.GetInnerType();
            // The returned collection should be grouped by the foreign key value.
            // We have to do this manually as the Linq GroupBy extension needs compile-time constant types

            IDictionary<object,IList> groupedCollection=GetGroupedCollection(propType,relatedObjects,relatedObjectKeyGetter);
            var relatedValueSetter=SPConfigurator<T>.GetStaticSetter(prop);
            return (obj) => {
                object key=keyGetter(obj);
                if(key!=null&&groupedCollection.ContainsKey(key)) {
                    var collectionForObject=groupedCollection[key];
                    if(propertyType.IsCollection()) {
                        relatedValueSetter(obj,collectionForObject);
                    } else if(collectionForObject.Count>1) {
                        throw new MultipleValuesForPropertyException(typeof(T),prop.Name,collectionForObject,obj);
                    } else {
                        relatedValueSetter(obj,collectionForObject[0]);
                    }
                }
            };
        }

        /// <summary>
        /// For a collection of objects "objects" and a property "prop", fetch values for that property for each element in "objects" by making a single SP 
        /// invocation, where the invocation and results are inferred from the type of the property.
        /// 
        ///
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="prop"></param>
        /// <returns>An update action that can be used in parallel with others to set the values retrieved from the SP:s on all objects in the collection</returns>
        public static Action<T> GetUpdateObjectsAction(IEnumerable<T> objects,PropertyInfo prop) {
            if(prop.Attribute<SPAttribute>()!=default(SPAttribute)&&prop.PropertyType.IsSimpleValue()) {
                return GetSimpleValueUpdater(objects,prop);
            } else if(prop.Attribute<SPAttribute>()!=default(SPAttribute)) {
                return GetCompositeObjectsUpdater(objects,prop);
            } else if(prop.Attribute<MapByAttribute>()!=default(MapByAttribute)&&!prop.PropertyType.IsSimpleValue()) {
                return GetCompositeObjectsUpdaterFromClassMapping(objects,prop);
            } else if(prop.Attribute<MapByAttribute>()!=default(MapByAttribute)&&prop.PropertyType.IsSimpleValue()) {
                throw new ArgumentException(string.Format(@"MapBy attribute not allowed for simple value property {0} in class {1}",prop.Name,typeof(T).Name));
            } else {
                throw new AttributeMissingException(string.Format("Missing SP/MapBy attribute for property {0} in class {1}",
                                                        prop.Name,
                                                        typeof(T).Name));
            }
        }

        /// <summary>
        /// Use parallel or serial processing to fetch related objects, depending on the collection passed
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="relatedProperties"></param>
        /// <returns></returns>
        protected internal static IEnumerable<T> FetchRelated(IEnumerable<T> objects,IEnumerable<PropertyInfo> relatedProperties) {

            Type type=objects.GetType();
            if(type.IsGenericType&&typeof(ConcurrentList<>).IsAssignableFrom(type.GetGenericTypeDefinition())) {
                // Only use parallel processing for collections that allow synchronized access
                Parallel.ForEach(relatedProperties,
                     prop => {
                         var updater=GetUpdateObjectsAction(objects,prop);
                         foreach(var obj in objects) {
                             updater.Invoke(obj);
                         }
                     });

            } else {
                foreach(var prop in relatedProperties) {
                    var updater=GetUpdateObjectsAction(objects,
                                         prop);
                    foreach(var obj in objects) {
                        updater.Invoke(obj);
                    }
                }
            }
            return objects;
        }


        public static IEnumerable<T> FetchRelatedProperty(IEnumerable<T> objects,PropertyInfo prop) {
            return FetchRelated(objects,new List<PropertyInfo>() { prop });
        }

        /// <summary>
        /// For a collection "objects" of class T and a set of expressions selecting instance properties of T,
        /// invoke one stored procedure to fetch the values for each property for all "objects".
        /// 
        /// For N objects and M propertySelector expressions, there will be M invocations of stored procedures.
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="propertySelectors"></param>
        /// <returns></returns>
        public static IEnumerable<T> FetchRelated(IEnumerable<T> objects,Expression<Func<T,object>>[] propertySelectors) {
            var relatedProperties=new List<PropertyInfo>();
            foreach(var propertySelector in propertySelectors) {
                PropertyInfo prop=null;
                if(typeof(MemberExpression).IsAssignableFrom(propertySelector.Body.GetType())) {
                    prop=(PropertyInfo)((MemberExpression)propertySelector.Body).Member;
                } else {
                    // For boolean properties and possibly others, the body is considered a unary expression
                    prop=(PropertyInfo)((MemberExpression)((UnaryExpression)propertySelector.Body).Operand).Member;
                }
                relatedProperties.Add(prop);
            }
            return FetchRelated(objects,relatedProperties);
        }

        /// <summary>
        /// Fetch all instance properties of T marked with the attribute PrefetchSP for the collection "objects".
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static IEnumerable<T> FetchAllRelated(IEnumerable<T> objects) {
            var prefetchProperties=typeof(T)
                .GetProperties(BindingFlags.Instance|BindingFlags.Public)
                .Where(prop => prop.Attribute<SPAttribute>()!=default(SPAttribute));
            return FetchRelated(objects,prefetchProperties);
        }

    }
}
