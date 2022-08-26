using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityObject = UnityEngine.Object;

class UnityFavoritesTreeView : TreeView
{
    private EditorWindow _parentWindow;
    UnityFavoritesTreeAsset _treeAsset;

    public UnityFavoritesTreeView(TreeViewState treeViewState, EditorWindow parentWindow) : base(treeViewState)
    {
        _parentWindow = parentWindow;
        EnsureTreeAssetCreation("Assets/Packages/UnityFavorites/Favorites.asset");
        Reload();
    }

    #region TreeViewOverrides

    protected override TreeViewItem BuildRoot()
    {
        return new TreeViewItem { id = 0, depth = -1 };
    }

    protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        var rows = GetRows() ?? new List<TreeViewItem>(200);
        rows.Clear();

        for (int i = 1; i <= _treeAsset.TreeItems.Count; i++)
        {
            var folderData = _treeAsset.TreeItems[i - 1];
            var folderItem = new UnityFavoritesTreeViewFolderItem(1 + i, -1, folderData.FolderName, i.ToString());
            root.AddChild(folderItem);
            rows.Add(folderItem);

            if (IsExpanded(folderItem.id))
            {
                for (int k = 0; k < folderData.AssetPaths.Count; k++)
                {
                    var path = folderData.AssetPaths[k];
                    var displayName = GetAssetNameWithoutSuffix(path);
                    var assetItem = new UnityFavoritesTreeViewAssetItem(path.GetHashCode(), -1, displayName, path);

                    folderItem.AddChild(assetItem);
                    rows.Add(assetItem);
                }
            }
            else
            {
                if (folderData.AssetPaths.Count != 0)
                {
                    folderItem.children = CreateChildListForCollapsedParent();
                }
            }
        }

