using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.MarketData;

public class UploadExposureCommand : IRequest<ExposureVaRResultDto>
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string UploadedBy { get; set; } = string.Empty;
}

public class UploadExposureCommandValidator : AbstractValidator<UploadExposureCommand>
{
    public UploadExposureCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required");

        RuleFor(x => x.FileContent)
            .NotEmpty()
            .WithMessage("File content is required");

        RuleFor(x => x.UploadedBy)
            .NotEmpty()
            .WithMessage("Uploaded by is required");
    }
}
