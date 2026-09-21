using Application.Features.Loans;
using Domain.Entities;
using Domain.Enums;

namespace Application.FakeData
{
    public sealed record CustomerProfile(Guid Id, decimal AnnualIncome, decimal CreditLimit);

    public sealed record LoanPortfolio(IReadOnlyList<Loan> Loans, IReadOnlyList<LoanPayment> Payments);

    /// <summary>
    /// Generates demo loans together with their payments so that every figure agrees, and so that the numbers look
    /// like real consumer lending: amounts are a fraction of each customer's credit limit, installments stay within
    /// 35% of monthly income, rates follow the loan purpose, and most customers have one or two loans. Balances come
    /// from a real amortization schedule, payments add up to what each loan says was paid, and due dates and overdue
    /// days are relative to <c>today</c>.
    /// </summary>
    public static class FakeDbQueryLoanPortfolio
    {
        public const int WriteOffAfterDays = 90;
        public const decimal MaxInstallmentToIncome = 0.35m;
        public const decimal MinimumPrincipal = 5_000m;

        private static readonly Guid DominicanPesoId = new("77777777-7777-7777-7777-777777777777");

        private static readonly (LoanStatus Status, int Weight)[] StatusMix =
        [
            (LoanStatus.Pending, 4),
            (LoanStatus.Approved, 4),
            (LoanStatus.Disbursed, 6),
            (LoanStatus.Active, 48),
            (LoanStatus.PartiallyPaid, 14),
            (LoanStatus.FullyPaid, 12),
            (LoanStatus.Defaulted, 5),
            (LoanStatus.WrittenOff, 2),
            (LoanStatus.Restructured, 3),
            (LoanStatus.Closed, 6),
        ];

        private static readonly (LoanPurpose Purpose, int Weight, double MinRate, double MaxRate)[] Purposes =
        [
            (LoanPurpose.Personal, 60, 24, 36),
            (LoanPurpose.Business, 20, 18, 28),
            (LoanPurpose.Education, 8, 12, 18),
            (LoanPurpose.Agriculture, 4, 12, 18),
            (LoanPurpose.RealEstate, 8, 10, 16),
        ];

        private static readonly (int Months, int Weight)[] Terms = [(6, 8), (12, 25), (18, 12), (24, 25), (36, 18), (48, 8), (60, 4)];

        private static readonly PaymentMethod[] PaymentMethods =
        [
            PaymentMethod.BankTransfer, PaymentMethod.BankTransfer, PaymentMethod.Cash, PaymentMethod.Cash,
            PaymentMethod.OnlinePayment, PaymentMethod.Check, PaymentMethod.Debit, PaymentMethod.CreditCard,
        ];

        private sealed record LoanTerms(decimal Principal, decimal Rate, int Term, LoanPurpose Purpose);

        public static LoanPortfolio Generate(
            IReadOnlyList<CustomerProfile> customers, IReadOnlyList<Guid> branchIds, DateOnly today, int seed = 654321)
        {
            if (branchIds.Count == 0) throw new ArgumentException("At least one branch is required.", nameof(branchIds));

            var random = new Random(seed);
            var loans = new List<Loan>();
            var payments = new List<LoanPayment>();
            var loanNumbers = new HashSet<string>();

            foreach (CustomerProfile customer in customers)
            {
                decimal borrowed = 0m;

                for (int i = 0; i < LoanCountFor(random); i++)
                {
                    LoanTerms? terms = ChooseTerms(random, customer, borrowed);
                    if (terms is null) break;

                    borrowed += terms.Principal;

                    string number;
                    do
                    {
                        number = $"LN-{random.Next(100_000_000, 999_999_999)}";
                    }
                    while (!loanNumbers.Add(number));

                    (Loan loan, List<LoanPayment> loanPayments) = BuildLoan(
                        random, PickStatus(random), number, customer.Id, branchIds[random.Next(branchIds.Count)], terms, today);

                    loans.Add(loan);
                    payments.AddRange(loanPayments);
                }
            }

            return new LoanPortfolio(loans, payments);
        }

        private static int LoanCountFor(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.20) return 0;
            if (roll < 0.75) return 1;
            if (roll < 0.95) return 2;
            return 3;
        }

        private static LoanStatus PickStatus(Random random)
        {
            int roll = random.Next(StatusMix.Sum(m => m.Weight));
            foreach ((LoanStatus status, int weight) in StatusMix)
            {
                if (roll < weight) return status;
                roll -= weight;
            }

            return LoanStatus.Active;
        }

