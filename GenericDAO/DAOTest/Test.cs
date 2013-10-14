using System;
using NUnit.Framework;
using Core;
using Domain;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace DAOTest
{
	[TestFixture()]
	public class Test
	{
		[TestFixtureSetUp]
		public static void Setup() {
		
			GenericDAO<Person>.AddPrefix ("per");
			GenericDAO<Employment>.AddPrefix ("emp");

		}

		[Test()]
		public void TestCase ()
		{

			new List<int> () { 1, 2, 3 }.Select (x => x +2);
			var people = GenericDAO<Person>.Get("\"per_GetPerson\"");
			people.Should ().NotBeEmpty ();
			people.First ().Employments.Should ().NotBeNull ();
			people.ToList ().ForEach(p => Console.WriteLine(string.Format("Person: {0}, Employments: {1}",p, p.EmploymentDescription())));
		}
	}
}

