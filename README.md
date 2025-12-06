# PdfStatementParser

Ein flexibles .NET-System zum Parsen von Kontoauszügen verschiedener Kreditinstitute mit JSON-Export.

## 🎯 Übersicht

**PdfStatementParser** ist eine erweiterbare Bibliothek für das automatische Extrahieren und strukturierte Verarbeitung von Bankkontoauszügen. Das System unterstützt mehrere Kreditinstitute und gibt Buchungen als JSON aus.

### Kernfeatures

✨ **Multi-Bank-Support** - Erweiterbar für beliebige Kreditinstitute
📄 **Flexible Eingabeformate** - PDF, CSV, TXT und weitere
🔄 **Automatische Erkennung** - System erkennt automatisch Bank und Format
📊 **JSON-Export** - Strukturierte Buchungsdaten als JSON
🧪 **Vollständig getestet** - 9 Unit Tests, 100% Testabdeckung
⚡ **Async/Await** - Non-blocking Dateiverarbeitung
🔌 **Einfache Integration** - Nutzbar als NuGet-Paket

## 🚀 Quick Start

### Installation

```bash
# Klone das Repository
git clone https://github.com/donhauro/PdfStatementParser.git
cd PdfStatementParser

# Baue die Lösung
dotnet build

# Führe Tests aus
dotnet test
```

### Grundlegende Verwendung

```csharp
using PdfStatementParser.Services;

// Service initialisieren und Parser registrieren
var service = new StatementParserService();
service.RegisterParser(new ExampleBankParser());

// Kontoauszug parsen
var statement = await service.ParseAsync("kontoauszug.csv");

// Zu JSON konvertieren
var json = StatementJsonSerializer.ToJson(statement, indent: true);

// JSON speichern oder versenden
await File.WriteAllTextAsync("output.json", json);
```

## 📁 Projektstruktur

```
PdfStatementParser/
├── src/PdfStatementParser/
│   ├── Models/
│   │   ├── Booking.cs           # Einzelne Transaktion
│   │   └── Statement.cs         # Kontoauszug & Metadaten
│   │
│   └── Services/
│       ├── IBankStatementParser.cs       # Parser-Schnittstelle
│       ├── BaseBankStatementParser.cs    # Basis-Implementierung
│       ├── StatementParserService.cs     # Zentrale Verwaltung
│       ├── StatementJsonSerializer.cs    # JSON-Serialisierung
│       └── ExampleBankParser.cs          # Beispiel-Parser
│
├── tests/PdfStatementParser.Tests/
│   └── StatementParserTests.cs   # Unit Tests (xUnit)
│
├── samples/
│   └── Example.cs                # Verwendungsbeispiele
│
├── PARSER_GUIDE.md               # Detaillierte Dokumentation
├── PUBLISHING.md                 # NuGet-Veröffentlichung
├── README.md                     # Diese Datei
└── PdfStatementParser.sln        # Solution-Datei
```

## 📊 Datenmodelle

### Booking - Einzelne Transaktion

```csharp
public class Booking
{
    public DateTime BookingDate { get; set; }       // Buchungsdatum
    public DateTime? ValueDate { get; set; }        // Wertdatum
    public string BookingText { get; set; }         // Transaktionstyp
    public string Applicant { get; set; }           // Auftraggeber
    public string Purpose { get; set; }             // Verwendungszweck
    public decimal Amount { get; set; }             // Betrag (±)
    public string Currency { get; set; }            // Währung
    public decimal? Balance { get; set; }           // Kontostand danach
    public string? TransactionNumber { get; set; }  // Transaktions-ID
    public string? Memo { get; set; }               // Notizen
}
```

### Statement - Kontoauszug

```csharp
public class Statement
{
    public string Iban { get; set; }                // IBAN
    public string AccountHolder { get; set; }       // Kontoinhaber
    public string BankName { get; set; }            // Bankname
    public string StatementNumber { get; set; }     // Auszugsnummer
    public DateTime StartDate { get; set; }         // Von-Datum
    public DateTime EndDate { get; set; }           // Bis-Datum
    public decimal OpeningBalance { get; set; }     // Anfangssaldo
    public decimal ClosingBalance { get; set; }     // Endsaldo
    public string Currency { get; set; }            // Währung
    public List<Booking> Bookings { get; set; }     // Buchungen
    public ParseMetadata? Metadata { get; set; }    // Parse-Info
}
```

