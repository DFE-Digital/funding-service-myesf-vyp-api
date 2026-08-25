using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using ViewYourPayments.Core.DTOs;
using ViewYourPayments.Core.Models.Payments;
using ViewYourPayments.EntityFramework;
using ViewYourPayments.Infrastructure.Repositories;
using Xunit;


namespace ViewYourPayments.Infrastructure.Tests
{
    public class PaymentSummaryRepositoryTests
    {
        [Theory]
        [InlineData("1234", 2018, 01, 01, 2020, 10, 10, 0, 20, 10)]
        [InlineData("1234", 2019, 01, 01, 2019, 10, 10, 1, 2, 2)]// total 7 records, first page with page size 2 returns 2
        [InlineData("1234", 2019, 01, 01, 2019, 10, 10, 2, 3, 3)] // total 7 records, second page with page size 3 returns 3
        [InlineData("1234", 2019, 01, 01, 2019, 10, 10, 3, 3, 1)] // total 7 records, third page with page size 3 returns 1
        [InlineData("1234", 2019, 01, 01, 2019, 10, 10, 4, 3, 0)] // total 7 records, fourth page with page size 3 returns 0
        [InlineData("5555", 2019, 01, 01, 2020, 10, 10, 1, 5, 1)]
        [InlineData("9999", 2018, 01, 01, 2020, 10, 10, 1, 10, 0)]
        public async Task GetPaymentSummary_ShouldReturnCorrectRecordsForSelectedParams(string ukPrn,
                                                                                int startYear, int startMonth, int startDate,
                                                                                int endYear, int endMonth, int endDate,
                                                                                int pageNumber, int pageSize,
                                                                                int expectedResultCount)
        {
            // Arrange
            var allPayments = GetPaymentSummaryList();
            var usersMock = new Mock<DbSet<PaymentSummary>>();
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var paymentSummaryMock = new Mock<ViewYourPaymentsDbContext>();
            paymentSummaryMock.Setup(x => x.PaymentSummaries).Returns(usersMock.Object);

            var paymentSummaryRepository = new PaymentSummaryRepository(paymentSummaryMock.Object);

            var searchCriteria = new PaymentSummarySearchCriteria
            {
                StartDate = new DateTime(startYear, startMonth, startDate),
                EndDate = new DateTime(endYear, endMonth, endDate),
                Ukprn = ukPrn,
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            //// Act
            var result = await paymentSummaryRepository.GetPaymentSummaries(searchCriteria);

            // Assert
            result.Count().Should().Be(expectedResultCount);
        }

        [Fact]
        public async Task GetPaymentSummary_ShouldReturnInCorrecOrder()
        {
            // Arrange
            var allPayments = GetPaymentSummaryList();
            var usersMock = new Mock<DbSet<PaymentSummary>>();
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.Provider).Returns(allPayments.Provider);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.Expression).Returns(allPayments.Expression);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.ElementType).Returns(allPayments.ElementType);
            usersMock.As<IQueryable<PaymentSummary>>().Setup(m => m.GetEnumerator()).Returns(allPayments.GetEnumerator());

            var paymentSummaryMock = new Mock<ViewYourPaymentsDbContext>();
            paymentSummaryMock.Setup(x => x.PaymentSummaries).Returns(usersMock.Object);

            var paymentSummaryRepository = new PaymentSummaryRepository(paymentSummaryMock.Object);
            var searchCriteria = new PaymentSummarySearchCriteria
            {
                StartDate = new DateTime(2018, 01, 01),
                EndDate = new DateTime(2020, 12, 31),
                Ukprn = "1234",
                PageSize = 20,
                PageNumber = 1
            };

            //// Act
            var result = await paymentSummaryRepository.GetPaymentSummaries(searchCriteria);

            // Assert
            result.Count().Should().Be(10);
            result.First().Id.Should().Be(10);
            result.Select(x => x.Id).ToArray().Should().BeEquivalentTo(new[] { 10, 7, 6, 5, 4, 3, 2, 1, 9, 8 }, options => options.WithStrictOrdering());
        }

        private IQueryable<PaymentSummary> GetPaymentSummaryList()
        {
            return new[]
            {
                new PaymentSummary {Id = 1, PaymentDate = new DateTime(2019,01,01,10,10,20), Ukprn ="1234", PaymentTotal =122.4M  },
                new PaymentSummary {Id = 2, PaymentDate = new DateTime(2019,01,01,11,10,20), Ukprn ="1234", PaymentTotal =123.4M  },
                new PaymentSummary {Id = 3, PaymentDate = new DateTime(2019,01,02,10,10,20), Ukprn ="1234", PaymentTotal =192.4M  },
                new PaymentSummary {Id = 4, PaymentDate = new DateTime(2019,01,02,10,10,25), Ukprn ="1234", PaymentTotal =555.2M  },
                new PaymentSummary {Id = 5, PaymentDate = new DateTime(2019,01,03,10,10,25), Ukprn ="1234", PaymentTotal =922.4M  },
                new PaymentSummary {Id = 6, PaymentDate = new DateTime(2019,01,04,10,10,25), Ukprn ="1234", PaymentTotal =1022.4M  },
                new PaymentSummary {Id = 7, PaymentDate = new DateTime(2019,02,01,05,10,25), Ukprn ="1234", PaymentTotal =22.4M  },
                new PaymentSummary {Id = 8, PaymentDate = new DateTime(2018,11,01,10,10,20), Ukprn ="1234", PaymentTotal =652.4M  },
                new PaymentSummary {Id = 9, PaymentDate = new DateTime(2018,11,02,10,10,20), Ukprn ="1234", PaymentTotal =3343.5M  },
                new PaymentSummary {Id = 10, PaymentDate = new DateTime(2020,01,01,05,10,25), Ukprn ="1234", PaymentTotal =282.4M  },
                new PaymentSummary {Id = 11, PaymentDate = new DateTime(2020,01,01,05,10,25), Ukprn ="5555", PaymentTotal =282.4M  },
                new PaymentSummary {Id = 12, PaymentDate = new DateTime(2018,11,01,10,10,20), Ukprn ="5555", PaymentTotal =652.4M  },
                new PaymentSummary {Id = 13, PaymentDate = new DateTime(2019,01,01,10,10,20), Ukprn ="8080", PaymentTotal =422.4M  },
                new PaymentSummary {Id = 14, PaymentDate = new DateTime(2019,01,01,11,10,20), Ukprn ="8080", PaymentTotal =623.4M  },
            }.AsQueryable();
        }
    }
}
