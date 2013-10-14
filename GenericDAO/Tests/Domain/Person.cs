using System;
using System.Collections.Generic;
using CommonLib.DomainEntity;
using TcDomainEntity.Common;
using Attributes;
using System.Text.RegularExpressions;

namespace Domain
{
  
    public class Person : TObjectBase
    {
        #region "declaration"
        private string strShortName;
        private string strFullName;
        private string strContactCategoryShortName;
        #endregion

        #region "properties"
        [Key]
        public int PerId
        {
            get { return Id; }
            set { Id = value; } 
        }
        
        /// <summary>
        /// Gets or sets the FirstName of the Person.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the LastName of the Person.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the ShortName of the Person.
        /// </summary>
        public string ShortName
        {
            get
            {
                string str1 = "";
                if (strShortName != "")
                    str1 = this.strShortName;
                else
                {
                    if (this.FirstName.Length > 3)
                        str1 = this.FirstName.Substring(0, 3);
                    else str1 = this.FirstName;

                    if (this.LastName.Length > 3)
                        str1 += this.LastName.Substring(0, 3);
                    else str1 += this.LastName;
                }
                return str1;
            }
            set
            { this.strShortName = value; }
        }

        public string ContactCategoryName {
            get;
            set;
        }

        public string ContactCategoryShortName
        {
            get
            {
                string strShortName = string.Empty;
                if (strContactCategoryShortName != string.Empty && strContactCategoryShortName != null)
                {
                    strShortName = strContactCategoryShortName;
                }
                else
                {
                    strShortName = ContactCategoryName ;
                }
                return strShortName;
            }
            set { this.strContactCategoryShortName = value; }
        }

        [SP("emp_GetEmployment",UseIdList=false),ForeignKey("PerId")]
        public virtual ICollection<Employment> Employments { get;  set; }

        [SP("sch_GetScheduleShiftsOutcomeOnly",UseIdList=false),ForeignKey("PerId")]
        public virtual ICollection<ScheduleShift> OutcomeShifts {get;set;}

        /// <summary>
        /// Gets or sets FullName
        /// </summary>
        public string FullName
        {
            get
            {
                if (strFullName == null || strFullName == "")
                {
                    strFullName=String.Format("{0}, {1}",LastName,FirstName);
                }

                return strFullName;
            }
            set { strFullName = value; }
        }


        public string FullNameWithContactCategoryName
        {
            get
            {
                string strPersonAndCatgoryName = string.Empty;
                if (ContactCategoryName != string.Empty && ContactCategoryName != null)
                {
                    strPersonAndCatgoryName = String.Format("{0} ({1})", FullName, ContactCategoryName);
                }
                else
                {
                    strPersonAndCatgoryName = FullName;
                }
                return strPersonAndCatgoryName;
            }
        }

        /// <summary>
        /// Gets an informal FullName in the format FirstName + " " + LastName
        /// </summary>
        public string FullNameInformal
        {
            get
            {
                return String.Format("{0} {1}", FirstName, LastName);
            }
        }

        /// <summary>
        /// Gets or sets Comments
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Gets or sets FullTimeHours
        /// </summary>
        public float FullTimeHours { get; set; }

        /// <summary>
        /// Gets or sets UserName
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets Person Nr
        /// </summary>
        public string PersonNr { get; set; }

        /// <summary>
        /// Gets or sets EmploymentNumber
        /// </summary>
        public int EmploymentNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Must Change Password
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Wants To Work
        /// </summary>
        public bool WantsToWork { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ShortNoticeOk
        /// </summary>
        public bool ShortNoticeOk { get; set; }

        /// <summary>
        /// Gets or sets LanguageId
        /// </summary>
        public int DefaultLanguage { get; set; }

        /// <summary>
        /// Gets or sets ContactCategoryID
        /// </summary>
        public int ContactCategoryID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Person is enabled.
        /// </summary>
        public bool Active { get; set; }

        public int AuthorizationProfileId { get; set; }


        public string ScheduleNotes { get; set; }

        /// <summary>
        /// Gets or sets the Address of the Person.
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// Gets or sets the PrivateContact of the Person.
        /// </summary>
        public ContactInfo PrivateContact { get; set; }

        public string PrivateContactPhone
        {
            get { return this.PrivateContact.Phone; }
        }
        public string PrivateContactMobile
        {
            get { return this.PrivateContact.Mobile; }
        }
        public string PrivateContactBeeper
        {
            get { return this.PrivateContact.Beeper; }
        }
        public string PrivateContactEmail
        {
            get { return this.PrivateContact.Email; }
        }

        public int CompanyContactId {get;set;}

        public ContactInfo CompanyContact { get; set; }

        public string CompanyContactPhone
        {
            get { return this.CompanyContact.Phone; }
        }
        public string CompanyContactMobile
        {
            get { return this.CompanyContact.Mobile; }
        }
        public string CompanyContactBeeper
        {
            get { return this.CompanyContact.Beeper; }
        }
        public string CompanyContactEmail
        {
            get { return CompanyContact.Email; }
            set {
                if (CompanyContact == null)
                {
                    CompanyContact = new ContactInfo();
                }
                CompanyContact.Email = value; }
        }


        /// <summary>
        /// Gets or sets the SuperUser property of the Person.
        /// </summary>
        public bool SuperUser { get; set; }

        /// <summary>
        /// Gets or sets the MaxNumAttemptsExceeded property of the Person.
        /// </summary>
        public bool MaxNumAttemptsExceeded { get; set; }

        public PersonIntervalSettingCollection IntervalSettings { get; set; }

        public ActivityCollection Activities { get; set; }

        public SkillCollection Skills { get; set; }

        public PersonContractCollection RuleContracts { get; set; }

        #endregion




        public override bool Equals(object obj) {
            return typeof(Person).IsAssignableFrom(obj.GetType()) &&
                   ((Person) obj).Id == Id;
        }

        public override int GetHashCode() {
            return Id;
        }

        public string GetPersonNrWithoutDashes() {
            return Regex.Replace(PersonNr,"-","");
        }

    }

}
