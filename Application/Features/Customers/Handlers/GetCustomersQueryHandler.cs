using Application.Common.Extensions;
using Application.Features.Customers.Queries;
using Application.Models.Responses;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Features.Customers.Handlers
{
    public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedResponse<CustomerSummaryResponse>>
    {
        private readonly LoanDbContext _db;

        public GetCustomersQueryHandler(LoanDbContext db) => _db = db;

        public Task<PaginatedResponse<CustomerSummaryResponse>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Customer> customers = _db.Customers.AsNoTracking();

            string? term = request.Search?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                // Phones are stored as digits (+18095550100), so a typed 809-555 is matched by its digits.
                string phoneDigits = PhoneRules.Digits(term);

                // EF turns Contains into a parameterized match, so a term with % or _ is matched literally.
                customers = customers.Where(c =>
                    (c.FirstName + " " + c.LastName).Contains(term)
                    || (c.CompanyName != null && c.CompanyName.Contains(term))
                    || c.Identification.Contains(term)
                    || c.Phone.Contains(term)
                    || (phoneDigits.Length >= 3 && c.Phone.Contains(phoneDigits)));
            }

            return customers
                .OrderByDescending(c => c.CreatedOn)
                .ThenBy(c => c.Id)
                .Select(c => new CustomerSummaryResponse(
                    c.Id, c.FirstName, c.LastName, c.Identification, c.CustomerType,
                    c.Phone, c.Email, c.Status, c.RiskLevel, c.CreditLimit, c.IsBlacklisted))
                .ToPaginatedResponseAsync(request, cancellationToken);
        }
    }
}
