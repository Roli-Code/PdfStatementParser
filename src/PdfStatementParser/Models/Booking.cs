using System;

namespace PdfStatementParser.Models;

/// <summary>
/// Represents a single booking/transaction on a bank statement
/// </summary>
public class Booking
{
    /// <summary>
    /// The booking date (when the transaction was booked)
    /// </summary>
    public DateTime BookingDate { get; set; }

    /// <summary>
    /// The value date (when the amount was credited/debited)
    /// </summary>
    public DateTime? ValueDate { get; set; }

    /// <summary>
    /// The booking text/code (e.g., "SEPA Transfer", "Direct Debit")
    /// </summary>
    public string BookingText { get; set; } = string.Empty;

    /// <summary>
    /// The beneficiary/payer name
    /// </summary>
    public string Applicant { get; set; } = string.Empty;

    /// <summary>
    /// The purpose/description of the transaction
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// The amount (positive for credit, negative for debit)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency code (e.g., "EUR", "USD")
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// The balance after this transaction
    /// </summary>
    public decimal? Balance { get; set; }

    /// <summary>
    /// Bank-specific transaction number or reference
    /// </summary>
    public string? TransactionNumber { get; set; }

    /// <summary>
    /// Additional information or memo
    /// </summary>
    public string? Memo { get; set; }
}
