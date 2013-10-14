using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Exceptions {

    public class NonUniquePropertyTypeException : Exception
    {
        public NonUniquePropertyTypeException(Type t,
                                              Type relatedType,
                                              IEnumerable<PropertyInfo>
                                              propertiesOfTypeT1): base(string.Format("{0} contains more than one property of type {1}: {2}",
                                                                                      t,
                                                                                      relatedType,
                                                                                      propertiesOfTypeT1))
        {
            T = t;
            RelatedType = relatedType;
            PropertiesOfTypeT1 = propertiesOfTypeT1;
        }
        public Type T { get; set; }
        public Type RelatedType { get; set; }
        public IEnumerable<PropertyInfo> PropertiesOfTypeT1 { get; set; }
    }
}