        private static LoanTerms? ChooseTerms(Random random, CustomerProfile customer, decimal alreadyBorrowed)
        {
            int purposeRoll = random.Next(Purposes.Sum(p => p.Weight));
            var purpose = Purposes[0];
            foreach (var candidate in Purposes)
            {
                if (purposeRoll < candidate.Weight)
                {
                    purpose = candidate;
                    break;
                }

                purposeRoll -= candidate.Weight;
            }

            decimal rate = Math.Round((decimal)(purpose.MinRate + random.NextDouble() * (purpose.MaxRate - purpose.MinRate)) * 2m, 0) / 2m;

            decimal cap = Math.Min(customer.CreditLimit, customer.CreditLimit * 1.6m - alreadyBorrowed);
            decimal principal = RoundTo(cap * (decimal)(0.25 + 0.75 * Math.Pow(random.NextDouble(), 1.5)), 1_000m);
            if (principal < MinimumPrincipal) return null;

            int termRoll = random.Next(Terms.Sum(t => t.Weight));
            int chosenTerm = Terms[0].Months;
            foreach ((int months, int weight) in Terms)
            {
                if (termRoll < weight)
                {
                    chosenTerm = months;
                    break;
                }

                termRoll -= weight;
            }

            decimal maxInstallment = customer.AnnualIncome / 12m * MaxInstallmentToIncome;

            foreach ((int months, _) in Terms.Where(t => t.Months >= chosenTerm))
            {
                if (AmortizationSchedule.Build(principal, rate, months)[0].Payment <= maxInstallment)
                {
                    return new LoanTerms(principal, rate, months, purpose.Purpose);
                }
            }

            int longest = Terms[^1].Months;
            decimal installmentAtLongest = AmortizationSchedule.Build(principal, rate, longest)[0].Payment;
            decimal reduced = Math.Floor(principal * maxInstallment / installmentAtLongest / 1000m) * 1000m;

            return reduced < MinimumPrincipal ? null : new LoanTerms(reduced, rate, longest, purpose.Purpose);
        }

