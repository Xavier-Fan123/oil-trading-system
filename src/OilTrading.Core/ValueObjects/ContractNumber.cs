using OilTrading.Core.Common;
using System.Text.RegularExpressions;

namespace OilTrading.Core.ValueObjects;

public class ContractNumber : ValueObject
{
    public string Value { get; private set; } = string.Empty;
    public string Year { get; private set; } = string.Empty;
    public ContractType Type { get; private set; }
    public int SerialNumber { get; private set; }

    private static readonly Regex ContractNumberRegex = new(@"^ITGR-(\d{4})-(CARGO|EXW|DEL)-B(\d{4})$", RegexOptions.Compiled);

    private ContractNumber() { } // For EF Core

    private ContractNumber(string value, string year, ContractType type, int serialNumber)
    {
        Value = value;
        Year = year;
        Type = type;
        SerialNumber = serialNumber;
    }

    public static ContractNumber Create(int year, ContractType type, int serialNumber)
    {
        if (year < 2000 || year > 3000)
            throw new DomainException("Year must be between 2000 and 3000");
        
        if (serialNumber < 1 || serialNumber > 9999)
            throw new DomainException("Serial number must be between 1 and 9999");

        var typeString = type.ToString().ToUpper();
        var value = $"ITGR-{year}-{typeString}-B{serialNumber:D4}";
        
        return new ContractNumber(value, year.ToString(), type, serialNumber);
    }

    public static ContractNumber Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Contract number cannot be null or empty");

        var match = ContractNumberRegex.Match(value.Trim().ToUpper());
        if (!match.Success)
            throw new DomainException($"Invalid contract number format: {value}. Expected format: ITGR-YYYY-TYPE-BXXXX");

        var year = match.Groups[1].Value;
        var typeString = match.Groups[2].Value;
        var serialNumberString = match.Groups[3].Value;

        if (!Enum.TryParse<ContractType>(typeString, true, out var type))
            throw new DomainException($"Invalid contract type: {typeString}");

        if (!int.TryParse(serialNumberString, out var serialNumber))
            throw new DomainException($"Invalid serial number: {serialNumberString}");

        return new ContractNumber(value.Trim().ToUpper(), year, type, serialNumber);
    }

    public static bool TryParse(string value, out ContractNumber? contractNumber)
    {
        contractNumber = null;
        try
        {
            contractNumber = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ContractNumber NextSerial()
    {
        if (SerialNumber >= 9999)
            throw new DomainException("Cannot generate next serial number: maximum reached");
        
        return Create(int.Parse(Year), Type, SerialNumber + 1);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ContractNumber contractNumber) => contractNumber.Value;
}

public enum ContractType
{
    CARGO = 1,
    EXW = 2,   // Ex Works
    DEL = 3    // Delivered
}