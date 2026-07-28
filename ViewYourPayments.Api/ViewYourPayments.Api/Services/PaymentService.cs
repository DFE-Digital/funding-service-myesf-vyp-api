using System;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Interfaces.Services;
using ViewYourPayments.Core.Interfaces.Repositories;
using ViewYourPayments.Core.Interfaces;
using System.Collections.Generic;

namespace ViewYourPayments.Api.Services
{
    public class PaymentsService : IPaymentService
    {
        private readonly IPaymentSummaryRepository _paymentSummaryRepository;
        private readonly IPaymentLineRepository _paymentLineRepository;
        private readonly IApplicationLogger _logger;
        public PaymentsService(IPaymentSummaryRepository paymentSummaryRepository,
                                IPaymentLineRepository paymentLineRepository,
                                IApplicationLogger logger)
        {
            _paymentSummaryRepository = paymentSummaryRepository;
            _paymentLineRepository = paymentLineRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get payment details for selected payment identifier and provider identifier ukprn.
        /// </summary>
        /// <param name="ukprn">Provider identifier.</param>
        /// <param name="paymentIdentifier">Payment identifier.</param>
        /// <returns>List of PaymentItem.</returns>
        public async Task<PaymentDetailsResult> GetPaymentDetails(string ukprn, int paymentIdentifier)
        {
            var result = await _paymentLineRepository.GetPaymentLines(ukprn, paymentIdentifier);
            var paymentSummary = result.FirstOrDefault()?.PaymentSummary;
            var paymentLineItems = result
                                     .Select(p => new PaymentLineItem
                                     {
                                         BudgetDescription = p.BudgetGroup,
                                         ContractNumber = p.Contract,
                                         PaymentLineAmount = p.LineAmount,
                                         PaymentDate = p.PaymentSummary.PaymentDate,
                                         PaymentLineDescription = p.PaymentLineDescription,
                                         PaymentLineIdentifier = p.PaymentLineIdentifier
                                     }).ToArray();
            return new PaymentDetailsResult
            {
                PaymentLines = paymentLineItems,
                PaymentAmount = paymentSummary?.PaymentTotal,
                ProviderUkprn = ukprn,
                ProviderFinanceVendorIdentifier = paymentSummary?.VendorIdentifier,
                PaymentDate = paymentSummary?.PaymentDate
            };
        }

        /// <summary>
        /// Get payment transaction for selected search criteria
        /// </summary>
        /// <param name="searchCriteria">Search criteria for query e.g. ukprn, datefrom , dateto, searchTerms, sorting, paging etc.</param>
        /// <returns>Returns list of PaymentTransactionItems.</returns>
        public async Task<PaymentTransactionsResult> GetPaymentTransactions(PaymentTransactionSearchCriteria searchCriteria)
        {
            try
            {
                var budgetGroups = await _paymentLineRepository.GetBudgetGroupSummaries(searchCriteria);
                var paymentLines = await _paymentLineRepository.GetPaymentTransactions(searchCriteria);
                var totalCount = await _paymentLineRepository.GetPaymentTransactionCount(searchCriteria);
                var result = new PaymentTransactionsResult
                {
                    ProviderUkprn = searchCriteria.Ukprn,
                    DateFrom = searchCriteria.StartDate,
                    DateTo = searchCriteria.EndDate,
                    TotalRecords = totalCount,
                    CurrentPage = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize,
                    PaymentLines = Array.Empty<PaymentLineItem>(),
                };

                if (!paymentLines.Any()) return result;
                result.ProviderFinanceVendorCode = paymentLines.First().PaymentSummary.VendorIdentifier;
                result.BudgetGroupSummary = new BudgetGroupSummary
                {
                    TotalAmount = budgetGroups.Sum(x => x.BudgetAmount),
                    BudgetGroups = budgetGroups.ToArray()
                };
                result.PaymentLines = paymentLines.Select(x => new PaymentLineItem
                {
                    BudgetDescription = x.BudgetGroup,
                    ContractNumber = x.Contract,
                    PaymentLineAmount = x.LineAmount,
                    PaymentDate = x.PaymentSummary.PaymentDate,
                    PaymentLineDescription = x.PaymentLineDescription,
                    PaymentLineIdentifier = x.PaymentLineIdentifier
                }).ToArray();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Get unique/distinct transaction descrpition for given time period and ukprn.
        /// </summary>
        /// <param name="ukprn">Provider identifier ukprn.</param>
        /// <param name="startDate">Date to start search from.</param>
        /// <param name="endDate">>Date to search end.</param>
        /// <returns>Unique descriptions list as string array.</returns>
        public async Task<IEnumerable<string>> GetUniqueTransactionDescriptions(string ukprn, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _paymentLineRepository.GetUniqueTransactionDescriptions(ukprn,startDate,endDate);                
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Get payment summaries for selected search criteria.
        /// </summary>
        /// <param name="searchCriteria">Search criteria for query e.g. ukprn, date from , date to .</param>
        /// <returns>Retruns list of payment summary result item.</returns>
        public async Task<PaymentSummaryResult> GetPaymentSummaries(PaymentSummarySearchCriteria searchCriteria)
        {
            try
            {
                var payments = await _paymentSummaryRepository.GetPaymentSummaries(searchCriteria);
                var totalCount = await _paymentSummaryRepository.GetPaymentSummariesCount(searchCriteria);
                return new PaymentSummaryResult
                {
                    ProviderUKPRN = searchCriteria.Ukprn,
                    DateFrom = searchCriteria.StartDate,
                    DateTo = searchCriteria.EndDate,
                    TotalRecords = totalCount,
                    TotalPages =(int)Math.Ceiling((double)totalCount/searchCriteria.PageSize),
                    CurrentPage = searchCriteria.PageNumber,
                    PageSize = searchCriteria.PageSize,
                    PaymentSummaries = payments
                        .Select(p => new PaymentSummaryItem
                        {
                            PaymentIdentifier = p.PaymentIdentifier,
                            PaymentAmount = p.PaymentTotal,
                            PaymentDate = p.PaymentDate
                        })
                        .ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                throw;
            }           
        }

        public async Task<int> GetPaymentSummariesCount(PaymentSummarySearchCriteria searchCriteria)
        {
            try
            {
                return await _paymentSummaryRepository.GetPaymentSummariesCount(searchCriteria);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                throw;
            }
        }
    }
}
