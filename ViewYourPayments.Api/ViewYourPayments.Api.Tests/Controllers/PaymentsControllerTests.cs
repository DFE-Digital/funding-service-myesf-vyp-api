using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using ViewYourPayments.Api.Controllers;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Enums;
using ViewYourPayments.Core.Interfaces;
using ViewYourPayments.Core.Interfaces.Services;
using Xunit;

namespace ViewYourPayments.Api.Tests.Controllers
{
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly PaymentsController _paymentsController;
        public PaymentsControllerTests()
        {
            _mockPaymentService = new Mock<IPaymentService>();
            _paymentsController = new PaymentsController(_mockPaymentService.Object, Mock.Of<IApplicationLogger>());
        }

        [Theory]
        [InlineData("", 2019, 11, 11, 2019, 12, 12)]
        [InlineData(null, 2019, 11, 11, 2019, 12, 12)]
        [InlineData("12", 0001, 01, 01, 2019, 12, 12)]
        [InlineData("12", 2019, 11, 11, 0001, 01, 01)]
        public async Task Get_WhenRequestParameterAreNotValid_ShouldRetrunBadRequest(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay)
        {
            //Arrange
            _mockPaymentService.Setup(x => x.GetPaymentSummaries(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(new PaymentSummaryResult());
            //Action
            var result = await _paymentsController.Get(ukPrn, new DateTime(startYear, startMonth, startDay), new DateTime(endYear, endMonth, endDay), 0, 0);

            //Assert
            result.Result.GetType().Equals(typeof(BadRequestResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentSummaries(It.IsAny<PaymentSummarySearchCriteria>()), Times.Never);
        }

        [Theory]
        [InlineData("12", 2019, 01, 11, 2019, 12, 01)]
        public async Task Get_RequestIsValid_ShouldReturnResult(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay)
        {
            //Arrange
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = ukPrn,
                StartDate = new DateTime(startYear, startMonth, startDay),
                EndDate = new DateTime(endYear, endMonth, endDay),
                PageNumber = 1,
                PageSize = 1
            };
            _mockPaymentService.Setup(x => x.GetPaymentSummaries(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(new PaymentSummaryResult());
            //Action
            var result = await _paymentsController.Get(ukPrn, searchCriteria.StartDate, searchCriteria.EndDate,
                                                        searchCriteria.PageNumber, searchCriteria.PageSize);

            //Assert
            result.Result.GetType().Equals(typeof(OkObjectResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentSummaries(It.IsAny<PaymentSummarySearchCriteria>()), Times.Once);
        }

        [Fact]
        public async Task GetPaymentDetails_RequestIsValid_ShouldReturnOkObjectResult()
        {
            //Arrange
            _mockPaymentService.Setup(x => x.GetPaymentDetails(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new PaymentDetailsResult());
            //Action
            var result = await _paymentsController.GetPaymentDetails("2393931", 19282726);

            //Assert
            result.Result.GetType().Equals(typeof(OkObjectResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentDetails(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Theory]
        [InlineData("", 2019)]
        [InlineData("12", 0)]
        public async Task GetPaymentDetails_RequestIsInValid_ShouldReturnBadRequestResult(string ukprn, int paymentIdentifier)
        {
            //Arrange
            _mockPaymentService.Setup(x => x.GetPaymentDetails(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new PaymentDetailsResult());
            //Action
            var result = await _paymentsController.GetPaymentDetails(ukprn, paymentIdentifier);

            //Assert
            result.Result.GetType().Equals(typeof(BadRequestResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentDetails(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData("12", 2019, 01, 11, 2019, 12, 01, PaymentTransactionSortFields.Contract, SortDirection.Ascending)]
        public async Task GetPaymentTransactions_RequestIsValid_ShouldReturnResult(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay,
                                                                PaymentTransactionSortFields primarySortField,
                                                                 SortDirection sortDirection)
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = ukPrn,
                StartDate = new DateTime(startYear, startMonth, startDay),
                EndDate = new DateTime(endYear, endMonth, endDay),
                PageNumber = 1,
                PageSize = 1,
                PrimarySortField = primarySortField,
                SortDirection = sortDirection
            };
            _mockPaymentService.Setup(x => x.GetPaymentTransactions(searchCriteria)).ReturnsAsync(new PaymentTransactionsResult());
            //Action
            var result = await _paymentsController.GetPaymentTransactions(searchCriteria);

            //Assert
            result.Result.GetType().Equals(typeof(OkObjectResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentTransactions(
                    It.Is<PaymentTransactionSearchCriteria>(x =>
                    x.StartDate == searchCriteria.StartDate
                    && x.EndDate == searchCriteria.EndDate
                    && x.PageSize == searchCriteria.PageSize)),
                    Times.Once);
        }

        [Theory]
        [InlineData("12", 2019, 01, 11, 2019, 12, 01, PaymentTransactionSortFields.PaymentDate, SortDirection.Descending)]
        public async Task GetPaymentTransactions_RequestIsValidAndPageSizeAndNumberNotProvided_ShouldReturnResult(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay,
                                                                 PaymentTransactionSortFields expectedSortField,
                                                                 SortDirection expectedSortDirection)
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = ukPrn,
                StartDate = new DateTime(startYear, startMonth, startDay),
                EndDate = new DateTime(endYear, endMonth, endDay),
                SearchTerms = Array.Empty<string>(),
            };
            _mockPaymentService.Setup(x => x.GetPaymentTransactions(searchCriteria)).ReturnsAsync(new PaymentTransactionsResult());
            //Action
            var result = await _paymentsController.GetPaymentTransactions(searchCriteria);

            //Assert
            result.Result.GetType().Equals(typeof(OkObjectResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentTransactions(
                    It.Is<PaymentTransactionSearchCriteria>(x =>
                    x.StartDate == searchCriteria.StartDate
                    && x.EndDate == searchCriteria.EndDate
                    && x.PageSize == 20
                    && x.PageNumber == 1
                    && x.SortDirection == expectedSortDirection
                    && x.PrimarySortField == expectedSortField)),
                    Times.Once);
        }

        [Theory]
        [InlineData("", 2019, 11, 11, 2019, 12, 12)] //Ukprn empty
        [InlineData(null, 2019, 11, 11, 2019, 12, 12)] //Ukprn null 
        [InlineData("12", 0001, 01, 01, 2019, 12, 12)] // incorrect start date
        [InlineData("12", 2019, 11, 11, 0001, 01, 01)] // incorrect end date
        public async Task GetGetUniqueTransactioinDescriptions_WhenRequestParameterAreNotValid_ShouldRetrunBadRequest(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay)
        {
            //Arrange
            var startDate = new DateTime(startYear, startMonth, startDay);
            var endDate = new DateTime(endYear, endMonth, endDay);

            _mockPaymentService.Setup(x => x.GetUniqueTransactionDescriptions(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Array.Empty<string>());
            //Action
            var result = await _paymentsController.GetUniqueTransactioinDescriptions(ukPrn, startDate, endDate);

            //Assert
            result.Result.GetType().Equals(typeof(BadRequestResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetUniqueTransactionDescriptions(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Theory]
        [InlineData("12", 2019, 01, 11, 2019, 12, 01)]
        public async Task GetGetUniqueTransactioinDescriptions_RequestIsValid_ShouldReturnResult(string ukPrn,
                                                                int startYear, int startMonth, int startDay,
                                                                int endYear, int endMonth, int endDay)
        {
            //Arrange
            var startDate = new DateTime(startYear, startMonth, startDay);
            var endDate = new DateTime(endYear, endMonth, endDay);

            _mockPaymentService.Setup(x => x.GetUniqueTransactionDescriptions(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Array.Empty<string>());
            //Action
            var result = await _paymentsController.GetUniqueTransactioinDescriptions(ukPrn, startDate, endDate);

            //Assert
            result.Result.GetType().Equals(typeof(OkObjectResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetUniqueTransactionDescriptions(ukPrn, startDate, endDate), Times.Once);
        }

        [Theory]
        [InlineData("12345678", 2019, 11, 11, 2019, 01, 01)]
        public async Task GetPaymentSummariesCount_WhenRequestParameterAreValid_ShouldReturnResponse(string ukPrn,
                                                        int startYear, int startMonth, int startDay,
                                                        int endYear, int endMonth, int endDay)
        {
            //Arrange
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = ukPrn,
                StartDate = new DateTime(startYear, startMonth, startDay),
                EndDate = new DateTime(endYear, endMonth, endDay)
            };

            _mockPaymentService.Setup(x => x.GetPaymentSummariesCount(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(5);

            //Action
            var result = await _paymentsController.GetPaymentSummariesCount(ukPrn, searchCriteria.StartDate, searchCriteria.EndDate);

            //Assert
            Assert.Equal(5, ((ObjectResult)result.Result).Value);
            _mockPaymentService.Verify(x => x.GetPaymentSummariesCount(
                    It.Is<PaymentSummarySearchCriteria>(x =>
                    x.StartDate == searchCriteria.StartDate
                    && x.EndDate == searchCriteria.EndDate)),
                    Times.Once);
        }

        [Theory]
        [InlineData("", 2019, 11, 11, 2019, 12, 12)] //Ukprn empty
        [InlineData(null, 2019, 11, 11, 2019, 12, 12)] //Ukprn null 
        [InlineData("12", 0001, 01, 01, 2019, 12, 12)] // incorrect start date
        [InlineData("12", 2019, 11, 11, 0001, 01, 01)] // incorrect end date
        public async Task GetPaymentSummariesCount_WhenRequestParameterAreNotValid_ShouldRetrunBadRequest(string ukPrn,
                                                        int startYear, int startMonth, int startDay,
                                                        int endYear, int endMonth, int endDay)
        {
            //Arrange
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = ukPrn,
                StartDate = new DateTime(startYear, startMonth, startDay),
                EndDate = new DateTime(endYear, endMonth, endDay)
            };

            _mockPaymentService.Setup(x => x.GetPaymentSummariesCount(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(5);

            //Action
            var result = await _paymentsController.GetPaymentSummariesCount(ukPrn, searchCriteria.StartDate, searchCriteria.EndDate);

            //Assert
            result.Result.GetType().Equals(typeof(BadRequestResult)).Should().BeTrue();
            _mockPaymentService.Verify(x => x.GetPaymentSummariesCount(It.IsAny<PaymentSummarySearchCriteria>()), Times.Never);
        }
    }
}