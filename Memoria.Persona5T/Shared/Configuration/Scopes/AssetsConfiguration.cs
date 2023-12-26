using System;

namespace Memoria.Persona5T.Configuration;

[ConfigScope("Assets")]
public abstract partial class AssetsConfiguration
{
    [ConfigEntry($"Overwrite the supported resources from the {nameof(ModsDirectory)}." +
                 $"$[Russian]: Позволяет загружать поддерживаемые ресурсы из каталога {nameof(ModsDirectory)}")]
    public virtual Boolean ModsEnabled => true;
    
    [ConfigEntry($"Enables tracking of changes in the {nameof(ModsDirectory)}, allowing to load some kind of updated files without restarting the game." +
                 $"$[Russian]: Включает отслеживание изменений в {nameof(ModsDirectory)}, позволяя загружать некоторые типы обновленных файлов без перезагрузки игры.")]
    public virtual Boolean WatchingEnabled => true;
    
    [ConfigEntry($"Export the supported resources to the {nameof(ExportDirectory)}." +
                 $"$[Russian]: Позволяет экспортировать поддерживаемые ресурсы в каталог {nameof(ExportDirectory)}")]
    public virtual Boolean ExportEnabled => false;

    [ConfigEntry($"Directory from which the supported resources will be loaded." +
                 $"$[Russian]: Директория, из которой будут загружаться поддерживаемые ресурсы.")]
    [ConfigConverter(nameof(ModsDirectoryConverter))]
    [ConfigDependency(nameof(ModsEnabled), "String.Empty")]
    public virtual String ModsDirectory => "%StreamingAssets%/Mods";
    
    [ConfigEntry($"Directory to which the supported resources will be exported." +
                 $"$[Russian]: Директория, в которую будут экспортированы поддерживаемые ресурсы.")]
    [ConfigConverter(nameof(ExportDirectoryConverter))]
    [ConfigDependency(nameof(ExportEnabled), "String.Empty")]
    public virtual String ExportDirectory => "%StreamingAssets%/Export";

    protected IAcceptableValue<String> ExportDirectoryConverter { get; } = new AcceptableDirectoryPath(nameof(ExportDirectory));
    // protected IAcceptableValue<String> ImportDirectoryConverter { get; } = new AcceptableDirectoryPath(nameof(ImportDirectory));
    protected IAcceptableValue<String> ModsDirectoryConverter { get; } = new AcceptableDirectoryPath(nameof(ModsDirectory), create: true);

    public abstract void CopyFrom(AssetsConfiguration configuration);

    public String GetExportDirectoryIfEnabled() => ExportEnabled ? ExportDirectory : String.Empty;
    // public String GetImportDirectoryIfEnabled() => ImportEnabled ? ImportDirectory : String.Empty;
}