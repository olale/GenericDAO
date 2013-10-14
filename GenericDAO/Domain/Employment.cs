using System;
using Attributes;

namespace Domain
{
	public class Employment: DomainObject
	{
		[SP("\"per_GetPerson\"")]
		public virtual Person Person { get; set;}
		public int PersonId { get; set;}
		public DateTime StartDate { get; set;}
		public DateTime EndDate { get; set;}
		public String Description { get; set;}

		public override string ToString ()
		{
			return string.Format ("[Employment for {0}: StartDate={2}, EndDate={3}, Description='{4}']", Person, PersonId, StartDate, EndDate, Description);
		}
	}
}

