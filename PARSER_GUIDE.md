# Bank Statement Parser Service

Ein flexibles und erweiterbares .NET-System zum Parsen von Kontoauszügen verschiedener Kreditinstitute und Ausgabe als JSON.

## Features

- **Mehrbank-Unterstützung**: Architektur für beliebig viele Kreditinstitute erweiterbar
- **JSON-Export**: Strukturierte Ausgabe von Buchungen im JSON-Format
- **Automatische Erkennung**: System erkennt automatisch das richtige Datenformat und Institut
- **Typsichere Modelle**: Vollständig strukturierte Klassen für Kontoauszüge und Buchungen
- **Async/Await Support**: Nicht-blockierende Dateiverarbeitung
- **Unit Tests**: Umfangreiche Test-Suite mit xUnit

## Struktur

```
Models/
  ├── Booking.cs          # Einzelne Buchung/Transaktion
  ├── Statement.cs        # Kompletter Kontoauszug
  
Services/
  ├── IBankStatementParser.cs      # Parser-Interface
  ├── BaseBankStatementParser.cs   # Basis-Implementierung
  ├── StatementParserService.cs    # Zentrale Parser-Verwaltung
  ├── StatementJsonSerializer.cs   # JSON-Serialisierung
  └── ExampleBankParser.cs         # Beispiel-Parser
```

## Modelle

### Booking (Einzelne Buchung)

```csharp
public class Booking
{
    public DateTime BookingDate { get; set; }      // Buchungsdatum
    public DateTime? ValueDate { get; set; }       // Wertdatum
    public string BookingText { get; set; }        // Transaktionstyp (z.B. "SEPA Transfer")
    public string Applicant { get; set; }          // Auftraggeber/Empfänger
    public string Purpose { get; set; }            // Verwendungszweck
    public decimal Amount { get; set; }            // Betrag (+ Gutschrift, - Belastung)
    public string Currency { get; set; }           // Währung (z.B. "EUR")
    public decimal? Balance { get; set; }          // Kontostand nach Buchung
    public string? TransactionNumber { get; set; } // Transaktionsnummer
    public string? Memo { get; set; }              // Notizen
}
```

### Statement (Kontoauszug)

```csharp
public class Statement
{
    public string Iban { get; set; }               // IBAN
    public string AccountHolder { get; set; }      // Kontoinhaber
    public string BankName { get; set; }           // Bankname
    public string StatementNumber { get; set; }    // Auszugsnummer
    public DateTime StartDate { get; set; }        // Startdatum
    public DateTime EndDate { get; set; }          // Enddatum
    public decimal OpeningBalance { get; set; }    // Anfangssaldo
    public decimal ClosingBalance { get; set; }    // Endsaldo
    public string Currency { get; set; }           // Währung
    public List<Booking> Bookings { get; set; }    // Buchungsliste
    public string BankIdentifier { get; set; }     // Parser-Identifier
    public ParseMetadata? Metadata { get; set; }   // Parse-Informationen
}
```

## Grundlegende Verwendung

### Einfaches Parsing

```csharp
using PdfStatementParser.Services;

// Service initialisieren
var service = new StatementParserService();

// Parser registrieren
service.RegisterParser(new ExampleBankParser());

// Datei parsen
var statement = await service.ParseAsync("kontoauszug.csv");

// Zu JSON konvertieren
string json = StatementJsonSerializer.ToJson(statement);
```

### Mehrere Parser registrieren

```csharp
var service = new StatementParserService();

// Mehrere Banken unterstützen
service.RegisterParsers(
    new ExampleBankParser(),
    new DeutscheBankParser(),
    new CommerzBankParser(),
    new SpardasseParser()
    // ... weitere Parser
);

// System wählt automatisch den richtigen Parser
var statement = await service.ParseAsync("auszug_2025_08.pdf");
```

### Direktes Parsing ohne Service

```csharp
var parser = new ExampleBankParser();

if (parser.CanParse("statement.csv"))
{
    var statement = await parser.ParseAsync("statement.csv");
}
```

### Mit Byte-Content

```csharp
var content = await File.ReadAllBytesAsync("kontoauszug.pdf");
var statement = await service.ParseContentAsync(content, "kontoauszug.pdf");
```

## JSON-Serialisierung

### Zu JSON

```csharp
// Mit Formatierung
var json = StatementJsonSerializer.ToJson(statement, indent: true);

// Ohne Formatierung
var json = StatementJsonSerializer.ToJson(statement, indent: false);

// Als Bytes
byte[] jsonBytes = StatementJsonSerializer.ToJsonBytes(statement);
```

### Von JSON

```csharp
// Aus String
var statement = StatementJsonSerializer.FromJson(jsonString);

// Aus Bytes
var statement = StatementJsonSerializer.FromJsonBytes(jsonBytes);
```

