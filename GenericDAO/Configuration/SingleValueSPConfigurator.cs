using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Configuration {
    internal class SingleValueSPConfigurator: SPConfigurator<SingleValueSPConfigurator.SingleValue> {

        internal class SingleValue {
            public int MasterId {
                get;
                set;
            }
            public object Value {
                get;
                set;
            }
        }

        /// <summary>
        /// The type that Value in SingleValue should be converted to
        /// </summary>
        public Type PropertyType {
            get;
            set;
        }

        public SingleValueSPConfigurator(string name,Type propertyType): base(name) {
            PropertyType=propertyType;
        }

        /// <summary>
        /// We assume that objects returned that are single values have two fields: MasterId and Value
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public override SingleValue InjectFrom(IDataReader reader) {
            return new SingleValue() {
                MasterId=(int)GetValue(reader["MasterId"],typeof(Int32)),
                Value = GetValue(reader["Value"],PropertyType)
            };
        }

    }
}
