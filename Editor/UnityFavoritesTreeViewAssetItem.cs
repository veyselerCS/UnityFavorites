using UnityEditor.IMGUI.Controls;
using UnityObject = UnityEngine.Object;

public class UnityFavoritesTreeViewAssetItem : TreeViewItem
{
    public string AssetPath;

    public UnityFavoritesTreeViewAssetItem(int id, int depth, string displayName, string assetPath) : base(id, depth, displayName)
    {
        AssetPath = assetPath;
    }
}