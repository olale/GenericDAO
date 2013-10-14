using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Support {
    internal class StaffingRequirementDayActivityMapping: ClassMapping {

        /// <summary>
        /// Initializes a new instance of the StaffingRequirementDayActivityMapping class.
        /// </summary>
        public StaffingRequirementDayActivityMapping(string storedProcedureName): base(storedProcedureName) {
            
        }

        public override object GetParamObject(IEnumerable<object> objects) {
            throw new NotImplementedException();
        }

        public override object GetKeyForRelatedObject(object relatedObject) {
            throw new NotImplementedException();
        }

        public override object GetKeyForMainObject(object mainObject) {
            throw new NotImplementedException();
        }
    }
}
