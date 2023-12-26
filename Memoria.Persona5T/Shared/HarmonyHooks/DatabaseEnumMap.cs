using System;
using System.Collections;
using System.Collections.Generic;

namespace Memoria.Persona5T.HarmonyHooks;

public sealed class DatabaseEnumMap : IEnumerable
{
    public static DatabaseEnumMap Instance { get; } = new()
    {
        {typeof(Entity_award_database.Param), nameof(Entity_award_database.Param.AwardType), typeof(Battle.BattleDefine.AwardType) }
    };

    public Boolean TryConvert(Type entryType, String columnName, String value, out String result, out Type enumType)
    {
        if (!_dic.TryGetValue((entryType, columnName), out enumType))
        {
            result = default;
            return false;
        }

        Int32 integer = Int32.Parse(value);
        Object converted = Enum.ToObject(enumType, integer);
        result = $"@{converted}_{integer}";
        return true;
    }

    private readonly Dictionary<(Type databaseType, String columnName), Type> _dic = new();

    private void Add(Type databaseName, String columnName, Type enumType)
    {
        _dic.Add((databaseName, columnName), enumType);
    }

    public IEnumerator GetEnumerator()
    {
        return _dic.GetEnumerator();
    }
}