using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attributes {
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=true)]
    public class ParamAttribute:Attribute {

        public string Name {get;set;}
        public object Value {get;set;}


    }
}
