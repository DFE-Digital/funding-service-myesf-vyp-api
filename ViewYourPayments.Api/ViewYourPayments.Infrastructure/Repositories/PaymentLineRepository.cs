using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Enums;
using ViewYourPayments.Core.Interfaces.Repositories;
using ViewYourPayments.Core.Models.Payments;
using ViewYourPayments.EntityFramework;
using ViewYourPayments.Infrastructure.Extensions;

namespace ViewYourPayments.Infrastructure.Repositories
{
    public class PaymentLineRepository : IPaymentLineRepository
    {
        private readonly ViewYourPaymentsDbContext _context;

        public PaymentLineRepository(ViewYourPaymentsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BudgetGroupItem>> GetBudgetGroupSummaries(PaymentTransactionSearchCriteria searchCriteria)
        {
            return await GetPaymentTransactionQuery(searchCriteria).GroupBy(x=>new { x.BudgetGroup})
                                            .OrderBy(o=>o.Key.BudgetGroup)
                                            .Select(y=> new BudgetGroupItem { BudgetDescription = y.Key.BudgetGroup, BudgetAmount = y.Sum(x => x.LineAmount) })
                                            .AsNoTracking()
                                            .ToListAsyncSafe();
        }

        public async Task<IEnumerable<PaymentLine>> GetPaymentLines(string ukprn, int paymentIdentifier)
        {
          return await  _context.PaymentLines.Where(x => x.PaymentSummary.PaymentIdentifier == paymentIdentifier
                                        && x.PaymentSummary.Ukprn == ukprn).Include(x=>x.PaymentSummary).
                                        OrderBy(x=>x.Contract).ThenBy(x=>x.PaymentLineDescription).
                                        AsNoTracking().
                                        ToListAsyncSafe();
        }

        public async Task<IEnumerable<PaymentLine>> GetPaymentTransactions (PaymentTransactionSearchCriteria searchCriteria)
        {
            var paymentQuery = GetPaymentTransactionQuery(searchCriteria).Include(x => x.PaymentSummary).AsQueryable();
            var sortingParams = GetUpdatedSortListForPaymentTransaction(searchCriteria.PrimarySortField, searchCriteria.SortDirection);
            for (int i = 0; i < sortingParams.Count; i++)
            {
              paymentQuery =  paymentQuery.ApplyOrder(GetApplicableTableName(sortingParams[i].SortField), sortingParams[i].SortDirection == 0, i == 0);
            }


            if (searchCriteria.PageSize > 0 && searchCriteria.PageNumber > 0)
            {
                return await paymentQuery.Skip(searchCriteria.PageSize * (searchCriteria.PageNumber - 1)).
                                       Take(searchCriteria.PageSize)
                                       .AsNoTracking().ToListAsyncSafe();
            }
            return await paymentQuery.AsNoTracking().ToListAsyncSafe();
        }

        public async Task<IEnumerable<string>> GetUniqueTransactionDescriptions(string ukprn, DateTime startDate, DateTime endDate)
        {
            var paymentQuery  = _context.PaymentLines
                 .Where(x => x.PaymentSummary.Ukprn == ukprn
                 && x.PaymentSummary.PaymentDate >= startDate
                 && x.PaymentSummary.PaymentDate <= endDate).OrderBy(x=>x. PaymentLineGroupDescription);

            return await paymentQuery.Select(x=>x. PaymentLineGroupDescription).Distinct().ToListAsyncSafe();
        }

        public async Task<int> GetPaymentTransactionCount(PaymentTransactionSearchCriteria searchCriteria)
        {
            return await GetPaymentTransactionQuery(searchCriteria).CountAsync();
        }

        private IQueryable<PaymentLine> GetPaymentTransactionQuery(PaymentTransactionSearchCriteria searchCriteria)
        {
            var query = _context.PaymentLines
                 .Where(x => x.PaymentSummary.Ukprn == searchCriteria.Ukprn
                 && x.PaymentSummary.PaymentDate >= searchCriteria.StartDate
                 && x.PaymentSummary.PaymentDate <= searchCriteria.EndDate);

            if (searchCriteria.SearchTerms == null || !searchCriteria.SearchTerms.Any()) return query;

            query = query.GetWildcardQueryWithMulitpleSearchTerms("PaymentLineDescription", searchCriteria.SearchTerms);
            return query;
        }
        private  List<PaymentTransactionSortItem> GetDefaultSortListForPaymentTransaction()
        {
            var defaultSortList = new List<PaymentTransactionSortItem>
            {
                new PaymentTransactionSortItem(PaymentTransactionSortFields.PaymentDate, SortDirection.Descending),
                new PaymentTransactionSortItem(PaymentTransactionSortFields.Contract, SortDirection.Ascending),
                new PaymentTransactionSortItem(PaymentTransactionSortFields.BudgetGroup, SortDirection.Ascending),
                new PaymentTransactionSortItem(PaymentTransactionSortFields.LineAmount, SortDirection.Ascending),
                new PaymentTransactionSortItem(PaymentTransactionSortFields.PaymentLineDescription, SortDirection.Ascending)
            };
            return defaultSortList;
        }

        private  List<PaymentTransactionSortItem> GetUpdatedSortListForPaymentTransaction(PaymentTransactionSortFields primarySortField, SortDirection sortDirection)
        {
            var defaultList = GetDefaultSortListForPaymentTransaction();
            var updatedSortList = new List<PaymentTransactionSortItem>();
            if (defaultList.Any(x => x.SortField == primarySortField))
            {
                updatedSortList.Add(new PaymentTransactionSortItem(primarySortField, sortDirection));
                updatedSortList.AddRange(defaultList.Where(x => x.SortField != primarySortField));
            }
            return updatedSortList;
        }
        private string GetApplicableTableName(PaymentTransactionSortFields sortField)
        {
            if (sortField == PaymentTransactionSortFields.PaymentDate) return "PaymentSummary.PaymentDate";
            return sortField.ToString();
        }
    }
}
