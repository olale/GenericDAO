using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Exceptions;
using Core;

namespace Tests
{
    [TestClass]
    public class DAOTest : IntegrationTestBase
    {

        static DAOTest()
        {
            GenericDAO.AddPrefix<Domain.Person>("per");
            GenericDAO.AddPrefix<ScheduleShift>("sch");
            GenericDAO.AddPrefix<Employment>("emp");
            GenericDAO.AddPrefix<ContactInfo>("ci");

        }

        [TestMethod]
        public void TestGetPeopleByRoleUsingSPNameAsString()
        {
            var people = GenericDAO<Domain.Person>.Get("per_GetPersonsByRole", new { RoleId = 3 });
            people.Should().NotBeEmpty();
        }

        [TestMethod]
        public void TestGetPeopleByRoleUsingDynamicDispatch()
        {
            dynamic dao = GenericDAO<Domain.Person>.Dispatch();
            IEnumerable<Domain.Person> people = dao.GetPersonsByRole(RoleId: 3);
            people.Should().NotBeEmpty();
        }


        /// <summary>
        /// We can say that the return value of an SP that answers a question should be bool
        /// </summary>
        [TestMethod]
        public void TestDynamicApply()
        {
            dynamic dispatch = GenericDAO<ScheduleShift>.Dispatch();
            bool inUse = dispatch.IsActivityInUse<bool>(ActivityId: 1)
                && dispatch.IsActivityInUse<int>(new { ActivityId = 1 }) == 1;
            inUse.Should().BeTrue();
        }


        /// <summary>
        /// We can verify that an SP returns only fields that are used
        /// </summary>
        [TestMethod]
        public void TestAbortOnFieldsUnused()
        {
          dynamic dao = GenericDAO<Domain.Person>.Dispatch(GenericDAO.ExceptionPolicy.AbortOnFieldsUnused);
          IEnumerable<Domain.Person> people = null;
          Action peopleFetchAction = () => people = dao.GetPersonsByOrgNodeWithEmployment();
          peopleFetchAction.ShouldThrow<FieldsUnusedFromQueryException>("some fields of the SP per_GetPersonsByOrgNodeWithEmployment are not assigned to properties in Person");
        }

        /// <summary>
        /// We can include master objects and compound property objects by specifying how to map fields to properties in related objects. 
        /// If there are no ambiguities and every field name can be mapped to a property in one of the related objects, no mapping is necessary.
        /// </summary>
        [TestMethod]
        public void TestIncludeRelatedObjects()
        {
            GenericDAO<Domain.Person>.Configure("per_GetPersonsByOrgNodeWithEmployment").By(x => {
                x.Map("PerId").To(p => p.Id);
                x.Map("ShortNoticeOK").To(p => p.ShortNoticeOk);
                x.Include<ContactInfo>().By(y =>
                {
                    y.Map("ContactPrivateId").To(c => c.Id);

                    y.Map("Phone").To(c => c.Phone);
                    y.Map("ContactPhone").To(c => c.ContactPhone);
                    y.Map("ShowPhone").To(c => c.ShowPhone);

                    y.Map("Mobile").To(c => c.Mobile);
                    y.Map("ContactMobile").To(c => c.ContactMobile);
                    y.Map("ShowMobile").To(c => c.ShowMobile);

                    y.Map("Email").To(c => c.Email);
                    y.Map("ContactEmail").To(c => c.ContactEmail);
                    y.Map("ShowEmail").To(c => c.ShowEmail);
                    y.Map("URL").To(c => c.Url);

                    y.Through(p => p.PrivateContact);
                });
                x.Include<ContactInfo>().By(y =>
                {
                    y.Map("ContactCompanyId").To(c => c.Id);

                    y.Map("CompanyPhone").To(c => c.Phone);
                    y.Map("CompanyContactPhone").To(c => c.ContactPhone);
                    y.Map("CompanyShowPhone").To(c => c.ShowPhone);

                    y.Map("CompanyMobile").To(c => c.Mobile);
                    y.Map("CompanyContactMobile").To(c => c.ContactMobile);
                    y.Map("CompanyShowMobile").To(c => c.ShowMobile);

                    y.Map("CompanyEmail").To(c => c.Email);
                    y.Map("CompanyContactEmail").To(c => c.ContactEmail);
                    y.Map("CompanyShowEmail").To(c => c.ShowEmail);

                    y.Map("CompanyBeeper").To(c => c.Beeper);
                    y.Map("CompanyContactBeeper").To(c => c.ContactBeeper);
                    y.Map("CompanyShowBeeper").To(c => c.ShowBeeper);

                    y.Map("CompanyFax").To(c => c.Fax);

                    y.Map("CompanyUrl").To(c => c.Url);
                    y.Through(p => p.CompanyContact);
                });
                x.Include<Address>().By(y =>
                {
                    y.UsingClassNameId();
                });
                x.ScanForRelatedTypes();
            
            });
            dynamic dao = GenericDAO<Domain.Person>.Dispatch();
            IEnumerable<Domain.Person> people = dao.GetPersonsByOrgNodeWithEmployment();
            // All users should have related properties according to the mapping above
            people.Should().OnlyContain(p => p.PrivateContact != null && p.CompanyContact != null && p.Address != null);
            // All ordinary users (Id > 2) should also have Beeper numbers set for instance. 
            // CompanyContactBeeper in Domain.Person is a property that delegates lookup to the CompanyContact property object
            people.Where(p => p.Id > 2).Should().OnlyContain(p => !string.IsNullOrEmpty(p.CompanyContactBeeper));
        }

        /// <summary>
        /// Some people should have outcome shifts, fetched via a single SP invocation
        /// </summary>
        [TestMethod]
        public void TestUsePrefetch() {
            dynamic dao=GenericDAO<Domain.Person>.Dispatch(GenericDAO.FetchRelatedObjectsPolicy.UsePrefetch);

            IEnumerable<Domain.Person> people = null;
            Action fetchWithPrefetch = () =>
            {
                people = dao.GetPersonsByRole(RoleId: 2);
                people.Count(p => p.OutcomeShifts != null && p.Employments != null);
            };
            dynamic daoNoPrefetch = GenericDAO<Domain.Person>.Dispatch(GenericDAO.FetchRelatedObjectsPolicy.Off);
            Action fetchWithoutPrefetch = () =>
            {
                people = daoNoPrefetch.GetPersonsByRole(RoleId: 2);
                people.Count(p => p.OutcomeShifts != null && p.Employments != null);
            };
            DateTime t = DateTime.Now;
            fetchWithoutPrefetch.Invoke();
            DateTime t2 = DateTime.Now;
            fetchWithPrefetch.ExecutionTime().ShouldNotExceed(t2-t);
        }

        





    }
}
