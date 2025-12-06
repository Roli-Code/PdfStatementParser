using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services;

/// <summary>
/// Service for parsing bank statements from various institutions
/// </summary>
public class StatementParserService
{
    private readonly List<IBankStatementParser> _parsers;

    /// <summary>
    /// Initializes a new instance of the StatementParserService
    /// </summary>
    public StatementParserService()
    {
        _parsers = new List<IBankStatementParser>();
    }

    /// <summary>
    /// Registers a bank-specific parser
    /// </summary>
    public void RegisterParser(IBankStatementParser parser)
    {
        if (parser == null)
            throw new ArgumentNullException(nameof(parser));

        _parsers.Add(parser);
    }

    /// <summary>
    /// Registers multiple parsers
    /// </summary>
    public void RegisterParsers(params IBankStatementParser[] parsers)
    {
        foreach (var parser in parsers)
        {
            RegisterParser(parser);
        }
    }

    /// <summary>
    /// Gets all registered parsers
    /// </summary>
    public IReadOnlyList<IBankStatementParser> GetParsers() => _parsers.AsReadOnly();

    /// <summary>
    /// Parses a statement file by automatically detecting the appropriate parser
    /// </summary>
    /// <param name="filePath">Path to the statement file</param>
    /// <returns>Parsed statement, or null if no suitable parser was found</returns>
    public async Task<Statement?> ParseAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // Try each parser to find one that can handle the file
        var applicableParsers = _parsers.Where(p => p.CanParse(filePath)).ToList();

        if (!applicableParsers.Any())
            throw new InvalidOperationException($"No parser found for file: {filePath}");

        // Try parsers in order until one succeeds
        foreach (var parser in applicableParsers)
        {
            try
            {
                var statement = await parser.ParseAsync(filePath);
                if (statement != null)
                {
                    statement.Metadata ??= new ParseMetadata();
                    statement.Metadata.SourceFileName = System.IO.Path.GetFileName(filePath);
                    return statement;
                }
            }
            catch (Exception ex)
            {
                // Log but continue to next parser
                System.Diagnostics.Debug.WriteLine($"Parser {parser.BankIdentifier} failed: {ex.Message}");
            }
        }

        throw new InvalidOperationException($"All applicable parsers failed to parse file: {filePath}");
    }

    /// <summary>
    /// Parses statement content by automatically detecting the appropriate parser
    /// </summary>
    /// <param name="content">File content as bytes</param>
    /// <param name="fileName">Optional file name for context</param>
    /// <returns>Parsed statement, or null if no suitable parser was found</returns>
    public async Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
    {
        if (content == null || content.Length == 0)
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        // Try each parser to find one that can handle the content
        var applicableParsers = _parsers.Where(p => p.CanParseContent(content, fileName)).ToList();

        if (!applicableParsers.Any())
            throw new InvalidOperationException("No parser found for the given content");

        // Try parsers in order until one succeeds
        foreach (var parser in applicableParsers)
        {
            try
            {
                var statement = await parser.ParseContentAsync(content, fileName);
                if (statement != null)
                {
                    statement.Metadata ??= new ParseMetadata();
                    statement.Metadata.SourceFileName = fileName;
                    return statement;
                }
            }
            catch (Exception ex)
            {
                // Log but continue to next parser
                System.Diagnostics.Debug.WriteLine($"Parser {parser.BankIdentifier} failed: {ex.Message}");
            }
        }

        throw new InvalidOperationException("All applicable parsers failed to parse the content");
    }

    /// <summary>
    /// Finds a parser by bank identifier
    /// </summary>
    public IBankStatementParser? FindParser(string bankIdentifier)
    {
        return _parsers.FirstOrDefault(p => p.BankIdentifier.Equals(bankIdentifier, StringComparison.OrdinalIgnoreCase));
    }
}
