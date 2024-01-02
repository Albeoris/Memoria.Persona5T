using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Game.Common;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Memoria.Persona5T.Configuration;
using Memoria.Persona5T.Core;
using Memoria.Persona5T.IL2CPP;
using UnityEngine;
using Object = System.Object;

namespace Memoria.Persona5T.HarmonyHooks;

public static class DatabaseManager<T>
{
    private static Int32 _lastHashCode;
    private static T _patched;
    private static DatabasePackAccessor _packAccessor;

    public static void OnCall(T __result, Int32 hashCode, String databaseFolder)
    {
        if (__result is null)
            throw new ArgumentNullException(nameof(__result));

        if (_lastHashCode == hashCode)
            return;
            
        _lastHashCode = hashCode;
        _patched = __result;
            
        ModComponent.Log.LogInfo($"[Mod] Applying mods to database pack {__result.GetHashCode():X8} ({databaseFolder}) of type {typeof(T)}.");
        _packAccessor ??= new DatabasePackAccessor();
        ExportDatabases(_patched, databaseFolder);
        ApplyMods(_patched, databaseFolder);
    }
        
    public static void Refresh(String databaseFolder)
    {
        if (_patched is null)
            return;

        ModComponent.Log.LogInfo("[Mod] Refreshing changed DB entries.");
        ApplyMods(_patched, databaseFolder);
    }

    private static void ExportDatabases(T target, String databaseFolder)
    {
        String exportDirectory = ModComponent.Config.Assets.GetExportDirectoryIfEnabled();
        if (exportDirectory != String.Empty)
            _packAccessor.WriteToFile(target, exportDirectory, databaseFolder);
    }

    private static void ApplyMods(T target, String databaseFolder)
    {
        IReadOnlyList<String> modPaths = ModComponent.ModFiles.FindAllStartedWith($"Databases/{databaseFolder}/");
        IEnumerable<IGrouping<String, String>> databasePaths = modPaths.GroupBy(m =>
        {
            String fullName = Path.GetFileNameWithoutExtension(m).ToLowerInvariant();
            Int32 index = fullName.IndexOf('_');
            return index > 0 ? fullName.Substring(0, index) : fullName;
        });
        
        foreach (IGrouping<String, String> paths in databasePaths)
        {
            String databaseName = paths.Key;
            try
            {
                DatabaseAccessor databaseAccessor = _packAccessor.FindAccessor(databaseName);
                if (databaseAccessor is null)
                {
                    ModComponent.Log.LogWarning($"[Mod] Cannot find appropriate database to merge CSV-database {databaseName}");
                    continue;
                }

                if (!TryModWordDatabase(target, databaseAccessor, paths))
                    TryApplyModFromCsv(target, databaseAccessor, paths);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to mod database with name [{databaseName}]", ex);
            }
        }
    }

    private static Boolean TryModWordDatabase(T target, DatabaseAccessor databaseAccessor, IGrouping<String, String> paths)
    {
        if (!databaseAccessor.Is<Entity_word_database>(target, out var wordDatabase))
            return false;
        
        Dictionary<Int32, String> words = new();
        foreach (String fullPath in paths)
        {
            using (FileStream input = File.OpenRead(fullPath))
            {
                Dictionary<String, String> dic = JsonSerializer.Deserialize<Dictionary<String, String>>(input);
                foreach (KeyValuePair<String,String> pair in dic)
                {
                    try
                    {
                        Int32 id = Int32.Parse(pair.Key.Substring("$Word_".Length), CultureInfo.InvariantCulture);
                        words[id] = pair.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Failed to parse word ID from key {pair.Key}. Expected: $Word_#####", ex);
                    }
                }
            }
        }

        Dictionary<Int32, String> newWords = new(words);
        foreach (var param in wordDatabase.param)
        {
            Int32 wordId = param.Id;
            if (words.TryGetValue(wordId, out var newWord))
            {
                param.Word = newWord;
                newWords.Remove(wordId);
            }
        }

        Int32 changed = words.Count - newWords.Count;
        Int32 added = newWords.Count;
        foreach (KeyValuePair<Int32,String> pair in newWords)
            wordDatabase.param.Add(new Entity_word_database.Param { Id = pair.Key, Word = pair.Value });

        ModComponent.Log.LogInfo($"[Mod] Word database has been modified. Changed: {changed}, New: {added}");
        return true;
    }

