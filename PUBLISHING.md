# NuGet Publishing Guide

Dieses Dokument beschreibt, wie du dein PdfStatementParser-Paket auf NuGet veröffentlichst.

## Schritt 1: Vorbereitung

### 1.1 NuGet.org Account erstellen
1. Besuche [nuget.org](https://www.nuget.org)
2. Klicke auf "Sign up"
3. Registriere dich mit deinem Microsoft-Konto oder erstelle ein neues Konto

### 1.2 API Key generieren
1. Melde dich auf nuget.org an
2. Gehe zu "Account settings" → "API keys"
3. Klicke auf "Create"
4. Gib einen Namen ein (z.B. "PdfStatementParser-Publishing")
5. Wähle die erforderlichen Permissions:
   - `Push new packages and package versions`
   - `Push existing packages and package versions`
6. Wähle das Expiration Date (empfohlen: 1 Jahr)
7. Klicke "Create"
8. **Speichere den API Key sicher ab** (du kannst ihn später nicht wieder sehen!)

## Schritt 2: Paket aktualisieren

Vor der Veröffentlichung:

### 2.1 Versionsnummer aktualisieren
Bearbeite `src/PdfStatementParser/PdfStatementParser.csproj`:
```xml
<Version>1.0.0</Version>  <!-- Erhöhe die Versionsnummer -->
```

Folge Semantic Versioning:
- MAJOR.MINOR.PATCH
- Beispiel: 1.2.3 → 1.2.4 (Patch-Update)
- Beispiel: 1.2.3 → 1.3.0 (Minor-Update)
- Beispiel: 1.2.3 → 2.0.0 (Major-Update)

### 2.2 CHANGELOG aktualisieren
Bearbeite `CHANGELOG.md` und füge einen neuen Eintrag für deine Version hinzu.

### 2.3 Metadaten prüfen
Überprüfe in `src/PdfStatementParser/PdfStatementParser.csproj`:
- `Authors` - Dein Name
- `Description` - Korrekte Beschreibung
- `PackageProjectUrl` - Link zu deinem GitHub-Repo
- `PackageTags` - Relevante Tags

## Schritt 3: Build und Tests

```bash
# Lösen Sie alle Abhängigkeiten auf
dotnet restore

# Bauen Sie das Projekt
dotnet build

# Führen Sie die Tests aus
dotnet test
```

Stelle sicher, dass alle Tests erfolgreich sind!

## Schritt 4: Paket erstellen

### Methode 1: Automatisch mit Skript (empfohlen)

**Auf macOS/Linux:**
```bash
./publish.sh
```

**Auf Windows:**
```powershell
.\publish.ps1
```

### Methode 2: Manuell

```bash
dotnet pack src/PdfStatementParser/PdfStatementParser.csproj -c Release -o ./nupkg
```

Das Paket wird in `./nupkg/` erstellt:
- `PdfStatementParser.1.0.0.nupkg` - Das Hauptpaket
- `PdfStatementParser.1.0.0.snupkg` - Symbol-Paket (für Debug-Informationen)

## Schritt 5: Veröffentlichen auf NuGet.org

### Erstes Mal veröffentlichen

```bash
dotnet nuget push ./nupkg/PdfStatementParser.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

Ersetze `YOUR_API_KEY` mit deinem echten API Key!

### Mit Symbol-Paket (empfohlen)

Symbol-Pakete ermöglichen Debugging mit deinem Code:

```bash
dotnet nuget push ./nupkg/PdfStatementParser.1.0.0.snupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Sicherheit: API Key in Umgebungsvariable speichern

Um deinen API Key nicht in der Shell-Historie zu speichern:

**macOS/Linux:**
```bash
export NUGET_API_KEY="your_actual_api_key"
dotnet nuget push ./nupkg/PdfStatementParser.1.0.0.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

**Windows (PowerShell):**
```powershell
$env:NUGET_API_KEY = "your_actual_api_key"
dotnet nuget push ./nupkg/PdfStatementParser.1.0.0.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

## Schritt 6: Verifizierung

Nach erfolgreichem Push:

1. Besuche https://www.nuget.org/packages/PdfStatementParser
2. Überprüfe, dass dein Paket aufgelistet ist
3. Überprüfe die Paketinformationen (Version, Beschreibung, Tags)
4. Überprüfe, dass das Symbol-Paket hochgeladen wurde

## Private NuGet Feed

Falls du in einen privaten NuGet Feed veröffentlichen möchtest:

```bash
dotnet nuget push ./nupkg/PdfStatementParser.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source YOUR_FEED_URL
```

## GitHub Actions für automatische Veröffentlichung

`.github/workflows/build.yml` ist bereits konfiguriert zum:
- Bauen der Solution
- Ausführen von Tests
- Erstellen des NuGet-Pakets

Du kannst es erweitern, um automatisch zu pushen:

```yaml
- name: Push to NuGet
  run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

Gehe zu GitHub Settings → Secrets und füge `NUGET_API_KEY` hinzu.

## Troubleshooting

### Fehler: "The nuget.org service is not available"
- Überprüfe deine Internetverbindung
- Überprüfe den NuGet-Status: https://status.nuget.org

### Fehler: "The package name 'PdfStatementParser' is already reserved"
- Der Paketname ist bereits registriert
- Ändere den Namen in `PdfStatementParser.csproj`

### Fehler: "Invalid API key"
- Überprüfe, dass du den API Key korrekt kopiert hast
- Überprüfe, dass der API Key nicht abgelaufen ist
- Generiere einen neuen API Key

### Fehler: "The package version already exists"
- Erhöhe die Versionsnummer in `PdfStatementParser.csproj`
- Jede Version kann nur einmal veröffentlicht werden

## Weitere Ressourcen

- [NuGet Documentation](https://docs.microsoft.com/en-us/nuget/)
- [Semantic Versioning](https://semver.org/)
- [Create a NuGet Package](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package)
- [Publish to NuGet.org](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
