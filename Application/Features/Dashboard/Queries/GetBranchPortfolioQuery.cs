using Application.Models.Responses.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries
{
    public record GetBranchPortfolioQuery : IRequest<BranchPortfolioItemResponse[]>;
}
