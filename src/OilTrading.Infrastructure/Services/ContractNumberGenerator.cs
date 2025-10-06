using Microsoft.EntityFrameworkCore;
using OilTrading.Application.Services;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

public class ContractNumberGenerator : IContractNumberGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly object _lockObject = new object();

    public ContractNumberGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(ContractType contractType, int year)
    {
        // Format: ITGR-YYYY-TYPE-BXXXX
        var prefix = $"ITGR-{year}-{contractType}-B";
        
        // Get the last contract number for this year and type
        var lastContract = await _context.PurchaseContracts
            .Where(c => c.ContractNumber.Value.StartsWith(prefix))
            .OrderByDescending(c => c.ContractNumber.Value)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastContract != null)
        {
            // Extract the serial number from the last contract
            var lastNumberStr = lastContract.ContractNumber.Value.Substring(prefix.Length);
            if (int.TryParse(lastNumberStr, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        // Generate the new contract number
        return $"{prefix}{nextNumber:D4}";
    }

    public async Task<string> GenerateNextAsync(ContractType contractType)
    {
        return await GenerateAsync(contractType, DateTime.UtcNow.Year);
    }
}