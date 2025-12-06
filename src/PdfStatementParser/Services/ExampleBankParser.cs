using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services;

/// <summary>
/// Example parser for demonstration purposes
/// Implement this as a template for bank-specific parsers
/// </summary>
public class ExampleBankParser : BaseBankStatementParser
{
    /// <summary>
    /// Gets the bank identifier
    /// </summary>
    public override string BankIdentifier => "Example Bank";
    
    /// <summary>
    /// Gets the supported file formats
    /// </summary>
    public override string[] SupportedFormats => new[] { ".csv", ".txt" };

    /// <summary>
    /// Detects PDF magic bytes (for future PDF support)
    /// </summary>
    protected override bool CanParseContentBySignature(byte[] content)
    {
        // Example: Check for PDF signature
        if (content.Length >= 4)
        {
            // PDF files start with %PDF
            if (content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a statement from a file
    /// </summary>
    public override async Task<Statement?> ParseAsync(string filePath)
    {
        var content = await ReadFileAsync(filePath);
        return await ParseContentAsync(content, System.IO.Path.GetFileName(filePath));
    }

    /// <summary>
    /// Parses a statement from byte content
    /// </summary>
    public override async Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
    {
        if (!CanParseContent(content, fileName))
            return null;

        // TODO: Implement actual parsing logic
        // This is a placeholder that returns a sample statement
        return await Task.FromResult(CreateSampleStatement());
    }

    /// <summary>
    /// Creates a sample statement for demonstration
    /// </summary>
    private Statement CreateSampleStatement()
    {
        return new Statement
        {
            Iban = "DE89370400440532013000",
            AccountHolder = "Sample Account Holder",
            BankName = BankIdentifier,
            StatementNumber = "001",
            StartDate = new DateTime(2025, 08, 01),
            EndDate = new DateTime(2025, 08, 31),
            OpeningBalance = 1000.00m,
            ClosingBalance = 1050.00m,
            Currency = "EUR",
            BankIdentifier = BankIdentifier,
            Bookings = new List<Booking>
            {
                new()
                {
                    BookingDate = new DateTime(2025, 08, 05),
                    ValueDate = new DateTime(2025, 08, 05),
                    BookingText = "Transfer",
                    Applicant = "John Doe",
                    Purpose = "Invoice #12345",
                    Amount = 50.00m,
                    Currency = "EUR",
                    Balance = 1050.00m,
                    TransactionNumber = "TXN001"
                }
            },
            Metadata = new ParseMetadata
            {
                ParserVersion = "1.0",
                SourceFileName = "sample.csv",
                ConfidenceScore = 100
            }
        };
    }
}
