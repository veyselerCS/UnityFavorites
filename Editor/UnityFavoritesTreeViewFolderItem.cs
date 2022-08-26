using UnityEditor.IMGUI.Controls;

public class UnityFavoritesTreeViewFolderItem : TreeViewItem
{
    public string Name;

    public UnityFavoritesTreeViewFolderItem(int id, int depth, string displayName, string name) : base(id, depth, displayName)
    {
        Name = name;
    }
}