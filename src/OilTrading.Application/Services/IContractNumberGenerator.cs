using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface IContractNumberGenerator
{
    Task<string> GenerateAsync(ContractType contractType, int year);
    Task<string> GenerateNextAsync(ContractType contractType);
}