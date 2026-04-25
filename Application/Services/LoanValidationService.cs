using Domain.Entities;

namespace Application.Services
{
    public class LoanValidationService
    {
        public async Task<LoanEligibilityResult> CheckEligibilityAsync(Customer customer, decimal loanAmount)
        {
            var reasons = new List<string>();

            // Rule 1: Not blacklisted
            if (customer.IsBlacklisted)
            {
                reasons.Add($"Customer is blacklisted: {customer.BlacklistReason}");
                return new LoanEligibilityResult { IsEligible = false, Reasons = reasons };
            }

            // Rule 2: Credit score > 300
            if (customer.CreditScore?.Score < 300)
            {
                reasons.Add("Credit score is below minimum threshold (300)");
            }

            // Rule 3: Loan amount < Credit limit
            if (loanAmount > customer.CreditLimit)
            {
                reasons.Add($"Loan amount (DOP {loanAmount:N2}) exceeds credit limit (DOP {customer.CreditLimit:N2})");
            }

            // Rule 4: No active delinquent loans
            var hasDelinquentLoans = customer.Loans.Any(l => l.DaysOverdue > 30);
            if (hasDelinquentLoans)
            {
                reasons.Add("Customer has active delinquent loans");
            }

            // Rule 5: Income verification (annual income > loan amount / 12)
            var monthlyLoanPayment = loanAmount / 12;
            var monthlyIncome = customer.AnnualIncome / 12;
            if (monthlyIncome < monthlyLoanPayment * 3)
            {
                reasons.Add("Monthly income insufficient for loan repayment (income should be 3x monthly payment)");
            }

            return new LoanEligibilityResult
            {
                IsEligible = reasons.Count == 0,
                Reasons = reasons
            };
        }

        public List<AmortizationScheduleItem> GenerateAmortizationSchedule(
            decimal principal,
            decimal annualInterestRate,
            int months)
        {
            var schedule = new List<AmortizationScheduleItem>();
            var monthlyRate = annualInterestRate / 100 / 12;
            var monthlyPayment = principal * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months))
                                / ((decimal)Math.Pow((double)(1 + monthlyRate), months) - 1);

            var remainingBalance = principal;

            for (int i = 1; i <= months; i++)
            {
                var interestPayment = remainingBalance * monthlyRate;
                var principalPayment = monthlyPayment - interestPayment;
                remainingBalance -= principalPayment;

                schedule.Add(new AmortizationScheduleItem
                {
                    PaymentNumber = i,
                    PrincipalPayment = principalPayment,
                    InterestPayment = interestPayment,
                    RemainingBalance = Math.Max(0, remainingBalance)
                });
            }

            return schedule;
        }
    }

    public class LoanEligibilityResult
    {
        public bool IsEligible { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public class AmortizationScheduleItem
    {
        public int PaymentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PrincipalPayment { get; set; }
        public decimal InterestPayment { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
