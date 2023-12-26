using System;
using System.Collections.Generic;

namespace Memoria.Persona5T.Core;

public sealed class CsvChanges
{
    public String SheetName { get; }
    
    private readonly HashSet<Int32> _removedRowIndices = new();
    private readonly HashSet<Int32> _changedRowIndices = new();
    private readonly HashSet<Int32> _addedRowIndices = new();

    public CsvChanges(String sheetName)
    {
        SheetName = sheetName;
    }

    public IReadOnlySet<Int32> RemovedIndices => _removedRowIndices;
    public IReadOnlySet<Int32> ChangedIndices => _changedRowIndices;
    public IReadOnlySet<Int32> AddedIndices => _addedRowIndices;
    public Boolean HasChanges => ChangedIndices.Count > 0 || AddedIndices.Count > 0 || RemovedIndices.Count > 0;

    public Boolean MarkAsRemoved(Int32 index) => _removedRowIndices.Add(index);
    public Boolean MarkAsChanged(Int32 index) => _changedRowIndices.Add(index);
    public Boolean MarkAsAdded(Int32 index) => _addedRowIndices.Add(index);
}