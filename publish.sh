#!/bin/bash

# PdfStatementParser NuGet Publishing Script

set -e

echo "🔨 Building solution..."
dotnet build -c Release

echo "📦 Creating NuGet package..."
dotnet pack src/PdfStatementParser/PdfStatementParser.csproj -c Release -o ./nupkg

echo ""
echo "✅ NuGet package created successfully!"
echo ""
echo "📍 Location: ./nupkg/"
ls -lh ./nupkg/

echo ""
echo "📝 To publish to NuGet.org:"
echo "  1. Get your API key from https://www.nuget.org/account/apikeys"
echo "  2. Run the following command:"
echo ""
echo "  dotnet nuget push ./nupkg/PdfStatementParser.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
echo ""
echo "📝 To publish to a private feed:"
echo "  dotnet nuget push ./nupkg/PdfStatementParser.*.nupkg --api-key YOUR_API_KEY --source YOUR_FEED_URL"
echo ""
