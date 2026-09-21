using Application.Services;
using Domain.Entities;

namespace Application.FakeData
{
    public static class FakeDbQueryCreditScores
    {
        /// <summary>
        /// Runs each customer's loan history through the same <see cref="CreditScoreService"/> the application uses
        /// when a score is recalculated, so a seeded score is exactly what "recalculate" would produce.
        /// Customers with no loans have no history and therefore no score.
        /// </summary>
        public static IReadOnlyList<CreditScore> Generate(IEnumerable<Customer> customersWithLoans)
        {
            var service = new CreditScoreService();
            var scores = new List<CreditScore>();

            foreach (Customer customer in customersWithLoans)
            {
                if (customer.Loans.Count == 0) continue;

                CreditScore score = service.CalculateCreditScore(customer);
                score.Id = Guid.NewGuid();
                score.CustomerId = customer.Id;
                score.Customer = null!;
                score.CreatedOn = DateTime.UtcNow;
                score.IsActive = true;
                score.IsDeleted = false;

                scores.Add(score);
            }

            return scores;
        }
    }
}
