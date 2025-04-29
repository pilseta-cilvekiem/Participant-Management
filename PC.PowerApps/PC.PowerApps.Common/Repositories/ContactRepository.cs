using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Entities.Fetch;
using PC.PowerApps.Common.Extensions;
using PC.PowerApps.Common.FetchXmlBuilder;
using PC.PowerApps.Common.ScheduledJobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PC.PowerApps.Common.Repositories
{
    public static class ContactRepository
    {
        private static readonly Guid debtReminderEmailTemplateId = new("f190362f-cdf3-eb11-94ef-002248834145");
        private static readonly Guid memberWelcomeEmailTemplateId = new("6e1e1045-7492-ec11-b400-000d3a2b1acc");
        private static readonly Guid supporterWelcomeEmailTemplateId = new("227d1abb-95f6-eb11-94ef-002248834145");

        internal static void Recalculate(Context context, bool participantTill, bool participationLevel, bool requiredParticipationFee)
        {
            IQueryable<Contact> contacts = context.ServiceContext.ContactSet
                .Select(c => new Contact
                {
                    ContactId = c.ContactId,
                    pc_ParticipantTill = c.pc_ParticipantTill,
                    pc_ParticipationLevel = c.pc_ParticipationLevel,
                    pc_RequiredParticipationFee = c.pc_RequiredParticipationFee,
                });
            SyncActionQueue.ExecuteForAll(context, contacts, contact => Recalculate(context, contact, participantTill, participationLevel, requiredParticipationFee));
        }

        public static bool IsValidForGoogleMemberGroup(Contact contact)
        {
            bool isValidForGoogleMemberGroup = contact != null && CommonConstants.IsValidForGoogleMemberGroupFunc(contact);
            return isValidForGoogleMemberGroup;
        }

        public static void Recalculate(Context context, Guid? contactId, bool participantTill, bool participationLevel, bool requiredParticipationFee)
        {
            if (contactId == null)
            {
                return;
            }

            Contact contact = context.ServiceContext.Retrieve<Contact>(contactId.Value);
            Recalculate(context, contact, participantTill, participationLevel, requiredParticipationFee);
        }

        public static void ScheduleRecalculate(Context context, bool participantTill, bool participationLevel, bool requiredParticipationFee)
        {
            RecalculateContact recalculateContact = new()
            {
                Context = context,
                ParticipantTill = participantTill,
                ParticipationLevel = participationLevel,
                RequiredParticipationFee = requiredParticipationFee
            };
            recalculateContact.Schedule(allowDuplicates: false);
        }

        private static void Recalculate(Context context, Contact contact, bool participantTill, bool participationLevel, bool requiredParticipationFee)
        {
            if (participantTill)
            {
                UpdateParticipantTill(context, contact);
            }

            if (participationLevel)
            {
                UpdateParticipationLevel(context, contact);
            }

            if (requiredParticipationFee)
            {
                UpdateRequiredParticipationFee(context, contact);
            }

            _ = context.ServiceContext.UpdateModifiedAttributes(contact);
        }

        public static void SendWelcomeEmail(Context context, Contact contact)
        {
            Guid emailTemplateId;
            pc_EmailCategory emailCategory;

            switch (contact.pc_ParticipationLevel)
            {
                case pc_ParticipationLevel.Member:
                    if (contact.pc_SentMemberWelcomeEmailOn != null)
                    {
                        return;
                    }

                    emailCategory = pc_EmailCategory.MemberWelcomeEmail;
                    emailTemplateId = memberWelcomeEmailTemplateId;
                    break;

                case pc_ParticipationLevel.Supporter:
                    if (contact.pc_SentSupporterWelcomeEmailOn != null)
                    {
                        return;
                    }

                    emailCategory = pc_EmailCategory.SupporterWelcomeEmail;
                    emailTemplateId = supporterWelcomeEmailTemplateId;
                    break;

                default:
                    return;
            }

            pc_Participation participation = context.ServiceContext.pc_ParticipationSet
                .Where(p => p.pc_Contact.Id == contact.Id && p.pc_Level == contact.pc_ParticipationLevel)
                .OrderByDescending(p => p.pc_From)
                .TakeSingle();
            EntityReference contactReference = contact.ToEntityReference();
            Email email = Utils.CreateEmailFromTemplate(context, emailTemplateId, contactReference);
            string supporterFrom = participation.GetFormattedValue(p => p.pc_From);
            email.Description = email.Description.Replace("{!ParticipationFrom;}", supporterFrom);
            email.pc_Category = emailCategory;
            email.RegardingObjectId = contactReference;
            email.To = new List<ActivityParty> { new() { PartyId = contactReference } };
            context.OrganizationService.CreateWithoutNulls(email);
            Utils.SendEmail(context, email);
        }

        public static void CalculatePaidParticipationFee(Context context, Guid? contactId)
        {
            if (contactId == null)
            {
                return;
            }

            context.Logger.LogInformation($"Calculating a Paid Participation Fee for the Contact {contactId.Value}.");
            Contact contact = context.ServiceContext.Retrieve<Contact>(contactId.Value);
            const string AmountAlias = "amount";
            FetchXmlBuilder<pc_Payment> paymentQuery = new FetchXmlBuilder<pc_Payment>(aggregate: true)
                .AddAttribute(p => p.pc_Amount, AmountAlias, AggregateType.sum)
                .AddFilter(filterType.and, f => f
                    .AddCondition(p => p.pc_Contact, @operator.eq, contactId.Value)
                );
            pc_Payment aggregate = context.OrganizationService.RetrieveMultiple(paymentQuery).TakeSingle();
            contact.pc_PaidParticipationFee = aggregate.GetAliasedValue<Money>(AmountAlias);
            _ = context.ServiceContext.UpdateModifiedAttributes(contact);
        }

        public static void SetSentEmailOn(Context context, Email email)
        {
            if (email.StatusCode != Email_StatusCode.PendingSend)
            {
                return;
            }

            switch (email.pc_Category)
            {
                case pc_EmailCategory.DebtReminder:
                    Contact debtReminderContact = context.ServiceContext.Retrieve<Contact>(email.RegardingObjectId);
                    debtReminderContact.pc_SentDebtReminderOn = email.ModifiedOn;
                    _ = context.ServiceContext.UpdateModifiedAttributes(debtReminderContact);
                    break;

                case pc_EmailCategory.MemberWelcomeEmail:
                    Contact memberWelcomeEmailContact = context.ServiceContext.Retrieve<Contact>(email.RegardingObjectId);
                    memberWelcomeEmailContact.pc_SentMemberWelcomeEmailOn = email.ModifiedOn;
                    _ = context.ServiceContext.UpdateModifiedAttributes(memberWelcomeEmailContact);
                    break;

                case pc_EmailCategory.SupporterWelcomeEmail:
                    Contact supporterWelcomeEmailContact = context.ServiceContext.Retrieve<Contact>(email.RegardingObjectId);
                    supporterWelcomeEmailContact.pc_SentSupporterWelcomeEmailOn = email.ModifiedOn;
                    _ = context.ServiceContext.UpdateModifiedAttributes(supporterWelcomeEmailContact);
                    break;
            }
        }

        private static void UpdateParticipationLevel(Context context, Contact contact)
        {
            context.Logger.LogInformation($"Calculating Participation Level for the Contact {contact.Id}...");
            DateTime localNow = context.GetCurrentOrganizationTime();
            pc_Participation participation = context.ServiceContext.pc_ParticipationSet
                .Where(p => p.pc_Contact.Id == contact.Id && p.pc_From <= localNow.Date && (p.pc_Till == null || p.pc_Till >= localNow.Date))
                .FirstOrDefault();
            contact.pc_ParticipationLevel = participation?.pc_Level;
        }

        private static void UpdateParticipantTill(Context context, Contact contact)
        {
            context.Logger.LogInformation($"Calculating Participant Till for the Contact {contact.Id}...");
            DateTime localNow = context.GetCurrentOrganizationTime();
            pc_Participation lastParticipation = context.ServiceContext.pc_ParticipationSet
                .Where(p => p.pc_Contact.Id == contact.Id)
                .OrderByDescending(p => p.pc_From)
                .FirstOrDefault();
            contact.pc_ParticipantTill = lastParticipation?.pc_Till;
        }

        public static void UpdateRequiredParticipationFee(Context context, Contact contact)
        {
            DateTime toDate = context.GetCurrentOrganizationTime().GetFirstDayOfMonth().AddDays(-1);
            Period calculationPeriod = new(null, toDate);
            context.Logger.LogInformation($"Calculating a Required Participation Fee for the Contact {contact.Id}...");
            List<Period> participations = context.ServiceContext.pc_ParticipationSet
                .Where(p => p.pc_Contact.Id == contact.Id && p.pc_From <= toDate)
                .OrderBy(p => p.pc_From)
                .Select(p => new Period(p.pc_From, p.pc_Till))
                .ToList();
            List<Period> participationFeeExemptions = context.ServiceContext.pc_ParticipationFeeExemptionSet
                .Where(pfe => pfe.pc_Contact.Id == contact.Id && pfe.pc_From <= toDate)
                .OrderBy(pfe => pfe.pc_From)
                .Select(pfe => new Period(pfe.pc_From, pfe.pc_Till))
                .ToList();

            List<Period> participationsToDate = Period.Intersect(participations, calculationPeriod);
            List<Period> mergedParticipations = Period.Merge(participationsToDate);
            List<Period> mergedParticipationFirstMonths = mergedParticipations
                .Select(p => p.GetFirstCalendarMonth())
                .ToList();
            List<Period> mergedParticipationFeeExemptions = Period.Merge(participationFeeExemptions);
            List<Period> participationsWithoutFeeExemptions = Period.Subtract(mergedParticipations, mergedParticipationFeeExemptions);
            List<Period> participationsWithoutFeeExemptionsWholeMonths = participationsWithoutFeeExemptions
                .Select(p => p.ToCalendarMonths())
                .ToList();
            List<Period> mergedParticipationsWithoutFeeExemptionsWholeMonths = Period.Merge(participationsWithoutFeeExemptionsWholeMonths);
            List<Period> mergedParticipationsWithoutFeeExemptionsAndFirstMonthsWholeMonths =
                Period.Subtract(mergedParticipationsWithoutFeeExemptionsWholeMonths, mergedParticipationFirstMonths);

            contact.pc_RequiredParticipationFee = new();
            foreach (pc_ParticipationFeeRule participationFeeRule in context.ParticipationFeeRules)
            {
                Period participationFeePeriod = new(participationFeeRule.pc_From, participationFeeRule.pc_Till);
                List<Period> participationsToUse = participationFeeRule.pc_ApplyToFirstMonth == true
                    ? mergedParticipationsWithoutFeeExemptionsWholeMonths
                    : mergedParticipationsWithoutFeeExemptionsAndFirstMonthsWholeMonths;
                List<Period> participationFeePeriods = Period.Intersect(participationsToUse, participationFeePeriod);
                List<int> participationFeePeriodLengths = participationFeePeriods
                    .Select(p => p.CalendarMonths)
                    .ToList();
                int contactParticipationFeePeriodTotalLength = participationFeePeriodLengths.Sum();
                contact.pc_RequiredParticipationFee.Value += contactParticipationFeePeriodTotalLength * Utils.GetAmountOrZero(participationFeeRule.pc_Amount);
            }
        }

        public static void SendDebtReminder(Context context, Guid contactId)
        {
            context.EnsureAttributesNotEmpty(context.Settings, s => new { s.pc_EmailSender });
            Contact contact = context.ServiceContext.Retrieve<Contact>(contactId);

            if (contact.pc_Debt == null || contact.pc_Debt.Value <= 0)
            {
                return;
            }

            EntityReference contactReference = contact.ToEntityReference();
            Email email = Utils.CreateEmailFromTemplate(context, debtReminderEmailTemplateId, contactReference);
            email.pc_Category = pc_EmailCategory.DebtReminder;
            email.RegardingObjectId = contactReference;
            email.To = new List<ActivityParty> { new() { PartyId = contactReference } };
            context.OrganizationService.CreateWithoutNulls(email);
            Utils.SendEmail(context, email);
        }

        public static void ScheduleSynchronizeGoogleParticipantGroupMembers(Context context, bool members, bool supporters)
        {
            if ((!members || string.IsNullOrEmpty(context.Settings.pc_GoogleMemberGroup)) &&
                (!supporters || string.IsNullOrEmpty(context.Settings.pc_GoogleSupporterGroup)))
            {
                return;
            }

            SynchronizeGoogleParticipantGroupMembersBase synchronizeGoogleParticipantGroupMembersBase = new()
            {
                Context = context,
                Members = members,
                Supporters = supporters,
            };
            synchronizeGoogleParticipantGroupMembersBase.Schedule(allowDuplicates: false);
        }

        public static void SetDefaults(Contact contact)
        {
            contact.pc_PaidParticipationFee ??= new Money();
        }

        public static void ClearParticipantInfo(Contact contact)
        {
            if (contact.StatusCode == Contact_StatusCode.Active)
            {
                return;
            }

            contact.Address1_City = null;
            contact.Address1_Country = null;
            contact.Address1_County = null;
            contact.Address1_Line1 = null;
            contact.Address1_PostalCode = null;
            contact.Address1_StateOrProvince = null;
            contact.Description = "Bijušais dalībnieks.";
            contact.EMailAddress1 = null;
            contact.MobilePhone = null;
            contact.pc_CalledOn = null;
            contact.pc_CallResults = null;
            contact.pc_ForceAddToGoogleGroup = false;
            contact.pc_Neighbourhood = null;
            contact.pc_WelcomeMeetingStatus = null;
            contact.pc_WishesToBeActive = false;
            contact.pc_WillCall = null;
        }

        public static bool IsValidForGoogleSupporterGroup(Contact contact)
        {
            bool isValidForGoogleSupporterGroup = contact != null && CommonConstants.IsValidForGoogleSupporterGroupFunc(contact);
            return isValidForGoogleSupporterGroup;
        }

        public static void DeleteChangeHistory(Context context, Guid contactId)
        {
            EntityReference contactReference = new(Contact.EntityLogicalName, contactId);
            Utils.DeleteRecordChangeHistory(context, contactReference);
        }

        public static void ScheduleDeleteChangeHistory(Context context, Contact contact)
        {
            if (contact.StatusCode == Contact_StatusCode.Active)
            {
                return;
            }

            DeleteContactChangeHistory deleteContactChangeHistory = new()
            {
                ContactId = contact.Id,
                Context = context,
            };
            deleteContactChangeHistory.Schedule(allowDuplicates: false);
        }

        public static void ClearWillCall(Contact contact)
        {
            if (contact.pc_CalledOn == null)
            {
                return;
            }

            contact.pc_WillCall = null;
        }

        public static string GetEmail(Contact contact)
        {
            if (contact == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(contact.EMailAddress2))
            {
                return contact.EMailAddress2;
            }

            return contact.EMailAddress1;
        }

        public static void SetEmail(Contact contact, string email)
        {
            if (!string.IsNullOrEmpty(contact.EMailAddress2))
            {
                contact.EMailAddress2 = email;
                return;
            }

            contact.EMailAddress1 = email;
        }
    }
}
