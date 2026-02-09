using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.MarketData;

public class UploadMarketDataCommand : IRequest<MarketDataUploadResultDto>
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "Spot" or "Futures"
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string UploadedBy { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
}

public class UploadMarketDataCommandValidator : AbstractValidator<UploadMarketDataCommand>
{
    public UploadMarketDataCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required");

        RuleFor(x => x.FileType)
            .NotEmpty()
            .Must(BeValidFileType)
            .WithMessage("File type must be 'Spot', 'Futures', or 'XGroup'");

        RuleFor(x => x.FileContent)
            .NotEmpty()
            .WithMessage("File content is required");

        RuleFor(x => x.UploadedBy)
            .NotEmpty()
            .WithMessage("Uploaded by is required");
    }

    private static bool BeValidFileType(string fileType)
    {
        return new[] { "Spot", "Futures", "XGroup" }.Contains(fileType);
    }
}