using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Memoria.Persona5T.Core;

public sealed class CsvContent
{
    private const Char Separator = ';';

    public readonly String[] ColumnNames;
    public readonly Dictionary<String, Int32> ColumnNameIndices;
    public readonly List<CsvRow> Rows;
    public readonly Dictionary<(String, Int32), Int32> RowNativeIndexToListIndex;
    public readonly HashSet<String> SheetNames = new();

    public CsvContent(String csvContent, StringComparer columnNameComparer)
    {
        if (csvContent is null) throw new ArgumentNullException(nameof(csvContent));
        if (csvContent == String.Empty) throw new ArgumentException(nameof(csvContent));

        using (var sr = new StringReader(csvContent))
        {
            if (!TryReadData(sr, out String[] parts))
                throw new FormatException($"The file doesn't contain the header with column names.");

            HashSet<String> processedColumns = new();
            if (parts.Length < 3 || parts[0] != "@Sheet" || parts[1] != "@Index")
                throw new FormatException($"The header must start with special columns @Sheet and @Index.");

            ColumnNames = parts.Skip(2).TakeWhile(p => !String.IsNullOrWhiteSpace(p)).ToArray();
            ColumnNameIndices = new(ColumnNames.Length, columnNameComparer);
            for (Int32 i = 0; i < ColumnNames.Length; i++)
            {
                String columnName = ColumnNames[i];
                if (!processedColumns.Add(columnName))
                    throw new FormatException($"The header contains several columns with the same name: [{columnName}]");

                ColumnNameIndices.Add(columnName, i);
            }

            Rows = new List<CsvRow>();
            RowNativeIndexToListIndex = new Dictionary<(String, Int32), Int32>();
            while (TryReadContent(sr, out CsvRow row))
                AddRow(row);
        }
    }

    public Int32 AddRow(CsvRow row)
    {
        Int32 listIndex = Rows.Count;
        RowNativeIndexToListIndex.Add((row.SheetName, row.Index), listIndex);
        Rows.Add(row);
        SheetNames.Add(row.SheetName);
        return listIndex;
    }

    public String CalculateColumnHash()
    {
        using Stream stream = new HashStream(ColumnNames);
        return CalculateHashString(stream);
    }

    public String CalculateDataHash()
    {
        using Stream stream = new HashStream(Rows);
        return CalculateHashString(stream);
    }

    private static String CalculateHashString(Stream stream)
    {
        using var md5 = MD5.Create();
        Byte[] hash = md5.ComputeHash(stream);
        Span<Byte> p1 = hash.AsSpan(0, 8);
        Span<Byte> p2 = hash.AsSpan(8);
        for (Int32 i = 0; i < 8; i++)
            p1[i] = (Byte)((p1[i] * 367) ^ p2[i]);

        String hashString = Convert.ToBase64String(p1);

        StringBuilder sb = new StringBuilder(capacity: 6);
        for (Int32 i = 0; i < 6; i++)
        {
            Char ch1 = hashString[i * 2 + 0];
            Char ch2 = hashString[i * 2 + 1];

            Int32 ch = ch1 + ch2;
            try
            {
                again:
                if (ch < '0')
                    ch += '0';
                if (ch <= '9')
                    continue;

                if (ch < 'A')
                    ch += 'A';
                if (ch <= 'Z')
                    continue;

                if (ch < 'a')
                    ch += 'a';
                if (ch <= 'z')
                {
                    ch -= 'a';
                    ch += 'A';
                    continue;
                }

                ch = '0' + (ch - 'z');
                goto again;
            }
            finally
            {
                sb.Append((Char)ch);
            }
        }

        return sb.ToString();
    }
    
    private static Boolean TryReadData(TextReader reader, out String[] parts)
    {
        while (true)
        {
            String line = reader.ReadLine();
            if (line is null)
            {
                parts = null;
                return false;
            }
            
            if (String.IsNullOrWhiteSpace(line))
                continue;

            Int32 commentIndex = line.IndexOf('#');
            if (commentIndex == 0)
            {
                if (line.StartsWith("# Enums"))
                {
                    parts = null;
                    return false;
                }
                continue;
            }

            if (commentIndex > 0)
                line = line.Substring(0, commentIndex);
            
            if (!String.IsNullOrWhiteSpace(line))
            {
                parts = line.Split(Separator).Select(p => p.Trim()).ToArray();
                return true;
            }
        }
    }
    
    private static Boolean TryReadContent(TextReader reader, out CsvRow row)
    {
        while (TryReadData(reader, out String[] parts))
        {
            row = CsvRow.Parse(parts);
            return true;
        }

        row = null;
        return false;
    }

    private sealed class HashStream : Stream
    {
        private readonly IEnumerator<String> _strings;
        private Memory<Byte> _buffer = Memory<Byte>.Empty;
        
        public HashStream(IReadOnlyList<String> columns)
        {
            _strings = columns.GetEnumerator();
        }

        public HashStream(IReadOnlyList<CsvRow> rows)
        {
            _strings = rows.SelectMany(EnumerateRow).GetEnumerator();
            return;

            static IEnumerable<String> EnumerateRow(CsvRow row)
            {
                yield return row.SheetName;
                yield return row.Index.ToString(CultureInfo.InvariantCulture);
                foreach (String value in row.Data)
                    yield return value;
            }
        }
        
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
                _strings.Dispose();
        }

        public override Boolean CanRead => true;

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            Int32 size = 0;

            Memory<Byte> target = buffer;
            
            do
            {
                if (_buffer.Length > 0)
                {
                    Int32 limit = Math.Min(_buffer.Length, count - size);
                    _buffer.Slice(0, limit).CopyTo(target);

                    size += limit;
                    _buffer = _buffer.Slice(limit);
                    target = target.Slice(limit);
                }

                if (_buffer.Length > 0)
                    return size;

            } while (TryReadNextString());

            return size;
        }

        private Boolean TryReadNextString()
        {
            if (_strings.MoveNext())
            {
                _buffer = Encoding.UTF8.GetBytes(_strings.Current);
                return true;
            }

            return false;
        }

        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => throw new NotImplementedException();

        public override Int64 Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override void Flush() => throw new NotImplementedException();
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(Int64 value) => throw new NotImplementedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotImplementedException();
    }
}