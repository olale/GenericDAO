using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;

namespace Extensions {
    public static class SqlExtensions {
        public static IEnumerable<string> Names(this IDataReader reader) {
            var names=new List<string>();
            for(int i=0;i<reader.FieldCount;i++) {
                names.Add(reader.GetName(i));
            }
            return names;
        }

    }
}
