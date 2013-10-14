using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions {

    public class FieldsUnusedFromQueryException : Exception
    {
        public IEnumerable<string> UnUsedFields { get; set; }
        public FieldsUnusedFromQueryException(IEnumerable<string> unusedFields)
            : base(string.Format("Fields \"{0}\" from SQL query were not used",
                                 unusedFields.Aggregate((x, y) => string.Format("{0},{1}",
                                                                                x,
                                                                                y))))
        {
            UnUsedFields = unusedFields;
        }
    }
}

