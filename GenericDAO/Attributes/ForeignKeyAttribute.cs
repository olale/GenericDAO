using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attributes {

    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ForeignKeyAttribute: Attribute {

        /// <summary>
        /// Name of the foreign key property in this class
        /// </summary>
        public string Name {get;set;
        }

        public ForeignKeyAttribute() {
        }

        /// <summary>
        /// Initializes a new instance of the ForeignKeyAttribute class.
        /// </summary>
        public ForeignKeyAttribute(string name) {
            Name=name;
        }

    }
}
