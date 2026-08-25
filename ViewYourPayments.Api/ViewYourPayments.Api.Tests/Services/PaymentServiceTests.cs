using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Api.Services;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Interfaces;
using ViewYourPayments.Core.Interfaces.Repositories;
using ViewYourPayments.Core.Interfaces.Services;
using ViewYourPayments.Core.Models.Payments;
using Xunit;

namespace ViewYourPayments.Api.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentSummaryRepository> _mockPaymentSummaryRepository;
        private readonly Mock<IPaymentLineRepository> _mockPaymentLineRepository;

        private readonly Mock<IApplicationLogger> _mockLogger;
        private readonly IPaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockLogger = new Mock<IApplicationLogger>();
            _mockPaymentSummaryRepository = new Mock<IPaymentSummaryRepository>();
            _mockPaymentLineRepository = new Mock<IPaymentLineRepository>();
            _paymentService = new PaymentsService(_mockPaymentSummaryRepository.Object, _mockPaymentLineRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPaymentSummaries_WheHasMatchingRecords_ShouldReturnCorrectData()
        {
            //Arrange
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 12, 31),
                PageNumber = 1,
                PageSize = 2
            };
            _mockPaymentSummaryRepository.Setup(x => x.GetPaymentSummaries(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(GetPaymentSummaryList());
            _mockPaymentSummaryRepository.Setup(x => x.GetPaymentSummariesCount(It.IsAny<PaymentSummarySearchCriteria>())).ReturnsAsync(GetPaymentSummaryList().Count());

            //Action
            var result = await _paymentService.GetPaymentSummaries(searchCriteria);

            //Assert
            result.ProviderUKPRN.Should().Be("1234");
            result.DateFrom.Should().Be(searchCriteria.StartDate);
            result.DateTo.Should().Be(searchCriteria.EndDate);
            result.TotalPages.Should().Be(4);
            result.TotalRecords.Should().Be(7);
            result.CurrentPage.Should().Be(searchCriteria.PageNumber);
            result.PaymentSummaries.Length.Should().Be(7);
        }

        [Fact]
        public async Task GetPaymentSummaries_WhenNoMatchingRecords_ShouldNotReturnPaymentSummaries()
        {
            //Arrange
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 12, 31),
                PageNumber = 0,
                PageSize = 10
            };
            _mockPaymentSummaryRepository.Setup(x => x.GetPaymentSummaries(searchCriteria)).ReturnsAsync(GetEmptyPaymentSummaryList());
            _mockPaymentSummaryRepository.Setup(x => x.GetPaymentSummariesCount(searchCriteria)).ReturnsAsync(0);

            //Action
            var result = await _paymentService.GetPaymentSummaries(searchCriteria);

            //Assert
            result.ProviderUKPRN.Should().Be("1234");
            result.DateFrom.Should().Be(searchCriteria.StartDate);
            result.DateTo.Should().Be(searchCriteria.EndDate);
            result.PaymentSummaries.Length.Should().Be(0);
        }

        [Fact]
        public async Task GetPaymentDetails_WhenNoMatchingRecords_ShouldReturnEmpty()
        {
            //Arrange
            _mockPaymentLineRepository.Setup(x => x.GetPaymentLines(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(Enumerable.Empty<PaymentLine>());

            //Action
            var result = await _paymentService.GetPaymentDetails("123455", 1211111);

            //Assert
            result.PaymentLines.Count().Should().Be(0);
            result.PaymentAmount.Should().Be(null);
            result.PaymentDate.Should().Be(null);
            result.ProviderFinanceVendorIdentifier.Should().Be(null);
        }

        [Fact]
        public async Task GetPaymentDetails_WhenHasMatchingRecords_ShouldReturnPaymentDetails()
        {
            //Arrange
            _mockPaymentLineRepository.Setup(x => x.GetPaymentLines(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(GetPaymentLines());

            //Action
            var result = await _paymentService.GetPaymentDetails("123455", 1211111);

            //Assert
            result.PaymentLines.Count().Should().Be(6);
            result.PaymentAmount.Should().Be(122.4M);
            result.ProviderFinanceVendorIdentifier.Should().Be("P100");
        }

        [Fact]
        public async Task GetPaymentTransactions_WhenNoMatchingRecords_ShouldReturnEmptyResult()
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 12, 31),
                PageNumber = 0,
                PageSize = 10
            };
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactions(searchCriteria)).ReturnsAsync(Enumerable.Empty<PaymentLine>());
            _mockPaymentLineRepository.Setup(x => x.GetBudgetGroupSummaries(searchCriteria)).ReturnsAsync(Enumerable.Empty<BudgetGroupItem>());
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactionCount(searchCriteria)).ReturnsAsync(0);
            //Action
            var result = await _paymentService.GetPaymentTransactions(searchCriteria);

            //Assert
            result.PaymentLines.Count().Should().Be(0);
            result.BudgetGroupSummary.Should().BeNull();
            result.TotalRecords.Should().Be(0);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactions(searchCriteria), Times.Once);
            _mockPaymentLineRepository.Verify(x => x.GetBudgetGroupSummaries(searchCriteria), Times.Once);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactionCount(searchCriteria), Times.Once);
        }

        [Fact]
        public async Task GetPaymentTransactions_WhenHasMatchingRecords_ShouldReturnPaymentTransactions()
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 12, 31),
                PageNumber = 0,
                PageSize = 10
            };
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactions(searchCriteria)).ReturnsAsync(GetPaymentLines());
            _mockPaymentLineRepository.Setup(x => x.GetBudgetGroupSummaries(searchCriteria)).ReturnsAsync(GetBudgetGroupSummaries());
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactionCount(searchCriteria)).ReturnsAsync(10);
            //Action
            var result = await _paymentService.GetPaymentTransactions(searchCriteria);

            //Assert
            result.PaymentLines.Count().Should().Be(6);
            result.BudgetGroupSummary.BudgetGroups.Count().Should().Be(2);
            result.TotalRecords.Should().Be(10);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactions(searchCriteria), Times.Once);
            _mockPaymentLineRepository.Verify(x => x.GetBudgetGroupSummaries(searchCriteria), Times.Once);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactionCount(searchCriteria), Times.Once);
        }

        [Fact]
        public async Task GetPaymentSummariesCount_WhenHasMatchingRecords_ShouldReturnGetPaymentSummariesCount()
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 01, 02)
            };
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactionCount(searchCriteria)).ReturnsAsync(4);
            //Action
            var result = await _paymentService.GetPaymentTransactions(searchCriteria);

            //Assert
            result.TotalRecords.Should().Be(4);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactionCount(searchCriteria), Times.Once);
        }

        [Fact]
        public async Task GetPaymentSummariesCount_WhenNoMatchingRecords_ShouldReturnsZero()
        {
            //Arrange
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                Ukprn = "1234",
                StartDate = new DateTime(2020, 01, 01),
                EndDate = new DateTime(2020, 01, 02)
            };
            _mockPaymentLineRepository.Setup(x => x.GetPaymentTransactionCount(searchCriteria)).ReturnsAsync(0);
            //Action
            var result = await _paymentService.GetPaymentTransactions(searchCriteria);

            //Assert
            result.TotalRecords.Should().Be(0);
            _mockPaymentLineRepository.Verify(x => x.GetPaymentTransactionCount(searchCriteria), Times.Once);
        }

        private IEnumerable<PaymentSummary> GetPaymentSummaryList()
        {
            return new[]
            {
                new PaymentSummary {PaymentIdentifier = 1, PaymentDate = new DateTime(2019,01,01,10,10,20), Ukprn ="1234", PaymentTotal =122.4M, VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 2, PaymentDate = new DateTime(2019,01,01,11,10,20), Ukprn ="1234", PaymentTotal =123.4M, VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 3, PaymentDate = new DateTime(2019,01,02,10,10,20), Ukprn ="1234", PaymentTotal =192.4M, VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 4, PaymentDate = new DateTime(2019,01,02,10,10,25), Ukprn ="1234", PaymentTotal =555.2M, VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 5, PaymentDate = new DateTime(2019,01,03,10,10,25), Ukprn ="1234", PaymentTotal =922.4M , VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 6, PaymentDate = new DateTime(2019,01,04,10,10,25), Ukprn ="1234", PaymentTotal =1022.4M, VendorIdentifier="P100"  },
                new PaymentSummary {PaymentIdentifier = 7, PaymentDate = new DateTime(2019,02,01,05,10,25), Ukprn ="1234", PaymentTotal =22.4M, VendorIdentifier="P100"  }
            };
        }
        private PaymentSummary GetPaymentSummaryItem(string ukprn, int paymentIdentifier, int paymentSummaryId)
        {
            return new PaymentSummary { Id = paymentSummaryId, PaymentDate = new DateTime(2019, 01, 01, 10, 10, 20), Ukprn = ukprn, VendorIdentifier = "P100", PaymentTotal = 122.4M, PaymentIdentifier = paymentIdentifier };
        }
        private IEnumerable<PaymentLine> GetPaymentLines()
        {
            return new[]
            {
                 new PaymentLine {Id =1, BudgetGroup="Test1", PaymentLineIdentifier ="101",LineAmount=1001.22M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 2",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },
                new PaymentLine {Id =2, BudgetGroup="Test2", PaymentLineIdentifier ="102",LineAmount=-50.05M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 3",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },
                new PaymentLine {Id =3, BudgetGroup="Test3", PaymentLineIdentifier ="103",LineAmount=121.22M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 4",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },
                new PaymentLine {Id =4, BudgetGroup="Test4", PaymentLineIdentifier ="104",LineAmount=199.67M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 5",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },
                new PaymentLine {Id =5, BudgetGroup="Test5", PaymentLineIdentifier ="105",LineAmount=100001.22M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 6",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },
                new PaymentLine {Id =6, BudgetGroup="Other", PaymentLineIdentifier ="106",LineAmount=41001.22M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 7",
                    PaymentSummary = GetPaymentSummaryItem("1234",101,1001)
                },            };
        }

        private IEnumerable<BudgetGroupItem> GetBudgetGroupSummaries()
        {
            return new[]
            {
                new BudgetGroupItem{BudgetDescription = "Budget Group1", BudgetAmount= 2232.99M },
                new BudgetGroupItem{BudgetDescription ="Budget Group2", BudgetAmount= -122.9M }
            };
        }
        private IEnumerable<PaymentSummary> GetEmptyPaymentSummaryList()
        {
            return Enumerable.Empty<PaymentSummary>();
        }
    }
}
