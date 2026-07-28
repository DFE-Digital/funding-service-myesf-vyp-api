using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Enums;
using ViewYourPayments.Core.Models.Payments;
using ViewYourPayments.EntityFramework;
using ViewYourPayments.Infrastructure.Repositories;
using Xunit;


namespace ViewYourPayments.Infrastructure.Tests
{
    public class PaymentLineRepositoryTests
    {
        [Theory]
        [InlineData("1234", 1, 3)]       //Record exists for ukprn and paymentIdentifier with matching combination
        [InlineData("1234", 2, 1)]      //Record exists for ukprn and paymentIdentifier with matching combination
        [InlineData("5555", 201, 1)]     //Record exists for ukprn and paymentIdentifier with matching combination
        [InlineData("5555", 1, 0)]      //Record exists for ukprn and paymentIdentifier but not in matching combination
        [InlineData("1234", 121000, 0)]    //Record exist for ukprn but not for paymentIdentifier
        public async Task GetPaymentDetails_ShouldReturnCorrectRecordsForSelectedParams(string ukPrn,
                                                                                int paymentIdentifier,
                                                                                int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var paymentSummaryMock = new Mock<ViewYourPaymentsDbContext>();
            paymentSummaryMock.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(paymentSummaryMock.Object);

            //// Act
            var result = await paymentLineRepository.GetPaymentLines(ukPrn, paymentIdentifier);

            // Assert
            result.Count().Should().Be(expectedResultCount);
        }

        [Theory]
        [InlineData("1234", 1, 3)]       //Record exists for ukprn and paymentIdentifier with matching combination
        public async Task GetPaymentDetails_ShouldReturnRecordsInCorrectOrder(string ukPrn,
                                                                                int paymentIdentifier,
                                                                                int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var paymentSummaryMock = new Mock<ViewYourPaymentsDbContext>();
            paymentSummaryMock.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(paymentSummaryMock.Object);

            //// Act
            var result = await paymentLineRepository.GetPaymentLines(ukPrn, paymentIdentifier);

            // Assert
            result.Count().Should().Be(expectedResultCount);
            //Order by Contract,PaymentLineDescription
            result.Select(x => x.Id).ToArray().Should().BeEquivalentTo(new[] { 2, 3, 1 }, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData("1234", 2019, 01, 01, 2019, 01, 20, 0, 20, 14)] //Total 14 Records -- No paging (Page number is zero)
        [InlineData("1234", 2019, 01, 01, 2019, 01, 20, 4, 0, 14)] //Total 14 Records -- No paging (Page sixe is zero)
        [InlineData("1234", 2019, 01, 01, 2019, 01, 10, 1, 2, 2)]// total 7 records, first page with page size 2 returns 2
        [InlineData("1234", 2019, 01, 01, 2019, 01, 10, 2, 3, 3)] // total 7 records, second page with page size 3 returns 3
        [InlineData("1234", 2019, 01, 01, 2019, 01, 10, 3, 3, 1)] // total 7 records, third page with page size 3 returns 1
        [InlineData("1234", 2019, 01, 01, 2019, 01, 10, 4, 3, 0)] // total 7 records, fourth page with page size 3 returns 0
        [InlineData("5555", 2019, 01, 01, 2020, 01, 10, 1, 5, 2)]
        [InlineData("9999", 2018, 01, 01, 2020, 01, 10, 1, 10, 0)]
        public async Task GetPaymentTransactions_ShouldReturnCorrectRecordsForSelectedParams(string ukPrn,
                                                                                int startYear, int startMonth, int startDate,
                                                                                int endYear, int endMonth, int endDate,
                                                                                int pageNumber, int pageSize,
                                                                                int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var viewYourPaymentDbContext = new Mock<ViewYourPaymentsDbContext>();
            viewYourPaymentDbContext.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(viewYourPaymentDbContext.Object);

            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                StartDate = new DateTime(startYear, startMonth, startDate),
                EndDate = new DateTime(endYear, endMonth, endDate),
                Ukprn = ukPrn,
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            //// Act
            var result = await paymentLineRepository.GetPaymentTransactions(searchCriteria);

            // Assert
            result.Count().Should().Be(expectedResultCount);
        }

        [Theory]
        [InlineData(PaymentTransactionSortFields.PaymentDate,SortDirection.Descending, "17,14,15,16,13,12,11,10,9,5,4,3,2,1")]
        [InlineData(PaymentTransactionSortFields.PaymentDate, SortDirection.Ascending, "1,2,3,4,5,9,10,11,12,13,17,14,15,16")]
        [InlineData(PaymentTransactionSortFields.Contract, SortDirection.Ascending, "3,2,5,4,17,14,15,16,10,1,9,11,12,13")] //1DUT,A1BC,A124BCD,ABC1,BCDEE,EDUE,EDUT,Levi,NILR,NILV,PAYT
        [InlineData(PaymentTransactionSortFields.Contract, SortDirection.Descending, "13,12,11,9,1,10,14,15,16,17,5,4,3,2")]
        [InlineData(PaymentTransactionSortFields.LineAmount, SortDirection.Ascending, "2,1,3,4,9,14,5,13,10,12,11,17,16,15")]//100.1,200.1,300.33,801.22,10100.44,50001.22,71001.22,91001.22, 111001.22,221001.22, 471001.22 ,571001.22,671001.22
        [InlineData(PaymentTransactionSortFields.PaymentLineDescription, SortDirection.Ascending, "2,3,1,9,10,14,15,16,17,11,12,13,4,5")]
        public async Task GetPaymentTransactions_ShouldReturnRecordsInCorrectOrder(PaymentTransactionSortFields primarySortField, SortDirection sortDirection, string expectedResultOrder)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var paymentSummaryMock = new Mock<ViewYourPaymentsDbContext>();
            paymentSummaryMock.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(paymentSummaryMock.Object);
            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                StartDate = new DateTime(2019, 01, 01),
                EndDate = new DateTime(2019, 01, 20),
                Ukprn = "1234",
                PageSize = 20,
                PageNumber = 1,
                PrimarySortField = primarySortField,
                SortDirection = sortDirection
            };

