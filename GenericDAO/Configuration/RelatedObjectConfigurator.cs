using System;
using System.Collections.Generic;
using System.Linq;
using Fasterflect;
using System.Linq.Expressions;
using System.Reflection;
using Exceptions;
using System.Data;

namespace Configuration {
    public class RelatedObjectConfigurator<T,T1>:Configurator,IRelatedObjectConfigurator<T,T1>
        where T:class, new()
        where T1:class, new() {
        public Action<object,IDataReader,IEnumerable<string>> Config {
            get;
            set;
        }

        /// <summary>
        /// Map fields in 
        /// </summary>
        /// <param name="t">the target object</param>
        /// <param name="o">the source object</param>
        /// <param name="t1">the type of the related object</param>
        /// <param name="setter">the delegate for the property setter method in type T for target object t</param>
        private void AddRelatedObject(object t,
                                             IDataReader reader,
                                             MemberSetter setter,IEnumerable<string> allowedFields) {
            var relatedObject=CreateObject<T1>(reader,allowedFields);
            setter.Invoke(t,
                          relatedObject);
        }

        private static void TooManyProperties(IEnumerable<PropertyInfo> properties) {
            throw new NonUniquePropertyTypeException(typeof(T),typeof(T1),properties);
        }

        public RelatedObjectConfigurator(IEnumerable<PropertyInfo> properties) {
            if(properties.Count()>1) {
                // Create a config action that will throw an exception at runtime if there are several properties of the specified type
                Config=(t,reader,fields) => TooManyProperties(properties);
            } else {
                var setter=properties.First().DelegateForSetPropertyValue();
                Config=(t,reader,fields) => AddRelatedObject(t,reader,
                                        setter,fields);
            }
        }


        /// <summary>
        /// specify the property to assign objects of type T to
        /// </summary>
        /// <param name="propertySelector"></param>
        /// <returns></returns>
        public void Through(Expression<Func<T,T1>> propertySelector) {
            var selectorExpression=(MemberExpression)propertySelector.Body;
            var prop=(PropertyInfo)selectorExpression.Member;
            var setter=prop.DelegateForSetPropertyValue();
            Config=(t,reader,allowedFields) => AddRelatedObject(t,reader,setter,allowedFields);
        }

        protected override IEnumerable<Configurator> GetRelatedConfigurators() {
            return new List<Configurator>();
        }

        public override IEnumerable<string> GetAllFieldsUsedBy(IDataReader reader) {
            return GetFieldsToPropertiesMap<T1>(reader).Select(x => x.Key);
        }

        protected override IEnumerable<string> GetFieldNamesUsedBy(IDataReader reader) {
            return GetAllFieldsUsedBy(reader);
        }

        public IEnumerable<string> GetFieldNamesUsedByThisRelatedObject(IDataReader reader) {
            return GetFieldNamesUsedBy(reader);
        }

        public void By(Action<IRelatedObjectConfigurator<T, T1>> config)
        {
            config.Invoke(this);
        }

        public void UsingClassNameId() {
            CustomFieldsToPropertiesMap[typeof(T1).Name+
                                        "Id"]=typeof(T1).GetProperty("Id");

        }

        public FieldMappingConfigurator<T1> Map(string fieldName) {
            return new FieldMappingConfigurator<T1>(fieldName,this);
        }

        protected internal static readonly Dictionary<string,MemberSetter> setters=new Dictionary<string,MemberSetter>();

        protected override MemberSetter GetSetter(PropertyInfo prop) {
            if(!setters.ContainsKey(prop.Name)) {
                setters[prop.Name]=prop.DelegateForSetPropertyValue();
            }
            return setters[prop.Name];
        }

        internal override string GetDescription() {
            return string.Format(@"{0} => {1}",typeof(T),typeof(T1));
        }
    }
}
