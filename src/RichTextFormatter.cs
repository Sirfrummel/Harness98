using System;
using System.Text;

namespace Harness98
{
    public static class RichTextFormatter
    {
        public static string FormatAssistantText(string text)
        {
            if (text == null) return "";
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            StringBuilder rtf = new StringBuilder();
            int cursor = 0;
            while (cursor < normalized.Length)
            {
                int openingStart;
                int contentStart;
                int contentEnd;
                int closingEnd;
                if (!FindCompleteFence(normalized, cursor, out openingStart,
                    out contentStart, out contentEnd, out closingEnd))
                {
                    rtf.Append(Encode(normalized.Substring(cursor)));
                    break;
                }

                string normal = normalized.Substring(cursor,
                    openingStart - cursor);
                rtf.Append(Encode(normal));
                if (normal.Length > 0 && !EndsWithNewline(normal))
                    rtf.Append("\\line ");

                string code = normalized.Substring(contentStart,
                    contentEnd - contentStart);
                if (EndsWithNewline(code))
                    code = code.Substring(0, code.Length - 1);
                AppendCode(rtf, code);
                cursor = closingEnd;
                if (cursor < normalized.Length) rtf.Append("\\line ");
            }
            return rtf.ToString();
        }

        public static string Encode(string text)
        {
            StringBuilder encoded = new StringBuilder();
            if (text == null) return "";
            for (int i = 0; i < text.Length; i++)
            {
                char value = text[i];
                if (value == '\r') continue;
                if (value == '\n') encoded.Append("\\line ");
                else if (value == '\\' || value == '{' || value == '}')
                    encoded.Append('\\').Append(value);
                else if (value > 127)
                    encoded.Append("\\u").Append(
                        ((short)value).ToString()).Append('?');
                else encoded.Append(value);
            }
            return encoded.ToString();
        }

        private static void AppendCode(StringBuilder rtf, string code)
        {
            rtf.Append("\\f1\\highlight6 ");
            string[] lines = code.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) rtf.Append("\\line ");
                rtf.Append("\\tab ");
                rtf.Append(Encode(lines[i]));
            }
            rtf.Append("\\highlight0\\f0 ");
        }

        private static bool FindCompleteFence(string text, int searchStart,
            out int openingStart, out int contentStart, out int contentEnd,
            out int closingEnd)
        {
            openingStart = contentStart = contentEnd = closingEnd = 0;
            int lineStart = searchStart;
            if (lineStart > 0 && text[lineStart - 1] != '\n')
            {
                int next = text.IndexOf('\n', lineStart);
                if (next < 0) return false;
                lineStart = next + 1;
            }

            while (lineStart < text.Length)
            {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = text.Length;
                char fence;
                int length;
                if (ReadOpeningFence(text, lineStart, lineEnd,
                    out fence, out length))
                {
                    int candidate = lineEnd < text.Length ? lineEnd + 1 : lineEnd;
                    while (candidate < text.Length)
                    {
                        int candidateEnd = text.IndexOf('\n', candidate);
                        if (candidateEnd < 0) candidateEnd = text.Length;
                        if (IsClosingFence(text, candidate, candidateEnd,
                            fence, length))
                        {
                            openingStart = lineStart;
                            contentStart = lineEnd < text.Length ? lineEnd + 1 :
                                lineEnd;
                            contentEnd = candidate;
                            closingEnd = candidateEnd < text.Length ?
                                candidateEnd + 1 : candidateEnd;
                            return true;
                        }
                        if (candidateEnd == text.Length) break;
                        candidate = candidateEnd + 1;
                    }
                    return false;
                }
                if (lineEnd == text.Length) break;
                lineStart = lineEnd + 1;
            }
            return false;
        }

        private static bool ReadOpeningFence(string text, int start, int end,
            out char fence, out int length)
        {
            fence = '\0';
            length = 0;
            int position = start;
            while (position < end && text[position] == ' ' &&
                position - start < 3) position++;
            if (position >= end || (text[position] != '`' &&
                text[position] != '~')) return false;
            fence = text[position];
            while (position + length < end &&
                text[position + length] == fence) length++;
            return length >= 3;
        }

        private static bool IsClosingFence(string text, int start, int end,
            char fence, int minimumLength)
        {
            while (start < end && text[start] == ' ') start++;
            int count = 0;
            while (start < end && text[start] == fence)
            {
                count++;
                start++;
            }
            while (start < end && text[start] == ' ') start++;
            return count >= minimumLength && start == end;
        }

        private static bool EndsWithNewline(string text)
        {
            return text.Length > 0 && text[text.Length - 1] == '\n';
        }
    }
}