    private static Boolean TryApplyModFromCsv(T target, DatabaseAccessor databaseAccessor, IGrouping<String, String> paths)
    {
        StringWriter sw = new StringWriter();
        databaseAccessor.WriteToCsv(target, sw);

        String nativeCsvText = sw.ToString();
        CsvContent nativeCsv = new(nativeCsvText, StringComparer.InvariantCultureIgnoreCase);
        String columnHash = nativeCsv.CalculateColumnHash();
        String dataHash = nativeCsv.CalculateDataHash();

        CsvMerger merger = new CsvMerger(nativeCsv);

        foreach (String fullPath in paths)
        {
            String shortPath = ApplicationPathConverter.ReturnPlaceholders(fullPath);
            String fileName = Path.GetFileNameWithoutExtension(fullPath);
            if (fileName.EndsWith(dataHash) || fileName.EndsWith(columnHash))
            {
                ModComponent.Log.LogInfo($"[Mod] Merging data from {shortPath}");
                CsvContent content = new CsvContent(File.ReadAllText(fullPath), StringComparer.InvariantCultureIgnoreCase);
                merger.MergeFile(content);
            }
            else
            {
                ModComponent.Log.LogError($"[Mod] File {shortPath} has been skipped because it mods another version of database. Expected ending: _{columnHash}.csv or _{columnHash}_{dataHash}.csv");
            }
        }

        if (!merger.HasChanges)
            return false;

        databaseAccessor.ApplyFromCsv(target, merger);
        return true;
    }

    private sealed class DatabasePackAccessor
    {
        private readonly Dictionary<String, DatabaseAccessor> _databaseByName;
        
        public DatabasePackAccessor()
        {
            _databaseByName = BuildDatabaseWriters();
        }

        public void WriteToFile(T target, String exportDirectory, String databaseFolder)
        {
            String outputDirectory = $"{exportDirectory}/Databases/{databaseFolder}/";
            Directory.CreateDirectory(outputDirectory);

            foreach (KeyValuePair<String, DatabaseAccessor> databaseAccessor in _databaseByName)
            {
                try
                {
                    if (databaseAccessor.Value.Is<Entity_word_database>(target, out var wordDatabase))
                    {
                        WriteWordsToJson(outputDirectory, databaseAccessor, wordDatabase);
                        continue;
                    }

                    using (StringWriter sw = new())
                    {
                        databaseAccessor.Value.WriteToCsv(target, sw);
                        String csvText = sw.ToString();

                        CsvContent content = new CsvContent(csvText, StringComparer.InvariantCultureIgnoreCase);
                        String columnHash = content.CalculateColumnHash();
                        String dataHash = content.CalculateDataHash();

                        foreach (String file in Directory.GetFiles(outputDirectory, $"{databaseAccessor.Key}_*.csv"))
                            File.Delete(file);

                        using (StreamWriter output = File.CreateText($@"{outputDirectory}/{databaseAccessor.Key}_{columnHash}_{dataHash}.csv"))
                            output.WriteLine(csvText);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to apply mod to database {databaseAccessor.Key}", ex);
                }
            }
        }

        private static void WriteWordsToJson(String outputDirectory, KeyValuePair<String, DatabaseAccessor> databaseAccessor, Entity_word_database wordDatabase)
        {
            using (Stream output = File.Create($@"{outputDirectory}/{databaseAccessor.Key}.resjson"))
            using (Utf8JsonWriter json = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping}))
            {
                json.WriteStartObject();
                {
                    foreach (Entity_word_database.Param param in wordDatabase.param)
                        json.WriteString($"$Word_{param.Id.ToString(CultureInfo.InvariantCulture)}", param.Word);
                }
                json.WriteEndObject();
            }
        }

        private static Dictionary<String, DatabaseAccessor> BuildDatabaseWriters()
        {
            Dictionary<String, DatabaseAccessor> result = new(StringComparer.InvariantCultureIgnoreCase);

            Type packType = typeof(T);

            PropertyInfo[] databaseProperties = packType.GetPropertiesOfType<ScriptableObject>();
            foreach (PropertyInfo databaseProperty in databaseProperties)
            {
                String databaseName = databaseProperty.Name;
                if (databaseName.StartsWith("m_"))
                    databaseName = databaseName.Substring(startIndex: 2);
                
                if (DatabaseAccessor.TryCreate(databaseProperty, out DatabaseAccessor accessor))
                {
                    if (result.ContainsKey(databaseName))
                        throw new NotSupportedException($"There is two instance of database [{databaseName}]");
                    result.Add(databaseName, accessor);
                }
                else
                {
                    ModComponent.Log.LogWarning($"[Mod] Unknown database of type [{databaseName}]");
                }
            }
            
            return result;
        }

