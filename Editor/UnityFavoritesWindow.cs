using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class UnityFavoritesWindow : EditorWindow
{
    [SerializeField] TreeViewState m_TreeViewState;
    
    UnityFavoritesTreeView _mSimpleTreeViewView;

    private void OnEnable ()
    {
        // Check whether there is already a serialized view state (state 
        // that survived assembly reloading)
        if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState ();

        _mSimpleTreeViewView = new UnityFavoritesTreeView(m_TreeViewState, this);
    }
    
    public void OnGUI()
    {
        _mSimpleTreeViewView.OnGUI(new Rect(0, 0, position.width, position.height));
    }
    
    // Add menu named "My Window" to the Window menu
    [MenuItem ("Helpers/Favorites")]
    static void ShowWindow ()
    {
        // Get existing open window or if none, make a new one:
        var window = GetWindow<UnityFavoritesWindow> ();
        window.titleContent = new GUIContent ("Favorites");
        window.Show ();
    }
}

