using UnityEditor;
using UnityEngine;
using System.IO;

public class HitEffectSetup : EditorWindow
{
    [MenuItem("CSS_RPG/Setup Hit Effects")]
    public static void Setup()
    {
        // 1. 디렉토리 생성
        if (!Directory.Exists("Assets/Materials/Effects")) Directory.CreateDirectory("Assets/Materials/Effects");
        if (!Directory.Exists("Assets/Prefab/Effects")) Directory.CreateDirectory("Assets/Prefab/Effects");

        // 2. Spark 재질 및 프리팹
        GameObject sparkPrefab = CreateVFXPrefab("Spark", "HitImpact_Spark.png", new Color(1, 1, 1, 1));
        
        // 3. Blood 재질 및 프리팹
        GameObject bloodPrefab = CreateVFXPrefab("Blood", "HitImpact_Blood.png", new Color(1, 0, 0, 1));

        // 4. Player 프리팹에 할당 (기본값으로 Spark 할당)
        string playerPrefabPath = "Assets/Prefab/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab != null)
        {
            var pState = playerPrefab.GetComponentInChildren<PlayerState>();
            if (pState != null)
            {
                pState.hitEffectPrefab = sparkPrefab;
                EditorUtility.SetDirty(pState);
                PrefabUtility.SavePrefabAsset(playerPrefab);
                Debug.Log("[HitEffectSetup] Player 프리팹에 HitEffect_Spark 할당 완료!");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HitEffectSetup] 타격 이펙트 세팅 완료!");
    }

    private static GameObject CreateVFXPrefab(string name, string texName, Color color)
    {
        string matPath = $"Assets/Materials/Effects/Hit{name}_Mat.mat";
        string prefabPath = $"Assets/Prefab/Effects/HitEffect_{name}.prefab";

        // 재질 생성
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/Effects/{texName}");
        if (tex != null)
        {
             mat.mainTexture = tex;
             mat.SetColor("_Color", color);
             mat.SetInt("_Mode", 4); // Additive?
             // 셰이더 설정 등 (간단하게)
             AssetDatabase.CreateAsset(mat, matPath);
        }

        // 프리팹 생성
        GameObject go = new GameObject($"HitEffect_{name}");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 3f;
        main.startSize = 1.0f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;

        GameObject result = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return result;
    }
}