        public DatabaseAccessor FindAccessor(String databaseName)
        {
            return _databaseByName.GetValueOrDefault(databaseName);
        }
    }

    private sealed class PropertyAccessor
    {
        private delegate Object FromStringDelegate(String value);

        private delegate String ToStringDelegate(Object value);

        private readonly PropertyInfo _property;
        private readonly FromStringDelegate _fromString;
        private readonly ToStringDelegate _toString;

        private PropertyAccessor(PropertyInfo property, FromStringDelegate fromString, ToStringDelegate toString)
        {
            _property = property;
            _fromString = fromString;
            _toString = toString;
        }

        public Object GetRaw(Object entry)
        {
            Object casted = entry;
            try
            {
                return _property.GetValue(casted);
            }
            catch
            {
                ModComponent.Log.LogInfo(casted.GetType().FullName);
                throw;
            }
        }

        public String GetString(Object entry)
        {
            Object rawValue = GetRaw(entry);
            return _toString(rawValue); 
        }
        
        public void ApplyString(Object entry, String value)
        {
            Object casted = entry;
            try
            {
                Object data = _fromString(value);
                _property.SetValue(entry, data);
            }
            catch
            {
                ModComponent.Log.LogError($"Property: {_property.Name} ({_property.PropertyType.Name}), Value: {value}");
                throw;
            }
        }

        public static Boolean TryCreate(PropertyInfo info, out PropertyAccessor accessor)
        {
            Type propertyType = info.PropertyType;
            if (!IsSuitableProperty(info))
            {
                accessor = null;
                return false;
            }

            CreateConverters(propertyType, out FromStringDelegate fromString, out ToStringDelegate toString);

            accessor = new PropertyAccessor(info, fromString, toString);
            return true;
        }

        private static Boolean IsSuitableProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType == typeof(IntPtr))
                return false;

            return true;
        }

        private static void CreateConverters(Type propertyType, out FromStringDelegate fromString, out ToStringDelegate toString)
        {
            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.Boolean:
                    fromString = str => Boolean.Parse(str.Trim());
                    toString = val => ((Boolean)val).ToString();
                    return;
                case TypeCode.Char:
                    fromString = str => Char.Parse(str.Trim());
                    toString = val => ((Char)val).ToString();
                    return;
                case TypeCode.SByte:
                    fromString = str => SByte.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((SByte)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Byte:
                    fromString = str => Byte.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Byte)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Int16:
                    fromString = str => Int16.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Int16)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.UInt16:
                    fromString = str => UInt16.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((UInt16)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Int32:
                    fromString = str => Int32.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Int32)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.UInt32:
                    fromString = str => UInt32.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((UInt32)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Int64:
                    fromString = str => Int64.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Int64)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.UInt64:
                    fromString = str => UInt64.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((UInt64)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Single:
                    fromString = str => Single.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Single)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Double:
                    fromString = str => Double.Parse(str.Trim(), CultureInfo.InvariantCulture);
                    toString = val => ((Double)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.String:
                    fromString = str => str.Trim();
                    toString = val => ((String)val).ToString(CultureInfo.InvariantCulture);
                    return;
                case TypeCode.Object:
                    if (propertyType.GenericTypeArguments.Length == 1)
                    {
                        CreateConverters(propertyType.GenericTypeArguments[0], out var from, out var to);
                        fromString = str =>
                        {
                            var list = Activator.CreateInstance(propertyType);
                            IEnumerable<Object> items = str.Split(',').Select(v => from(v));

                            var listInstance = ((Il2CppSystem.Object)list).Cast<Il2CppSystem.Collections.IList>();
                            if (listInstance != null)
                            {
                                foreach (var value in items)
                                {
                                    if (value is Int32 integer)
                                        listInstance.Add(integer);
                                    else if (value is Single single)
                                        listInstance.Add(single);
                                    else if (value is String text)
                                        listInstance.Add(text);
                                    else if (value is Il2CppSystem.Object ilObj)
                                        listInstance.Add(ilObj);
                                    else if (value is null)
                                        listInstance.Add(null);
                                    else
                                        throw new NotSupportedException($"Cannot add to list object {value} of type {value.GetType()}");
                                }
                            }

                            return list;
                        };
                        toString = val =>
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (Object value in val.Enumerate())
                            {
                                sb.Append(to(value));
                                sb.Append(", ");
                            }

                            if (sb.Length > 0)
                                sb.Length -= 2;
                            return sb.ToString();
                        };
                        
                        return;
                    }

                    break;
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    break;
            }
            
            throw new NotSupportedException($"Property of type {propertyType.FullName} with type code: {Type.GetTypeCode(propertyType)} is not supported.");
        }
    }

    private abstract class DatabaseAccessor
    {
        public abstract Boolean Is<TDatabase>(T pack, out TDatabase database) where TDatabase : Il2CppObjectBase;
        public abstract void WriteToCsv(T pack, TextWriter csvOutput);
        public abstract void ApplyFromCsv(T pack, CsvMerger merger);

        public static Boolean TryCreate(PropertyInfo databaseProperty, out DatabaseAccessor databaseAccessor)
        {
            PropertyInfo entriesProperty = databaseProperty.PropertyType.FindSinglePropertyByName("param", StringComparison.InvariantCultureIgnoreCase);
            if (entriesProperty != null)
            {
                databaseAccessor = new DatabaseEntriesAccessor(databaseProperty, entriesProperty);
                return true;
            }

            PropertyInfo sheetsProperty = databaseProperty.PropertyType.FindSinglePropertyByName("sheets", StringComparison.InvariantCultureIgnoreCase);
            if (sheetsProperty != null)
            {
                entriesProperty = sheetsProperty.PropertyType.GenericTypeArguments.Single().FindSinglePropertyByName("list", StringComparison.InvariantCultureIgnoreCase);
                if (entriesProperty != null)
                {
                    databaseAccessor = new DatabaseSheetAccessor(databaseProperty, sheetsProperty, entriesProperty);
                    return true;
                }
            }

            databaseAccessor = null;
            return false;
        }

        protected static OrderedDictionary<String, PropertyAccessor> GetEntryProperties(Type entryType)
        {
            OrderedDictionary<String, PropertyAccessor> result = new(StringComparer.InvariantCultureIgnoreCase);

            foreach (PropertyInfo property in entryType.GetProperties().Where(p => p.DeclaringType == entryType))
            {
                if (PropertyAccessor.TryCreate(property, out PropertyAccessor accessor))
                    result.TryAdd(property.Name, accessor);
            }

            return result;
        }

        protected static void WriteHeaderToCsv(OrderedDictionary<String, PropertyAccessor> entryAccessor, TextWriter csvOutput)
        {
            csvOutput.Write("@Sheet;");
            csvOutput.Write("@Index;");

            foreach ((String key, PropertyAccessor value) pairs in entryAccessor)
            {
                csvOutput.Write(pairs.key);
                csvOutput.Write(';');
            }

            csvOutput.WriteLine();
        }

        protected static void WriteDataToCsv(String sheetName, Object entries, OrderedDictionary<String, PropertyAccessor> entryAccessor, TextWriter csvOutput)
        {
            CommonResource resource = CommonResource.GetInstance();
            StringBuilder sb = new();
            HashSet<Type> enums = new();
            Type databaseType = entries.GetType().GenericTypeArguments.Single();

            Int32 index = 1;
            foreach (Object entry in entries.Enumerate())
            {
                csvOutput.Write(sheetName);
                csvOutput.Write(';');
                csvOutput.Write(index++);
                csvOutput.Write(';');

                foreach ((String key, PropertyAccessor value) pairs in entryAccessor)
                {
                    String formattedValue = pairs.value.GetString(entry);
                    
                    if (DatabaseEnumMap.Instance.TryConvert(databaseType, pairs.key, formattedValue, out String convertedValue, out Type enumType))
                    {
                        formattedValue = convertedValue;
                        enums.Add(enumType);
                    }
                    else if (pairs.key.EndsWith("WordKey") && Int32.TryParse(formattedValue, out Int32 wordId) && wordId != 0)
                    {
                        String word = resource.GetWord(wordId).Replace('\n', ' ');
                        if (String.IsNullOrWhiteSpace(word))
                            continue;

                        if (sb.Length > 0)
                        {
                            sb.Append(", ");
                            sb.Append(pairs.key.Substring(0, pairs.key.Length - "WordKey".Length));
                            sb.Append(": ");
                        }

                        sb.Append(word);
                        formattedValue = $"$Word_{wordId}";
                    }
                    
                    if (formattedValue.StartsWith('"') || formattedValue.Contains(';') || formattedValue.Trim().Length != formattedValue.Length)
                    {
                        csvOutput.Write('"');
                        csvOutput.Write(formattedValue.Replace("\"", "\"\""));
                        csvOutput.Write('"');
                    }
                    else
                    {
                        csvOutput.Write(formattedValue);
                    }
                    csvOutput.Write(';');
                }

                if (sb.Length > 0)
                {
                    csvOutput.Write("# ");
                    csvOutput.Write(sb.ToString());
                    sb.Clear();
                }
                
                csvOutput.WriteLine();
            }

            if (enums.Count > 0)
            {
                csvOutput.WriteLine("#########################");
                csvOutput.WriteLine("# Enums");
                csvOutput.WriteLine("#########################");

                foreach (Type enumType in enums.OrderBy(e => e.Name))
                {
                    String[] names = Enum.GetNames(enumType);
                    Array values = Enum.GetValues(enumType);

                    csvOutput.WriteLine(enumType.Name);
                    csvOutput.WriteLine("{");
                    for (Int32 i = 0; i < names.Length; i++)
                        csvOutput.WriteLine($"    @{names[i]}_{(Int32)values.GetValue(i)}");
                    csvOutput.WriteLine("}");
                    csvOutput.WriteLine();
                }
            }
        }

        protected static void ApplyFromCsv(Object entries, Type entryType, OrderedDictionary<String, PropertyAccessor> entryAccessor, CsvContent csv, CsvChanges changes)
        {
            MethodInfo addMethod = null;
            MethodInfo removeAt = null;

            foreach (Int32 listIndex in changes.AddedIndices)
            {
                Object entry = Activator.CreateInstance(entryType);
                Apply(listIndex, entry);

                if (addMethod is null)
                    addMethod = AccessTools.Method(entries.GetType(), $"Add") ?? throw new Exception("Cannot find method Add");

                addMethod.Invoke(entries, new[] { entry });
            }

            Int32 index = 1;
            foreach (Object entry in entries.Enumerate())
            {
                Int32 id = index++;
                if (!csv.RowNativeIndexToListIndex.TryGetValue((changes.SheetName, id), out Int32 listIndex))
                    continue;

                if (!changes.ChangedIndices.Contains(listIndex))
                    continue;

                Apply(listIndex, entry);
            }

            foreach (Int32 rowIndex in changes.RemovedIndices.OrderByDescending(i => i))
            {
                if (removeAt is null)
                    removeAt = AccessTools.Method(entries.GetType(), "RemoveAt") ?? throw new Exception("Cannot find method RemoveAt");

                removeAt.Invoke(entries, new Object[] { rowIndex });
            }

            return;

            void Apply(Int32 listIndex, Object entry)
            {
                CsvRow row = csv.Rows[listIndex];

                foreach ((String key, PropertyAccessor value) pairs in entryAccessor)
                {
                    if (!csv.ColumnNameIndices.TryGetValue(pairs.key, out Int32 columnIndex))
                        continue;

                    pairs.value.ApplyString(entry, row.Data[columnIndex]);
                }
            }
        }
        
        protected static Boolean Is<TDatabase>(Object db, out TDatabase database) where TDatabase : Il2CppObjectBase
        {
            if (db is Il2CppSystem.Object il2cpp)
            {
                database = il2cpp.TryCast<TDatabase>();
                return database != null;
            }
            
            if (db is TDatabase casted)
            {
                database = casted;
                return true;
            }

            database = null;
            return false;
        }
    }

    private sealed class DatabaseSheetAccessor : DatabaseAccessor
    {
        private readonly PropertyInfo _databaseProperty;
        private readonly PropertyInfo _sheetsProperty;
        private readonly PropertyInfo _sheetNameProperty;
        private readonly PropertyInfo _entriesProperty;
        private readonly OrderedDictionary<String, PropertyAccessor> _entryAccessor;
        private readonly Type _entryType;

        public DatabaseSheetAccessor(PropertyInfo databaseProperty, PropertyInfo sheetsProperty, PropertyInfo entriesProperty)
        {
            _databaseProperty = databaseProperty;
            _sheetsProperty = sheetsProperty;
            _sheetNameProperty = _sheetsProperty.PropertyType.GenericTypeArguments.Single().FindSinglePropertyByName("name", StringComparison.InvariantCultureIgnoreCase);
            _entriesProperty = entriesProperty;
            _entryType = entriesProperty.PropertyType.GenericTypeArguments.Single();
            _entryAccessor = GetEntryProperties(_entryType);
        }
        
        public override Boolean Is<TDatabase>(T pack, out TDatabase database)
        {
            Object db = _databaseProperty.GetValue(pack);
            return Is(db, out database);
        }
        
        public override void WriteToCsv(T pack, TextWriter csvOutput)
        {
            Object database = _databaseProperty.GetValue(pack);
            Object sheets = _sheetsProperty.GetValue(database);

            WriteHeaderToCsv(_entryAccessor, csvOutput);
            foreach (Il2CppSystem.Object sheet in sheets.Enumerate())
            {
                String name = (String)_sheetNameProperty.GetValue(sheet);
                Object entries = _entriesProperty.GetValue(sheet);
                WriteDataToCsv(name, entries, _entryAccessor, csvOutput);
            }
        }

        public override void ApplyFromCsv(T pack, CsvMerger newData)
        {
            Object database = _databaseProperty.GetValue(pack);
            Object sheets = _sheetsProperty.GetValue(database);

            HashSet<String> existingSheets = new(StringComparer.InvariantCultureIgnoreCase);
            foreach (Il2CppSystem.Object sheet in sheets.Enumerate())
            {
                String name = (String)_sheetNameProperty.GetValue(sheet);
                existingSheets.Add(name);
                
                if (!newData.TryGetChanges(name, out CsvChanges changes))
                {
                    ModComponent.Log.LogError($"[Mod] Cannot find sheet with name [{name}].");
                    continue;
                }
                
                Object entries = _entriesProperty.GetValue(database);
                ApplyFromCsv(entries, _entryType, _entryAccessor, newData.Merged, changes);
            }

            foreach (String sheetName in newData.Merged.SheetNames)
            {
                if (existingSheets.Contains(sheetName))
                    continue;

                throw new NotSupportedException($"[Mod] Adding of new sheet is not yet supported. New sheet: {sheetName}");
            }
        }
    }
    
    private sealed class DatabaseEntriesAccessor : DatabaseAccessor
    {
        private readonly PropertyInfo _databaseProperty;
        private readonly PropertyInfo _entriesProperty;
        private readonly OrderedDictionary<String, PropertyAccessor> _entryAccessor;
        private readonly Type _entryType;

        public DatabaseEntriesAccessor(PropertyInfo databaseProperty, PropertyInfo entriesProperty)
        {
            _databaseProperty = databaseProperty;
            _entriesProperty = entriesProperty;
            _entryType = entriesProperty.PropertyType.GenericTypeArguments.Single();
            _entryAccessor = GetEntryProperties(_entryType);
        }

        public override Boolean Is<TDatabase>(T pack, out TDatabase database)
        {
            Object db = _databaseProperty.GetValue(pack);
            return Is(db, out database);
        }

        public override void WriteToCsv(T pack, TextWriter csvOutput)
        {
            Object database = _databaseProperty.GetValue(pack);
            Object entries = _entriesProperty.GetValue(database);
            WriteHeaderToCsv(_entryAccessor, csvOutput);
            WriteDataToCsv("Main", entries, _entryAccessor, csvOutput);
        }

        public override void ApplyFromCsv(T pack, CsvMerger csv)
        {
            CsvContent content = csv.Merged;
            if (content.SheetNames.Count != 1 || !csv.TryGetChanges("Main", out CsvChanges changes))
                throw new NotSupportedException($"[Mod] Unexpected sheets [{String.Join(", ", content.SheetNames)}]. Expected: [Main]");

            Object database = _databaseProperty.GetValue(pack);
            Object entries = _entriesProperty.GetValue(database);
            ApplyFromCsv(entries, _entryType, _entryAccessor, csv.Merged, changes);
        }
    }
}