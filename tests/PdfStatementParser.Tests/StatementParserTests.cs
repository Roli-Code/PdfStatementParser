using System;
using System.Threading.Tasks;
using Xunit;
using PdfStatementParser.Models;
using PdfStatementParser.Services;
using System.Reflection;

namespace PdfStatementParser.Tests;

public class StatementParserTests
{
    [Fact]
    public void ExampleBankParser_CanIdentifyFormat()
    {
        // Arrange
        var parser = new ExampleBankParser();

        // Act & Assert
        Assert.Equal("Example Bank", parser.BankIdentifier);
        Assert.Contains(".csv", parser.SupportedFormats);
        Assert.Contains(".txt", parser.SupportedFormats);
    }

    [Fact]
    public void ExampleBankParser_CanParse_CsvFile()
    {
        // Arrange
        var parser = new ExampleBankParser();

        // Act
        var canParse = parser.CanParse("statement.csv");

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void ExampleBankParser_CanParse_RejectsPdfFile()
    {
        // Arrange
        var parser = new ExampleBankParser();

        // Act
        var canParse = parser.CanParse("statement.pdf");

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public async Task ExampleBankParser_ParseContent_ReturnsSampleStatement()
    {
        // Arrange
        var parser = new ExampleBankParser();
        byte[] sampleContent = System.Text.Encoding.UTF8.GetBytes("test,data");

        // Act
        var statement = await parser.ParseContentAsync(sampleContent, "test.csv");

        // Assert
        Assert.NotNull(statement);
        Assert.Equal("Example Bank", statement.BankIdentifier);
        Assert.NotEmpty(statement.Bookings);
    }

    [Fact]
    public void StatementParserService_RegisterParser_AddsParser()
    {
        // Arrange
        var service = new StatementParserService();
        var parser = new ExampleBankParser();

        // Act
        service.RegisterParser(parser);
        var parsers = service.GetParsers();

        // Assert
        Assert.Single(parsers);
    }

    [Fact]
    public void StatementParserService_FindParser_ReturnsCorrectParser()
    {
        // Arrange
        var service = new StatementParserService();
        var parser = new ExampleBankParser();
        service.RegisterParser(parser);

        // Act
        var foundParser = service.FindParser("Example Bank");

        // Assert
        Assert.NotNull(foundParser);
        Assert.Equal("Example Bank", foundParser.BankIdentifier);
    }

    [Fact]
    public void BookingModel_CanBeCreated()
    {
        // Arrange & Act
        var booking = new Booking
        {
            BookingDate = DateTime.Now,
            Applicant = "Test User",
            Purpose = "Test Payment",
            Amount = 100.50m,
            Currency = "EUR"
        };

        // Assert
        Assert.NotNull(booking);
        Assert.Equal("Test User", booking.Applicant);
        Assert.Equal(100.50m, booking.Amount);
    }

    [Fact]
    public void StatementJsonSerializer_CanSerializeStatement()
    {
        // Arrange
        var statement = new Statement
        {
            Iban = "DE89370400440532013000",
            AccountHolder = "Test Account",
            Currency = "EUR",
            ClosingBalance = 1000.00m
        };

        // Act
        var json = StatementJsonSerializer.ToJson(statement);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("iban", json.ToLower());
            Assert.Contains("accountholder", json.ToLower());
    }

    [Fact]
    public void StatementJsonSerializer_CanDeserializeStatement()
    {
        // Arrange
        var statement = new Statement
        {
            Iban = "DE89370400440532013000",
            AccountHolder = "Test",
            Currency = "EUR",
            ClosingBalance = 100.00m
        };
        var json = StatementJsonSerializer.ToJson(statement);

        // Act
        var deserialized = StatementJsonSerializer.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("DE89370400440532013000", deserialized.Iban);
        Assert.Equal("Test", deserialized.AccountHolder);
        Assert.Equal(100.00m, deserialized.ClosingBalance);
    }

    [Fact]
    public void GenericPdfParser_CanIdentifyFormat()
    {
        // Arrange
        var parser = new GenericPdfParser();

        // Act & Assert
        Assert.Equal("Generic PDF Parser", parser.BankIdentifier);
        Assert.Single(parser.SupportedFormats);
        Assert.Contains(".pdf", parser.SupportedFormats);
    }

    [Fact]
    public void GenericPdfParser_CanParse_PdfFile()
    {
        // Arrange
        var parser = new GenericPdfParser();

        // Act
        var canParse = parser.CanParse("statement.pdf");

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void GenericPdfParser_CanParse_RejectsCsvFile()
    {
        // Arrange
        var parser = new GenericPdfParser();

        // Act
        var canParse = parser.CanParse("statement.csv");

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void GenericPdfParser_DetectsPdfSignature()
    {
        // Arrange
        var parser = new GenericPdfParser();
        byte[] pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        byte[] jpgSignature = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPG

        // Act & Assert
        Assert.True(parser.CanParseContent(pdfSignature, "test.pdf"));
        Assert.False(parser.CanParseContent(jpgSignature, "test.jpg"));
    }

    [Fact]
    public void StatementParserService_CanRegisterPdfParser()
    {
        // Arrange
        var service = new StatementParserService();
        var pdfParser = new GenericPdfParser();

        // Act
        service.RegisterParser(pdfParser);
        var foundParser = service.FindParser("Generic PDF Parser");

        // Assert
        Assert.NotNull(foundParser);
        Assert.Equal("Generic PDF Parser", foundParser.BankIdentifier);
    }

    [Fact]
    public void RaiffeisenParser_PNAmountConcatenation_IsParsedCorrectly()
    {
        // Arrange
        var parser = new RaiffeisenBankParser();
        // craft a minimal raw-extracted-text sample that mimics the PDF text extraction output
        var sample = "01.10. 01.10. EU-ÜBERWEISUNG SEPA PN:900 200,00 S\n Nebenkostenvorauszahlung";

        // Act
        var result = parser.ParseStatementFromText(sample);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Bookings);
        // the amount should be -200.00 (S -> debit)
        Assert.Contains(result.Bookings, b => b.Amount == -200.00m);
    }

    [Fact]
    public void RaiffeisenParser_StopsParsing_AfterSummaryMarkers()
    {
        // Arrange
        var parser = new RaiffeisenBankParser();
        var sample = "01.10. 01.10. SEPA PN:931 50,00 S\nPayPal Europe S.a.r.l.\nneuer Kontostand vom 31.10.2025 6.505,57 H\nSehr geehrte Kundin, sehr geehrter Kunde,";

        // Act
        var result = parser.ParseStatementFromText(sample);

        // Assert
        Assert.NotNull(result);
        // Should contain the one booking before the summary and not parse the trailing greeting as additional bookings
        Assert.Single(result.Bookings);
        Assert.Equal(-50.00m, result.Bookings[0].Amount);
    }
}
