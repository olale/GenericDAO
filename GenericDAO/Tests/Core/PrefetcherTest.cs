using Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tests
{


    /// <summary>
    ///This is a test class for PrefetcherTest and is intended
    ///to contain all PrefetcherTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PrefetcherTest
    {

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestMethod]
        public void GetIdStringTest1()
        {
           var people = new List<Person>() {
                    new Person(){
                        PerId=1
                    },
                    new Person(){
                        PerId=2
                    }
            };
            var idString = Prefetcher<Person>.GetIdString(people, typeof(Person).GetProperty("Employments"));
            idString.Should().BeEquivalentTo("1,2", "The primary key values of two people should be used to fetch employments");
        }

        [TestMethod]
        public void GetIdStringTest2()
        {
            var shifts = new List<ScheduleShift>() {
                    new ScheduleShift(){
                        ScheduleShiftId=1,
                        RequestShiftId=14
                    },
                    new ScheduleShift(){
                        ScheduleShiftId=2,
                        RequestShiftId=14
                    }
            };
            var idString = Prefetcher<ScheduleShift>.GetIdString(shifts, typeof(ScheduleShift).GetProperty("RequestShift"));
            idString.Should().BeEquivalentTo("14",
@"for two shifts sharing the same RequestShift reference, only a single Id should be returned, using the convention ""Property.Name"" + ""Id""");
        }



    }
}
