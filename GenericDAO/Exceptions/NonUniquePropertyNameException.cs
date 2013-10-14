using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions {

    public class NonUniquePropertyNameException:Exception {
        public NonUniquePropertyNameException(string message,IEnumerable<string> nonUniqueProperties)
            : base(string.Format(@"Non-unique properties ""{0}"": {1}",nonUniqueProperties,message)) {
            NonUniqueProperties=nonUniqueProperties;
        }

        public IEnumerable<string> NonUniqueProperties {
            get;
            set;
        }
    }
}