        SetupDepthsFromParentsAndChildren(root);
        return rows;
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        if (args.item is UnityFavoritesTreeViewFolderItem)
        {
            var folderItem = (UnityFavoritesTreeViewFolderItem)args.item;
            var rect = args.rowRect;

            rect.x += GetContentIndent(folderItem);
            rect.width -= GetContentIndent(folderItem);
            
            GUI.Label(rect, folderItem.displayName);
        }
        else if (args.item is UnityFavoritesTreeViewAssetItem assetItem)
        {
            var rect = args.rowRect;
            var type = GetTypeAtPath(assetItem.AssetPath);
            rect.x += GetContentIndent(assetItem);
            rect.width -= GetContentIndent(assetItem);
            
            if(type != null)
            {
                var icon = EditorGUIUtility.ObjectContent(null, type).image;
                if (icon != null)
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 16, 16), icon);
                }
            }
            rect.x += 16;
            rect.width -= 16;
            GUI.Label(rect, assetItem.displayName);
        }
    }

    private string GetAssetNameWithoutSuffix(string path)
    {
        var slashIndex = path.LastIndexOf('/');
        var withoutPrefix = slashIndex != -1 ? path.Substring(slashIndex + 1) : path;
        var dotIndex = withoutPrefix.LastIndexOf('.');
        return dotIndex != -1 ? withoutPrefix.Substring(0, dotIndex) : withoutPrefix;
    }

    protected override bool CanRename(TreeViewItem item)
    {
        return item is UnityFavoritesTreeViewFolderItem;
    }

    protected override void RenameEnded(RenameEndedArgs args)
    {
        if(args.newName == args.originalName)
            return;
        
        if (args.acceptedRename)
        {
            foreach (var folderItem in _treeAsset.TreeItems)
            {
                if (folderItem.FolderName == args.newName)
                {
                    var item = FindItem(args.itemID ,rootItem);
                    Debug.LogWarning("A folder with same name already exists. Please choose a different name.");
                    BeginRename(item);
                    return;
                }
            }
        
            foreach (var folderItem in _treeAsset.TreeItems)
            {
                if (folderItem.FolderName == args.originalName)
                {
                    folderItem.FolderName = args.newName;
                    break;
                }
            }

            EditorUtility.SetDirty(_treeAsset);
            AssetDatabase.SaveAssets();
            Reload();  
        }
    }
    
    //TODO: find a better way to do this
    public Type GetTypeAtPath(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityObject));
        
        if (asset == null)
            return null;
        
        return asset.GetType();
    }
    
    protected override bool CanMultiSelect(TreeViewItem item)
    {
        return false;
    }

    protected override void DoubleClickedItem(int id)
    {
        var item = FindItem(id, rootItem);
        if (item is UnityFavoritesTreeViewAssetItem assetItem)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetItem.AssetPath);
            if (obj == null)
            {
                Debug.LogWarning("Item with path " + assetItem.AssetPath + " cannot be found.");
                RemoveAssetItem(assetItem);
                return;
            }
            AssetDatabase.OpenAsset(obj);
        }
    }

    protected override void ContextClickedItem(int id)
    {
        if (Event.current.button == (int)PointerEventData.InputButton.Right)
        {
            var item = FindItem(id, rootItem);
            if (item is UnityFavoritesTreeViewFolderItem)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Rename"), false, () => BeginRename(item));
                menu.AddItem(new GUIContent("Clear"), false, () => ClearFolder(item.displayName));
                menu.AddItem(new GUIContent("Remove"), false, () => RemoveFolder(item.displayName));
                menu.ShowAsContext();
            }

            if (item is UnityFavoritesTreeViewAssetItem assetItem)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove"), false, () => RemoveAssetItem(assetItem));
                menu.ShowAsContext();
            }

            Event.current.Use();
            Reload();
        }

        base.ContextClickedItem(id);
    }

    protected override void ContextClicked()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Create New Folder"), false, CreateFolder);
        menu.AddItem(new GUIContent("Clear All"), false, ClearAll);
        menu.ShowAsContext();
    }

    protected override bool CanStartDrag(CanStartDragArgs args)
    {
        return args.draggedItem is UnityFavoritesTreeViewAssetItem;
    }

    protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        DragAndDrop.PrepareStartDrag();
        var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);

        List<UnityObject> objList = new List<UnityObject>(sortedDraggedIDs.Count);
        foreach (var id in sortedDraggedIDs)
        {
            var item = FindItem(id, rootItem);
            if (item is UnityFavoritesTreeViewAssetItem assetItem)
            {
                UnityObject obj = AssetDatabase.LoadMainAssetAtPath(assetItem.AssetPath);
                if (obj != null)
                    objList.Add(obj);
            }
        }

        DragAndDrop.objectReferences = objList.ToArray();

        string title = objList.Count > 1 ? "<Multiple>" : objList[0].name;
        DragAndDrop.StartDrag(title);
    }

    protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
    {
        var draggedObjects = DragAndDrop.paths;

        if (args.performDrop)
        {
            var parentItem = args.parentItem;
            var isParentFolderItem = parentItem != null && parentItem is UnityFavoritesTreeViewFolderItem;
            var isParentParentFolderItem = parentItem != null && parentItem.parent != null &&
                                           parentItem.parent is UnityFavoritesTreeViewFolderItem;
            
            if (isParentFolderItem || isParentParentFolderItem)
            {
                var folderName = isParentFolderItem ? parentItem.displayName : parentItem.parent.displayName;
                AddToFolder(folderName, draggedObjects);
                Reload();
                return DragAndDropVisualMode.Move;
            }
        }

        return DragAndDropVisualMode.Move;
    }
    #endregion TreeViewOverrides

    private void EnsureTreeAssetCreation(string path)
    {
        _treeAsset = AssetDatabase.LoadAssetAtPath<UnityFavoritesTreeAsset>(path);

        if (_treeAsset == null)
        {
            EnsureFolderExists("Assets", "Packages");
            EnsureFolderExists("Assets/Packages", "UnityFavorites");
            
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<UnityFavoritesTreeAsset>(), path);
        }

        _treeAsset = AssetDatabase.LoadAssetAtPath<UnityFavoritesTreeAsset>(path);
        
        if (_treeAsset.TreeItems.Count == 0)
        {
            _treeAsset.AddFolder("Favorites");
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(_treeAsset);
    }

    private void EnsureFolderExists(string parentFolder, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + folderName))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private void CreateFolder()
    {
        var folderName = GetNewFolderName();
        _treeAsset.AddFolder(folderName);
        
        Reload();
        
        var folderItem = FindFolderByDisplayName(folderName);
        BeginRename(folderItem);
    }
    
    private string GetNewFolderName()
    {
        var newFolderName = "New Folder";
        
        for (var i = 0; i < _treeAsset.TreeItems.Count; i++)
        {
            if(_treeAsset.TreeItems[i].FolderName == newFolderName)
            {
                newFolderName = "New Folder " + (i + 1);
            }
        }

        return newFolderName;
    }
    
    private UnityFavoritesTreeViewFolderItem FindFolderByDisplayName(string displayName)
    {
        var rows = GetRows();
        foreach (var row in rows)
        {
            if (row.displayName == displayName && row is UnityFavoritesTreeViewFolderItem folderItem)
            {
                return folderItem;
            }
        }

        return null;
    }
    
    private void ClearFolder(string folderName)
    {
        foreach (var folderData in _treeAsset.TreeItems)
        {
            if (folderData.FolderName == folderName)
            {
                folderData.AssetPaths.Clear();
            }
        }
    }

    private void RemoveFolder(string folderName)
    {
        for (var i = 0; i < _treeAsset.TreeItems.Count; i++)
        {
            if (_treeAsset.TreeItems[i].FolderName == folderName)
            {
                _treeAsset.TreeItems.RemoveAt(i);
                break;
            }
        }
        Reload();
    }

    private void ClearAll()
    {
        _treeAsset.TreeItems.Clear();
        Reload();
    }

    private void RemoveAssetItem(UnityFavoritesTreeViewAssetItem item)
    {
        var folderItem = (UnityFavoritesTreeViewFolderItem)(item.parent);
        foreach (var folderData in _treeAsset.TreeItems)
        {
            if (folderData.FolderName == folderItem.displayName)
            {
                for (var ındex = 0; ındex < folderData.AssetPaths.Count; ındex++)
                {
                    var path = folderData.AssetPaths[ındex];
                    if (path == item.AssetPath)
                    {
                        folderData.AssetPaths.Remove(path);
                        Reload();
                        return;
                    }
                }
            }
        }
    }

    private void AddToFolder(string folderName, string[] draggedObjects)
    {
        foreach (var folderData in _treeAsset.TreeItems)
        {
            foreach (var draggedObject in draggedObjects)
            {
                if (folderData.FolderName == folderName)
                {
                    if(!folderData.AssetPaths.Contains(draggedObject))
                        folderData.AssetPaths.Add(draggedObject);
                }
                else
                {
                    folderData.AssetPaths.Remove(draggedObject);
                }
            }
        }

        EditorUtility.SetDirty(_treeAsset);
        AssetDatabase.SaveAssets();
    }
}