using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class DelinquencyService
    {
        public async Task UpdateDelinquencyAsync(Loan loan, DelinquencyPolicy policy)
        {
            if (loan.NextPaymentDueDate == default)
                return;

            var daysOverdue = (DateTime.UtcNow.Date - loan.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue)).Days;

            if (daysOverdue > 0)
            {
                loan.DaysOverdue = daysOverdue;

                // Calculate delinquency percentage based on days overdue
                loan.DelinquencyPercentage = Math.Min(100, daysOverdue * policy.DailyPenaltyRate * 100);

                // Apply early delinquency penalty
                if (daysOverdue >= policy.DaysUntilEarlyDelinquency && loan.LoanStatus != LoanStatus.Defaulted)
                {
                    var earlyPenalty = loan.InstallmentAmount * policy.EarlyDelinquencyPenalty;
                    // Would be recorded in LoanPayment as penalty
                }

                // Apply severe delinquency penalty
                if (daysOverdue >= policy.DaysUntilSevereDelinquency && loan.LoanStatus != LoanStatus.Defaulted)
                {
                    var severePenalty = loan.InstallmentAmount * policy.SevereDelinquencyPenalty;
                    loan.LoanStatus = LoanStatus.Defaulted;
                }

                // Write off after configured days
                if (daysOverdue >= policy.DaysUntilWriteOff)
                {
                    loan.LoanStatus = LoanStatus.WrittenOff;
                }
            }
            else if (daysOverdue <= 0)
            {
                loan.DaysOverdue = 0;
                loan.DelinquencyPercentage = 0m;

                // Revert to Active if it was just Defaulted
                if (loan.LoanStatus == LoanStatus.Defaulted && daysOverdue == 0)
                {
                    loan.LoanStatus = LoanStatus.Active;
                }
            }
        }

        public decimal CalculateLateFeesAsync(Loan loan, DelinquencyPolicy policy)
        {
            if (!policy.AutomaticallyChargeLateFees || loan.DaysOverdue == 0)
                return 0m;

            var lateFees = loan.InstallmentAmount * policy.DailyPenaltyRate * loan.DaysOverdue;
            return lateFees;
        }
    }
}
