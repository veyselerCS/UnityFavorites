using System;
using System.Collections.Generic;

[Serializable]
public class UnityFavoritesFolderData
{
    public string FolderName;
    public List<string> AssetPaths = new List<string>();

    public UnityFavoritesFolderData(string folderName)
    {
        FolderName = folderName;
    }
}

