using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions {

    public class PropertyNotSetByQueryException : Exception
    {
        public IEnumerable<string> UnSetProperties { get; set; }
        public PropertyNotSetByQueryException(IEnumerable<string> unsetProperties)
            : base(string.Format("Properties \"{0}\" were not returned by the query",
                                 unsetProperties.Aggregate((x, y) => string.Format("{0},{1}",
                                                                                   x,
                                                                                   y))))
        {
            UnSetProperties = unsetProperties;
        }
    }
}

