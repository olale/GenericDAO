using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions {

    class IllegalPropertyException:Exception {
        public IllegalPropertyException(string message): base(message) {
        }
    }
}