            //// Act
            var result = await paymentLineRepository.GetPaymentTransactions(searchCriteria);

            // Assert
            result.Count().Should().Be(14);
            var expectedResult = Array.ConvertAll(expectedResultOrder.Split(","),int.Parse);
            //Order by Contract,PaymentLineDescription
            result.Select(x => x.Id).ToArray().Should().BeEquivalentTo(expectedResult, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData("1234","" , 14)]
        [InlineData("1234", null, 14)]
        [InlineData("1234", "Test", 11)]
        [InlineData("1234", "Description,Test", 14)]
        [InlineData("1234", "Description,6", 14)]
        [InlineData("1234", "5,6", 4)]
        [InlineData("1234", "Three,6", 3)]
        public async Task GetPaymentTransactions_ShouldReturnCorrectRecordsForSelectedParams_withSearchTerms(string ukPrn,
                                                                               string searchTerms,
                                                                               int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var viewYourPaymentDbContext = new Mock<ViewYourPaymentsDbContext>();
            viewYourPaymentDbContext.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(viewYourPaymentDbContext.Object);

            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                StartDate = new DateTime(2018,01,01),
                EndDate = new DateTime(2019,12,12),
                Ukprn = ukPrn,
                PageSize = 100,
                PageNumber = 1,
                SearchTerms = searchTerms?.Split(",")
            };

            //// Act
            var result = await paymentLineRepository.GetPaymentTransactions(searchCriteria);

            // Assert
            result.Count().Should().Be(expectedResultCount);
        }

        [Theory]
        [InlineData("1234", 2019, 01, 01, 2019, 01, 20, 5)] 
        [InlineData("1234", 2019, 01, 01, 2019, 01, 10,  4)]
        [InlineData("9999", 2018, 01, 01, 2020, 01, 10,  0)]
        public async Task GetUniqueTransactionDescriptions_ShouldReturnCorrectRecordsForSelectedParams(string ukPrn,
                                                                                int startYear, int startMonth, int startDate,
                                                                                int endYear, int endMonth, int endDate,
                                                                                int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var viewYourPaymentDbContext = new Mock<ViewYourPaymentsDbContext>();
            viewYourPaymentDbContext.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(viewYourPaymentDbContext.Object);

            //// Act
            var result = await paymentLineRepository.GetUniqueTransactionDescriptions(ukPrn,new DateTime(startYear,startMonth,startDate),new DateTime(endYear,endMonth,endDate));

            // Assert
            result.Count().Should().Be(expectedResultCount);
        }

