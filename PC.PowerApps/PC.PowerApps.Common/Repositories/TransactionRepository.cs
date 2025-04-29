using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Entities.Fetch;
using PC.PowerApps.Common.Entities.Fidavista;
using PC.PowerApps.Common.Extensions;
using PC.PowerApps.Common.FetchXmlBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace PC.PowerApps.Common.Repositories
{
    public static class TransactionRepository
    {
        public static void ImportFromBankAccount(Context context, Guid bankAccountId)
        {
            pc_BankAccount bankAccount = context.ServiceContext.Retrieve<pc_BankAccount>(bankAccountId);

            try
            {
                bankAccount.pc_TransactionImportStatus = pc_TransactionImportStatus.InProgress;
                _ = context.ServiceContext.UpdateModifiedAttributes(bankAccount);

                byte[] file = FileRepository.Download(context, bankAccount, ba => ba.pc_TransactionImportFile);
                using MemoryStream memoryStream = new MemoryStream(file);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(FIDAVISTA));
                FIDAVISTA document = (FIDAVISTA)xmlSerializer.Deserialize(memoryStream);

                Lazy<TransactionCurrency> transactionCurrency = new Lazy<TransactionCurrency>(() => context.ServiceContext.TransactionCurrencySet
                     .Where(tc => tc.ISOCurrencyCode == document.Statement.AccountSet.CcyStmt.Ccy)
                     .TakeSingle());

                Lazy<List<pc_Transaction>> existingTransactions = new Lazy<List<pc_Transaction>>(() => context.ServiceContext.pc_TransactionSet
                    .Select(t => new pc_Transaction
                    {
                        pc_Name = t.pc_Name,
                    })
                    .ToList());

                foreach (FIDAVISTAStatementAccountSetCcyStmtTrxSet bankTransaction in document.Statement.AccountSet.CcyStmt.TrxSet)
                {
                    Import(context, bankTransaction, bankAccount, transactionCurrency, existingTransactions);
                }

                DateTime lastImportedTransactionDate = document.Statement.AccountSet.CcyStmt.TrxSet.Max(t => t.BookDate);

                if (bankAccount.pc_LastImportedTransactionDate == null || bankAccount.pc_LastImportedTransactionDate < lastImportedTransactionDate)
                {
                    bankAccount.pc_LastImportedTransactionDate = lastImportedTransactionDate;
                }

                bankAccount.pc_TransactionImportStatus = pc_TransactionImportStatus.Completed;
                _ = context.ServiceContext.UpdateModifiedAttributes(bankAccount);
            }
            catch (Exception e)
            {
                bankAccount.pc_TransactionImportError = e.ToString().TakeFirst(CommonConstants.MultilineTextMaxLength);
                bankAccount.pc_TransactionImportStatus = pc_TransactionImportStatus.Failed;
                _ = context.ServiceContext.UpdateModifiedAttributes(bankAccount);
            }

            FileRepository.Delete(context, bankAccount.pc_TransactionImportFile);
        }

        public static void SetDefaults(pc_Transaction transaction)
        {
            transaction.pc_NonPaymentAmount ??= new();
            transaction.pc_PaymentTotalAmount ??= new();
        }

        public static void CalculatePaymentTotalAmount(Context context, Guid? transactionId)
        {
            if (transactionId == null)
            {
                return;
            }

            pc_Transaction transaction = context.ServiceContext.Retrieve<pc_Transaction>(transactionId.Value);
            context.Logger.LogInformation($"Calculating a Payment Total Amount for the Transaction {transaction.pc_Name}.");
            const string AmountAlias = "amount";
            FetchXmlBuilder<pc_Payment> paymentQuery = new FetchXmlBuilder<pc_Payment>(aggregate: true)
                .AddAttribute(p => p.pc_Amount, AmountAlias, AggregateType.sum)
                .AddFilter(filterType.and, f => f
                    .AddCondition(p => p.pc_Transaction, @operator.eq, transactionId.Value)
                );
            pc_Payment aggregate = context.OrganizationService.RetrieveMultiple(paymentQuery).TakeSingle();
            transaction.pc_PaymentTotalAmount = aggregate.GetAliasedValue<Money>(AmountAlias);
            _ = context.ServiceContext.UpdateModifiedAttributes(transaction);
        }

        public static void CalculateRemainingAmount(pc_Transaction transaction)
        {
            transaction.pc_RemainingAmount = new(Utils.GetAmountOrZero(transaction.pc_Amount) - Utils.GetAmountOrZero(transaction.pc_PaymentTotalAmount) - Utils.GetAmountOrZero(transaction.pc_NonPaymentAmount));
        }

        public static void SetStatusCode(pc_Transaction transaction)
        {
            if (transaction.pc_RemainingAmount.Value == 0)
            {
                transaction.StateCode = pc_TransactionState.Inactive;
                transaction.StatusCode = pc_Transaction_StatusCode.Inactive;
            }
            else
            {
                transaction.StateCode = pc_TransactionState.Active;
                transaction.StatusCode = pc_Transaction_StatusCode.Active;
            }
        }

        private static void Import(Context context, FIDAVISTAStatementAccountSetCcyStmtTrxSet bankTransaction, pc_BankAccount bankAccount, Lazy<TransactionCurrency> transactionCurrency, Lazy<List<pc_Transaction>> existingTransactions)
        {
            if (bankTransaction.TypeCode != "INP")
            {
                context.Logger.LogInformation($"The Transaction {bankTransaction.BankRef} is not an incoming payment.");
                return;
            }

            pc_Transaction transaction = existingTransactions.Value
                .Where(t => t.pc_Name == bankTransaction.BankRef)
                .FirstOrDefault();

            if (transaction != null)
            {
                context.Logger.LogInformation($"The Transaction {bankTransaction.BankRef} is already imported.");
                return;
            }

            transaction = new pc_Transaction
            {
                pc_Amount = new Money(bankTransaction.AccAmt),
                pc_BankAccount = bankAccount.ToEntityReference(),
                pc_Date = bankTransaction.BookDate,
                pc_Details = bankTransaction.PmtInfo,
                pc_Name = bankTransaction.BankRef,
                pc_PayerId = bankTransaction.CPartySet.AccHolder.LegalId,
                pc_PayerName = bankTransaction.CPartySet.AccHolder.Name,
                TransactionCurrencyId = transactionCurrency.Value.ToEntityReference(),
            };
            context.OrganizationService.CreateWithoutNulls(transaction);
            context.Logger.LogInformation($"The Transaction {bankTransaction.BankRef} has been imported.");
        }

        public static void Process(Context context, pc_Transaction transaction)
        {
            context.Logger.LogInformation($"Processing the Transaction {transaction.pc_Name}.");

            if (Utils.GetAmountOrZero(transaction.pc_RemainingAmount) <= 0)
            {
                context.Logger.LogInformation("Transaction Remaining Amount is less than or equal to 0 - skipping.");
                return;
            }

            if (transaction.pc_Details == null)
            {
                context.Logger.LogInformation("Transaction Details are empty - skipping.");
                return;
            }

            Lazy<Regex> participantFeeRegex = new(() => new($@"(?:{context.Settings.pc_ParticipantFeeRegularExpressions.Replace("\n", "|")})", RegexOptions.IgnoreCase));
            Lazy<Regex> nonParticipantFeeRegex = new(() => new($@"(?:{context.Settings.pc_NonParticipantFeeRegularExpressions.Replace("\n", "|")})", RegexOptions.IgnoreCase));

            if (!string.IsNullOrEmpty(context.Settings.pc_ParticipantFeeRegularExpressions) && participantFeeRegex.Value.IsMatch(transaction.pc_Details))
            {
                HashSet<string> personalIdentityNumbers = new();
                Lazy<Regex> personalIdentityNumberRegex = new(() => new(@"\b(\d{6})-?(\d{5})\b"));
                AddPersonalIdentityNumbers(personalIdentityNumbers, personalIdentityNumberRegex, transaction.pc_PayerId);
                AddPersonalIdentityNumbers(personalIdentityNumbers, personalIdentityNumberRegex, transaction.pc_Details);
                List<Contact> contacts = personalIdentityNumbers
                    .SelectMany(pin => context.ServiceContext.ContactSet
                        .Where(c => c.pc_PersonalIdentityNumber == pin))
                    .ToList();

                if (contacts.Count != 1)
                {
                    context.Logger.LogInformation($"There are {contacts.Count} matching Contacts - skipping.");
                    return;
                }

                pc_Payment payment = new pc_Payment
                {
                    pc_Contact = contacts[0].ToEntityReference(),
                    pc_Transaction = transaction.ToEntityReference(),
                };
                context.OrganizationService.CreateWithoutNulls(payment);

                context.Logger.LogInformation($"Created a Payment for the Contact {contacts[0].FullName}.");
                return;
            }

            if (!string.IsNullOrEmpty(context.Settings.pc_NonParticipantFeeRegularExpressions) && nonParticipantFeeRegex.Value.IsMatch(transaction.pc_Details))
            {
                transaction.pc_NonPaymentAmount = new(Utils.GetAmountOrZero(transaction.pc_RemainingAmount));
                _ = context.ServiceContext.UpdateModifiedAttributes(transaction);
                context.Logger.LogInformation($"Set Non-Payment Amount to {transaction.pc_NonPaymentAmount.Value}.");
            }
        }

        private static void AddPersonalIdentityNumbers(HashSet<string> personalIdentityNumbers, Lazy<Regex> personalIdentityNumberRegex, string value)
        {
            if (value != null)
            {
                MatchCollection matches = personalIdentityNumberRegex.Value.Matches(value);

                foreach (Match match in matches)
                {
                    _ = personalIdentityNumbers.Add($"{match.Groups[1]}-{match.Groups[2]}");
                }
            }
        }

        public static void ClearNonPaymentAmount(pc_Transaction transaction)
        {
            transaction.pc_NonPaymentAmount = new();
        }

        public static void MarkAsNonPayment(pc_Transaction transaction)
        {
            transaction.pc_NonPaymentAmount = new(Utils.GetAmountOrZero(transaction.pc_RemainingAmount));
        }
    }
}
