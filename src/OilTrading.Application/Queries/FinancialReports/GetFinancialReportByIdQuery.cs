using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetFinancialReportByIdQuery : IRequest<FinancialReportDto?>
{
    public Guid Id { get; set; }

    public GetFinancialReportByIdQuery(Guid id)
    {
        Id = id;
    }
}

public class GetFinancialReportByIdQueryValidator : AbstractValidator<GetFinancialReportByIdQuery>
{
    public GetFinancialReportByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Financial report ID is required");
    }
}