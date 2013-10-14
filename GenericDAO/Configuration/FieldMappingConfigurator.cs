using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace Configuration {
    public class FieldMappingConfigurator<T> where T: class, new()
    {
        private Configurator Configurator { get; set; }
        private string FieldName { get; set; }
        internal FieldMappingConfigurator(string fieldName,
                                         Configurator configurator)
        {
            Configurator = configurator;
            FieldName = fieldName;
        }

        public void To<T1>(Expression<Func<T,T1>> propertySelector)
        {
            var selectorExpression = (MemberExpression) propertySelector.Body;
            var prop = (PropertyInfo) selectorExpression.Member;
            Configurator.CustomFieldsToPropertiesMap[FieldName]=prop;
        }
    }
}
