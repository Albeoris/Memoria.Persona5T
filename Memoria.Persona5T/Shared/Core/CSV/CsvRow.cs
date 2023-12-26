using System;
using System.Globalization;
using System.Linq;
using Memoria.Persona5T.IL2CPP;

namespace Memoria.Persona5T.Core;

public sealed class CsvRow
{
    public String SheetName { get; }
    public Int32 Index { get; }
    public String[] Data { get; }

    public CsvRow(String sheetName, Int32 index, String[] data)
    {
        SheetName = sheetName;
        Index = index;
        Data = data;
    }

    public static CsvRow Parse(String[] parts)
    {
        if (parts.Length < 2)
            throw new FormatException($"The each row must start with @Sheet and @Index.");

        String sheet = parts[0];
        Int32 index = Int32.Parse(parts[1], CultureInfo.InvariantCulture);
        parts = parts.Skip(2).ToArray();
        for (Int32 i = 0; i < parts.Length; i++)
        {
            String value = parts[i];
            if (value.Length == 0)
                continue;
            
            if (value[0] == '$' || value[0] == '@')
            {
                Int32 underscoreIndex = value.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    String ending = value.Substring(underscoreIndex + 1);
                    if (Int32.TryParse(ending, out _))
                    {
                        value = ending;
                        parts[i] = value;
                    }
                }
            }
            
            if (value[0] != '"')
                continue;

            if (value[value.Length-1] != '"')
                throw new FormatException($"Invalid value format: [{value}] of row [{sheet}, {index}]. Value must end with [\"].");

            String withoutLeadingQuotes = value.Substring(1, value.Length - 2);
            String withoutDoubleQuotes = withoutLeadingQuotes.Replace("\"\"", "\"");

            if (withoutDoubleQuotes.Length != withoutLeadingQuotes.Length)
            {
                Int32 quoteCount = withoutLeadingQuotes.Count(c => c == '"');
                if (quoteCount % 2 != 0 || withoutLeadingQuotes.Length - withoutDoubleQuotes.Length != quoteCount / 2)
                    throw new FormatException($"Invalid value format: [{value}] of row [{sheet}, {index}]. Each quote inside a quoted value must be escaped with another one: [\"\"].");
            }
            
            parts[i] = withoutDoubleQuotes;
        }
        return new CsvRow(sheet, index, parts);
    }

    public override String ToString()
    {
        return $"{SheetName}; {Index}; {String.Join(';', Data)}";
    }
}