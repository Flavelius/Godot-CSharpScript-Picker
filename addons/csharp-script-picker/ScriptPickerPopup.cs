using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;
using File = System.IO.File;
using Object = Godot.Object;
using Path = System.IO.Path;
using Resource = Godot.Resource;

namespace CSharpScriptPicker
{
    [Tool]
    public class ScriptPickerPopup : Popup
    {
        [Export]
        NodePath containerPath = new NodePath("Content");
        [Export]
        NodePath searchFieldPath = new NodePath("SearchField");
        ScriptPickerResources resources;
        ScriptMenuTreeNode menuTreeNode;
        readonly ScriptMenuTreeNode searchResultNode = new ScriptMenuTreeNode("Search Results", ScriptMenuTreeNode.EntryTypes.SubSection);

        ScriptMenuTreeNode displayedNode;

        Control container;
        LineEdit searchField;
        Object target;

        static readonly char[] namespaceSplitChar = { '.' };
        static readonly char[] folderSplitChar = { '/' };

        const string MetaIdentifier = "script-tree-guid";

        public void Load(ScriptPickerResources resources, Object target)
        {
            this.resources = resources;
            this.target = target;
            container = GetNode<Container>(containerPath);
            searchField = GetNode<LineEdit>(searchFieldPath);
            searchField.RightIcon = GetIcon("Search", "EditorIcons");
            searchField.Connect("text_changed", this, nameof(OnSearchTextChanged));
            searchField.CallDeferred("release_focus");
            menuTreeNode = ScriptMenuTreeNode.CreateTree(target is Node ? typeof(Node) : target is Resource ? typeof(Resource) : throw new Exception("Invalid target"),
                ScriptPickerPlugin.ShouldSortByNamespace, ScriptPickerPlugin.IgnoredTypes, ScriptPickerPlugin.IgnoredPaths);
            SetDisplayedNode(menuTreeNode);
        }

        bool HasSearchInput() => !string.IsNullOrEmpty(searchField.Text);

        void OnSearchTextChanged(string newValue)
        {
            if (HasSearchInput())
            {
                searchResultNode.Entries.Clear();
                TraverseNodes(menuTreeNode, newValue, searchResultNode.Entries);
                SetDisplayedNode(searchResultNode);
                void TraverseNodes(ScriptMenuTreeNode node, string searchString, List<ScriptMenuTreeNode> appendChildren)
                {
                    foreach (var n in node.Entries)
                    {
                        if (n.Type == ScriptMenuTreeNode.EntryTypes.SubSection)
                        {
                            TraverseNodes(n, searchString, appendChildren);
                        }
                        else
                        {
                            if (n.Path.MatchesWildcardedExpression(searchString)) appendChildren.Add(n);
                        }
                    }
                }
            }
            else
            {
                SetDisplayedNode(menuTreeNode);
            }
        }

