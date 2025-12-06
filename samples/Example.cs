using System;
using System.Threading.Tasks;
using PdfStatementParser.Services;

namespace PdfStatementParser.Examples;

/// <summary>
/// Example usage of the PdfStatementParser library
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("PdfStatementParser - Example Usage\n");

        // Initialize the parser service
        var service = new StatementParserService();
        
        // Register parsers for different banks
        service.RegisterParser(new ExampleBankParser());
        // Add more parsers here:
        // service.RegisterParser(new DeutscheBankParser());
        // service.RegisterParser(new CommerzBankParser());
        
        Console.WriteLine($"Registered {service.GetParsers().Count} parser(s)\n");
        
        foreach (var parser in service.GetParsers())
        {
            Console.WriteLine($"  • {parser.BankIdentifier}: {string.Join(", ", parser.SupportedFormats)}");
        }

        // Example 1: Parse with automatic bank detection
        await Example_AutomaticDetection(service);
        
        // Example 2: Create statement with JSON output
        await Example_JsonOutput(service);
        
        // Example 3: Direct parser usage
        await Example_DirectParser();
    }

    /// <summary>
    /// Example: Automatic bank detection
    /// </summary>
    private static async Task Example_AutomaticDetection(StatementParserService service)
    {
        Console.WriteLine("\n--- Example 1: Automatic Bank Detection ---\n");

        try
        {
            // The service automatically finds the right parser
            // var statement = await service.ParseAsync("kontoauszug.csv");
            
            // For demo purposes, parse with bytes instead
            var content = System.Text.Encoding.UTF8.GetBytes("Sample statement content");
            var statement = await service.ParseContentAsync(content, "statement.csv");

            if (statement != null)
            {
                Console.WriteLine($"✓ Successfully parsed statement from {statement.BankName}");
                Console.WriteLine($"  Account: {statement.AccountHolder}");
                Console.WriteLine($"  IBAN: {statement.Iban}");
                Console.WriteLine($"  Period: {statement.StartDate:yyyy-MM-dd} to {statement.EndDate:yyyy-MM-dd}");
                Console.WriteLine($"  Opening Balance: {statement.OpeningBalance:C}");
                Console.WriteLine($"  Closing Balance: {statement.ClosingBalance:C}");
                Console.WriteLine($"  Bookings: {statement.Bookings.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: JSON output
    /// </summary>
    private static async Task Example_JsonOutput(StatementParserService service)
    {
        Console.WriteLine("\n--- Example 2: JSON Output ---\n");

        try
        {
            var content = System.Text.Encoding.UTF8.GetBytes("Sample");
            var statement = await service.ParseContentAsync(content, "statement.csv");

            if (statement != null)
            {
                // Serialize to JSON
                var json = StatementJsonSerializer.ToJson(statement, indent: true);
                
                Console.WriteLine("JSON Output (first 500 chars):");
                var preview = json.Length > 500 ? json[..500] + "..." : json;
                Console.WriteLine(preview);
                
                // In practice, you would save or send this JSON
                // System.IO.File.WriteAllText("statement.json", json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example: Direct parser usage
    /// </summary>
    private static async Task Example_DirectParser()
    {
        Console.WriteLine("\n--- Example 3: Direct Parser Usage ---\n");

        try
        {
            var parser = new ExampleBankParser();
            
            // Check if parser can handle file
            var canParse = parser.CanParse("statement.csv");
            Console.WriteLine($"Can parse CSV: {canParse}");
            
            // Parse with direct access
            var content = System.Text.Encoding.UTF8.GetBytes("Sample");
            var statement = await parser.ParseContentAsync(content, "statement.csv");

            if (statement != null)
            {
                Console.WriteLine($"✓ Parsed with {parser.BankIdentifier}");
                Console.WriteLine($"  Total Bookings: {statement.Bookings.Count}");
                
                if (statement.Bookings.Count > 0)
                {
                    var booking = statement.Bookings[0];
                    Console.WriteLine($"  First Booking: {booking.BookingDate:yyyy-MM-dd}");
                    Console.WriteLine($"    Amount: {booking.Amount:C} {booking.Currency}");
                    Console.WriteLine($"    Purpose: {booking.Purpose}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }
}
