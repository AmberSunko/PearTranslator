namespace PearTranslator.Core.Assets;

public static class RuntimeAssetLocator
{
    public static string DefaultUserAssetRootDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PearTranslator",
            "Assets");

    public static string ResolvePath(string relativePath)
    {
        return ResolvePath(relativePath, DefaultUserAssetRootDirectory, AppContext.BaseDirectory);
    }

    public static string ResolvePath(
        string relativePath,
        string userAssetRootDirectory,
        string applicationRootDirectory)
    {
        var userPath = Combine(userAssetRootDirectory, relativePath);
        if (File.Exists(userPath))
        {
            return userPath;
        }

        return Combine(applicationRootDirectory, relativePath);
    }

    public static string Combine(string rootDirectory, string relativePath)
    {
        var path = rootDirectory;
        foreach (var part in relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
        {
            path = Path.Combine(path, part);
        }

        return path;
    }
}
