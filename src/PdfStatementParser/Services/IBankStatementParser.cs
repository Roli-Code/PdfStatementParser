using System.Threading.Tasks;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services;

/// <summary>
/// Interface for bank-specific statement parsers
/// </summary>
public interface IBankStatementParser
{
    /// <summary>
    /// The bank identifier (e.g., "Deutsche Bank", "Commerzbank")
    /// </summary>
    string BankIdentifier { get; }

    /// <summary>
    /// The supported statement formats/file extensions
    /// </summary>
    string[] SupportedFormats { get; }

    /// <summary>
    /// Indicates whether this parser can handle the given file
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if this parser can process the file</returns>
    bool CanParse(string filePath);

    /// <summary>
    /// Indicates whether this parser can handle the given byte content
    /// </summary>
    /// <param name="content">File content as bytes</param>
    /// <param name="fileName">File name for additional context</param>
    /// <returns>True if this parser can process the content</returns>
    bool CanParseContent(byte[] content, string? fileName = null);

    /// <summary>
    /// Parses a statement from a file
    /// </summary>
    /// <param name="filePath">Path to the statement file</param>
    /// <returns>Parsed statement or null if parsing fails</returns>
    Task<Statement?> ParseAsync(string filePath);

    /// <summary>
    /// Parses a statement from byte content
    /// </summary>
    /// <param name="content">File content as bytes</param>
    /// <param name="fileName">File name for context</param>
    /// <returns>Parsed statement or null if parsing fails</returns>
    Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null);
}