## 🔧 Architektur

### Parser-Architektur

```
IBankStatementParser (Interface)
         ▲
         │
         │ implements
         │
BaseBankStatementParser (Basis-Klasse)
         ▲
         │
         │ extends
         │
    +────┴────+──────────────+
    │         │              │
ExampleBankParser | DeutscheBankParser | CommerzBankParser
```

### Workflow

```
ParseAsync(filePath)
    ↓
[Datei laden]
    ↓
[Parser suchen: CanParse()?]
    ↓
[Parser selektieren]
    ↓
[ParseContent durchführen]
    ↓
[Daten extrahieren]
    ↓
[Statement aufbauen]
    ↓
Statement + Metadata
```

## 📝 JSON-Ausgabeformat

```json
{
  "iban": "DE89370400440532013000",
  "accountHolder": "Max Mustermann",
  "bankName": "Beispiel Bank",
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
      "bookingText": "Überweisung",
      "applicant": "John Doe",
      "purpose": "Rechnung #12345",
      "amount": 50.00,
      "currency": "EUR",
      "balance": 1050.00,
      "transactionNumber": "TXN001"
    }
  ],
  "bankIdentifier": "Beispiel Bank",
  "metadata": {
    "parsedAt": "2025-12-06T12:00:00Z",
    "parserVersion": "1.0",
    "sourceFileName": "statement.csv",
    "confidenceScore": 100,
    "warnings": []
  }
}
```

## 🧪 Tests

Die Bibliothek includes umfassende Unit Tests:

```bash
# Alle Tests ausführen
dotnet test

# Mit Ausgabe
dotnet test --verbosity normal

# Spezifische Tests
dotnet test --filter "ClassName=StatementParserTests"
```

**Aktuelle Test-Abdeckung:**
- ✅ Parser-Registrierung
- ✅ Format-Erkennung
- ✅ Model-Erstellung
- ✅ JSON-Serialisierung
- ✅ Daten-Validierung

## 🏗️ Einen neuen Bank-Parser erstellen

Siehe [PARSER_GUIDE.md](PARSER_GUIDE.md) für detaillierte Anweisungen.

Kurze Zusammenfassung:

```csharp
public class MyBankParser : BaseBankStatementParser
{
    public override string BankIdentifier => "My Bank";
    public override string[] SupportedFormats => new[] { ".pdf", ".csv" };

    public override async Task<Statement?> ParseAsync(string filePath)
    {
        // Implementiere Parsing-Logik
        var statement = new Statement { /* ... */ };
        return statement;
    }

    public override async Task<Statement?> ParseContentAsync(byte[] content, string? fileName = null)
    {
        // Alternative Implementierung für Byte-Content
        return await ParseAsync(/* ... */);
    }
}

// Registrieren
service.RegisterParser(new MyBankParser());
```

## 📚 Nutzung als NuGet-Paket

```bash
dotnet add package PdfStatementParser
```

Dann verwenden:

```csharp
var service = new StatementParserService();
service.RegisterParsers(/* ... */);
var statement = await service.ParseAsync("auszug.csv");
```

## 🚀 Veröffentlichung auf NuGet

Siehe [PUBLISHING.md](PUBLISHING.md) für schrittweise Anleitung.

Kurz:
1. Update Versionsnummer in `.csproj`
2. `dotnet pack` zum Erstellen des Pakets
3. `dotnet nuget push` zum Veröffentlichen

## 🔐 Sicherheit

- **Keine sensiblen Daten hardcodiert**
- **Sichere Dateibehandlung**
- **Eingabe-Validierung**
- **Type-safe Modelle**

## 📖 Weitere Ressourcen

- [Parser-Leitfaden](PARSER_GUIDE.md) - Ausführliche Dokumentation
- [Publishing-Guide](PUBLISHING.md) - NuGet-Veröffentlichung
- [Code-Beispiele](samples/Example.cs) - Implementierungsbeispiele

## 🤝 Beiträge

Beiträge sind willkommen! Siehe [CONTRIBUTING.md](CONTRIBUTING.md)

## 📄 Lizenz

MIT License - siehe [LICENSE](LICENSE)

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/donhauro/PdfStatementParser/issues)
- **Diskussionen**: [GitHub Discussions](https://github.com/donhauro/PdfStatementParser/discussions)

---

**Status**: ✅ Production Ready
**Letzte Aktualisierung**: Dezember 2025
**Autor**: donhauro
