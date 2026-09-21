namespace Application.Features.Loans
{
    public sealed record AmortizationInstallment(int Number, decimal Principal, decimal Interest, decimal BalanceAfter)
    {
        public decimal Payment => Principal + Interest;
    }

    /// <summary>
    /// Standard fixed-installment (annuity) amortization. It is the single source of the installment amount,
    /// the schedule shown to users, the interest a loan will accrue and the seeded loans and payments.
    /// </summary>
    public static class AmortizationSchedule
    {
        /// <summary>The fixed monthly payment (principal plus interest), rounded to cents.</summary>
        public static decimal Installment(decimal principal, decimal annualRatePercent, int months)
        {
            if (principal <= 0) throw new ArgumentOutOfRangeException(nameof(principal));
            if (months <= 0) throw new ArgumentOutOfRangeException(nameof(months));

            decimal monthlyRate = annualRatePercent / 1200m;
            return monthlyRate == 0
                ? Math.Round(principal / months, 2)
                : Math.Round(principal * monthlyRate / (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -months)), 2);
        }

        public static IReadOnlyList<AmortizationInstallment> Build(decimal principal, decimal annualRatePercent, int months)
        {
            decimal installment = Installment(principal, annualRatePercent, months);
            decimal monthlyRate = annualRatePercent / 1200m;

            var schedule = new List<AmortizationInstallment>(months);
            decimal balance = principal;

            for (int number = 1; number <= months; number++)
            {
                decimal interest = Math.Round(balance * monthlyRate, 2);
                decimal principalPart = number == months ? balance : Math.Min(balance, installment - interest);
                balance -= principalPart;

                schedule.Add(new AmortizationInstallment(number, principalPart, interest, balance));
            }

            return schedule;
        }

        public static decimal TotalInterest(decimal principal, decimal annualRatePercent, int months)
            => Build(principal, annualRatePercent, months).Sum(i => i.Interest);
    }
}
