using Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Collections.Generic;
using Moq;
using FluentAssertions;
namespace Tests
{
    
    
    /// <summary>
    ///This is a test class for SPConfiguratorTest and is intended
    ///to contain all SPConfiguratorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SPConfiguratorTest {

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get;
            set;
        }


        private static Mock<IDataReader> GetReaderMock() {
            var readerMock=new Mock<IDataReader>();
            readerMock.Setup(x => x.FieldCount).Returns(3);
            readerMock.Setup(x => x.GetName(It.Is<int>(i => i==0))).Returns("PerId");
            readerMock.Setup(x => x.GetName(It.Is<int>(i => i==1))).Returns("FirstName");
            readerMock.Setup(x => x.GetName(It.Is<int>(i => i==2))).Returns("LastName");
            return readerMock;
        }

        /// <summary>
        ///A test for GetFieldNamesUsedBy
        ///</summary>
        public static void GetFieldNamesUsedByTestHelper<T>()
            where T:class , new() {
            SPConfigurator_Accessor<T> target=new SPConfigurator_Accessor<T>();
            Mock<IDataReader> readerMock=GetReaderMock();
            IEnumerable<string> expected=new List<string>() { "FirstName","LastName","PerId" };
            IEnumerable<string> actual=target.GetFieldNamesUsedBy(readerMock.Object);
            actual.Should().BeEquivalentTo(expected);

        }

        [TestMethod()]
        [DeploymentItem("GenericDAO.dll")]
        public void GetFieldNamesUsedByTest() {
            GetFieldNamesUsedByTestHelper<Person>();
        }
    }
}
