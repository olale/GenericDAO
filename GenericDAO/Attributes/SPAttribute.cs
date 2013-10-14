using System;
using System.Collections.Generic;
using System.Linq;

namespace Attributes {
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class SPAttribute:Attribute {
        public string Name {
            get;
            set;
        }
        public bool UseIdList {
            get;
            set;
        }
        /// <summary>
        /// Initializes a new instance of the SPAttribute class.
        /// </summary>
        public SPAttribute(string name) {
            Name=name;
            UseIdList=false;
        }


    }
}
