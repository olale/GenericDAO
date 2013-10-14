using System;
using System.Collections.Generic;
using System.Linq;
using CommonExtensions;

namespace Support {
    class StaffingRequirementDayKey {

        public int StaffingRequirementId {get;set;}
        public DateTime Day {get;set;}

        /// <summary>
        /// Initializes a new instance of the StaffingRequirementDayKey class.
        /// </summary>
        public StaffingRequirementDayKey(int staffingRequirementId,DateTime day) {
            StaffingRequirementId=staffingRequirementId;
            Day=day;
        }

        public override bool Equals(object obj) {
            return obj!=null && obj.GetType().Equals(GetType()) && this.ToPropertyString().Equals(obj.ToPropertyString());
        }

        public override int GetHashCode() {
            return this.ToPropertyString().GetHashCode();
        }
    }
}
