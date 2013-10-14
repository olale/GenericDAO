using System;
using System.Collections.Generic;
using System.Linq;

namespace Setup {
    public static class SPSettings {
        
        /// <summary>
        /// This method illustrates some configuration possibilities available for stored procedures.
        /// 
        /// To discover fields unused from stored procedure invocations, use the exception policy AbortOnFieldsUnused
        /// 
        /// [TestMethod]
        /// public void TestGetPeopleInOutcomeOnlyUsesAllFields() {
        ///     dynamic dao=GenericDAO<ScheduleShift>.Dispatch(GenericDAO.ExceptionPolicy.AbortOnFieldsUnused);
        ///     IEnumerable<ScheduleShift> shiftsInOutcome=null;
        ///     Action selectShiftsInOutcomeAction=() => shiftsInOutcome=dao.GetScheduleShiftsOutcomeOnly();
        ///     selectShiftsInOutcomeAction.ShouldThrow<FieldsUnusedFromQueryException>("some fields of the SP sch_GetScheduleShiftsOutcomeOnly are not assigned to properties in ScheduleShift");
        /// }
        /// </summary>
        public static void Setup() {
            /*
               GenericDAO.AddPrefix<IntegrationOrgNodeSettingValue>("int");
                GenericDAO<Employment>.Configure("emp_GetEmployment").UsingClassNameId();
                GenericDAO<Person>.Configure("per_GetPersonsByRole").By(x => {
                    x.Map("PerId").To(p => p.Id);
                    x.Include<ContactInfo>().By(y => {
                        y.Map("ContactPrivateId").To(c => c.Id);
                        y.Map("Mobile").To(c => c.Mobile);
                        y.Map("Email").To(c => c.Email);
                        y.Through(p => p.PrivateContact);
                    });
                    x.Include<ContactInfo>().By(y => {
                        y.Map("ContactCompanyId").To(c => c.Id);
                        y.Map("CompanyMobile").To(c => c.Mobile);
                        y.Map("CompanyEmail").To(c => c.Email);
                        y.Through(p => p.CompanyContact);
                    });
                    x.Include<Address>();
                });

                GenericDAO<ReportedShift>.Configure("shf_GetAttestedShifts")
                    .Map("ReportedWorkShiftId").To(s => s.Id);

                GenericDAO<ReportedOnCallInterrupt>.Configure("shf_GetReportedOnCallShiftInterrupt")
                    .Map("ReportedOnCallShiftId").To(i => i.ReportedWorkShiftId);

                GenericDAO<ScheduleShift>.Configure("sch_GetScheduleShifts").By(x => {
                    x.UsingClassNameId();
                });

                GenericDAO<RequestShift>.Configure("sch_GetRequestShifts").UsingClassNameId();

                GenericDAO.AddPrefix<AuditLogData>("logging",".");
                GenericDAO.AddPrefix<LogEvent>("logging",".");

                GenericDAO<LogEvent>.Configure("logging.GetLogEvents").By(x => {
                    x.Map("LogId").To(o => o.Id);
                });

                GenericDAO<Activity>.Configure("act_GetAssociateDoubleBookableActivities").By(x => {
                    x.ScanForRelatedTypes(GenericDAO.FetchRelatedObjectsPolicy.ScanFields);
                });

                GenericDAO<ProgramSetting>.Configure("ps_GetProgramSetting").By(x => {
                    x.Map("SettingId").To(s => s.Id);
                    x.Map("Data").To(s => s.DataValue);
                    x.Map("TableDescr").To(s => s.TableDescription);
                });

                GenericDAO<PartialLeave>.Configure("emp_GetPartialLeave").By(x => {
                    x.UsingClassNameId();
                });
             */

        }
    }
}