        private static (Loan, List<LoanPayment>) BuildLoan(
            Random random, LoanStatus requestedStatus, string number, Guid customerId, Guid branchId, LoanTerms terms, DateOnly today)
        {
            decimal principal = terms.Principal;
            decimal rate = terms.Rate;
            int term = terms.Term;
            IReadOnlyList<AmortizationInstallment> schedule = AmortizationSchedule.Build(principal, rate, term);
            Guid currencyId = DominicanPesoId;

            LoanStatus status = requestedStatus;
            DateOnly disbursement;
            int missed = 0;

            switch (requestedStatus)
            {
                case LoanStatus.Pending:
                    disbursement = today.AddDays(random.Next(7, 30));
                    break;
                case LoanStatus.Approved:
                    disbursement = today.AddDays(random.Next(1, 10));
                    break;
                case LoanStatus.Disbursed:
                    disbursement = today.AddDays(-random.Next(1, 25));
                    break;
                case LoanStatus.FullyPaid:
                    disbursement = MonthsAgo(today, term + random.Next(1, 13), random);
                    break;
                case LoanStatus.Closed:
                    disbursement = MonthsAgo(today, term + random.Next(6, 18), random);
                    break;
                case LoanStatus.Defaulted:
                    missed = random.Next(1, 4);
                    disbursement = MonthsAgo(today, random.Next(missed + 1, term + 1), random);
                    break;
                case LoanStatus.WrittenOff:
                    missed = random.Next(4, 9);
                    disbursement = MonthsAgo(today, random.Next(missed + 1, Math.Max(missed + 2, term + 1)), random);
                    break;
                case LoanStatus.PartiallyPaid:
                    disbursement = MonthsAgo(today, random.Next(2, Math.Max(3, term)), random);
                    break;
                default: // Active, Restructured
                    disbursement = MonthsAgo(today, random.Next(1, Math.Max(2, term)), random);
                    break;
            }

            int dueCount = Enumerable.Range(1, term).Count(k => disbursement.AddMonths(k) <= today);
            int paidCount = requestedStatus switch
            {
                LoanStatus.Pending or LoanStatus.Approved or LoanStatus.Disbursed => 0,
                LoanStatus.FullyPaid or LoanStatus.Closed => term,
                LoanStatus.Defaulted or LoanStatus.WrittenOff => Math.Max(1, dueCount - missed),
                _ => dueCount,
            };
            paidCount = Math.Min(paidCount, term);

            DateOnly maturity = disbursement.AddMonths(term);
            DateOnly nextDue = paidCount < term ? disbursement.AddMonths(paidCount + 1) : maturity;

            bool collectable = requestedStatus is LoanStatus.Active or LoanStatus.PartiallyPaid or LoanStatus.Restructured
                or LoanStatus.Disbursed or LoanStatus.Defaulted or LoanStatus.WrittenOff;
            int daysOverdue = collectable && nextDue < today ? today.DayNumber - nextDue.DayNumber : 0;

            if (requestedStatus == LoanStatus.WrittenOff && daysOverdue <= WriteOffAfterDays) status = LoanStatus.Defaulted;
            if (requestedStatus == LoanStatus.Defaulted && daysOverdue == 0) status = LoanStatus.Active;
            if (requestedStatus == LoanStatus.Active && daysOverdue > 0) status = LoanStatus.Defaulted;

            var loanId = Guid.NewGuid();
            var payments = new List<LoanPayment>();

            for (int k = 1; k <= paidCount; k++)
            {
                AmortizationInstallment installment = schedule[k - 1];
                DateOnly due = disbursement.AddMonths(k);
                bool late = random.NextDouble() < 0.1;
                int lateDays = late ? random.Next(1, 6) : 0;
                DateOnly paidOn = late ? Min(due.AddDays(lateDays), today) : Min(due.AddDays(-random.Next(0, 4)), today);
                decimal penalty = late ? Math.Round(installment.Payment * 0.01m, 2) : 0m;

                payments.Add(NewPayment(random, loanId, currencyId, paidOn, installment.Principal, installment.Interest, penalty, PaymentStatus.Completed));

                if (random.NextDouble() < 0.03)
                {
                    payments.Add(NewPayment(random, loanId, currencyId, Min(due, today), installment.Principal, installment.Interest, 0m, PaymentStatus.Failed));
                }
            }

            if (status == LoanStatus.PartiallyPaid && paidCount > 0 && paidCount < term)
            {
                decimal remaining = schedule[paidCount - 1].BalanceAfter;
                decimal prepayment = Math.Min(remaining, Math.Max(500m, RoundTo(remaining * (decimal)(0.05 + random.NextDouble() * 0.15), 100m)));
                payments.Add(NewPayment(random, loanId, currencyId, today.AddDays(-random.Next(1, 20)), prepayment, 0m, 0m, PaymentStatus.Completed));
            }

            List<LoanPayment> completed = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
            decimal principalPaid = completed.Sum(p => p.PrincipalAmount);
            decimal interestPaid = completed.Sum(p => p.InterestAmount);
            decimal totalInterest = schedule.Sum(s => s.Interest);
            DateTime? lastPayment = completed.Count == 0 ? null : completed.Max(p => p.PaymentDate).ToDateTime(TimeOnly.MinValue);
            DateTime createdOn = disbursement <= today
                ? disbursement.AddDays(-3).ToDateTime(TimeOnly.MinValue)
                : DateTime.UtcNow;

            var loan = new Loan
            {
                Id = loanId,
                LoanNumber = number,
                CustomerId = customerId,
                BranchId = branchId,
                CurrencyId = currencyId,
                PrincipalAmount = principal,
                InterestRate = rate,
                TermMonths = term,
                InstallmentAmount = schedule[0].Payment,
                LoanStatus = status,
                DisbursementDate = disbursement,
                MaturityDate = maturity,
                NextPaymentDueDate = nextDue,
                DaysOverdue = daysOverdue,
                DelinquencyPercentage = daysOverdue > 0 ? Math.Round(daysOverdue / 30m * 100, 2) : 0m,
                LastPaymentDate = lastPayment,
                TotalInterestAmount = totalInterest,
                PaidInterest = interestPaid,
                RemainingInterest = Math.Max(0m, totalInterest - interestPaid),
                AmortizationBalance = principal - principalPaid,
                PrincipalPaid = principalPaid,
                PrincipalRemaining = principal - principalPaid,
                LoanPurpose = terms.Purpose,
                MaxCredit = principal,
                RequiresGuarantor = principal >= 150_000m,
                CreatedOn = createdOn,
                IsActive = true,
                IsDeleted = false,
            };

            return (loan, payments);
        }

        private static LoanPayment NewPayment(
            Random random, Guid loanId, Guid currencyId, DateOnly date, decimal principal, decimal interest, decimal penalty, PaymentStatus status) =>
            new()
            {
                Id = Guid.NewGuid(),
                LoanId = loanId,
                CurrencyId = currencyId,
                PaymentDate = date,
                PrincipalAmount = principal,
                InterestAmount = interest,
                PenaltyAmount = penalty,
                TotalPaymentAmount = principal + interest + penalty,
                PaymentMethod = PaymentMethods[random.Next(PaymentMethods.Length)],
                ReferenceNumber = $"REF-{random.Next(100_000_000, 999_999_999)}",
                Status = status,
                CreatedOn = date.ToDateTime(TimeOnly.MinValue),
                IsActive = true,
                IsDeleted = false,
            };

        private static DateOnly MonthsAgo(DateOnly today, int months, Random random) =>
            today.AddMonths(-months).AddDays(-random.Next(1, 25));

        private static DateOnly Min(DateOnly a, DateOnly b) => a <= b ? a : b;

        private static decimal RoundTo(decimal value, decimal step) => Math.Round(value / step, 0) * step;
    }
}
