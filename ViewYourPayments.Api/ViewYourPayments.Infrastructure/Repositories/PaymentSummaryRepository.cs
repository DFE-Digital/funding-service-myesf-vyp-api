using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Interfaces.Repositories;
using ViewYourPayments.Core.Models.Payments;
using ViewYourPayments.EntityFramework;
using ViewYourPayments.Infrastructure.Extensions;

namespace ViewYourPayments.Infrastructure.Repositories
{
    public class PaymentSummaryRepository : IPaymentSummaryRepository
    {
        private readonly ViewYourPaymentsDbContext _context;

        public PaymentSummaryRepository(ViewYourPaymentsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentSummary>> GetPaymentSummaries(PaymentSummarySearchCriteria searchCriteria)
        {
            return await GetPaymentQuery(searchCriteria).OrderByDescending(x => x.PaymentDate)
                .Skip(searchCriteria.PageSize * (searchCriteria.PageNumber - 1)).Take(searchCriteria.PageSize).ToListAsyncSafe();
        }

        public async Task<int> GetPaymentSummariesCount(PaymentSummarySearchCriteria searchCriteria)
        {
            return await GetPaymentQuery(searchCriteria).CountAsync();
        }

        private IQueryable<PaymentSummary> GetPaymentQuery(PaymentSummarySearchCriteria searchCriteria)
        {
            return _context.PaymentSummaries
                 .Where(x => x.Ukprn == searchCriteria.Ukprn && x.PaymentDate >= searchCriteria.StartDate && x.PaymentDate <= searchCriteria.EndDate);
        }
    }
   
}
