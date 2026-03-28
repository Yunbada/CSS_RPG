using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[InitializeOnLoad]
public class AutoPlayerSetup
{
    private static readonly string BASE = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";
    private static readonly string CONTROLLER_PATH = "Assets/Animation/PlayerAnimator.controller";
    private static readonly string PREFAB_PATH = "Assets/Prefab/Player.prefab";
    private static readonly string MODEL_PATH = "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx";

    static AutoPlayerSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    static void RunSetup()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) == null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null) return;

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH) == null)
        {
            CreateFullAnimatorController();
            Debug.Log("[AutoPlayerSetup] AnimatorController 생성 완료");
        }

        // 유저 요청에 따라 자동 프리팹 세팅 기능은 더 이상 사용하지 않음.
        // 프리팹(Player.prefab)은 에디터에서 직접 수동으로 관리합니다.
        // SetupPrefab(); 
    }

    static BlendTree CreateSavedBlendTree(AnimatorController ctrl, string name, string paramX, string paramY)
    {
        var tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = paramX,
            blendParameterY = paramY,
            hideFlags = HideFlags.HideInHierarchy
        };
        // 중요: BlendTree를 AnimatorController 에셋의 서브 오브젝트로 저장
        AssetDatabase.AddObjectToAsset(tree, ctrl);
        return tree;
    }

    static void CreateFullAnimatorController()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animation"))
            AssetDatabase.CreateFolder("Assets", "Animation");

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        ctrl.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsJumping", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("TurnValue", AnimatorControllerParameterType.Float);

        var sm = ctrl.layers[0].stateMachine;

        // ===== IDLE =====
        var idleState = sm.AddState("Idle", new Vector3(0, 0, 0));
        idleState.motion = LoadClip(BASE + "Idles/HumanM@Idle01.fbx");
        sm.defaultState = idleState;

        var idle2State = sm.AddState("Idle2", new Vector3(0, 80, 0));
        idle2State.motion = LoadClip(BASE + "Idles/HumanM@Idle02.fbx");

        var idle1to2 = sm.AddState("Idle1to2", new Vector3(-150, 40, 0));
        idle1to2.motion = LoadClip(BASE + "Idles/HumanM@Idle01-Idle02.fbx");

        var idle2to1 = sm.AddState("Idle2to1", new Vector3(150, 40, 0));
        idle2to1.motion = LoadClip(BASE + "Idles/HumanM@Idle02-Idle01.fbx");

        // Idle 순환
        var t = idleState.AddTransition(idle1to2);
        t.hasExitTime = true; t.exitTime = 5f; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        t = idle1to2.AddTransition(idle2State);
        t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f;

        t = idle2State.AddTransition(idle2to1);
        t.hasExitTime = true; t.exitTime = 5f; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        t = idle2to1.AddTransition(idleState);
        t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f;

        // ===== WALK BLEND TREE (8방향) =====
        var walkTree = CreateSavedBlendTree(ctrl, "WalkBlend", "MoveX", "MoveY");
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_Forward.fbx"),        new Vector2(0, 1));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_ForwardLeft.fbx"),     new Vector2(-0.7f, 0.7f));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_ForwardRight.fbx"),    new Vector2(0.7f, 0.7f));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_Left.fbx"),            new Vector2(-1, 0));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_Right.fbx"),           new Vector2(1, 0));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_Backward.fbx"),        new Vector2(0, -1));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_BackwardLeft.fbx"),    new Vector2(-0.7f, -0.7f));
        walkTree.AddChild(LoadClip(BASE + "Movement/Walk/HumanM@Walk01_BackwardRight.fbx"),   new Vector2(0.7f, -0.7f));

        var walkState = sm.AddState("Walk", new Vector3(300, 0, 0));
        walkState.motion = walkTree;

        // ===== RUN BLEND TREE (8방향) =====
        var runTree = CreateSavedBlendTree(ctrl, "RunBlend", "MoveX", "MoveY");
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_Forward.fbx"),        new Vector2(0, 1));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_ForwardLeft.fbx"),     new Vector2(-0.7f, 0.7f));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_ForwardRight.fbx"),    new Vector2(0.7f, 0.7f));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_Left.fbx"),            new Vector2(-1, 0));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_Right.fbx"),           new Vector2(1, 0));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_Backward.fbx"),        new Vector2(0, -1));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_BackwardLeft.fbx"),    new Vector2(-0.7f, -0.7f));
        runTree.AddChild(LoadClip(BASE + "Movement/Run/HumanM@Run01_BackwardRight.fbx"),   new Vector2(0.7f, -0.7f));

        var runState = sm.AddState("Run", new Vector3(300, 100, 0));
        runState.motion = runTree;

        // ===== SPRINT BLEND TREE (5방향) =====
        var sprintTree = CreateSavedBlendTree(ctrl, "SprintBlend", "MoveX", "MoveY");
        sprintTree.AddChild(LoadClip(BASE + "Movement/Sprint/HumanM@Sprint01_Forward.fbx"),      new Vector2(0, 1));
        sprintTree.AddChild(LoadClip(BASE + "Movement/Sprint/HumanM@Sprint01_ForwardLeft.fbx"),   new Vector2(-0.7f, 0.7f));
        sprintTree.AddChild(LoadClip(BASE + "Movement/Sprint/HumanM@Sprint01_ForwardRight.fbx"),  new Vector2(0.7f, 0.7f));
        sprintTree.AddChild(LoadClip(BASE + "Movement/Sprint/HumanM@Sprint01_Left.fbx"),          new Vector2(-1, 0));
        sprintTree.AddChild(LoadClip(BASE + "Movement/Sprint/HumanM@Sprint01_Right.fbx"),         new Vector2(1, 0));

        var sprintState = sm.AddState("Sprint", new Vector3(300, 200, 0));
        sprintState.motion = sprintTree;

        // ===== TURN =====
        var turnLeftState = sm.AddState("TurnLeft", new Vector3(-300, 200, 0));
        turnLeftState.motion = LoadClip(BASE + "Movement/Turn/HumanM@Turn01_Left.fbx");

        var turnRightState = sm.AddState("TurnRight", new Vector3(-300, 280, 0));
        turnRightState.motion = LoadClip(BASE + "Movement/Turn/HumanM@Turn01_Right.fbx");

        // ===== JUMP / FALL / LAND =====
        var jumpState = sm.AddState("JumpBegin", new Vector3(600, 0, 0));
        jumpState.motion = LoadClip(BASE + "Movement/Jump/HumanM@Jump01 - Begin.fbx");

        var fallState = sm.AddState("Fall", new Vector3(600, 80, 0));
        fallState.motion = LoadClip(BASE + "Movement/Jump/HumanM@Fall01.fbx");

        var landState = sm.AddState("Land", new Vector3(600, 160, 0));
        landState.motion = LoadClip(BASE + "Movement/Jump/HumanM@Jump01 - Land.fbx");

        // ============ TRANSITIONS ============

        // Idle 계열 → Walk / Run / Turn
        foreach (var idleSt in new[] { idleState, idle2State })
        {
            t = idleSt.AddTransition(walkState);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            t.duration = 0.15f; t.hasExitTime = false;

            t = idleSt.AddTransition(runState);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            t.duration = 0.15f; t.hasExitTime = false;

            t = idleSt.AddTransition(turnLeftState);
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, "TurnValue");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.duration = 0.1f; t.hasExitTime = false;

            t = idleSt.AddTransition(turnRightState);
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "TurnValue");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.duration = 0.1f; t.hasExitTime = false;
        }

        // Walk ↔ Idle
        t = walkState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.duration = 0.2f; t.hasExitTime = false;

        // Walk ↔ Run
        t = walkState.AddTransition(runState);
        t.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        t.duration = 0.15f; t.hasExitTime = false;

        t = runState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.duration = 0.15f; t.hasExitTime = false;

        t = runState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.duration = 0.2f; t.hasExitTime = false;

        // Turn → Idle / Walk
        t = turnLeftState.AddTransition(idleState);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.15f;

        t = turnRightState.AddTransition(idleState);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.15f;

        t = turnLeftState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.duration = 0.15f; t.hasExitTime = false;

        t = turnRightState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.duration = 0.15f; t.hasExitTime = false;

        // Any → Jump
        var anyToJump = sm.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
        anyToJump.duration = 0.1f; anyToJump.hasExitTime = false;
        anyToJump.canTransitionToSelf = false;

        // Jump → Fall → Land → Idle
        t = jumpState.AddTransition(fallState);
        t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.1f;

        t = fallState.AddTransition(landState);
        t.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        t.duration = 0.1f; t.hasExitTime = false;

        t = landState.AddTransition(idleState);
        t.hasExitTime = true; t.exitTime = 0.8f; t.duration = 0.15f;

        t = landState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.duration = 0.15f; t.hasExitTime = false;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
    }

    // ============================================================
    //  PREFAB 세팅
    // ============================================================
    static void SetupPrefab()
    {
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
        if (animController == null) return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);

        // 1. 기존 PlayerController(삭제됨) 등 Missing Script 가장 먼저 제거
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);

        // 1-A. 루트(Root)에 잘못 붙어있는 Animator/NetworkAnimator 찌꺼기 무조건 삭제
        // (자식 객체인 PlayerModel에 붙어있는 진짜 Animator와 충돌하여 NRE를 일으키는 주범)
        foreach (var a in prefabRoot.GetComponents<Animator>()) Object.DestroyImmediate(a, true);
        foreach (var n in prefabRoot.GetComponents<Unity.Netcode.Components.NetworkAnimator>()) Object.DestroyImmediate(n, true);

        // 2. 새 모듈형 컴포넌트들 항상 보장
        if (prefabRoot.GetComponent<InputHandle>() == null) prefabRoot.AddComponent<InputHandle>();
        if (prefabRoot.GetComponent<CharacterController>() == null) prefabRoot.AddComponent<CharacterController>();
        
        if (prefabRoot.GetComponent<PlayerMovement>() == null) prefabRoot.AddComponent<PlayerMovement>();
        if (prefabRoot.GetComponent<PlayerCamera>() == null) prefabRoot.AddComponent<PlayerCamera>();
        if (prefabRoot.GetComponent<PlayerAnimation>() == null) prefabRoot.AddComponent<PlayerAnimation>();

        Transform existingModel = prefabRoot.transform.Find("PlayerModel");
        Animator existingAnimator = prefabRoot.GetComponentInChildren<Animator>();
        if (existingModel != null && existingAnimator != null && existingAnimator.runtimeAnimatorController != null)
        {
            // 이미 세팅 되어있으면 NetworkAnimator만 확인
            EnsureNetworkAnimator(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        if (existingModel != null)
            Object.DestroyImmediate(existingModel.gameObject);

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        if (modelPrefab != null)
        {
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            modelInstance.name = "PlayerModel";
            modelInstance.transform.SetParent(prefabRoot.transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null) animator = modelInstance.AddComponent<Animator>();
            animator.runtimeAnimatorController = animController;
            animator.applyRootMotion = false;
        }

        EnsureNetworkAnimator(prefabRoot);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AutoPlayerSetup] Player 프리팹 자동 세팅 완료!");
    }

    static void EnsureNetworkAnimator(GameObject prefabRoot)
    {
        Animator childAnimator = prefabRoot.GetComponentInChildren<Animator>();
        if (childAnimator == null || childAnimator.runtimeAnimatorController == null) return;

        // 잘못된 곳(최상단 루트 등)에 붙어있는 NetworkAnimator들을 싹 다 강제 제거
        var oldNetAnims = prefabRoot.GetComponentsInChildren<Unity.Netcode.Components.NetworkAnimator>(true);
        foreach (var old in oldNetAnims)
        {
            if (old.gameObject != childAnimator.gameObject || old.GetType() != typeof(OwnerNetworkAnimator))
            {
                Object.DestroyImmediate(old, true);
            }
        }

        // Animator와 '같은' 자식 게임오브젝트 요소에만 OwnerNetworkAnimator를 부착!
        // 이렇게 해야 Model을 삭제할 때 NetworkAnimator 찌꺼기도 함께 삭제되어 OnValidate 버그가 방지됨.
        var netAnim = childAnimator.gameObject.GetComponent<OwnerNetworkAnimator>();
        if (netAnim == null)
        {
            netAnim = childAnimator.gameObject.AddComponent<OwnerNetworkAnimator>();
        }

        netAnim.Animator = childAnimator;
    }

    static AnimationClip LoadClip(string path)
    {
        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
        if (objs == null) return null;
        foreach (var obj in objs)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        Debug.LogWarning("[AutoPlayerSetup] 클립 없음: " + path);
        return null;
    }
}
