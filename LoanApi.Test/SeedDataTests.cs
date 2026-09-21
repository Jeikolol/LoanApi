using Application.Features.Loans;
using Application.FakeData;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class SeedDataTests
    {
        private static readonly DateOnly Today = new(2026, 9, 20);
        private static readonly Guid[] Branches = [Guid.NewGuid(), Guid.NewGuid()];

        private IReadOnlyList<Customer> _customers = null!;
        private Dictionary<Guid, Customer> _customersById = null!;
        private LoanPortfolio _portfolio = null!;

        [OneTimeSetUp]
        public void Generate()
        {
            _customers = FakeDbQueryCustomers.Generate();
            _customersById = _customers.ToDictionary(c => c.Id);

            List<CustomerProfile> eligible = _customers
                .Where(c => c.Status == CustomerStatus.Active && !c.IsBlacklisted)
                .Select(c => new CustomerProfile(c.Id, c.AnnualIncome ?? 0m, c.CreditLimit))
                .ToList();

            _portfolio = FakeDbQueryLoanPortfolio.Generate(eligible, Branches, Today);
        }

        private Dictionary<Guid, List<LoanPayment>> PaymentsByLoan =>
            _portfolio.Payments.GroupBy(p => p.LoanId).ToDictionary(g => g.Key, g => g.ToList());

        [Test]
        public void AmortizationSchedule_RepaysExactlyThePrincipal()
        {
            IReadOnlyList<AmortizationInstallment> schedule = AmortizationSchedule.Build(120_000m, 24m, 24);

            schedule.Should().HaveCount(24);
            schedule.Sum(s => s.Principal).Should().Be(120_000m);
            schedule[^1].BalanceAfter.Should().Be(0m);
            schedule.Select(s => s.BalanceAfter).Should().BeInDescendingOrder();
            schedule[0].Interest.Should().Be(2_400m);
            schedule[0].Payment.Should().BeApproximately(6_344m, 1m);
        }

        [Test]
        public void AmortizationSchedule_WithZeroInterest_SplitsEvenly()
        {
            IReadOnlyList<AmortizationInstallment> schedule = AmortizationSchedule.Build(12_000m, 0m, 12);

            schedule.Sum(s => s.Interest).Should().Be(0m);
            schedule.Sum(s => s.Principal).Should().Be(12_000m);
        }

        [Test]
        public void Customers_AreMostlyActiveIndividualsWithUniqueIds()
        {
            _customers.Should().HaveCount(150);
            _customers.Count(c => c.CustomerType == CustomerType.PhysicalPerson).Should().BeGreaterThan(130);
            _customers.Count(c => c.Status == CustomerStatus.Active).Should().BeGreaterThan(125);
            _customers.Select(c => c.Identification).Should().OnlyHaveUniqueItems();
            _customers.Select(c => c.Phone).Should().OnlyHaveUniqueItems();
            _customers.Where(c => c.IsBlacklisted).Should().OnlyContain(c => c.Status == CustomerStatus.Blocked);
        }

        [Test]
        public void Customers_IndividualsHaveNoCompanyAndACédulaOrPassport()
        {
            IEnumerable<Customer> individuals = _customers.Where(c => c.CustomerType == CustomerType.PhysicalPerson);

            individuals.Should().OnlyContain(c => c.CompanyName == null);
            individuals.Where(c => c.IdentificationTypeId == Domain.Constants.SystemConstants.CedulaIdentificationTypeId)
                .Should().OnlyContain(c => System.Text.RegularExpressions.Regex.IsMatch(c.Identification, @"^\d{3}-\d{7}-\d$"));
        }

        [Test]
        public void Customers_HaveDocumentsThatFollowTheFormatRulesOfTheirType()
        {
            Dictionary<Guid, Country> countries = FakeDbQueryCountries.Countries.ToDictionary(c => c.Id);

            foreach (Customer customer in _customers)
            {
                string typeCode =
                    customer.IdentificationTypeId == Domain.Constants.SystemConstants.RncIdentificationTypeId ? Application.Features.Customers.IdentificationRules.RncCode
                    : customer.IdentificationTypeId == Domain.Constants.SystemConstants.PassportIdentificationTypeId ? Application.Features.Customers.IdentificationRules.PassportCode
                    : Application.Features.Customers.IdentificationRules.CedulaCode;

                Application.Features.Customers.IdentificationRules
                    .Validate(typeCode, customer.Identification, countries[customer.NationalityCountryId].CodeIso2)
                    .Should().BeNull($"{customer.Identification} is a {typeCode}");

                (customer.CustomerType == CustomerType.LegalPerson).Should().Be(typeCode == Application.Features.Customers.IdentificationRules.RncCode);

                Application.Features.Customers.IdentificationRules
                    .ValidateCombination(customer.CustomerType, typeCode, countries[customer.NationalityCountryId].CodeIso2)
                    .Should().BeEmpty($"{customer.Identification} ({typeCode}) for a {customer.CustomerType}");
            }
        }

        [Test]
        public void Customers_HaveModestIncomesAndLimitsThatFollowFromThem()
        {
            List<Customer> individuals = _customers.Where(c => c.CustomerType == CustomerType.PhysicalPerson).ToList();

            individuals.Should().OnlyContain(c => c.AnnualIncome >= 144_000m && c.AnnualIncome <= 1_560_000m);
            individuals.Should().OnlyContain(c => c.CreditLimit >= 20_000m && c.CreditLimit <= c.AnnualIncome * 0.61m + 5_000m);

            decimal medianMonthly = individuals.Select(c => c.AnnualIncome!.Value / 12m).OrderBy(x => x).ElementAt(individuals.Count / 2);
            medianMonthly.Should().BeInRange(22_000m, 48_000m);
        }

        [Test]
        public void Loans_AreBelievableConsumerLoans()
        {
            List<decimal> principals = _portfolio.Loans.Select(l => l.PrincipalAmount).OrderBy(x => x).ToList();

            _portfolio.Loans.Count.Should().BeInRange(100, 200);
            principals[principals.Count / 2].Should().BeInRange(25_000m, 150_000m);
            principals[(int)(principals.Count * 0.95)].Should().BeLessThan(400_000m);
            principals.Min().Should().BeGreaterThanOrEqualTo(FakeDbQueryLoanPortfolio.MinimumPrincipal);

            _portfolio.Loans.Should().OnlyContain(l => l.InterestRate >= 10m && l.InterestRate <= 36m);
            _portfolio.Loans.Select(l => l.TermMonths).Distinct().Should().BeSubsetOf([6, 12, 18, 24, 36, 48, 60]);
            _portfolio.Loans.Select(l => l.LoanNumber).Should().OnlyHaveUniqueItems();
            _portfolio.Loans.Should().OnlyContain(l => l.CurrencyId == new Guid("77777777-7777-7777-7777-777777777777"));
        }

        [Test]
        public void Loans_AreSizedFromTheBorrowersMeans()
        {
            foreach (Loan loan in _portfolio.Loans)
            {
                Customer customer = _customersById[loan.CustomerId];

                loan.PrincipalAmount.Should().BeLessThanOrEqualTo(customer.CreditLimit);
                loan.InstallmentAmount.Should().BeLessThanOrEqualTo(customer.AnnualIncome!.Value / 12m * FakeDbQueryLoanPortfolio.MaxInstallmentToIncome + 1m);
            }
        }

        [Test]
        public void Loans_GoOnlyToCustomersWhoMayBorrow_MostlyOneOrTwoEach()
        {
            IEnumerable<Customer> borrowers = _portfolio.Loans.Select(l => _customersById[l.CustomerId]).Distinct();

            borrowers.Should().OnlyContain(c => c.Status == CustomerStatus.Active && !c.IsBlacklisted);

            List<int> perCustomer = _portfolio.Loans.GroupBy(l => l.CustomerId).Select(g => g.Count()).ToList();
            perCustomer.Max().Should().BeLessThanOrEqualTo(3);
            perCustomer.Count(n => n <= 2).Should().BeGreaterThan((int)(perCustomer.Count * 0.85));
            _customers.Count(c => _portfolio.Loans.All(l => l.CustomerId != c.Id)).Should().BeGreaterThan(10);
        }

        [Test]
        public void Payments_AddUpToWhatEachLoanReportsAsPaid()
        {
            Dictionary<Guid, List<LoanPayment>> byLoan = PaymentsByLoan;

            foreach (Loan loan in _portfolio.Loans)
            {
                List<LoanPayment> completed = byLoan.GetValueOrDefault(loan.Id, []).Where(p => p.Status == PaymentStatus.Completed).ToList();

                completed.Sum(p => p.PrincipalAmount).Should().Be(loan.PrincipalPaid, loan.LoanNumber);
                completed.Sum(p => p.InterestAmount).Should().Be(loan.PaidInterest, loan.LoanNumber);
                (loan.PrincipalPaid + loan.PrincipalRemaining).Should().Be(loan.PrincipalAmount, loan.LoanNumber);
                loan.PrincipalRemaining.Should().BeGreaterThanOrEqualTo(0m);
                loan.RemainingInterest.Should().BeGreaterThanOrEqualTo(0m);
            }
        }

        [Test]
        public void Payments_HaveTheirLoansCurrencyAndNeverLieInTheFuture()
        {
            Dictionary<Guid, Loan> loans = _portfolio.Loans.ToDictionary(l => l.Id);

            _portfolio.Payments.Should().OnlyContain(p => p.PaymentDate <= Today);
            _portfolio.Payments.Should().OnlyContain(p => p.CurrencyId == loans[p.LoanId].CurrencyId);
            _portfolio.Payments.Should().OnlyContain(p => p.TotalPaymentAmount == p.PrincipalAmount + p.InterestAmount + p.PenaltyAmount);
        }

        [Test]
        public void Statuses_AgreeWithBalancesAndDates()
        {
            Dictionary<Guid, List<LoanPayment>> byLoan = PaymentsByLoan;
            LoanStatus[] beingCollected = [LoanStatus.Disbursed, LoanStatus.Active, LoanStatus.PartiallyPaid, LoanStatus.Restructured];

            foreach (Loan loan in _portfolio.Loans)
            {
                switch (loan.LoanStatus)
                {
                    case LoanStatus.Pending or LoanStatus.Approved or LoanStatus.Disbursed:
                        byLoan.GetValueOrDefault(loan.Id, []).Should().BeEmpty(loan.LoanNumber);
                        loan.PrincipalRemaining.Should().Be(loan.PrincipalAmount);
                        break;
                    case LoanStatus.FullyPaid or LoanStatus.Closed:
                        loan.PrincipalRemaining.Should().Be(0m, loan.LoanNumber);
                        loan.DaysOverdue.Should().Be(0);
                        break;
                    case LoanStatus.Defaulted:
                        loan.DaysOverdue.Should().BeGreaterThan(0, loan.LoanNumber);
                        break;
                    case LoanStatus.WrittenOff:
                        loan.DaysOverdue.Should().BeGreaterThan(FakeDbQueryLoanPortfolio.WriteOffAfterDays, loan.LoanNumber);
                        break;
                }

                if (beingCollected.Contains(loan.LoanStatus))
                {
                    loan.DaysOverdue.Should().Be(0, $"{loan.LoanNumber} is {loan.LoanStatus}");
                    loan.NextPaymentDueDate.Should().BeOnOrAfter(Today);
                }

                if (loan.DaysOverdue > 0)
                {
                    loan.DaysOverdue.Should().Be(Today.DayNumber - loan.NextPaymentDueDate.DayNumber, loan.LoanNumber);
                }
            }
        }

        [Test]
        public void Portfolio_CoversTheWholeLifecycle_WithASensibleShareOfProblemLoans()
        {
            _portfolio.Loans.Select(l => l.LoanStatus).Distinct().Count().Should().BeGreaterThanOrEqualTo(8);

            double share = _portfolio.Loans.Count(l => l.DaysOverdue > 0) / (double)_portfolio.Loans.Count;
            share.Should().BeInRange(0.03, 0.15);
        }

        [Test]
        public void CreditScores_ExistOnlyForCustomersWithHistory_AndAreOnTheApplicationsScale()
        {
            Dictionary<Guid, List<LoanPayment>> byLoan = PaymentsByLoan;
            List<Customer> withHistory = _customers.Select(c => new Customer
            {
                Id = c.Id,
                CreditLimit = c.CreditLimit,
                IsBlacklisted = c.IsBlacklisted,
                Loans = _portfolio.Loans.Where(l => l.CustomerId == c.Id).Select(l =>
                {
                    l.Payments = byLoan.GetValueOrDefault(l.Id, []);
                    return l;
                }).ToList(),
            }).ToList();

            IReadOnlyList<CreditScore> scores = FakeDbQueryCreditScores.Generate(withHistory);

            scores.Should().HaveCount(withHistory.Count(c => c.Loans.Count > 0));
            scores.Should().OnlyContain(s => s.Score >= 0 && s.Score <= 1000);
            scores.Select(s => s.CustomerId).Should().OnlyHaveUniqueItems();
            scores.Select(s => s.CreditRating).Distinct().Count().Should().BeGreaterThan(1);

            HashSet<Guid> seriouslyOverdue = _portfolio.Loans.Where(l => l.DaysOverdue > 60).Select(l => l.CustomerId).ToHashSet();
            scores.Where(s => seriouslyOverdue.Contains(s.CustomerId)).Should().OnlyContain(s => s.CreditRating != CreditRating.Excellent);
        }

        [Test]
        public void LoanDocuments_AreCoherentWithTheirLoan()
        {
            IReadOnlyList<LoanDocument> documents = FakeDbQueryLoanDocuments.GenerateLoanDocuments(_portfolio.Loans).ToList();
            Dictionary<Guid, Loan> loans = _portfolio.Loans.ToDictionary(l => l.Id);

            documents.Select(d => d.LoanId).Distinct().Should().HaveCount(_portfolio.Loans.Count);

            foreach (LoanDocument document in documents)
            {
                Application.Features.Documents.DocumentRules.TryGetMimeType(document.FileName, out MimeTypes mime).Should().BeTrue(document.FileName);
                document.MimeType.Should().Be(mime, document.FileName);
                document.FileName.Should().EndWith($"{loans[document.LoanId].LoanNumber}.{System.IO.Path.GetExtension(document.FileName).TrimStart('.')}");
                document.UploadedDate.Should().BeOnOrBefore(DateTime.UtcNow);
                Application.Features.Documents.DocumentRules.Validate(document.FileName, document.FileUrl, document.FileSizeBytes).Should().BeEmpty();
            }

            foreach (IGrouping<Guid, LoanDocument> perLoan in documents.GroupBy(d => d.LoanId))
            {
                perLoan.Select(d => d.DocumentType).Should().OnlyHaveUniqueItems();
                perLoan.Select(d => d.DocumentType).Should().Contain([DocumentTypes.Contract, DocumentTypes.PromissoryNote]);
            }
        }

        [Test]
        public void Customers_HavePhonesInTheCanonicalInternationalForm()
        {
            foreach (Customer customer in _customers)
            {
                Application.Features.Customers.PhoneRules.IsValid(customer.Phone).Should().BeTrue(customer.Phone);
                customer.Phone.Should().MatchRegex(@"^\+1(809|829|849)\d{7}$", customer.Phone);

                if (customer.AlternativePhone is not null) Application.Features.Customers.PhoneRules.IsValid(customer.AlternativePhone).Should().BeTrue(customer.AlternativePhone);
                if (customer.EmergencyContactPhone is not null) Application.Features.Customers.PhoneRules.IsValid(customer.EmergencyContactPhone).Should().BeTrue(customer.EmergencyContactPhone);
            }
        }

        [Test]
        public void EverySeededCountryHasADialCodeAndAMaskThatFitsIt()
        {
            foreach (Country country in FakeDbQueryCountries.Countries)
            {
                country.PhoneCode.Should().MatchRegex(@"^[1-9]\d{0,3}$", country.CodeIso2);
                country.PhoneMask.Should().MatchRegex(@"^[9 ()\-]+$", country.CodeIso2);

                int nationalDigits = country.PhoneMask!.Count(c => c == '9');
                (country.PhoneCode!.Length + nationalDigits).Should().BeInRange(7, 15, country.CodeIso2);
            }
        }
    }
}