        [Theory]
        [InlineData("1234", 2019, 01, 01, 2019, 01, 05, 4, "Other,Test1,Test2,Test3", 50501.65)] //Total 5 Records
        [InlineData("1234", 2020, 01, 01, 2020, 01, 10,  0, null,0)] // total 0 records
        [InlineData("4444", 2018, 01, 01, 2020, 01, 10, 1, "Test3", 41001.22)]
        public async Task GetBudgetGroupSummaries_ShouldReturnCorrectBudgetSummariesForSelectedParams(string ukPrn,
                                                                                int startYear, int startMonth, int startDate,
                                                                                int endYear, int endMonth, int endDate,
                                                                                int expectedResultCount,
                                                                                string budgetSummaries, decimal budgetAmount)
        {
            // Arrange
            var allPayments = GetPaymentLinesList();
            var usersMock = new Mock<DbSet<PaymentLine>>();
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentLine>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var viewYourPaymentDbContext = new Mock<ViewYourPaymentsDbContext>();
            viewYourPaymentDbContext.Setup(x => x.PaymentLines).Returns(usersMock.Object);

            var paymentLineRepository = new PaymentLineRepository(viewYourPaymentDbContext.Object);

            var searchCriteria = new PaymentTransactionSearchCriteria
            {
                StartDate = new DateTime(startYear, startMonth, startDate),
                EndDate = new DateTime(endYear, endMonth, endDate),
                Ukprn = ukPrn
            };

            //// Act
            var result = await paymentLineRepository.GetBudgetGroupSummaries(searchCriteria);

            // Assert
            var expectedBudgetItems = budgetSummaries?.Split(",")?? Array.Empty<string>();
            result.Count().Should().Be(expectedResultCount);
            result.Select(x => x.BudgetDescription).ToArray().Should().BeEquivalentTo(expectedBudgetItems, options => options.WithStrictOrdering());
            result.Sum(x => x.BudgetAmount).Should().Be(budgetAmount);
        }

