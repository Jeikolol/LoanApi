using Domain.Enums;

namespace Application.Models.Responses
{
    public record CreditScoreResponse(
        Guid Id,
        Guid CustomerId,
        int Score,
        CreditRating CreditRating,
        DateTime LastCalculatedDate,
        decimal PaymentHistoryScore,
        decimal CreditUtilizationScore,
        decimal CreditAgeScore,
        decimal DefaultHistoryScore
    );
}
