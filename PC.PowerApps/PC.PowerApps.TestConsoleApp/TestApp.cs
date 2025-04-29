using Microsoft.Extensions.Configuration;
using PC.PowerApps.ClientBase;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace PC.PowerApps.TestConsoleApp
{
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>")]
    internal class TestApp
    {
        private readonly Lazy<CrmServiceClientContext> context;
        private readonly IConfiguration configuration;

        private CrmServiceClientContext Context => context.Value;

        public TestApp(Lazy<CrmServiceClientContext> context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Execute()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //TransactionRepository.ImportFromAnnotation(Context, new Guid("177b97ba-92e8-eb11-bacb-000d3abb9ce3"));

            //await scheduledJobProcessor.Value.ExecuteAll();

            //List<string> attributeNames = Utils.GetAttributeNames<Annotation>(a => new Annotation { DocumentBody = a.DocumentBody, IsDocument = a.IsDocument });

            //Annotation annotation = new Annotation { };
            //AnnotationRepository.ScheduleImportSwedbankTransactions(Context, annotation);

            //List<pc_Transaction> transactions = Context.ServiceContext.pc_TransactionSet.ToList();

            //List<pc_Transaction> transactions = Context.ServiceContext.pc_TransactionSet
            //    .Where(t => t.StateCode == pc_TransactionState.Active && t.pc_Name == "2021080200010469")
            //    .ToList();

            //foreach (pc_Transaction transaction in transactions)
            //{
            //    //TransactionRepository.Process(Context, transaction);
            //    //TransactionRepository.SetDefaults(transaction);
            //    TransactionRepository.CalculatePaymentTotalAmount(Context, transaction.Id);
            //    //Console.Write("+");
            //}

            //Console.WriteLine(Utils.IsInNamespaces(GetType(), "Microsoft.Xrm.Sdk", nameof(System)));

            //await ContactRepository.UpdateRequiredParticipationFee(Context);

            //Contact contact = Context.ServiceContext.Retrieve<Contact>(new Guid("1c9490aa-40ed-eb11-bacb-000d3a3a2279"));
            //ContactRepository.UpdateRequiredParticipationFee(Context, contact);

            //Contact contact = Context.ServiceContext.Retrieve<Contact>(new Guid("0d5e1852-00e8-eb11-bacb-000d3abb9ce3"));
            ////Context.ServiceContext.ClearChanges();
            //contact = Context.ServiceContext.Retrieve<Contact>(new Guid("0d5e1852-00e8-eb11-bacb-000d3abb9ce3"));
            //_ = Context.ServiceContext.UpdateModifiedAttributes<Contact>(contact.ToEntityReference());

            //ContactRepository.SendDebtReminder(Context, new Guid("1a9490aa-40ed-eb11-bacb-000d3a3a2279"));

            //await GoogleGroupMemberRepository.SynchronizeParticipants(Context, true, false);

            //List<Contact> contacts = Context.ServiceContext.ContactSet.ToList();

            //foreach (Contact contact in contacts)
            //{
            //    ContactRepository.CalculatePaidParticipationFee(Context, contact.Id);
            //}

            //CrmServiceClient crmServiceClient = new(configuration.GetConnectionString("Dataverse"));
            ////QueryExpression queryExpression = new(SystemUser.EntityLogicalName);
            ////queryExpression.Criteria.AddCondition("systemuserid", ConditionOperator.EqualUserId);
            //Stopwatch stopwatch = Stopwatch.StartNew();
            //_ = crmServiceClient.GetMyCrmUserId();
            ////crmServiceClient.RetrieveMultiple(queryExpression);
            //Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds}");

            //Contact contact = Context.ServiceContext.Retrieve<Contact>(new Guid("0d5e1852-00e8-eb11-bacb-000d3abb9ce3"));
            //contact.FirstName = "Test";
            //Context.ServiceContext.UpdateModifiedAttributes(contact);

            //ContactRepository.UpdateParticipationLevels(Context);

            //pc_Payment payment = Context.ServiceContext.Retrieve<pc_Payment>(new Guid("04a7a7b1-d3f9-eb11-94ef-000d3a4bfac3"));
            //payment.pc_Amount = new(-1);
            //_ = Context.ServiceContext.UpdateModifiedAttributes(payment);

            //pc_Participation participation = new()
            //{
            //    pc_Contact = new EntityReference(Contact.EntityLogicalName, new Guid("0d5e1852-00e8-eb11-bacb-000d3abb9ce3")),
            //    pc_From = new DateTime(2017, 9, 25),
            //    pc_Till = new DateTime(2021, 9, 26),
            //};
            //_ = ParticipationRepository.GetParticipationWithinSamePeriod(Context, participation);

            //SystemUser systemUser = Context.ServiceContext.Retrieve<SystemUser>(Context.Organization.SystemUserId.Value);
            //systemUser.pc_DisableValidationTill = Context.OrganizationToUtcTime(new DateTime(9999, 12, 31, 23, 59, 0));
            //Context.ServiceContext.UpdateModifiedAttributes(systemUser);

            //pc_Payment updatePayment = new()
            //{
            //    Id = new Guid("ff8af53d-57fb-eb11-94ef-000d3a4bfac3"),
            //    pc_Amount = new(1),
            //};
            //Context.OrganizationService.Update(updatePayment);

            //Stopwatch stopwatch = Stopwatch.StartNew();
            //CultureInfo cultureInfo = CultureInfo.GetCultureInfo("lv-LV");
            //Console.WriteLine(DateTime.Now.ToString("d", cultureInfo));
            //Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);

            //List<pc_Payment> payments = Context.ServiceContext.pc_PaymentSet.ToList();

            //foreach (pc_Payment payment in payments)
            //{
            //    PaymentRepository.SetName(Context, payment);
            //    _ = Context.ServiceContext.UpdateModifiedAttributes(payment);
            //}

            //List<pc_Participation> participations = Context.ServiceContext.pc_ParticipationSet.ToList();

            //foreach (pc_Participation participation in participations)
            //{
            //    ParticipationRepository.SetName(Context, participation);
            //    _ = Context.ServiceContext.UpdateModifiedAttributes(participation);
            //}

            //Lazy<Expression<Func<Contact, bool>>> IsValidForGoogleSupporterGroupExpression = new(() => c => c.pc_ParticipationLevel == pc_ParticipationLevel.Supporter && c.pc_WishesToBeActive == true && c.pc_PaidParticipationFee != null && c.pc_PaidParticipationFee.Value >= 2 && c.EMailAddress1 != null && c.EMailAddress1 != string.Empty);
            //Stopwatch stopwatch = Stopwatch.StartNew();
            //////_ = IsValidForGoogleSupporterGroupExpression.Value;
            //////string a = (User);
            //TimeSpan oneDay = new TimeSpan(1, 0, 0, 0);
            //Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);
            //Lazy<Func<Contact, bool>> IsValidForGoogleSupporterGroupFunc = new(() => IsValidForGoogleSupporterGroupExpression.Value.Compile());

            //pc_ParticipationFeeExemption participationFeeExemption = Context.ServiceContext.pc_ParticipationFeeExemptionSet.TakeSingle();
            //participationFeeExemption.pc_Till = new DateTime(2021, 8, 31, 1, 2, 3);
            //Context.ServiceContext.UpdateModifiedAttributes(participationFeeExemption);

            //List<Period> periods1 = new()
            //{
            //    new Period(new(2020, 1, 1), new(2020, 12, 31)),
            //    new Period(new(2021, 1, 1), new(2021, 7, 31)),
            //    new Period(new(2022, 1, 1), new(2022, 7, 31)),
            //};
            //List<Period> periods2 = new()
            //{
            //    new Period(new(2020, 4, 1), new(2020, 7, 31)),
            //    new Period(new(2021, 4, 1), new(2021, 12, 31)),
            //    new Period(new(2022, 4, 1), new(2022, 5, 31)),
            //};
            ////List<Period> period = periods1[0].Subtract(periods2[0]);
            //List<Period> periods3 = Period.Subtract(periods1, periods2);

            //ContactRepository.UpdateParticipationLevels(Context);

            //Stopwatch stopwatch = Stopwatch.StartNew();
            //ResourceManager resourceManager = new(typeof(Resource));
            //throw Context.CreateException(nameof(Resource.AttributeCannotBeEmpty), "aaa", "bbb");
            //Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);

            //Utils.DeleteRecordChangeHistory(Context, new EntityReference(Contact.EntityLogicalName, new Guid("b59490aa-40ed-eb11-bacb-000d3a3a2279")));

            //throw Context.CreateException("AttributeCannotBeEmpty1", "123", "456");

            //pc_BankAccount bankAccount = Context.ServiceContext.pc_BankAccountSet.TakeSingle();
            //FileRepository.Download(Context, bankAccount, ba => ba.pc_TransactionImportFile);

            //TransactionRepository.CalculatePaymentTotalAmount(Context, new Guid("0e2e110b-04b5-ec11-983f-00224883c2f5"));

            //FetchXmlBuilder<pc_Transaction> transactionQuery = new FetchXmlBuilder<pc_Transaction>(aggregate: false);
            //List<pc_Transaction> transactions = Context.OrganizationService.RetrieveMultiple(transactionQuery).ToList();
        }
    }
}