        private IQueryable<PaymentLine> GetPaymentLinesList()
        {
            return new[]
            {
                new PaymentLine {Id =1,Contract="EDUT", BudgetGroup="Test1", PaymentLineIdentifier ="101",LineAmount=100.1M,
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Description Two", PaymentLineGroupDescription ="Description A",
                    PaymentSummary =GetPaymentSummaryItem("1234",1,1001,1)
                },
                new PaymentLine {Id =2,Contract="1DUT", BudgetGroup="Test2", PaymentLineIdentifier ="102",LineAmount=-100.1M, 
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Description One", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",1,1001,2)
                },
                new PaymentLine {Id =3,Contract="1DUT", BudgetGroup="Test2", PaymentLineIdentifier ="103",LineAmount=200.1M, 
                    PaymentSummaryId =1001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Description Three", PaymentLineGroupDescription ="Description C",
                    PaymentSummary =GetPaymentSummaryItem("1234",1,1001,3)
                },
                new PaymentLine {Id =4,Contract="A124BCD", BudgetGroup="Other", PaymentLineIdentifier ="104",LineAmount=300.33M, 
                    PaymentSummaryId =2001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 5", PaymentLineGroupDescription ="Description A",
                    PaymentSummary =GetPaymentSummaryItem("1234",2,2001,4)
                },
                new PaymentLine {Id =5,Contract="A124BCD", BudgetGroup="Test3", PaymentLineIdentifier ="105",LineAmount=50001.22M, 
                    PaymentSummaryId =3001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 6", PaymentLineGroupDescription ="Description",
                    PaymentSummary =GetPaymentSummaryItem("1234",3,3001,5)
                },
                new PaymentLine {Id =6,Contract="BCDEE", BudgetGroup="Test3", PaymentLineIdentifier ="106",LineAmount=41001.22M, 
                    PaymentSummaryId =10001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 7", PaymentLineGroupDescription ="Description",
                    PaymentSummary =GetPaymentSummaryItem("4444",101,10001,6)
                },
                new PaymentLine {Id =7,Contract="EDUT", BudgetGroup="Other", PaymentLineIdentifier ="107",LineAmount=-5001.22M, 
                    PaymentSummaryId =20001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 8", PaymentLineGroupDescription ="Description A",
                    PaymentSummary =GetPaymentSummaryItem("5555",201,20001,7)
                },
                new PaymentLine {Id =8,Contract="PAYT", BudgetGroup="Test5", PaymentLineIdentifier ="108",LineAmount=-7001.22M,
                    PaymentSummaryId =20002,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 9", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("5555",202, 20002,8)
                },
                new PaymentLine {Id =9,Contract="Levi", BudgetGroup="Test5", PaymentLineIdentifier ="109",LineAmount=801.22M, 
                    PaymentSummaryId =300001,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 10", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",301,300001,9)
                },
                new PaymentLine {Id =10,Contract="EDUE", BudgetGroup="Other", PaymentLineIdentifier ="110",LineAmount=91001.22M, 
                    PaymentSummaryId =300002,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 11", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",302, 300002,10)
                },
                new PaymentLine {Id =11,Contract="NILR", BudgetGroup="Test11", PaymentLineIdentifier ="111",LineAmount=221001.22M, 
                    PaymentSummaryId =300005,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 12", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",305,300005,11)
                },
                new PaymentLine {Id =12,Contract="NILV", BudgetGroup="Test12", PaymentLineIdentifier ="112",LineAmount=111001.22M,
                    PaymentSummaryId =300006,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 13", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",306,300006,12)
                },
                new PaymentLine {Id =13, Contract="PAYT", BudgetGroup="Test13", PaymentLineIdentifier ="113",LineAmount=71001.22M,
                    PaymentSummaryId =1,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 14", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",307,300007,13)
                },
                new PaymentLine {Id =14, Contract="ABC1", BudgetGroup="PQR", PaymentLineIdentifier ="114",LineAmount=10100.44M, 
                    PaymentSummaryId =1,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 114", PaymentLineGroupDescription ="Description B",
                    PaymentSummary =GetPaymentSummaryItem("1234",307,300007,14)
                },
                new PaymentLine {Id =15, Contract="ABC1", BudgetGroup="PQR", PaymentLineIdentifier ="115",LineAmount=671001.22M,
                    PaymentSummaryId =1,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 115", PaymentLineGroupDescription ="Description C",
                    PaymentSummary =GetPaymentSummaryItem("1234",307,300007,14)
                },
                new PaymentLine {Id =16, Contract="ABC1", BudgetGroup="PRT", PaymentLineIdentifier ="116",LineAmount=571001.22M, 
                    PaymentSummaryId =1,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 116", PaymentLineGroupDescription ="Description D",
                    PaymentSummary =GetPaymentSummaryItem("1234",307,300007,14)
                },
                new PaymentLine {Id =17, Contract="A1BC", BudgetGroup="XYZ", PaymentLineIdentifier ="116",LineAmount=471001.22M,
                    PaymentSummaryId =1,PostingDate=DateTime.UtcNow,PaymentLineDescription ="Test Description 117", PaymentLineGroupDescription ="Description C",
                    PaymentSummary =GetPaymentSummaryItem("1234",307,300007,14)
                },
            }.AsQueryable();
        }

        private PaymentSummary GetPaymentSummaryItem(string ukprn,int paymentIdentifier, int paymentSummaryId, int PaymentDay =1)
        {
            return new PaymentSummary { Id = paymentSummaryId, PaymentDate = new DateTime(2019, 01, PaymentDay), Ukprn = ukprn, PaymentTotal = 122.4M, PaymentIdentifier =paymentIdentifier };
        }
    }
}
