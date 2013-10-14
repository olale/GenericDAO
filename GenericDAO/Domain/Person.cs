using System;
using Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
	public class Person: DomainObject
	{

		public String Name { get; set;}
		public String Age { get; set;}

		[SP("\"emp_GetEmployment\"")]
		[ForeignKey("PersonId")]
		public virtual ICollection<Employment> Employments { get; set;}

		public override string ToString ()
		{
			return string.Format ("[Person: Name={0}, Age={1}]", Name, Age);
		}

		public string EmploymentDescription() {
			return Employments.Aggregate ("", (str,e) => string.IsNullOrEmpty (str) ? e.ToString () : string.Format ("{0},{1}", str, e.ToString ()));
		}

	}
}

