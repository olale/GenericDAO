using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Exceptions {

    public class MultipleValuesForPropertyException:Exception {

        public string P {
            get;
            set;
        }
        public Type Type {
            get;
            set;
        }
        public IList CollectionForObject {
            get;
            set;
        }
        public MultipleValuesForPropertyException(Type type,string p,IList collectionForObject)
            : base(string.Format("Multiple values not allowed for property {0} in class {1}",p,type)) {
            Type=type;
            P=p;
            CollectionForObject=collectionForObject;
        }


        public MultipleValuesForPropertyException(Type type,string p,IList collectionForObject,object obj)
            : base(string.Format(@"Multiple values ""{0}"" not allowed for property {1} of object {2}",collectionForObject,p,obj)) {
            Type=type;
            P=p;
            CollectionForObject=collectionForObject;

        }

    }
}
