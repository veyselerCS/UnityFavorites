using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

[CreateAssetMenu (fileName = "FavoritesData", menuName = "FavoritesData", order = 100)]
public class UnityFavoritesTreeAsset : ScriptableObject
{
    [SerializeField] List<UnityFavoritesFolderData> _treeItems = new List<UnityFavoritesFolderData> ();
    internal List<UnityFavoritesFolderData> TreeItems
    {
        get { return _treeItems; }
        set { _treeItems = value; }
    }

    public void AddFolder(string name)
    {
        TreeItems.Add(new UnityFavoritesFolderData(name));
    }
}