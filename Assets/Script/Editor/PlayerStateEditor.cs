using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(PlayerState))]
public class PlayerStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player VFX Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Setup Fighter VFX (Generate & Assign)", GUILayout.Height(30)))
        {
            SetupVFX((PlayerState)target);
        }
    }

    private void SetupVFX(PlayerState pState)
    {
        // 1. 디렉토리 생성
        if (!Directory.Exists("Assets/Materials/VFX")) Directory.CreateDirectory("Assets/Materials/VFX");
        if (!Directory.Exists("Assets/Prefab/VFX")) Directory.CreateDirectory("Assets/Prefab/VFX");

        // 2. 스킬별 VFX 프리팹 생성 및 할당 (Built-in RP 최적화 셰이더 적용 완료된 최종판)
        GameObject straightPrefab = CreateSkillVFXPrefab("Straight", "VFX_Dot_StraightImpact.png", new Color(1, 1, 1, 1), 0.3f, 2.0f);
        GameObject risingPrefab = CreateSkillVFXPrefab("Rising", "VFX_Dot_RisingImpact.png", new Color(0.7f, 0.9f, 1f, 1), 0.4f, 2.5f);
        GameObject typhoonPrefab = CreateSkillVFXPrefab("Typhoon", "VFX_Dot_TyphoonEye.png", new Color(0.5f, 0.8f, 1f, 1), 0.8f, 4.0f);
        GameObject rupturePrefab = CreateSkillVFXPrefab("Rupture", "VFX_Dot_CrushRupture.png", new Color(1, 0.6f, 0.2f, 1), 0.6f, 3.0f);
        GameObject lightningPrefab = CreateSkillVFXPrefab("Lightning", "VFX_Dot_LightningBlast.png", new Color(1, 1, 0.8f, 1), 0.4f, 3.5f);
        GameObject orbPrefab = CreateSkillVFXPrefab("Orb", "VFX_Dot_EnergyOrb.png", new Color(0.2f, 1f, 0.8f, 1), 0.5f, 2.0f);
        GameObject smearPrefab = CreateSkillVFXPrefab("Smear", "VFX_Dot_MotionSmear.png", new Color(0.4f, 0.6f, 1f, 0.8f), 0.25f, 3.0f);
        GameObject flashPrefab = CreateSkillVFXPrefab("Flash", "VFX_Dot_AbstractFlash.png", new Color(1, 1, 1, 1), 0.2f, 1.5f);

        // 3. 현재 PlayerState 타겟에 할당
        pState.vfxStraight = straightPrefab;
        pState.vfxRising = risingPrefab;
        pState.vfxTyphoon = typhoonPrefab;
        pState.vfxRupture = rupturePrefab;
        pState.vfxLightning = lightningPrefab;
        pState.vfxOrb = orbPrefab;
        pState.vfxSmear = smearPrefab;
        pState.vfxAbstractFlash = flashPrefab;
        
        EditorUtility.SetDirty(pState);
        
        // 프리팹 인스턴스가 아니라 에셋 자체라면 SavePrefabAsset 필요
        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pState.gameObject);
        if (string.IsNullOrEmpty(assetPath))
        {
            // 만약 현재 Inspector를 확인중인 것이 프리팹 원본 Assets/Prefab/Player.prefab 일 경우
            assetPath = AssetDatabase.GetAssetPath(pState.gameObject);
        }
        
        if (!string.IsNullOrEmpty(assetPath))
        {
            PrefabUtility.SavePrefabAsset(pState.gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerStateEditor] 무투가 전용 스킬 이펙트 세팅 완료!");
    }

    private GameObject CreateSkillVFXPrefab(string name, string texName, Color color, float lifetime, float size)
    {
        string matPath = $"Assets/Materials/VFX/VFX_{name}_Mat.mat";
        string prefabPath = $"Assets/Prefab/VFX/VFX_Skill_{name}.prefab";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader targetShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (targetShader == null) targetShader = Shader.Find("Particles/Additive");
        if (targetShader == null) targetShader = Shader.Find("UI/Default");

        if (mat == null)
        {
            mat = new Material(targetShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = targetShader;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/VFX/{texName}");
        if (tex != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }
        EditorUtility.SetDirty(mat);

        if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);

        GameObject go = new GameObject($"VFX_Skill_{name}");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = lifetime; main.loop = false; main.startLifetime = lifetime;
        main.startSpeed = 0f; main.startSize = size; main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0; emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
        var shape = ps.shape; shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;

        // Add 64-bit arcade feeling by forcing Billboard render mode
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        GameObject result = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);
        return result;
    }
}
