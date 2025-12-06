using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using PdfStatementParser.Models;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
namespace PdfStatementParser.Services
{
    /// <summary>
    /// Generic PDF statement parser that extracts text from PDF files.
    /// </summary>
    public class GenericPdfParser : BaseBankStatementParser
    {
        /// <summary>
        /// Gets the bank identifier for this parser.
        /// </summary>
        public override string BankIdentifier => "Generic PDF Parser";

        /// <summary>
        /// Gets the supported file formats.
        /// </summary>
        public override string[] SupportedFormats => new[] { ".pdf" };

        /// <summary>
        /// Determines if the provided content looks like a PDF file based on signature.
        /// </summary>
        /// <returns>True if content starts with PDF magic bytes (%PDF), false otherwise.</returns>
        protected override bool CanParseContentBySignature(byte[] content)
        {
            if (content == null || content.Length < 4)
                return false;

            // PDF files start with %PDF magic bytes
            return content[0] == 0x25 && // %
                   content[1] == 0x50 && // P
                   content[2] == 0x44 && // D
                   content[3] == 0x46;   // F
        }

        /// <summary>
        /// Parses a PDF file and extracts banking statement information.
        /// </summary>
        /// <param name="filePath">Path to the PDF file.</param>
        /// <returns>Extracted banking statement or null if parsing fails.</returns>
        public override async Task<Statement?> ParseAsync(string filePath)
        {
            try
            {
                var content = await ReadFileAsync(filePath);
                var statement = await ParseContentAsync(content, Path.GetFileName(filePath));
                return statement;
                        }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing PDF {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses PDF content from bytes and extracts banking statement information.
        /// </summary>
        /// <param name="content">PDF file content as bytes.</param>
        /// <param name="fileName">Optional filename for metadata.</param>
        /// <returns>Extracted banking statement or null if parsing fails.</returns>
        public override async Task<Statement?> ParseContentAsync(byte[] content, string? fileName)
        {
            try
            {
                var text = ExtractTextFromPdf(content);
                if (string.IsNullOrEmpty(text))
                    return null;

                var statement = ParseStatementFromText(text);
                if (statement != null && fileName != null)
                {
                    statement.Metadata ??= new ParseMetadata();
                    statement.Metadata.SourceFileName = fileName;
                }
                return statement;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing PDF content: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts text from PDF file using iText7.
        /// </summary>
        private string ExtractTextFromPdf(byte[] content)
        {
            var textBuilder = new StringBuilder();

            try
            {
                using (var reader = new PdfReader(new MemoryStream(content)))
                using (var document = new PdfDocument(reader))
                {
                    int pageCount = document.GetNumberOfPages();
                    
                    for (int pageNum = 1; pageNum <= pageCount; pageNum++)
                    {
                        var page = document.GetPage(pageNum);
                        var text = ExtractTextFromPage(page);
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            textBuilder.AppendLine($"--- Page {pageNum} ---");
                            textBuilder.AppendLine(text);
                            textBuilder.AppendLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from PDF: {ex.Message}");
            }

            return textBuilder.ToString();
        }

        /// <summary>
        /// Public wrapper to extract text from PDF bytes. Useful for callers that want raw text.
        /// </summary>
        public string ExtractTextFromBytes(byte[] content)
        {
            return ExtractTextFromPdf(content);
        }

        /// <summary>
        /// Extracts text from a single PDF page.
        /// </summary>
        private string ExtractTextFromPage(iText.Kernel.Pdf.PdfPage page)
        {
            try
            {
                // Use LocationTextExtractionStrategy for better text extraction
                var strategy = new LocationTextExtractionStrategy();
                var textFromPage = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                return textFromPage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from page: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses statement data from extracted text.
        /// This is a basic implementation that creates a statement structure.
        /// Bank-specific parsers should override this for more sophisticated parsing.
        /// </summary>
        private Statement? ParseStatementFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            var statement = new Statement
            {
                BankIdentifier = BankIdentifier,
                Currency = "EUR",
                Bookings = new List<Booking>(),
                Metadata = new ParseMetadata
                {
                    ParserVersion = "1.0",
                    ConfidenceScore = 50, // Low confidence for generic parser
                    Warnings = new List<string> { "Generic PDF parser - bank-specific parsing not implemented" }
                }
            };

            // Extract basic information from lines
            ParseStatementMetadata(lines, statement);
            
            // Attempt to extract bookings
            ParseBookings(lines, statement);

            return statement;
        }

        /// <summary>
        /// Parses statement-level metadata from text lines.
        /// </summary>
        private void ParseStatementMetadata(string[] lines, Statement statement)
        {
            System.Text.RegularExpressions.Regex ibanRegex = new System.Text.RegularExpressions.Regex(@"DE\s*\d{2}\s*\d{4}\s*\d{4}\s*\d{4}\s*\d{4}\s*\d{2}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex compactIbanRegex = new System.Text.RegularExpressions.Regex(@"DE\d{20}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var germanCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            foreach (var line in lines)
            {
                var lowerLine = line.ToLower();
                
                // Try to find IBAN (formatted or compact)
                if (lowerLine.Contains("iban") || lowerLine.Contains("konto") || lowerLine.Contains("bic"))
                {
                    // Try to find formatted IBAN (with spaces)
                    var m = ibanRegex.Match(line);
                    if (m.Success)
                    {
                        var candidate = System.Text.RegularExpressions.Regex.Replace(m.Value, "\\s+", "");
                        if (candidate.StartsWith("DE") && candidate.Length == 22)
                        {
                            statement.Iban = candidate;
                        }
                    }

                    // Try compact IBAN (no spaces)
                    if (string.IsNullOrEmpty(statement.Iban))
                    {
                        var m2 = compactIbanRegex.Match(line);
                        if (m2.Success)
                        {
                            statement.Iban = m2.Value;
                        }
                    }
                }
                
                // Try to find account holder
                if (lowerLine.Contains("inhaber") || lowerLine.Contains("name") || lowerLine.Contains("account holder"))
                {
                    statement.AccountHolder = ExtractValueAfterLabel(line, new[] { ":", "=" });
                }
                
                // Try to find bank name
                if (lowerLine.Contains("bank"))
                {
                    statement.BankName = ExtractValueAfterLabel(line, new[] { ":", "=" });
                }
                
                // Try to find dates
                if (lowerLine.Contains("von") || lowerLine.Contains("from") || lowerLine.Contains("erstellt am"))
                {
                    TryParseDateRange(line, statement);
                }

                // Try to find opening/closing balance patterns
                if (lowerLine.Contains("alter kontostand") || lowerLine.Contains("neuer kontostand") || lowerLine.Contains("kontostand"))
                {
                    // find german formatted amount like 2.134,39
                    var amtRegex = new System.Text.RegularExpressions.Regex(@"\d{1,3}(?:[\.\s]\d{3})*,\d{2}");
                    var m = amtRegex.Match(line);
                    if (m.Success)
                    {
                        var numeric = m.Value.Replace(".", "").Replace(" ", "").Replace(",", ".");
                        if (decimal.TryParse(numeric, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var val))
                        {
                            if (lowerLine.Contains("alter kontostand"))
                                statement.OpeningBalance = val;
                            else if (lowerLine.Contains("neuer kontostand") || lowerLine.Contains("neuer kontostand vom"))
                                statement.ClosingBalance = val;
                            else
                            {
                                // generic kontostand - set closing balance
                                statement.ClosingBalance = val;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parses booking/transaction lines from text.
        /// </summary>
        private void ParseBookings(string[] lines, Statement statement)
        {
            // Heuristic approach: detect lines that start with a date (e.g. "18.08.")
            // Many German Kontoauszüge use a layout where a booking header line starts with the booking and value date,
            // followed by one or more lines with the counterparty and purpose.
            var dateStartRegex = new System.Text.RegularExpressions.Regex(@"^\d{1,2}\.\d{1,2}\.");
            var amtRegex = new System.Text.RegularExpressions.Regex(@"\d{1,3}(?:[\.\s]\d{3})*,\d{2}");
            var culture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (dateStartRegex.IsMatch(line))
                {
                    // Start of a booking block
                    var booking = new Booking { Currency = statement.Currency ?? "EUR" };

                    // Attempt to parse booking date from the start of the line
                    var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    DateTime parsedDate;
                    if (DateTime.TryParse(tokens[0], out parsedDate))
                    {
                        booking.BookingDate = parsedDate;
                    }
                    else
                    {
                        // Try dd.MM. with current year
                        var dayMonth = tokens[0];
                        if (DateTime.TryParseExact(dayMonth, new[] { "d.M.", "dd.MM." }, culture, System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            // assume year from statement endDate if available, else current year
                            int year = statement.EndDate != default ? statement.EndDate.Year : DateTime.Now.Year;
                            booking.BookingDate = new DateTime(year, parsedDate.Month, parsedDate.Day);
                        }
                    }

                    // Extract amount from the header line if present
                    var m = amtRegex.Match(line);
                    if (m.Success)
                    {
                        var numeric = m.Value.Replace(".", "").Replace(" ", "").Replace(",", ".");
                        if (decimal.TryParse(numeric, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amt))
                        {
                            booking.Amount = amt;
                        }
                    }

                    // Detect debit/credit marker (S for Soll/debit, H for Haben/credit)
                    if (line.EndsWith(" H") || line.Contains(" H "))
                        booking.Amount = Math.Abs(booking.Amount); // credit (positive)
                    if (line.EndsWith(" S") || line.Contains(" S "))
                        booking.Amount = -Math.Abs(booking.Amount); // debit (negative)

                    // Look ahead for applicant and purpose lines (commonly the next 1-2 lines)
                    var applicant = new List<string>();
                    var purpose = new List<string>();
                    int look = i + 1;
                    while (look < lines.Length && !dateStartRegex.IsMatch(lines[look]) && applicant.Count < 2)
                    {
                        var l = lines[look].Trim();
                        if (!string.IsNullOrEmpty(l))
                        {
                            applicant.Add(l);
                        }
                        look++;
                    }

                    if (applicant.Count > 0)
                    {
                        booking.Applicant = applicant[0];
                        if (applicant.Count > 1)
                            booking.Purpose = string.Join(" ", applicant.Skip(1));
                    }

                    statement.Bookings.Add(booking);
                }
            }
        }

        /// <summary>
        /// Attempts to parse a single booking from a line of text.
        /// </summary>
        private bool TryParseBookingLine(string line, out Booking booking)
        {
            booking = new Booking();
            
            // Look for date pattern DD.MM.YYYY or DD/MM/YYYY
            var parts = line.Split(new[] { '\t', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3)
                return false;

            // Try to parse date from first part
            if (!DateTime.TryParse(parts[0], out var bookingDate))
                return false;

            booking = new Booking
            {
                BookingDate = bookingDate,
                Applicant = parts.Length > 1 ? parts[1].Trim() : "",
                Purpose = parts.Length > 2 ? parts[2].Trim() : "",
                Currency = "EUR"
            };

            // Try to extract amount (usually decimal value)
            for (int i = 3; i < parts.Length; i++)
            {
                if (decimal.TryParse(parts[i].Replace(".", ","), out var amount))
                {
                    booking.Amount = amount;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Extracts a value after a label in a line.
        /// </summary>
        private string ExtractValueAfterLabel(string line, string[] separators)
        {
            foreach (var sep in separators)
            {
                if (line.Contains(sep))
                {
                    var parts = line.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 1 ? parts.Last().Trim() : "";
                }
            }
            return "";
        }

        /// <summary>
        /// Attempts to parse date range from a line.
        /// </summary>
        private void TryParseDateRange(string line, Statement statement)
        {
            // Look for date patterns and try to parse them
            var parts = line.Split(new[] { ' ', '\t', '-' }, StringSplitOptions.RemoveEmptyEntries);
            
            var dates = new List<DateTime>();
            foreach (var part in parts)
            {
                if (DateTime.TryParse(part, out var date))
                {
                    dates.Add(date);
                }
            }

            if (dates.Count >= 2)
            {
                statement.StartDate = dates[0];
                statement.EndDate = dates[dates.Count - 1];
            }
            else if (dates.Count == 1)
            {
                statement.StartDate = dates[0];
            }
        }
    }
}