        void SetDisplayedNode(ScriptMenuTreeNode node)
        {
            foreach (Node child in container.GetChildren())
            {
                child.RemoveMeta(MetaIdentifier);
                child.QueueFree();
            }
            var boldFont = GetFont("bold", "EditorFonts");
            if (node.Parent != null)
            {
                AddButtonEntryFor(resources.BackButton, node.Parent, nameOverride: node.Path, fontOverride: boldFont);
            }
            else
            {
                AddButtonEntryFor(resources.BackButton, node, nameOverride: node.Path, hideDirectionLabel: true, fontOverride: boldFont);
            }
            if (node.Entries.Count > 0)
            {
                var namespaceEntries = node.Entries.Where(e => e.Type == ScriptMenuTreeNode.EntryTypes.SubSection).ToArray();
                var scriptEntries = node.Entries.Where(e => e.Type == ScriptMenuTreeNode.EntryTypes.ScriptPath || e.Type == ScriptMenuTreeNode.EntryTypes.TypeName);
                if (namespaceEntries.Length > 0)
                {
                    for (int i = 0; i < namespaceEntries.Length; i++)
                    {
                        AddButtonEntryFor(resources.ContentButton, namespaceEntries[i]);
                    }
                    container.AddChild(new HSeparator());
                }
                foreach (var scriptEntry in scriptEntries)
                {
                    AddButtonEntryFor(resources.ContentButton, scriptEntry, GetIcon("CSharpScript", "EditorIcons"), hideDirectionLabel: true);
                }
            }
            displayedNode = node;
            void AddButtonEntryFor(PackedScene prefab, ScriptMenuTreeNode buttonNode, Texture icon = null, string nameOverride = null, bool hideDirectionLabel = false, Font fontOverride = null)
            {
                var btn = prefab.Instance<Button>();
                btn.Name = Path.GetFileNameWithoutExtension(buttonNode.Path);
                btn.SetMeta(MetaIdentifier, buttonNode.Guid.ToString());
                if (fontOverride != null) btn.AddFontOverride("font", fontOverride);
                if (nameOverride == null)
                {
                    switch (buttonNode.Type)
                    {
                        case ScriptMenuTreeNode.EntryTypes.SubSection:
                            btn.Text = buttonNode.Path;
                            break;
                        case ScriptMenuTreeNode.EntryTypes.TypeName:
                            btn.Text = buttonNode.Path.Split(namespaceSplitChar, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                            break;
                        default:
                            btn.Text = Path.GetFileNameWithoutExtension(buttonNode.Path);
                            break;
                    }
                }
                else
                {
                    btn.Text = nameOverride;
                }
                if (icon != null) btn.Icon = icon;
                btn.Connect("pressed", this, nameof(OnContentButtonPressed), new Array(btn), (uint)ConnectFlags.Oneshot);
                if (hideDirectionLabel)
                {
                    var childLabel = btn.GetChildOrNull<Label>(0);
                    if (childLabel != null) childLabel.QueueFree();
                }
                container.AddChild(btn);
            }
        }

        void OnContentButtonPressed(Button sender)
        {
            var guid = new Guid((string)sender.GetMeta(MetaIdentifier));
            if (displayedNode.Guid.Equals(guid))
            {
                SetDisplayedNode(displayedNode);
            }
            else if (displayedNode.Parent != null && displayedNode.Parent.Guid.Equals(guid))
            {
                SetDisplayedNode(displayedNode.Parent);
            }
            else
            {
                foreach (var n in displayedNode.Entries)
                {
                    if (!n.Guid.Equals(guid)) continue;
                    if (n.Type == ScriptMenuTreeNode.EntryTypes.ScriptPath || n.Type == ScriptMenuTreeNode.EntryTypes.TypeName)
                    {
                        if (n.Script != null)
                        {
                            if (ScriptPickerPlugin.EnableLogging) GD.Print($"Assign Script {n.Path} to {target}");
                            target.SetScript(n.Script);
                        }
                        else
                        {
                            if (ScriptPickerPlugin.EnableLogging) GD.PushError($"Script to assign missing: {n.Path}");
                        }
                        Hide();
                        QueueFree();
                    }
                    else
                    {
                        SetDisplayedNode(n);
                    }
                    break;
                }
            }
        }

        class ScriptMenuTreeNode
        {
            public Guid Guid { get; }
            public ScriptMenuTreeNode Parent { get; set; }
            public string Path { get; }
            public CSharpScript Script { get; set; }
            public EntryTypes Type { get; }

            public enum EntryTypes
            {
                SubSection = 0,
                ScriptPath = 1,
                TypeName = 3
            }

            public ScriptMenuTreeNode(string path, EntryTypes type)
            {
                Path = path;
                Type = type;
                Guid = Guid.NewGuid();
            }

            public ScriptMenuTreeNode AddChild(string path, EntryTypes type)
            {
                var entry = new ScriptMenuTreeNode(path, type) { Parent = this };
                Entries.Add(entry);
                return entry;
            }

            public readonly List<ScriptMenuTreeNode> Entries = new List<ScriptMenuTreeNode>();

            ScriptMenuTreeNode FindChild(string path)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (string.Equals(Entries[i].Path, path, StringComparison.Ordinal)) return Entries[i];
                }
                return null;
            }

            public static ScriptMenuTreeNode CreateTree(Type inheritanceTypeMatch, bool sortByNamespace, string[] typeFilterExpressions, string[] pathFilterExpressions)
            {
                var root = new ScriptMenuTreeNode("Root", EntryTypes.SubSection);
                if (sortByNamespace)
                {
                    TryCreateNamespaceBasedTree(root, inheritanceTypeMatch, typeFilterExpressions, pathFilterExpressions);
                }
                else
                {
                    CreateFolderBasedTree(root, inheritanceTypeMatch, typeFilterExpressions, pathFilterExpressions);
                }
                return root;
            }