### Ausgabe-Format

```json
{
  "iban": "DE89370400440532013000",
  "accountHolder": "Sample Account Holder",
  "bankName": "Example Bank",
  "statementNumber": "001",
  "startDate": "2025-08-01T00:00:00",
  "endDate": "2025-08-31T00:00:00",
  "openingBalance": 1000.00,
  "closingBalance": 1050.00,
  "currency": "EUR",
  "bookings": [
    {
      "bookingDate": "2025-08-05T00:00:00",
      "valueDate": "2025-08-05T00:00:00",
      "bookingText": "Transfer",
      "applicant": "John Doe",
      "purpose": "Invoice #12345",
      "amount": 50.00,
      "currency": "EUR",
      "balance": 1050.00,
      "transactionNumber": "TXN001"
    }
  ],
  "bankIdentifier": "Example Bank",
  "metadata": {
    "parsedAt": "2025-12-06T12:00:00Z",
    "parserVersion": "1.0",
    "sourceFileName": "statement.csv",
    "confidenceScore": 100,
    "warnings": []
  }
}
```

## Einen neuen Bank-Parser erstellen

### 1. IBankStatementParser implementieren

```csharp
using System.Threading.Tasks;
using PdfStatementParser.Models;
using PdfStatementParser.Services;

public class MyBankParser : BaseBankStatementParser
{
    public override string BankIdentifier => "My Bank";
    public override string[] SupportedFormats => new[] { ".pdf" };

    protected override bool CanParseContentBySignature(byte[] content)
    {
        // Optional: Datei-Signatur prüfen (Magic Bytes)
        // PDF-Dateien beginnen mit %PDF
        if (content.Length >= 4)
        {
            if (content[0] == 0x25 && content[1] == 0x50 && 
                content[2] == 0x44 && content[3] == 0x46)
            {
                return true;
            }
        }
        return false;
    }

    public override async Task<Statement?> ParseAsync(string filePath)
    {
        var content = await ReadFileAsync(filePath);
        return await ParseContentAsync(content, System.IO.Path.GetFileName(filePath));
    }

    public override async Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
    {
        // Implementiere deine Parse-Logik hier
        
        var statement = new Statement
        {
            Iban = "...",
            AccountHolder = "...",
            BankName = BankIdentifier,
            // ... weitere Felder füllen
            Bookings = new()
            {
                new Booking
                {
                    BookingDate = DateTime.Now,
                    Amount = 100.00m,
                    // ... weitere Felder
                }
            }
        };
        
        return await Task.FromResult(statement);
    }
}
```

### 2. Parser registrieren und nutzen

```csharp
var service = new StatementParserService();
service.RegisterParser(new MyBankParser());

var statement = await service.ParseAsync("kontoauszug.pdf");
var json = StatementJsonSerializer.ToJson(statement);
```

## Testen

```bash
# Alle Tests ausführen
dotnet test

# Mit Coverage
dotnet test /p:CollectCoverage=true

# Spezifische Tests
dotnet test --filter "ClassName=StatementParserTests"
```

## NuGet-Paket

Das Projekt ist als NuGet-Paket veröffentlicht:

```bash
dotnet add package PdfStatementParser
```

## API-Referenz

### StatementParserService

```csharp
// Parser registrieren
void RegisterParser(IBankStatementParser parser)
void RegisterParsers(params IBankStatementParser[] parsers)

// Parser abrufen
IReadOnlyList<IBankStatementParser> GetParsers()
IBankStatementParser? FindParser(string bankIdentifier)

// Parsing
Task<Statement?> ParseAsync(string filePath)
Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
```

### IBankStatementParser

```csharp
// Properties
string BankIdentifier { get; }
string[] SupportedFormats { get; }

// Erkennung
bool CanParse(string filePath)
bool CanParseContent(byte[] content, string? fileName = null)

// Parsing
Task<Statement?> ParseAsync(string filePath)
Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
```

## Performance-Tipps

1. **Wiederverwendung des Service**: Erstelle den Service einmal und verwende ihn mehrfach
2. **Parser-Caching**: Registrierte Parser werden gecacht
3. **Async/Await**: Nutze async APIs für große Dateien
4. **Batch-Verarbeitung**: Parst mehrere Dateien parallel mit `Task.WhenAll`

## Fehlerbehandlung

```csharp
try
{
    var statement = await service.ParseAsync("kontoauszug.csv");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Datei nicht gefunden: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Kein passender Parser gefunden: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Parse-Fehler: {ex.Message}");
}
```

## Lizenz

MIT License - siehe LICENSE Datei

## Support

Für Bugs, Feature-Requests und Fragen siehe GitHub Issues.
