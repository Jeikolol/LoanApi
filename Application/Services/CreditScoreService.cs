using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class CreditScoreService
    {
        private const int MaxScore = 1000;
        private const int MinScore = 0;

        public CreditScore CalculateCreditScore(Customer customer)
        {
            var creditScore = customer.CreditScore ?? new CreditScore
            {
                CustomerId = customer.Id,
                Customer = customer
            };

            // Calculate payment history score (35% weight)
            var paymentHistoryScore = CalculatePaymentHistoryScore(customer);
            creditScore.PaymentHistoryScore = paymentHistoryScore;

            // Calculate credit utilization score (30% weight)
            var creditUtilizationScore = CalculateCreditUtilizationScore(customer);
            creditScore.CreditUtilizationScore = creditUtilizationScore;

            // Calculate credit age score (15% weight)
            var creditAgeScore = CalculateCreditAgeScore(customer);
            creditScore.CreditAgeScore = creditAgeScore;

            // Calculate default history score (20% weight)
            var defaultHistoryScore = CalculateDefaultHistoryScore(customer);
            creditScore.DefaultHistoryScore = defaultHistoryScore;

            // Calculate final score
            var finalScore = (int)((paymentHistoryScore * 0.35m) +
                                   (creditUtilizationScore * 0.30m) +
                                   (creditAgeScore * 0.15m) +
                                   (defaultHistoryScore * 0.20m));

            creditScore.Score = Math.Clamp(finalScore, MinScore, MaxScore);
            creditScore.LastCalculatedDate = DateTime.UtcNow;
            creditScore.CreditRating = GetCreditRating(creditScore.Score);

            return creditScore;
        }

        private decimal CalculatePaymentHistoryScore(Customer customer)
        {
            if (customer.Loans == null || customer.Loans.Count == 0)
                return 500m; // Neutral score for new customers

            var totalPayments = customer.Loans.Sum(l => l.Payments.Count);
            if (totalPayments == 0)
                return 300m;

            var onTimePayments = customer.Loans.Sum(l =>
                l.Payments.Count(p => p.Status == Domain.Enums.PaymentStatus.Completed)
            );

            var onTimePercentage = totalPayments > 0 ? (decimal)onTimePayments / totalPayments : 0;
            return (int)(onTimePercentage * 1000);
        }

        private decimal CalculateCreditUtilizationScore(Customer customer)
        {
            if (customer.CreditLimit == 0)
                return 500m;

            var totalOutstandingBalance = customer.Loans
                .Where(l => l.LoanStatus != Domain.Enums.LoanStatus.FullyPaid &&
                           l.LoanStatus != Domain.Enums.LoanStatus.Closed)
                .Sum(l => l.PrincipalRemaining);

            var utilizationRatio = totalOutstandingBalance / customer.CreditLimit;

            // Lower utilization is better
            if (utilizationRatio <= 0.30m)
                return 900m;
            else if (utilizationRatio <= 0.50m)
                return 800m;
            else if (utilizationRatio <= 0.70m)
                return 600m;
            else if (utilizationRatio <= 0.90m)
                return 400m;
            else
                return 200m;
        }

        private decimal CalculateCreditAgeScore(Customer customer)
        {
            if (customer.Loans == null || customer.Loans.Count == 0)
                return 300m;

            var earliestLoan = customer.Loans.Min(l => l.CreatedOn);
            var accountAge = DateTime.UtcNow - earliestLoan;

            // Older accounts are better
            if (accountAge.TotalDays > 7 * 365) // 7+ years
                return 900m;
            else if (accountAge.TotalDays > 5 * 365) // 5+ years
                return 800m;
            else if (accountAge.TotalDays > 2 * 365) // 2+ years
                return 700m;
            else if (accountAge.TotalDays > 365) // 1+ year
                return 500m;
            else if (accountAge.TotalDays > 180) // 6+ months
                return 300m;
            else
                return 100m;
        }

        private decimal CalculateDefaultHistoryScore(Customer customer)
        {
            if (customer.IsBlacklisted)
                return 0m;

            var defaultedLoans = customer.Loans.Count(l =>
                l.LoanStatus == Domain.Enums.LoanStatus.Defaulted ||
                l.LoanStatus == Domain.Enums.LoanStatus.WrittenOff
            );

            if (defaultedLoans == 0)
                return 1000m;
            else if (defaultedLoans == 1)
                return 600m;
            else if (defaultedLoans == 2)
                return 300m;
            else
                return 0m;
        }

        private CreditRating GetCreditRating(int score)
        {
            return score switch
            {
                >= 800 => CreditRating.Excellent,
                >= 600 => CreditRating.Good,
                >= 400 => CreditRating.Fair,
                _ => CreditRating.Poor
            };
        }
    }
}
