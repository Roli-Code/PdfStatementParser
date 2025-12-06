# PdfStatementParser NuGet Publishing Script for Windows

Write-Host "🔨 Building solution..." -ForegroundColor Cyan
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "📦 Creating NuGet package..." -ForegroundColor Cyan
dotnet pack src/PdfStatementParser/PdfStatementParser.csproj -c Release -o ./nupkg

if ($LASTEXITCODE -ne 0) {
    Write-Host "Packing failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ NuGet package created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Location: ./nupkg/" -ForegroundColor Yellow
Get-ChildItem ./nupkg/

Write-Host ""
Write-Host "📝 To publish to NuGet.org:" -ForegroundColor Cyan
Write-Host "  1. Get your API key from https://www.nuget.org/account/apikeys" -ForegroundColor White
Write-Host "  2. Run the following command:" -ForegroundColor White
Write-Host ""
Write-Host "  dotnet nuget push ./nupkg/PdfStatementParser.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Yellow
Write-Host ""
Write-Host "📝 To publish to a private feed:" -ForegroundColor Cyan
Write-Host "  dotnet nuget push ./nupkg/PdfStatementParser.*.nupkg --api-key YOUR_API_KEY --source YOUR_FEED_URL" -ForegroundColor Yellow
Write-Host ""
