using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Exceptions {
    public class AttributeMissingException:Exception {
        public AttributeMissingException(string message)
            : base(message,null) {
        }
        public AttributeMissingException() {
        }
        public AttributeMissingException(string message,
                                         Exception innerException)
            : base(message,innerException) {
        }
        protected AttributeMissingException(SerializationInfo info,
                                            StreamingContext context)
            : base(info,context) {
        }
    }
}

