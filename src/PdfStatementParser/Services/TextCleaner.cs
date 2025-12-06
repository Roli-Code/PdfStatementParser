using System;
using System.Text.RegularExpressions;

namespace PdfStatementParser.Services
{
    public interface ITextCleaner
    {
        string CleanLine(string s);
        bool IsFooterLine(string s);
    }

    public class TextCleaner : ITextCleaner
    {
        public string CleanLine(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s ?? string.Empty;
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

        public bool IsFooterLine(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;
            var trimmed = s.Trim();
            if (trimmed.Length < 4 && Regex.IsMatch(trimmed, "^\\d{1,4}$")) return true;
            if (trimmed.StartsWith("K000", StringComparison.OrdinalIgnoreCase)) return true;
            if (trimmed.Equals("1994") || trimmed.Equals("001") || trimmed.Equals("5M")) return true;
            if (Regex.IsMatch(trimmed, "^(?:\\d{2,6}(?:\\s+\\d{1,6}){0,4}|K\\d{3,})$")) return true;
            return false;
        }
    }
}
