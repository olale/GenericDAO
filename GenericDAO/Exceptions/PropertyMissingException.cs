using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions {

    public class PropertyMissingException:Exception {
        public PropertyMissingException(Type t,string propName)
            : base(string.Format("Property named {0} missing in {1}",
                              propName,
                              t)) {
            T=t;
        }

        public PropertyMissingException(Type t,
                                        Type relatedType)
            : base(string.Format("Property of type {0} missing in {1}",
                                 relatedType,
                                 t)) {
            T=t;
            RelatedType=relatedType;
        }


        /// <summary>
        /// Missing key property in class t
        /// </summary>
        public PropertyMissingException(Type t)
            : base(string.Format("Key property missing in class {0}",t)) {
            T=t;
        }

        public Type T {
            get;
            set;
        }
        public Type RelatedType {
            get;
            set;
        }
    }
}

