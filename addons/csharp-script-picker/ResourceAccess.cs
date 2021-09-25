using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using Path = System.IO.Path;

namespace CSharpScriptPicker
{
    public static class ResourceAccess
    {
        public static IEnumerable<string> GetFiles(string folderPath, bool recursive, bool skipHidden = true)
        {
            var dir = new Directory();
            if (!dir.DirExists(folderPath)) yield break;
            var result = dir.Open(folderPath);
            if (result != Error.Ok)
            {
#if DEBUG
                GD.PushError($"Error opening folder '{folderPath}' -> {result.ToString()}");
#endif
                yield break;
            }
            result = dir.ListDirBegin(true);
            if (result != Error.Ok)
            {
#if DEBUG
                GD.PushError($"Error beginning to list folder content '{folderPath}' -> {result.ToString()}");
#endif
                yield break;
            }
            string currentItem;
            while (!string.IsNullOrEmpty(currentItem = dir.GetNext()))
            {
                if (skipHidden && currentItem.StartsWith(".")) continue; //skip hidden files
                var subItem = Path.Combine(folderPath, currentItem);
                if (dir.CurrentIsDir())
                {
                    if (!recursive) continue;
                    foreach (var subFile in GetFiles(subItem, true))
                    {
                        yield return subFile;
                    }
                }
                else
                {
                    yield return subItem;
                }
            }
            dir.ListDirEnd();
        }

        public static IEnumerable<T> LoadAssets<T>(string folderPath, bool recursive, string filter = null) where T: Resource
        {
            foreach (var filePath in GetFiles(folderPath, recursive))
            {
                if (!string.IsNullOrEmpty(filter) && !Regex.IsMatch(filePath, filter)) continue;
                T obj = null;
                try
                {
                    obj = ResourceLoader.Load(filePath) as T;
                }
                catch (Exception ex)
                {
                    GD.PushError(ex.Message);
                }
                if (obj != null) yield return obj;
            }
        }
    }
}