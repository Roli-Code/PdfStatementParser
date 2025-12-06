using System;
using System.IO;
using System.Threading.Tasks;
using PdfStatementParser.Services;
using PdfStatementParser.Models;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run --project tools/ParserRunner -- <path-to-pdf>");
            return 1;
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return 2;
        }

        var service = new StatementParserService();
        // Register specialized Raiffeisen parser first, then generic fallback
        service.RegisterParser(new RaiffeisenBankParser());
        service.RegisterParser(new GenericPdfParser());

        Console.WriteLine($"Parsing file: {path}");

        // Read raw bytes and print extracted text for inspection
        var fileBytes = await File.ReadAllBytesAsync(path);
        Console.WriteLine();
        Console.WriteLine("--- Raw Extracted Text (page-separated) ---");
        var rawText = new GenericPdfParser().ExtractTextFromBytes(fileBytes);
        Console.WriteLine(rawText);
        Console.WriteLine("--- End Raw Extracted Text ---");
        Console.WriteLine();

        try
        {
            var statement = await service.ParseAsync(path);
            if (statement == null)
            {
                Console.WriteLine("No statement could be parsed (null result). The PDF may be encrypted or in an unsupported layout.");
                return 3;
            }

            Console.WriteLine("--- Extracted Statement Metadata ---");
            Console.WriteLine($"BankIdentifier: {statement.BankIdentifier}");
            Console.WriteLine($"IBAN: {statement.Iban}");
            Console.WriteLine($"AccountHolder: {statement.AccountHolder}");
            Console.WriteLine($"Period: {statement.StartDate} - {statement.EndDate}");
            Console.WriteLine($"Bookings: {statement.Bookings?.Count ?? 0}");

            Console.WriteLine();
            Console.WriteLine("--- Raw Bookings (JSON) ---");
            var json = StatementJsonSerializer.ToJson(statement, indent: true);
            Console.WriteLine(json);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while parsing: {ex.Message}");
            return 4;
        }
    }
}
