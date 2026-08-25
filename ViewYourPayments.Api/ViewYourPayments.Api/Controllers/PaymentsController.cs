using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Interfaces;
using ViewYourPayments.Core.Interfaces.Services;

namespace ViewYourPayments.Api.Controllers
{
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IApplicationLogger _logger;

        public PaymentsController(IPaymentService paymentService, IApplicationLogger logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the payments for a provider (UKPRN) for a date range.
        /// </summary>
        /// <param name="ukprn">Provider Identifier-UKPR.</param>
        /// <param name="dateFrom">Date from which payments are required, must be used together with DateTo.</param>
        /// <param name="dateTo">Date until which payments are required,, must be used together with DateFrom.</param>
        /// <param name="page">Page number (optional). If not specified, first page will be returned.</param>
        /// <param name="pageSize">Page size (optional). If not specified, 20 records will be returned.</param>
        /// <returns>List of payment summary result.</returns>
        [HttpGet]
        [Authorize]
        [Route("/Provider/{ukprn}/Payments")]
        public async Task<ActionResult<PaymentSummaryResult>> Get(
                                [FromRoute] string ukprn,
                                [FromQuery] DateTime dateFrom,
                                [FromQuery] DateTime dateTo,
                                [FromQuery] int page,
                                [FromQuery] int pageSize)
        {
            _logger.LogInfo($"Payment summary request received with Ukprn: {ukprn} , startDate - endDate: {dateFrom.ToShortDateString()} - {dateTo.ToShortDateString()}");

            if (string.IsNullOrWhiteSpace(ukprn) || dateFrom == DateTime.MinValue || dateTo == DateTime.MinValue)
            {
                return new BadRequestResult();
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = ukprn,
                StartDate = dateFrom,
                EndDate = dateTo,
                PageNumber = page,
                PageSize = pageSize
            };
            return new OkObjectResult(await _paymentService.GetPaymentSummaries(searchCriteria));
        }

        /// <summary>
        /// Get payment detail for selected payment identifier made to a provider (UKPRN).
        /// </summary>
        /// <param name="ukprn">Provider Identifier-UKPRN.</param>
        /// <param name="paymentIdentifier">Payment Identifier.</param>
        /// <returns>Pamyent details result.</returns>
        [HttpGet]
        [Authorize]
        [Route("/Provider/{ukprn}/Payment/{paymentIdentifier}")]
        public async Task<ActionResult<PaymentDetailsResult>> GetPaymentDetails(
                        [FromRoute] string ukprn,
                        [FromRoute] int paymentIdentifier)
        {
            _logger.LogInfo($"Payment details request received with Ukprn: {ukprn} - PaymentIdentifier {paymentIdentifier}");

            if (string.IsNullOrWhiteSpace(ukprn) || paymentIdentifier < 1)
            {
                return new BadRequestResult();
            }
            return new OkObjectResult(await _paymentService.GetPaymentDetails(ukprn, paymentIdentifier));
        }


        /// <summary>
        /// Gets the transactions for a provider (UKPRN) for a selected criteria.
        /// </summary>
        /// <param name="searchCriteria"> Search criteria for selected request.</param>
        /// <returns>List of Provider transactions based on selected criteria.</returns>
        [HttpPost]
        [Authorize]
        [Route("/Provider/Transactions")]
        public async Task<ActionResult<PaymentTransactionsResult>> GetPaymentTransactions([FromBody] PaymentTransactionSearchCriteria searchCriteria)
        {
            _logger.LogInfo($"GetPaymentTransactions request received with Ukprn: {searchCriteria.Ukprn} , startDate - endDate: {searchCriteria.StartDate} - {searchCriteria.EndDate} and SortField :{searchCriteria.PrimarySortField} with sortDirection: {searchCriteria.SortDirection}");
            //Set default value if field are empty or null

            if (string.IsNullOrWhiteSpace(searchCriteria.Ukprn) || searchCriteria.StartDate == DateTime.MinValue || searchCriteria.EndDate == DateTime.MinValue)
            {
                return new BadRequestResult();
            }
            searchCriteria.PageNumber = searchCriteria.PageNumber < 1 ? 1 : searchCriteria.PageNumber;

            return new OkObjectResult(await _paymentService.GetPaymentTransactions(searchCriteria));
        }


        /// <summary>
        /// Gets the transactions for a provider (UKPRN) for a selected criteria.
        /// </summary>
        /// <param name="ukprn">Provider Identifier-UKPRN.</param>
        /// <param name="dateFrom">Date from which payments are required, must be used together with DateTo.</param>
        /// <param name="dateTo">Date until which payments are required,, must be used together with DateFrom.</param>
        /// <returns>List of unique transaction descripitons for selected criteria.</returns>
        [HttpGet]
        [Authorize]
        [Route("/Provider/{ukprn}/UniqueTransactionDescriptions")]
        public async Task<ActionResult<string[]>> GetUniqueTransactioinDescriptions([FromRoute] string ukprn,
                                                                                    [FromQuery] DateTime dateFrom,
                                                                                    [FromQuery] DateTime dateTo)
        {
            _logger.LogInfo($"GetPaymentTransactions request received with Ukprn: {ukprn} , startDate - endDate: {dateFrom} - {dateTo} ");
            //Set default value if field are empty or null

            if (string.IsNullOrWhiteSpace(ukprn) || dateFrom == DateTime.MinValue || dateTo == DateTime.MinValue)
            {
                return new BadRequestResult();
            }

            return new OkObjectResult(await _paymentService.GetUniqueTransactionDescriptions(ukprn, dateFrom, dateTo));
        }

        /// <summary>
        /// Gets the payment summaries count for a provider (UKPRN) for a selected criteria.
        /// </summary>
        /// <param name="ukprn">Provider Identifier-UKPRN.</param>
        /// <param name="dateFrom">Date from which payments are required, must be used together with DateTo.</param>
        /// <param name="dateTo">Date until which payments are required,, must be used together with DateFrom.</param>
        /// <returns>Payment Summaries Count</returns>
        [HttpGet]
        [Authorize]
        [Route("/Provider/{ukprn}/PaymentSummariesCount")]
        public async Task<ActionResult<int>> GetPaymentSummariesCount([FromRoute] string ukprn,
                                                                            [FromQuery] DateTime? dateFrom,
                                                                            [FromQuery] DateTime? dateTo)
        {
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = ukprn,
                StartDate = dateFrom.HasValue ? dateFrom.Value : DateTime.UtcNow.AddYears(-3),
                EndDate = dateTo.HasValue ? dateTo.Value : DateTime.UtcNow
            };

            _logger.LogInfo($"GetPaymentSummariesCount request received with Ukprn: {ukprn} , startDate - endDate: {dateFrom} - {dateTo} ");
            //Set default value if field are empty or null

            if (string.IsNullOrWhiteSpace(ukprn) || dateFrom == DateTime.MinValue || dateTo == DateTime.MinValue)
            {
                return new BadRequestResult();
            }

            return new OkObjectResult(await _paymentService.GetPaymentSummariesCount(searchCriteria));
        }

        [HttpGet]
        [Route("/Heartbeat")]
        public ActionResult<bool> Get()
        {
            _logger.LogInfo($"heartbeat request received");


            return new OkObjectResult(true);
        }
    }
}
