using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services
{
    /// <summary>
    /// Parser specialized for Raiffeisenbank Unteres Vilstal eG account statements.
    /// Uses stricter heuristics for dates, amounts and multi-line purposes.
    /// </summary>
    public class RaiffeisenBankParser : BaseBankStatementParser
    {
        /// <summary>
        /// Identifier for this bank parser.
        /// </summary>
        public override string BankIdentifier => "Raiffeisenbank Unteres Vilstal eG";

        /// <summary>
        /// Supported file extensions.
        /// </summary>
        public override string[] SupportedFormats => new[] { ".pdf" };

        /// <summary>
        /// Determines whether the content appears to be a PDF by checking magic bytes.
        /// </summary>
        protected override bool CanParseContentBySignature(byte[] content)
        {
            if (content == null || content.Length < 4)
                return false;
            return content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46;
        }

        /// <summary>
        /// Parse PDF content bytes into a <see cref="Statement"/> for this bank.
        /// </summary>
        public override async Task<Statement?> ParseContentAsync(byte[] content, string? fileName)
        {
            try
            {
                // Reuse GenericPdfParser's text extraction for robustness
                var extractor = new GenericPdfParser();
                var text = extractor.ExtractTextFromBytes(content);
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                var statement = ParseStatementFromText(text);
                if (statement != null && fileName != null)
                {
                    statement.Metadata ??= new ParseMetadata();
                    statement.Metadata.SourceFileName = fileName;
                }
                return await Task.FromResult(statement);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Raiffeisen parser error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse a PDF file path into a <see cref="Statement"/> for this bank.
        /// </summary>
        public override async Task<Statement?> ParseAsync(string filePath)
        {
            try
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return await ParseContentAsync(bytes, System.IO.Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Raiffeisen ParseAsync error: {ex.Message}");
                return null;
            }
        }

        internal Statement? ParseStatementFromText(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).ToArray();

            var statement = new Statement
            {
                BankIdentifier = BankIdentifier,
                Currency = "EUR",
                Bookings = new List<Booking>(),
                Metadata = new ParseMetadata { ParserVersion = "1.0" }
            };

                // extract IBAN and balances using patterns
                var ibanRegex = new Regex(@"DE\s*\d{2}\s*\d{4}\s*\d{4}\s*\d{4}\s*\d{4}\s*\d{2}", RegexOptions.IgnoreCase);
                var compactIban = new Regex(@"DE\d{20}", RegexOptions.IgnoreCase);
                // prefer '.' as thousands separator to avoid false matches like 'PN:900 200,00'
                var amountRegex = new Regex(@"\d{1,3}(?:\.\d{3})*,\d{2}");
                var startDateRegex = new Regex(@"erstellt am\s*(\d{1,2}\.\d{1,2}\.\d{4})", RegexOptions.IgnoreCase);
            // Helper: detect footer-like lines (short numeric ids, page markers, or known footer tokens)
            bool IsFooterLine(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return true;
                var trimmed = s.Trim();
                if (trimmed.Length < 4 && Regex.IsMatch(trimmed, "^\\d{1,4}$")) return true;
                if (trimmed.StartsWith("K000", StringComparison.OrdinalIgnoreCase)) return true;
                if (trimmed.Equals("1994") || trimmed.Equals("001") || trimmed.Equals("5M") ) return true;
                // tokens like '1994 001 K00004147' are common footer sequences
                if (Regex.IsMatch(trimmed, @"^(?:\d{2,6}(?:\s+\d{1,6}){0,4}|K\d{3,})$")) return true;
                return false;
            }

            string CleanLine(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                // remove page-transfer markers which sometimes appear mid-details
                s = Regex.Replace(s, "\\bUebertrag auf Blatt\\b", "", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, "\\bUebertrag von Blatt\\b", "", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, "\\bÜbertrag auf Blatt\\b", "", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, "\\bÜbertrag von Blatt\\b", "", RegexOptions.IgnoreCase);
                s = s.Replace("\u2500", ""); // remove box-drawing dashes
                // remove explicit page markers that sometimes appear in the middle of details
                s = s.Replace("--- Page", "");
                if (s.Contains("www.")) return string.Empty;
                return s.Trim();
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i];
                if (string.IsNullOrEmpty(statement.Iban))
                {
                    var m = ibanRegex.Match(l);
                    if (!m.Success) m = compactIban.Match(l);
                    if (m.Success)
                        statement.Iban = Regex.Replace(m.Value, "\\s+", "");
                }

                var sd = startDateRegex.Match(l);
                if (sd.Success && DateTime.TryParseExact(sd.Groups[1].Value, "d.M.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var created))
                {
                    statement.Metadata.ParsedAt = DateTime.UtcNow;
                    statement.StartDate = created;
                }

                // opening/closing balances and their dates
                if (l.ToLower().Contains("alter kontostand") || l.ToLower().Contains("neuer kontostand"))
                {
                    var m = amountRegex.Match(l);
                    if (m.Success)
                    {
                        var num = m.Value.Replace(".", "").Replace(" ", "").Replace(",", ".");
                        if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                        {
                            // try to extract the date associated with this balance line (e.g. "Kontostand vom 31.10.2025")
                            var kontostandDate = new Regex(@"kontostand\s*(?:vom)\s*(\d{1,2}\.\d{1,2}\.\d{4})", RegexOptions.IgnoreCase);
                            var dmatch = kontostandDate.Match(l);
                            DateTime parsedDate;
                            if (l.ToLower().Contains("alter kontostand"))
                            {
                                statement.OpeningBalance = v;
                                if (dmatch.Success && DateTime.TryParseExact(dmatch.Groups[1].Value, "d.M.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out parsedDate))
                                {
                                    // use the opening-balance date as the statement start date (more authoritative)
                                    statement.StartDate = parsedDate;
                                }
                                else
                                {
                                    // fallback: pick any date-like token on the line
                                    var anyDate = Regex.Match(l, "\\d{1,2}\\.\\d{1,2}\\.\\d{4}");
                                    if (anyDate.Success && DateTime.TryParseExact(anyDate.Value, "d.M.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out parsedDate))
                                    {
                                        statement.StartDate = parsedDate;
                                    }
                                }
                            }
                            else
                            {
                                statement.ClosingBalance = v;
                                if (dmatch.Success && DateTime.TryParseExact(dmatch.Groups[1].Value, "d.M.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out parsedDate))
                                {
                                    // set end date for the statement
                                    statement.EndDate = parsedDate;
                                }
                            }
                        }
                    }
                }
            }

            // Parse bookings: stricter date header detection (DD.MM.) and follow-up lines
            var simpleDate = new Regex(@"^\d{1,2}\.\d{1,2}\.");
                var amountWithSH = new Regex(@"(?<!\w)(?<amount>\d{1,3}(?:[\.\s]\d{3})*,\d{2})\s*(?<sh>[SH])\b", RegexOptions.IgnoreCase);

            Booking? lastBooking = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // stop parsing bookings when we reach closing-balance / summary sections
                var low = line.ToLowerInvariant();
                if (low.Contains("neuer kontostand") || low.Contains("summe abschlussposten") || low.Contains("kontoabschluss") || low.Contains("anlage 1 zu auszug") || low.Contains("sehr geehrte"))
                    break;

                // skip transfer/summary/footer lines
                if (string.IsNullOrWhiteSpace(line) || line.IndexOf("Bitte beachten", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Clean candidate line early
                line = CleanLine(line);
                if (IsFooterLine(line)) continue;

                if (!simpleDate.IsMatch(line))
                {
                    // continuation line: append to previous booking's purpose if available, avoid duplicates
                    if (lastBooking != null && !string.IsNullOrWhiteSpace(line))
                    {
                        if (lastBooking.Purpose == null) lastBooking.Purpose = line;
                        else if (!lastBooking.Purpose.Contains(line)) lastBooking.Purpose += " " + line;
                    }
                    continue;
                }

                // If we reach here, line starts with a date -> start a new booking candidate
                var booking = new Booking { Currency = "EUR" };

                // parse booking date (first token like d.M.)
                var firstToken = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                if (DateTime.TryParseExact(firstToken, new[] { "d.M.", "dd.MM." }, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var dd))
                {
                    int year = (statement.StartDate != default) ? statement.StartDate.Year : DateTime.Now.Year;
                    booking.BookingDate = new DateTime(year, dd.Month, dd.Day);
                }

                // try to extract amount that is explicitly marked with S/H
                decimal? parsedAmount = null;
                // find the last standalone S or H marker on the line (word boundary)
                var shMarkerMatches = Regex.Matches(line, "\\b([SH])\\b", RegexOptions.IgnoreCase).Cast<Match>().ToArray();
                if (shMarkerMatches.Any())
                {
                    var lastSh = shMarkerMatches.Last();
                    // search for the right-most amount-like token before the S/H marker
                    var before = line.Substring(0, lastSh.Index);
                    var amountTokenPattern = new Regex("\\d{1,3}(?:\\.\\d{3})*,\\d{2}"); // prefer '.' thousands separator
                    var amtMatches = amountTokenPattern.Matches(before).Cast<Match>().ToArray();
                    if (amtMatches.Any())
                    {
                        var lastAmt = amtMatches.Last().Value;
                        var num = lastAmt.Replace(".", "").Replace(" ", "").Replace(",", ".");
                        if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                            parsedAmount = val * (lastSh.Groups[1].Value.Equals("S", StringComparison.OrdinalIgnoreCase) ? -1 : 1);
                    }
                }
                else
                {
                    // fallback: find last amount-like token on the line but only accept if it's near the line end
                    var amountMatches = amountRegex.Matches(line).Cast<Match>().ToArray();
                    if (amountMatches.Any())
                    {
                        var last = amountMatches.Last();
                        var tail = line.Substring(Math.Max(0, line.Length - 20));
                        if (tail.Contains(last.Value) || line.EndsWith(last.Value) || line.IndexOf(" S", StringComparison.Ordinal) >= 0 || line.IndexOf(" H", StringComparison.Ordinal) >= 0)
                        {
                            var num = last.Value.Replace(".", "").Replace(" ", "").Replace(",", ".");
                            if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                                parsedAmount = val * (line.Contains(" S") ? -1 : 1);
                        }
                    }
                }

                // collect following lines as details until next date or footer
                var details = new List<string>();
                int j = i + 1;
                int safety = 0;
                while (j < lines.Length && !simpleDate.IsMatch(lines[j]) && safety < 6)
                {
                    var candidate = CleanLine(lines[j]);
                    if (string.IsNullOrWhiteSpace(candidate)) break;
                    if (candidate.IndexOf("Bitte beachten", StringComparison.OrdinalIgnoreCase) >= 0) break;
                    if (candidate.StartsWith("Sehr geehrte", StringComparison.OrdinalIgnoreCase)) break;
                    if (candidate.Contains("Blatt")) break;
                    if (candidate.StartsWith("--- Page") || candidate.StartsWith("Page") || candidate.StartsWith("www.")) break;
                    if (candidate.Length > 250) break;
                    if (IsFooterLine(candidate)) break;

                    // avoid adding a detail that duplicates the last added fragment
                    var combined = string.Join(" ", details);
                    if (!string.IsNullOrWhiteSpace(candidate) && !combined.Contains(candidate))
                        details.Add(candidate);

                    j++; safety++;
                }

                if (details.Any())
                {
                    // set applicant to first meaningful detail, but avoid setting numeric/footer tokens
                    var first = details.First();
                    if (!IsFooterLine(first)) booking.Applicant = first;
                    var rest = details.Skip(1).Where(d => !IsFooterLine(d)).ToArray();
                    if (rest.Any()) booking.Purpose = string.Join(" ", rest);
                }

                // if amount looks suspiciously large, try to find a smaller amount in details
                if (parsedAmount.HasValue && Math.Abs(parsedAmount.Value) > 100000m)
                {
                    var detailAmountMatch = amountRegex.Matches(string.Join(" ", details)).Cast<Match>().LastOrDefault();
                    if (detailAmountMatch != null && detailAmountMatch.Success)
                    {
                        var num = detailAmountMatch.Value.Replace(".", "").Replace(" ", "").Replace(",", ".");
                        if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var dv))
                        {
                            parsedAmount = dv * (parsedAmount < 0 ? -1 : 1);
                        }
                    }
                }

                // only add booking if we found a reasonable amount or at least an applicant/purpose
                if (parsedAmount.HasValue && Math.Abs(parsedAmount.Value) < 100000000m)
                {
                    booking.Amount = parsedAmount.Value;
                    statement.Bookings.Add(booking);
                    lastBooking = booking;
                }
                else if (!string.IsNullOrWhiteSpace(booking.Applicant) || !string.IsNullOrWhiteSpace(booking.Purpose))
                {
                    // If no amount but we have applicant/purpose, append as continuation to last booking instead
                    if (lastBooking != null)
                    {
                        if (!string.IsNullOrWhiteSpace(booking.Applicant)) lastBooking.Purpose = (lastBooking.Purpose ?? "") + " " + booking.Applicant;
                        if (!string.IsNullOrWhiteSpace(booking.Purpose)) lastBooking.Purpose = (lastBooking.Purpose ?? "") + " " + booking.Purpose;
                    }
                }
                else
                {
                    // otherwise skip noisy line
                    continue;
                }
            }

            return statement;
        }
    }
}
