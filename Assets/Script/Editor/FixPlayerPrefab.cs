#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FixPlayerPrefab
{
    static bool hasRun = false;

    static FixPlayerPrefab()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        if (hasRun) return;
        hasRun = true;
        EditorApplication.delayCall -= Execute;

        string path = "Assets/Prefab/Player.prefab";
        GameObject contentsRoot = PrefabUtility.LoadPrefabContents(path);
        
        if (contentsRoot == null)
        {
            Debug.LogError("[FixPlayerPrefab] Failed to load Player.prefab");
            return;
        }

        Transform rpgSys = contentsRoot.transform.Find("RPG_Systems");
        if (rpgSys == null)
        {
            Debug.LogError("[FixPlayerPrefab] RPG_Systems not found in Player.prefab");
            PrefabUtility.UnloadPrefabContents(contentsRoot);
            return;
        }

        bool modified = false;

        if (rpgSys.GetComponent<PlayerAuthentication>() == null)
        {
            rpgSys.gameObject.AddComponent<PlayerAuthentication>();
            modified = true;
        }
            
        if (rpgSys.GetComponent<PlayerHealth>() == null)
        {
            rpgSys.gameObject.AddComponent<PlayerHealth>();
            modified = true;
        }
            
        if (rpgSys.GetComponent<PlayerVFXController>() == null)
        {
            rpgSys.gameObject.AddComponent<PlayerVFXController>();
            modified = true;
        }
            
        if (rpgSys.GetComponent<PlayerLifecycleManager>() == null)
        {
            rpgSys.gameObject.AddComponent<PlayerLifecycleManager>();
            modified = true;
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
            Debug.Log("<color=green>[FixPlayerPrefab] Successfully attached 4 missing components to RPG_Systems in Player.prefab!</color>");
        }
        
        PrefabUtility.UnloadPrefabContents(contentsRoot);
    }
}
#endif
