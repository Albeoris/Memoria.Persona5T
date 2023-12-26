using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Memoria.Persona5T.IL2CPP;

namespace Memoria.Persona5T.Core;

public sealed class CsvMerger
{
    private readonly CsvContent _csvContent;
    private readonly Dictionary<String, CsvChanges> _changesPerSheet = new();

    public CsvMerger(CsvContent nativeContext)
    {
        if (nativeContext is null)
            throw new ArgumentNullException(nameof(nativeContext));
        
        _csvContent = nativeContext;
    }

    public CsvContent Merged => _csvContent;
    public Boolean HasChanges => _changesPerSheet.Any(p => p.Value.HasChanges);
    public Boolean TryGetChanges(String sheetName, out CsvChanges changes) => _changesPerSheet.TryGetValue(sheetName, out changes);

    public void MergeFile(CsvContent content)
    {
        StringBuilder sb = new();

        List<String> newColumns = new();
        Dictionary<Int32, Int32> currentColumnIndexToNative = new();
        for (var index = 0; index < content.ColumnNames.Length; index++)
        {
            String columnName = content.ColumnNames[index];
            if (_csvContent.ColumnNameIndices.TryGetValue(columnName, out Int32 nativeIndex))
                currentColumnIndexToNative.Add(index, nativeIndex);
            else
                newColumns.Add(columnName);
        }

        if (newColumns.Count > 0)
            ModComponent.Log.LogWarning($"These columns doesn't exist in original database and will not affect anything: {String.Join(", ", newColumns)}");

        foreach (CsvRow row in content.Rows)
        {
            if (!_changesPerSheet.TryGetValue(row.SheetName, out CsvChanges changes))
            {
                changes = new CsvChanges(row.SheetName);
                _changesPerSheet.Add(row.SheetName, changes);
            }
            
            Boolean toRemove = false;
            Int32 id = row.Index;
            if (id < 0)
            {
                toRemove = true;
                id *= -1;
            }

            if (!_csvContent.RowNativeIndexToListIndex.TryGetValue((row.SheetName, id), out var listIndex))
            {
                if (toRemove)
                {
                    ModComponent.Log.LogWarning($"[Mod] Cannot find row with id [{id}] to remove it.");
                    continue;
                }

                if (row.Data.Length != _csvContent.ColumnNames.Length)
                {
                    throw new FormatException($"Cannot add row with id [{id}]. Expected {_csvContent.ColumnNames.Length} columns [{String.Join(";", _csvContent.ColumnNames)}], but there is {row.Data.Length} [{String.Join(";", row.Data)}].");
                }

                String[] normalizedData = new String[_csvContent.ColumnNames.Length];
                for (Int32 i = 0; i < normalizedData.Length; i++)
                {
                    Int32 columnIndex = currentColumnIndexToNative[i];
                    normalizedData[columnIndex] = row.Data[i];
                }

                CsvRow newRow = new CsvRow(row.SheetName, row.Index, normalizedData);
                listIndex = _csvContent.AddRow(newRow);
                changes.MarkAsAdded(listIndex);
                ModComponent.Log.LogInfo($"[Mod] Added new row: {newRow}.");
                continue;
            }

            if (toRemove)
            {
                if (changes.MarkAsRemoved(listIndex))
                {
                    CsvRow oldRow = _csvContent.Rows[listIndex];
                    ModComponent.Log.LogInfo($"[Mod] Removed existing row [{id}]. {oldRow}.");
                }

                continue;
            }

            CsvRow modifyingRow = _csvContent.Rows[listIndex];

            for (Int32 i = 0; i < row.Data.Length; i++)
            {
                if (!currentColumnIndexToNative.TryGetValue(i, out Int32 columnIndex))
                    continue;

                String columnName = _csvContent.ColumnNames[columnIndex];
                String oldValue = modifyingRow.Data[columnIndex];
                String newValue = row.Data[i];
                if (oldValue != newValue)
                {
                    modifyingRow.Data[columnIndex] = newValue;
                    sb.Append($" {columnName} ({oldValue} -> {newValue})");
                }
            }

            if (sb.Length > 0)
            {
                changes.MarkAsChanged(listIndex);
                ModComponent.Log.LogInfo($"[Mod] Changed row [{id}]. {sb.ToString()}");
                sb.Clear();
            }
        }
    }
}
