using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services;

/// <summary>
/// Base class for bank statement parsers with common functionality
/// </summary>
public abstract class BaseBankStatementParser : IBankStatementParser
{
    /// <summary>
    /// The identifier for this bank parser
    /// </summary>
    public abstract string BankIdentifier { get; }
    
    /// <summary>
    /// The supported file formats (e.g., ".pdf", ".csv")
    /// </summary>
    public abstract string[] SupportedFormats { get; }

    /// <summary>
    /// Checks if this parser can handle the given file
    /// </summary>
    public virtual bool CanParse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedFormats.Any(fmt => fmt.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if this parser can handle the given content
    /// </summary>
    public virtual bool CanParseContent(byte[] content, string? fileName = null)
    {
        if (content == null || content.Length == 0)
            return false;

        // First check file extension if available
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!SupportedFormats.Any(fmt => fmt.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // Then check content signature/magic bytes
        return CanParseContentBySignature(content);
    }

    /// <summary>
    /// Override to implement content signature detection (magic bytes)
    /// </summary>
    protected virtual bool CanParseContentBySignature(byte[] content)
    {
        return true; // Default: assume we can parse if format matches
    }

    /// <summary>
    /// Parses a statement from a file
    /// </summary>
    public abstract Task<Statement?> ParseAsync(string filePath);

    /// <summary>
    /// Parses a statement from byte content
    /// </summary>
    public abstract Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null);

    /// <summary>
    /// Helper method to read file content
    /// </summary>
    protected async Task<byte[]> ReadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllBytesAsync(filePath);
    }
}
