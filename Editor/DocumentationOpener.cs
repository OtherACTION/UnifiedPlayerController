using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnifiedPlayerController
{
    public class DocumentationOpener : EditorWindow
    {
        [MenuItem("Help/UPC Documentation")]
        public static void ShowWindow()
        {
            GetWindow<DocumentationOpener>("UPC Documentation");
        }

        private void OnGUI()
        {
            GUILayout.Label("UPC Documentation", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Documentation"))
            {
                OpenDocumentation();
            }

            if (GUILayout.Button("Close"))
            {
                this.Close();
            }
        }

        private void OpenDocumentation()
        {
            // Search for "Unified Player Controller" anywhere under the Assets folder.
            string[] foundFolders = Directory.GetDirectories(Application.dataPath, "Unified Player Controller", SearchOption.AllDirectories);
            
            if (foundFolders.Length == 0)
            {
                Debug.LogError("Folder 'Unified Player Controller' not found in the project.");
                return;
            }

            // Use the first occurrence found.
            string unifiedFolderPath = foundFolders[0];
            string docsPath = Path.Combine(unifiedFolderPath, "Editor", "Documentation", "html", "index.html");

            if (File.Exists(docsPath))
            {
                string url = "file:///" + docsPath.Replace("\\", "/");
                Application.OpenURL(url);
            }
            else
            {
                Debug.LogError("Documentation not found at: " + docsPath);
            }
        }
    }
}
