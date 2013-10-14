using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Exceptions {

    class UnsupportedParameterException:Exception {
        public string StoredProcedureName {
            get;
            set;
        }
        public SqlParameter Parameter {
            get;
            set;
        }
        public UnsupportedParameterException(string storedProcedureName,SqlParameter param): 
            base(string.Format("{0} has parameter {1} of unsupported type", storedProcedureName,param.ParameterName)) {
            StoredProcedureName=storedProcedureName;
            Parameter=param;
        }
    }
}
