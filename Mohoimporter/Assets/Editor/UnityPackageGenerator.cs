using System.Collections.Generic;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public class UnityPackageGenerator
{
    static UnityPackageGenerator()
    {
        UnityPackage();
    }


    [MenuItem("Window/MohoImporter/Generate UnityPackage")]
    public static void UnityPackage()
    {
        var assetPaths = new List<string>();

        var path = "Assets/mohoToolkit";
        CollectPathRecursive(path, assetPaths);

        AssetDatabase.ExportPackage(assetPaths.ToArray(), "MohoImporter.unitypackage", ExportPackageOptions.IncludeDependencies);
    }

    private static void CollectPathRecursive(string path, List<string> collectedPaths)
    {
        var filePaths = Directory.GetFiles(path);
        foreach (var filePath in filePaths)
        {
            collectedPaths.Add(filePath);
        }

        var modulePaths = Directory.GetDirectories(path);
        foreach (var folderPath in modulePaths)
        {
            CollectPathRecursive(folderPath, collectedPaths);
        }
    }
}