using System;
using System.Collections.Generic;

namespace PdfStatementParser.Models;

/// <summary>
/// Represents a complete bank statement
/// </summary>
public class Statement
{
    /// <summary>
    /// The IBAN of the account
    /// </summary>
    public string Iban { get; set; } = string.Empty;

    /// <summary>
    /// The account holder name
    /// </summary>
    public string AccountHolder { get; set; } = string.Empty;

    /// <summary>
    /// The bank/institution name
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// The statement number or period identifier
    /// </summary>
    public string StatementNumber { get; set; } = string.Empty;

    /// <summary>
    /// The start date of the statement period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the statement period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Opening balance at the start of the period
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Closing balance at the end of the period
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// List of bookings/transactions
    /// </summary>
    public List<Booking> Bookings { get; set; } = new();

    /// <summary>
    /// The bank identifier that parsed this statement
    /// </summary>
    public string BankIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Parsing metadata
    /// </summary>
    public ParseMetadata? Metadata { get; set; }
}

/// <summary>
/// Metadata about the parsing process
/// </summary>
public class ParseMetadata
{
    /// <summary>
    /// When the parsing occurred
    /// </summary>
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The parsing version/format
    /// </summary>
    public string ParserVersion { get; set; } = "1.0";

    /// <summary>
    /// Source file name
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// Parsing quality/confidence (0-100)
    /// </summary>
    public int? ConfidenceScore { get; set; }

    /// <summary>
    /// Any warnings or issues during parsing
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
