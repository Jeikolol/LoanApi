using Application.Features.Delinquency.Commands;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Handlers
{
    public class ChargeLateFeeCommandHandler : IRequestHandler<ChargeLateFeeCommand, LateFeeChargeResponse>
    {
        private readonly ILateFeeCharger _charger;

        public ChargeLateFeeCommandHandler(ILateFeeCharger charger) => _charger = charger;

        public Task<LateFeeChargeResponse> Handle(ChargeLateFeeCommand request, CancellationToken cancellationToken) =>
            _charger.ChargeAsync(request.LoanId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
    }
}
