using System;
using System.Collections.Generic;
using System.Linq;
using Support;

namespace Configuration {
    public interface IConfigurator<T> where T: class, new()
    {
        FieldMappingConfigurator<T> Map(string fieldName);

        /// <summary>
        /// Maps the field typeof(T).Name+"Id" to the property "Id"
        /// </T>
        /// <returns></returns>
        /// </T></summary>
        void UsingClassNameId();

    }
}
