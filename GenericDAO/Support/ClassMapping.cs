using System;
using System.Collections.Generic;
using System.Linq;

namespace Support {
    public abstract class ClassMapping {

        public string StoredProcedureName {get;set;}

        /// <summary>
        /// Associated the class mapping the an SQL stored procedure
        /// </summary>
        public ClassMapping(string storedProcedureName) {
            StoredProcedureName=storedProcedureName;
        }

        /// <summary>
        /// Given a set of objects, extract the parameter object that can be supplied to the stored procedure 
        /// </summary>
        /// <param name="objects">the objects for which related objects are to be fetched</param>
        /// <returns>the arguments to supply to the stored procedure used when fetching related objects</returns>
        public abstract object GetParamObject(IEnumerable<object> objects);

        /// <summary>
        /// Provide a key object for a related object, with appropriate Equals and GetHashCode implementations
        /// </summary>
        /// <param name="relatedObject"></param>
        /// <returns></returns>
        public abstract object GetKeyForRelatedObject(object relatedObject);

        /// <summary>
        /// Provide a key object for a main object, with appropriate Equals and GetHashCode implementations
        /// </summary>
        /// <param name="mainObject"></param>
        /// <returns></returns>
        public abstract object GetKeyForMainObject(object mainObject);

    }
}
