#!/bin/bash

# PdfStatementParser - Demonstrationsskript
# Zeigt die wichtigsten Funktionen des Parsers

set -e

echo "🏦 PdfStatementParser - Demo Script"
echo "===================================="
echo ""

# 1. Build
echo "📦 Building project..."
dotnet build -c Release > /dev/null

# 2. Run tests
echo "🧪 Running tests..."
dotnet test --no-build -c Release --verbosity quiet

# 3. Show project structure
echo ""
echo "📁 Project Structure:"
echo "===================================="
find ./src -name "*.cs" | grep -v "bin\|obj" | sed 's|^|  |'

echo ""
echo "📁 Tests:"
find ./tests -name "*.cs" | grep -v "bin\|obj" | sed 's|^|  |'

# 4. Create sample JSON
echo ""
echo "📋 Creating example JSON output..."
echo "===================================="

# Create a C# snippet that generates JSON and output it
dotnet run --no-build -c Release --project samples/ 2>/dev/null || echo "Note: Run 'dotnet new console -n Samples' in samples/ folder to add sample project"

echo ""
echo "✅ Demo complete!"
echo ""
echo "Next steps:"
echo "1. Implement bank-specific parsers (see PARSER_GUIDE.md)"
echo "2. Add PDF parsing libraries (iTextSharp, PdfSharp, etc.)"
echo "3. Deploy to NuGet (see PUBLISHING.md)"
echo ""