            static void CreateFolderBasedTree(ScriptMenuTreeNode rootNode, Type inheritanceTypMatch, string[] typeFilterExpressions, string[] pathFilterExpressions)
            {
                var godotAssembly = Assembly.GetAssembly(typeof(Object));
                var scripts = ResourceAccess.LoadAssets<CSharpScript>("res://", true, @".*\.cs$").ToList();
                for (int i = scripts.Count; i-- > 0;)
                {
                    if (FilterOutEntry(scripts[i], System.IO.Path.GetFileNameWithoutExtension(scripts[i].ResourcePath), godotAssembly, inheritanceTypMatch, typeFilterExpressions,
                        pathFilterExpressions))
                    {
                        scripts.RemoveAt(i);
                    }
                }
                for (int scriptIndex = 0; scriptIndex < scripts.Count; scriptIndex++)
                {
                    var currentMenuTreeNode = rootNode;
                    var pathSplit = scripts[scriptIndex].ResourcePath.Split(folderSplitChar, StringSplitOptions.RemoveEmptyEntries);
                    for (int splitIndex = 1; splitIndex < pathSplit.Length; splitIndex++)
                    {
                        if (splitIndex == pathSplit.Length - 1)
                        {
                            var scriptNode = currentMenuTreeNode.AddChild(scripts[scriptIndex].ResourcePath, EntryTypes.ScriptPath);
                            scriptNode.Script = scripts[scriptIndex];
                        }
                        else
                        {
                            var nextNode = currentMenuTreeNode.FindChild(pathSplit[splitIndex]);
                            currentMenuTreeNode = nextNode ?? currentMenuTreeNode.AddChild(pathSplit[splitIndex], EntryTypes.SubSection);
                        }
                    }
                }
            }

            static void TryCreateNamespaceBasedTree(ScriptMenuTreeNode rootNode, Type inheritanceTypeMatch, string[] typeFilterExpressions, string[] pathFilterExpressions)
            {
                var godotAssembly = Assembly.GetAssembly(typeof(Object));
                var projectAssembly = Assembly.GetAssembly(typeof(ScriptPickerPopup));
                var typeToScriptMatches = new List<(Type, CSharpScript)>(16);
                var metadataPath = ProjectSettings.GlobalizePath("res://.mono/metadata/scripts_metadata.editor_player");

                bool TryLoadMetadata(string path, out Dictionary data)
                {
                    try
                    {
                        var fileData = File.ReadAllText(metadataPath);
                        var metadataparseResult = JSON.Parse(fileData);
                        if (metadataparseResult.Error != Error.Ok)
                        {
                            data = null;
                            return false;
                        }
                        data = metadataparseResult.Result as Dictionary;
                        return data != null;
                    }
                    catch (Exception)
                    {
                        data = null;
                        return false;
                    }
                }
                if (TryLoadMetadata(metadataPath, out var metaDataDict)) //try to use godot's metadata to resolve types, will contain outdated information (scripts that no longer exist) - why though?
                {
                    foreach (DictionaryEntry dictionaryEntry in metaDataDict)
                    {
                        var scriptPath = (string)dictionaryEntry.Key;
                        var classInfo = (Dictionary)((Dictionary)dictionaryEntry.Value)["class"];
                        var namespaceName = (string)classInfo["namespace"];
                        var className = (string)classInfo["class_name"];
                        var fullTypeName = string.IsNullOrWhiteSpace(namespaceName) ? className : namespaceName + "." + className;
                        var type = projectAssembly.GetType(fullTypeName);
                        if (!ResourceLoader.Exists(scriptPath))
                        {
                            if (ScriptPickerPlugin.EnableLogging) GD.Print($"Script ({scriptPath}) listed in metadata does not exist as file-> will not be listed in script picker");
                            continue;
                        }
                        if (type == null)
                        {
                            if (ScriptPickerPlugin.EnableLogging) GD.PushWarning($"Error locating type of script in assembly ({className}) -> will not be listed in script picker");
                            continue;
                        }
                        var script = ResourceLoader.Load<CSharpScript>(scriptPath);
                        if (script == null)
                        {
                            if (ScriptPickerPlugin.EnableLogging) GD.PushWarning($"Error loading script ({scriptPath}) -> will not be listed in script picker");
                            continue;
                        }
                        if (!FilterOutEntry(script, fullTypeName, godotAssembly, inheritanceTypeMatch, typeFilterExpressions, pathFilterExpressions))
                        {
                            typeToScriptMatches.Add((type, script));
                        }
                    }
                }
                else //fall back to reflection based lookup
                {
                    if (ScriptPickerPlugin.EnableLogging) GD.PushWarning("Error reading script metadata, falling back to resolving via reflection");
                    var scripts = ResourceAccess.LoadAssets<CSharpScript>("res://", true, @".*\.cs$").ToList();
                    var possibleTargetTypes = projectAssembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && inheritanceTypeMatch.IsAssignableFrom(t))
                        .ToArray();
                    var closestSearchMatches = new List<Type>(2);
                    var isAmbiguousName = false;
                    foreach (var script in scripts)
                    {
                        closestSearchMatches.Clear();
                        closestSearchMatches.AddRange(possibleTargetTypes.Where(t => t.Name.Equals(System.IO.Path.GetFileNameWithoutExtension(script.ResourcePath), StringComparison.Ordinal)));
                        if (closestSearchMatches.Count > 1)
                        {
                            for (int i = 0; i < closestSearchMatches.Count; i++)
                            {
                                if (ScriptPickerPlugin.EnableLogging) GD.Print(closestSearchMatches[i].FullName);
                            }
                            isAmbiguousName = true;
                            break;
                        }
                        if (FilterOutEntry(script, closestSearchMatches[0].FullName, godotAssembly, inheritanceTypeMatch, typeFilterExpressions, pathFilterExpressions)) continue;
                        typeToScriptMatches.Add((closestSearchMatches[0], script));
                    }
                    if (isAmbiguousName)
                    {
                        if (ScriptPickerPlugin.EnableLogging) GD.PrintErr("Ambiguous type names found while parsing, falling back to folder based ordering");
                        CreateFolderBasedTree(rootNode, inheritanceTypeMatch, typeFilterExpressions, pathFilterExpressions);
                        return;
                    }
                }
                foreach (var typeToScriptMatch in typeToScriptMatches)
                {
                    var currentMenuTreeNode = rootNode;
                    var typeName = typeToScriptMatch.Item1.FullName;
                    if (typeName == null)
                    {
                        if (ScriptPickerPlugin.EnableLogging) GD.PrintErr($"Error occured that shouldn't have happened, unsupported type encountered: {typeToScriptMatch.Item1}, skipping");
                        continue;
                    }
                    var pathSplit = typeName.Split(namespaceSplitChar, StringSplitOptions.RemoveEmptyEntries);
                    for (int splitIndex = 0; splitIndex < pathSplit.Length; splitIndex++)
                    {
                        if (splitIndex == pathSplit.Length - 1)
                        {
                            var scriptNode = currentMenuTreeNode.AddChild(typeName, EntryTypes.TypeName);
                            scriptNode.Script = typeToScriptMatch.Item2;
                        }
                        else
                        {
                            var nextNode = currentMenuTreeNode.FindChild(pathSplit[splitIndex]);
                            currentMenuTreeNode = nextNode ?? currentMenuTreeNode.AddChild(pathSplit[splitIndex], EntryTypes.SubSection);
                        }
                    }
                }
            }
        }

        static bool FilterOutEntry(Script script, string typeName, Assembly godotAssembly, Type inheritanceTypeMatch, string[] typeFilterExpressions, string[] pathFilterExpressions)
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                foreach (var ignoredExpression in typeFilterExpressions)
                {
                    if (typeName.MatchesWildcardedExpression(ignoredExpression)) return true;
                }
            }
            foreach (var ignoredExpression in pathFilterExpressions)
            {
                if (script.ResourcePath.MatchesWildcardedExpression(ignoredExpression)) return true;
            }
            var baseTypeName = script.GetInstanceBaseType();
            if (string.IsNullOrEmpty(baseTypeName))
            {
                return true;
            }
            var scriptBaseType = godotAssembly.GetType($"Godot.{baseTypeName}");
            if (!inheritanceTypeMatch.IsAssignableFrom(scriptBaseType))
            {
                return true;
            }
            return false;
        }
    }
}