using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.ProBuilder;

namespace NatchapholAunjai
{
    public class Stage2Builder
    {
        [MenuItem("Tools/Build Stage 2 Foundation")]
        public static void BuildStage2()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage 2.unity", OpenSceneMode.Single);

            // ==========================================
            // 0. VERIFY TAGS
            // ==========================================
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            string[] requiredTags = new string[] { "Interactable", "CinemachineTarget", "DeathZone", "Player", "Item" };
            foreach (var tag in requiredTags)
            {
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag)) { found = true; break; }
                }
                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }
            tagManager.ApplyModifiedProperties();

            // ==========================================
            // 1. CLEAN EXISTING DYNAMIC OBJECTS
            // ==========================================
            string[] objectsToClean = new string[] { "Map", "Map_ProBuilder", "Items", "Player", "PlayerArmature", "PlayerFollowCamera", "DeathZone", "GameManager" };
            foreach (var objName in objectsToClean)
            {
                var go = GameObject.Find(objName);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            // Clean old placeholder buttons from Canvas if present
            GameObject existingCanvas = GameObject.Find("Canvas");
            if (existingCanvas != null)
            {
                string[] oldButtons = new string[] { "WinButton", "LoseButton", "BackButton" };
                foreach (var btnName in oldButtons)
                {
                    Transform t = existingCanvas.transform.Find(btnName);
                    if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            // ==========================================
            // 2. 1080p HUD CANVAS & GAMEMANAGER SETUP
            // ==========================================
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas");
                canvasGo.AddComponent<Canvas>();
            }

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem
            if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Font standardFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (standardFont == null) standardFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Func<string, string, Vector2, Vector2, Vector2, Vector2, Vector2, int, TextAnchor, Color, Text> CreateHUDText = 
                (name, textContent, anchorMin, anchorMax, pivot, anchoredPos, size, fontSize, alignment, color) => 
            {
                Transform existing = canvasGo.transform.Find(name);
                GameObject tGo = existing != null ? existing.gameObject : new GameObject(name);
                tGo.transform.SetParent(canvasGo.transform, false);

                RectTransform rt = tGo.GetComponent<RectTransform>();
                if (rt == null) rt = tGo.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;

                Text txt = tGo.GetComponent<Text>();
                if (txt == null) txt = tGo.AddComponent<Text>();
                txt.font = standardFont;
                txt.text = textContent;
                txt.fontSize = fontSize;
                txt.alignment = alignment;
                txt.color = color;
                txt.raycastTarget = false;
                return txt;
            };

            // StageTitle (Centered top)
            Text stageTitle = CreateHUDText("StageTitle", "STAGE 2", 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                new Vector2(0, 400), new Vector2(800, 150), 80, TextAnchor.MiddleCenter, Color.white);

            // TimerText (Top-Left)
            Text timerText = CreateHUDText("TimerText", "Time: 00:30", 
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), 
                new Vector2(40, -40), new Vector2(450, 100), 45, TextAnchor.UpperLeft, Color.yellow);

            // ScoreText (Top-Right)
            Text scoreText = CreateHUDText("ScoreText", "Score: 0", 
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), 
                new Vector2(-40, -40), new Vector2(450, 100), 45, TextAnchor.UpperRight, Color.yellow);

            // InteractPrompt (Center-Bottom)
            Text interactPrompt = CreateHUDText("InteractPrompt", "[E] Interact", 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                new Vector2(0, -220), new Vector2(600, 90), 48, TextAnchor.MiddleCenter, new Color(0.2f, 1f, 0.4f, 1f));
            interactPrompt.gameObject.SetActive(false); // Hidden until near an interactable object

            // GameManager setup
            GameObject gmGo = new GameObject("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.timerText = timerText;
            gm.scoreText = scoreText;
            gm.timeLimit = 30f;
            gm.winScoreThreshold = 10000;

            // ==========================================
            // 3. CAMERA SETUP (Cinemachine Brain)
            // ==========================================
            GameObject mainCamGo = GameObject.FindWithTag("MainCamera");
            if (mainCamGo == null)
            {
                mainCamGo = new GameObject("Main Camera");
                mainCamGo.tag = "MainCamera";
            }

            Camera cam = mainCamGo.GetComponent<Camera>();
            if (cam == null) cam = mainCamGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.2f;

            if (mainCamGo.GetComponent<AudioListener>() == null)
                mainCamGo.AddComponent<AudioListener>();

            // Ensure CinemachineBrain
            CinemachineBrain brain = mainCamGo.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamGo.AddComponent<CinemachineBrain>();
            }

            // Directional Light
            Light dirLight = GameObject.FindAnyObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.intensity = 0.65f;
                dirLight.color = new Color(0.96f, 0.92f, 0.88f);
            }

            // ==========================================
            // 4. PLAYER SETUP: HUMANOID CONTROLLER
            // ==========================================
            GameObject playerArmaturePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/PlayerArmature.prefab");
            if (playerArmaturePrefab == null)
            {
                Debug.LogError("PlayerArmature prefab not found in Starter Assets!");
                return;
            }

            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerArmaturePrefab);
            playerInstance.name = "Player";
            playerInstance.tag = "Player";
            // Spawn player at Entrance Bastion: floor top surface is y = 0.5m
            playerInstance.transform.position = new Vector3(0, 0.55f, 0);
            playerInstance.transform.rotation = Quaternion.identity;

            // Hide/Disable original default robot meshes and armature
            Transform defaultGeom = playerInstance.transform.Find("Geometry");
            if (defaultGeom != null) defaultGeom.gameObject.SetActive(false);
            Transform defaultArm = playerInstance.transform.Find("Armature");
            if (defaultArm != null) defaultArm.gameObject.SetActive(false);

            // Import Humanoid Mixamo Model as child
            GameObject ch02Fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Character Models/Ch02_nonPBR.fbx");
            Avatar ch02Avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Character Models/Ch02_nonPBR.fbx");
            RuntimeAnimatorController tpcAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");

            if (ch02Fbx != null)
            {
                GameObject ch02Model = (GameObject)PrefabUtility.InstantiatePrefab(ch02Fbx, playerInstance.transform);
                ch02Model.name = "Ch02_Model";
                ch02Model.transform.localPosition = Vector3.zero;
                ch02Model.transform.localRotation = Quaternion.identity;
                ch02Model.transform.localScale = Vector3.one;

                // Remove child animator so root animator controls the humanoid character
                Animator childAnim = ch02Model.GetComponent<Animator>();
                if (childAnim != null) UnityEngine.Object.DestroyImmediate(childAnim);
            }

            // Configure Root Animator
            Animator rootAnim = playerInstance.GetComponent<Animator>();
            if (rootAnim != null)
            {
                if (ch02Avatar != null) rootAnim.avatar = ch02Avatar;
                if (tpcAnimController != null) rootAnim.runtimeAnimatorController = tpcAnimController;
            }

            // Add Interactor script
            Interactor interactor = playerInstance.GetComponent<Interactor>();
            if (interactor == null) interactor = playerInstance.AddComponent<Interactor>();
            interactor.promptText = interactPrompt;
            interactor.raycastDistance = 3.5f;
            interactor.sphereRadius = 2.2f;

            // Add PlayerDeathZoneTrigger
            if (playerInstance.GetComponent<PlayerDeathZoneTrigger>() == null)
            {
                playerInstance.AddComponent<PlayerDeathZoneTrigger>();
            }

            // Ensure PlayerCameraRoot exists and is tagged
            Transform cameraRoot = playerInstance.transform.Find("PlayerCameraRoot");
            if (cameraRoot != null)
            {
                cameraRoot.gameObject.tag = "CinemachineTarget";
            }

            // ==========================================
            // 5. CINEMACHINE CAMERA TRACKING SETUP
            // ==========================================
            GameObject vcamGo = new GameObject("PlayerFollowCamera");
            CinemachineCamera vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Priority = 10;

            if (cameraRoot != null)
            {
                vcam.Target.TrackingTarget = cameraRoot;
                vcam.Target.LookAtTarget = cameraRoot;
            }

            CinemachineThirdPersonFollow thirdPersonFollow = vcamGo.AddComponent<CinemachineThirdPersonFollow>();
            // Generous clearance settings: distance 3.2m, slight shoulder offset, avoid clipping
            thirdPersonFollow.CameraDistance = 3.2f;
            thirdPersonFollow.VerticalArmLength = 0.35f;
            thirdPersonFollow.ShoulderOffset = new Vector3(0.2f, 0.3f, 0f);
            thirdPersonFollow.Damping = new Vector3(0.1f, 0.1f, 0.1f);
            thirdPersonFollow.AvoidObstacles.Enabled = true;
            thirdPersonFollow.AvoidObstacles.CameraRadius = 0.12f;
            thirdPersonFollow.AvoidObstacles.IgnoreTag = "Player";
            thirdPersonFollow.AvoidObstacles.DampingIntoCollision = 0.05f;
            thirdPersonFollow.AvoidObstacles.DampingFromCollision = 0.1f;

            // ==========================================
            // 6. PROBUILDER MAP GENERATION: 2-PATH FORTRESS CAUSEWAY
            // ==========================================
            Material caveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CaveStone_Mat.mat");
            if (caveMat == null)
            {
                caveMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                caveMat.color = new Color(0.35f, 0.37f, 0.4f);
                AssetDatabase.CreateAsset(caveMat, "Assets/Materials/CaveStone_Mat.mat");
            }

            GameObject mapRoot = new GameObject("Map_ProBuilder");

            // --- Helper: Generate ProBuilder Walkway Platform with solid MeshCollider ---
            Func<string, Vector3, Vector3, GameObject> CreatePBWalkway = (name, centerPos, size) => {
                ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
                pbMesh.name = name;
                pbMesh.transform.position = centerPos;
                pbMesh.transform.SetParent(mapRoot.transform);
                pbMesh.ToMesh();
                pbMesh.Refresh();

                MeshCollider mc = pbMesh.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = false;

                MeshRenderer mr = pbMesh.GetComponent<MeshRenderer>();
                if (mr != null && caveMat != null) mr.sharedMaterial = caveMat;
                return pbMesh.gameObject;
            };

            // --- Helper: Generate ProBuilder Boundary Parapet Wall with solid MeshCollider ---
            Func<string, Vector3, Vector3, GameObject> CreatePBParapet = (name, centerPos, size) => {
                ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
                pbMesh.name = name;
                pbMesh.transform.position = centerPos;
                pbMesh.transform.SetParent(mapRoot.transform);
                pbMesh.ToMesh();
                pbMesh.Refresh();

                MeshCollider mc = pbMesh.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = false;

                MeshRenderer mr = pbMesh.GetComponent<MeshRenderer>();
                if (mr != null && caveMat != null) mr.sharedMaterial = caveMat;
                return pbMesh.gameObject;
            };

            // --- Helper: Generate ProBuilder Stairs with solid MeshCollider ---
            Func<string, Vector3, Vector3, int, Quaternion, GameObject> CreatePBStaircase = (name, centerPos, size, steps, rot) => {
                ProBuilderMesh pbMesh = ShapeGenerator.GenerateStair(PivotLocation.Center, size, steps, false);
                pbMesh.name = name;
                pbMesh.transform.position = centerPos;
                pbMesh.transform.rotation = rot;
                pbMesh.transform.SetParent(mapRoot.transform);
                pbMesh.ToMesh();
                pbMesh.Refresh();

                MeshCollider mc = pbMesh.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = false;

                MeshRenderer mr = pbMesh.GetComponent<MeshRenderer>();
                if (mr != null && caveMat != null) mr.sharedMaterial = caveMat;
                return pbMesh.gameObject;
            };

            // --- Helper: Generate ProBuilder Decorative Pillar ---
            Func<string, Vector3, Vector3, GameObject> CreatePBPillar = (name, centerPos, size) => {
                ProBuilderMesh pbMesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
                pbMesh.name = name;
                pbMesh.transform.position = centerPos;
                pbMesh.transform.SetParent(mapRoot.transform);
                pbMesh.ToMesh();
                pbMesh.Refresh();

                MeshCollider mc = pbMesh.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = pbMesh.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = false;

                MeshRenderer mr = pbMesh.GetComponent<MeshRenderer>();
                if (mr != null && caveMat != null) mr.sharedMaterial = caveMat;
                return pbMesh.gameObject;
            };

            // --- Helper: Add Torch Point Light ---
            Action<string, Vector3, float> AddPBTorch = (name, pos, range) => {
                GameObject t = new GameObject(name);
                t.transform.position = pos;
                t.transform.SetParent(mapRoot.transform);
                Light l = t.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = range;
                l.color = new Color(1f, 0.72f, 0.38f);
                l.intensity = 2.8f;
            };

            // -------------------------------------------------------------
            // SECTION A: ENTRANCE SANCTUM (Spawn Area: y=0, walk surface=0.5m)
            // -------------------------------------------------------------
            CreatePBWalkway("PB_Entrance_Floor", new Vector3(0, 0, 0), new Vector3(8, 1, 8));
            // Rear wall preventing backward fall
            CreatePBParapet("PB_Entrance_RearWall", new Vector3(0, 1.5f, -4.2f), new Vector3(8.5f, 3.0f, 0.6f));
            CreatePBParapet("PB_Entrance_LeftWall", new Vector3(-4.2f, 0.7f, 0), new Vector3(0.6f, 1.4f, 8f));
            CreatePBParapet("PB_Entrance_RightWall", new Vector3(4.2f, 0.7f, 0), new Vector3(0.6f, 1.4f, 8f));
            CreatePBPillar("PB_Entrance_PillarL", new Vector3(-3.5f, 2.5f, -3.5f), new Vector3(1.2f, 5.0f, 1.2f));
            CreatePBPillar("PB_Entrance_PillarR", new Vector3(3.5f, 2.5f, -3.5f), new Vector3(1.2f, 5.0f, 1.2f));
            AddPBTorch("Torch_Entrance", new Vector3(0, 3.0f, 0), 12f);

            // -------------------------------------------------------------
            // SECTION B: MAIN CENTRAL CAUSEWAY (Linear Trunk: z=4 to z=24)
            // -------------------------------------------------------------
            CreatePBWalkway("PB_Trunk_Floor", new Vector3(0, 0, 14), new Vector3(5.5f, 1, 20));
            CreatePBParapet("PB_Trunk_LeftParapet", new Vector3(-2.9f, 0.7f, 14), new Vector3(0.5f, 1.4f, 20));
            CreatePBParapet("PB_Trunk_RightParapet", new Vector3(2.9f, 0.7f, 14), new Vector3(0.5f, 1.4f, 20));
            CreatePBPillar("PB_Trunk_PillarL", new Vector3(-3.2f, 2.5f, 14), new Vector3(1.0f, 5.0f, 1.0f));
            CreatePBPillar("PB_Trunk_PillarR", new Vector3(3.2f, 2.5f, 14), new Vector3(1.0f, 5.0f, 1.0f));
            AddPBTorch("Torch_Trunk", new Vector3(0, 3.0f, 14), 14f);

            // -------------------------------------------------------------
            // SECTION C: FORK / CROSSROADS BASTION (z=24 to z=32)
            // -------------------------------------------------------------
            CreatePBWalkway("PB_Crossroads_Floor", new Vector3(0, 0, 28), new Vector3(10, 1, 8));
            // North parapet blocks the center chasm, forcing player to split East or West!
            CreatePBParapet("PB_Crossroads_NorthChasmWall", new Vector3(0, 0.7f, 32.2f), new Vector3(10.5f, 1.4f, 0.5f));
            // Central split obelisk
            CreatePBPillar("PB_Crossroads_CenterObelisk", new Vector3(0, 2.0f, 28), new Vector3(1.4f, 4.0f, 1.4f));
            // South corners
            CreatePBParapet("PB_Crossroads_SouthWallL", new Vector3(-4.0f, 0.7f, 23.8f), new Vector3(2.5f, 1.4f, 0.5f));
            CreatePBParapet("PB_Crossroads_SouthWallR", new Vector3(4.0f, 0.7f, 23.8f), new Vector3(2.5f, 1.4f, 0.5f));
            AddPBTorch("Torch_Crossroads", new Vector3(0, 4.0f, 28), 16f);

            // -------------------------------------------------------------
            // PATH 1: EAST HIGH RAMPART (Right Route)
            // -------------------------------------------------------------
            // East Traverse Bridge (x=5 to 18, z=28)
            CreatePBWalkway("PB_Path1_Traverse_Floor", new Vector3(11.5f, 0, 28), new Vector3(13, 1, 5));
            CreatePBParapet("PB_Path1_Traverse_NorthWall", new Vector3(11.5f, 0.7f, 30.7f), new Vector3(13, 1.4f, 0.5f));
            CreatePBParapet("PB_Path1_Traverse_SouthWall", new Vector3(11.5f, 0.7f, 25.3f), new Vector3(13, 1.4f, 0.5f));

            // East Landing Terrace (x=18, z=28)
            CreatePBWalkway("PB_Path1_Landing_Floor", new Vector3(18, 0, 28), new Vector3(6, 1, 6));
            CreatePBParapet("PB_Path1_Landing_SouthWall", new Vector3(18, 0.7f, 24.8f), new Vector3(6.5f, 1.4f, 0.5f));
            CreatePBParapet("PB_Path1_Landing_EastWall", new Vector3(21.2f, 0.7f, 28), new Vector3(0.5f, 1.4f, 6.5f));
            AddPBTorch("Torch_EastLanding", new Vector3(18, 3.0f, 28), 12f);

            // East Grand Staircase (ascending z=31 to 41, y=0 to 2.4m)
            CreatePBStaircase("PB_Path1_Stairs", new Vector3(18, 1.2f, 36), new Vector3(5, 2.4f, 10), 10, Quaternion.identity);
            CreatePBParapet("PB_Path1_Stairs_EastWall", new Vector3(20.7f, 1.9f, 36), new Vector3(0.5f, 2.4f, 10));
            CreatePBParapet("PB_Path1_Stairs_WestWall", new Vector3(15.3f, 1.9f, 36), new Vector3(0.5f, 2.4f, 10));

            // High Rampart Walkway (x=18, y=2.4m, z=41 to 61)
            CreatePBWalkway("PB_Path1_HighRampart_Floor", new Vector3(18, 2.4f, 51), new Vector3(5, 1, 20));
            CreatePBParapet("PB_Path1_HighRampart_EastWall", new Vector3(20.7f, 3.1f, 51), new Vector3(0.5f, 1.4f, 20));
            CreatePBParapet("PB_Path1_HighRampart_WestWall", new Vector3(15.3f, 3.1f, 51), new Vector3(0.5f, 1.4f, 20));
            CreatePBPillar("PB_Path1_HighCol1", new Vector3(21.2f, 4.5f, 45), new Vector3(1.0f, 4.5f, 1.0f));
            CreatePBPillar("PB_Path1_HighCol2", new Vector3(21.2f, 4.5f, 51), new Vector3(1.0f, 4.5f, 1.0f));
            CreatePBPillar("PB_Path1_HighCol3", new Vector3(21.2f, 4.5f, 57), new Vector3(1.0f, 4.5f, 1.0f));
            AddPBTorch("Torch_EastHighRampart", new Vector3(18, 5.5f, 51), 14f);

            // High Skybridge West to Citadel (x=18 to 5, z=61, y=2.4m)
            CreatePBWalkway("PB_Path1_Skybridge_Floor", new Vector3(11.5f, 2.4f, 61), new Vector3(13, 1, 5));
            CreatePBParapet("PB_Path1_Skybridge_NorthWall", new Vector3(11.5f, 3.1f, 63.7f), new Vector3(13, 1.4f, 0.5f));
            CreatePBParapet("PB_Path1_Skybridge_SouthWall", new Vector3(11.5f, 3.1f, 58.3f), new Vector3(13, 1.4f, 0.5f));

            // -------------------------------------------------------------
            // PATH 2: WEST SUNKEN ARCADE (Left Route / The Side Path)
            // -------------------------------------------------------------
            // West Traverse Bridge (x=-5 to -18, z=28)
            CreatePBWalkway("PB_Path2_Traverse_Floor", new Vector3(-11.5f, 0, 28), new Vector3(13, 1, 5));
            CreatePBParapet("PB_Path2_Traverse_NorthWall", new Vector3(-11.5f, 0.7f, 30.7f), new Vector3(13, 1.4f, 0.5f));
            CreatePBParapet("PB_Path2_Traverse_SouthWall", new Vector3(-11.5f, 0.7f, 25.3f), new Vector3(13, 1.4f, 0.5f));

            // West Landing Terrace (x=-18, z=28)
            CreatePBWalkway("PB_Path2_Landing_Floor", new Vector3(-18, 0, 28), new Vector3(6, 1, 6));
            CreatePBParapet("PB_Path2_Landing_SouthWall", new Vector3(-18, 0.7f, 24.8f), new Vector3(6.5f, 1.4f, 0.5f));
            CreatePBParapet("PB_Path2_Landing_WestWall", new Vector3(-21.2f, 0.7f, 28), new Vector3(0.5f, 1.4f, 6.5f));
            AddPBTorch("Torch_WestLanding", new Vector3(-18, 3.0f, 28), 12f);

            // Low Winding Arcade (x=-18, y=0m, z=31 to 51)
            CreatePBWalkway("PB_Path2_LowArcade_Floor", new Vector3(-18, 0, 41), new Vector3(5, 1, 20));
            CreatePBParapet("PB_Path2_LowArcade_WestWall", new Vector3(-20.7f, 0.7f, 41), new Vector3(0.5f, 1.4f, 20));
            CreatePBParapet("PB_Path2_LowArcade_EastWall", new Vector3(-15.3f, 0.7f, 41), new Vector3(0.5f, 1.4f, 20));
            CreatePBPillar("PB_Path2_LowCol1", new Vector3(-21.2f, 2.5f, 35), new Vector3(1.0f, 5.0f, 1.0f));
            CreatePBPillar("PB_Path2_LowCol2", new Vector3(-21.2f, 2.5f, 41), new Vector3(1.0f, 5.0f, 1.0f));
            CreatePBPillar("PB_Path2_LowCol3", new Vector3(-21.2f, 2.5f, 47), new Vector3(1.0f, 5.0f, 1.0f));
            AddPBTorch("Torch_WestArcade", new Vector3(-18, 3.0f, 41), 14f);

            // West Ascending Staircase (x=-18, z=51 to 61, rising y=0 to 2.4m)
            CreatePBStaircase("PB_Path2_Stairs", new Vector3(-18, 1.2f, 56), new Vector3(5, 2.4f, 10), 10, Quaternion.identity);
            CreatePBParapet("PB_Path2_Stairs_WestWall", new Vector3(-20.7f, 1.9f, 56), new Vector3(0.5f, 2.4f, 10));
            CreatePBParapet("PB_Path2_Stairs_EastWall", new Vector3(-15.3f, 1.9f, 56), new Vector3(0.5f, 2.4f, 10));

            // High Skybridge East to Citadel (x=-18 to -5, z=61, y=2.4m)
            CreatePBWalkway("PB_Path2_Skybridge_Floor", new Vector3(-11.5f, 2.4f, 61), new Vector3(13, 1, 5));
            CreatePBParapet("PB_Path2_Skybridge_NorthWall", new Vector3(-11.5f, 3.1f, 63.7f), new Vector3(13, 1.4f, 0.5f));
            CreatePBParapet("PB_Path2_Skybridge_SouthWall", new Vector3(-11.5f, 3.1f, 58.3f), new Vector3(13, 1.4f, 0.5f));

            // -------------------------------------------------------------
            // SECTION D: THE CITADEL SANCTUM (Goal Courtyard: y=2.4m, z=59 to 73)
            // -------------------------------------------------------------
            CreatePBWalkway("PB_Citadel_Floor", new Vector3(0, 2.4f, 66), new Vector3(12, 1, 14));
            // South boundary wall between the two approach bridges
            CreatePBParapet("PB_Citadel_SouthWall", new Vector3(0, 3.1f, 58.7f), new Vector3(6, 1.4f, 0.5f));
            // North grand back wall
            CreatePBParapet("PB_Citadel_NorthWall", new Vector3(0, 4.0f, 73.2f), new Vector3(12.5f, 3.5f, 0.6f));
            CreatePBParapet("PB_Citadel_WestWall", new Vector3(-6.2f, 3.1f, 66), new Vector3(0.5f, 1.4f, 14f));
            CreatePBParapet("PB_Citadel_EastWall", new Vector3(6.2f, 3.1f, 66), new Vector3(0.5f, 1.4f, 14f));
            CreatePBPillar("PB_Citadel_PillarL", new Vector3(-4.5f, 5.0f, 71), new Vector3(1.2f, 5.5f, 1.2f));
            CreatePBPillar("PB_Citadel_PillarR", new Vector3(4.5f, 5.0f, 71), new Vector3(1.2f, 5.5f, 1.2f));
            AddPBTorch("Torch_Citadel", new Vector3(0, 5.5f, 66), 18f);

            // -------------------------------------------------------------
            // SECTION E: DEATH ZONE (Trigger boundary in abyss underneath)
            // -------------------------------------------------------------
            GameObject deathZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deathZone.name = "DeathZone";
            deathZone.tag = "DeathZone";
            UnityEngine.Object.DestroyImmediate(deathZone.GetComponent<MeshRenderer>());
            Collider deathCol = deathZone.GetComponent<Collider>();
            deathCol.isTrigger = true;
            deathZone.transform.position = new Vector3(0, -10, 35);
            deathZone.transform.localScale = new Vector3(250, 2, 250);

            // ==========================================
            // 7. ITEM SCATTER (TAGGED 'Interactable' - 40 ITEMS TOTAL)
            // ==========================================
            GameObject prefabType1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item_Type1_TimePlus.prefab");
            GameObject prefabType2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item_Type2_TimeMinus.prefab");
            GameObject prefabType3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item_Type3_ScorePlus.prefab");
            GameObject prefabType4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item_Type4_ScoreMinus.prefab");

            GameObject itemsRoot = new GameObject("Items");

            Action<GameObject, Vector3> SpawnItem = (prefab, pos) => {
                if (prefab == null) return;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = pos;
                inst.transform.SetParent(itemsRoot.transform);
                inst.tag = "Interactable"; // Enforce Interactable tag
            };

            // 1. Trunk Segment (8 items)
            SpawnItem(prefabType3, new Vector3(0f, 1.2f, 6f));       // [1] Score +500
            SpawnItem(prefabType3, new Vector3(0f, 1.2f, 9f));       // [2] Score +500
            SpawnItem(prefabType1, new Vector3(0f, 1.2f, 12f));      // [3] Time +3s
            SpawnItem(prefabType3, new Vector3(-1.5f, 1.2f, 14f));   // [4] Score +500
            SpawnItem(prefabType4, new Vector3(1.5f, 1.2f, 14f));    // [5] Hazard: Score -500
            SpawnItem(prefabType3, new Vector3(0f, 1.2f, 17f));      // [6] Score +500
            SpawnItem(prefabType1, new Vector3(0f, 1.2f, 20f));      // [7] Time +3s
            SpawnItem(prefabType3, new Vector3(0f, 1.2f, 23f));      // [8] Score +500

            // 2. Crossroads Bastion (2 items)
            SpawnItem(prefabType1, new Vector3(-3.0f, 1.2f, 28f));   // [9] Time +3s (leads West)
            SpawnItem(prefabType1, new Vector3(3.0f, 1.2f, 28f));    // [10] Time +3s (leads East)

            // 3. Path 1: East High Causeway (14 items)
            // 3A. East Traverse (3 items)
            SpawnItem(prefabType3, new Vector3(8.0f, 1.2f, 28f));    // [11] Score +500
            SpawnItem(prefabType3, new Vector3(12.0f, 1.2f, 28f));   // [12] Score +500
            SpawnItem(prefabType2, new Vector3(15.5f, 1.2f, 28f));   // [13] Hazard: Time -5s
            // 3B. East Grand Stairs (3 items)
            SpawnItem(prefabType3, new Vector3(18.0f, 1.7f, 33f));   // [14] Score +500
            SpawnItem(prefabType1, new Vector3(18.0f, 2.3f, 36f));   // [15] Time +3s
            SpawnItem(prefabType3, new Vector3(18.0f, 2.9f, 39f));   // [16] Score +500
            // 3C. High Rampart (5 items)
            SpawnItem(prefabType3, new Vector3(18.0f, 3.6f, 43f));   // [17] Score +500
            SpawnItem(prefabType3, new Vector3(18.0f, 3.6f, 47f));   // [18] Score +500
            SpawnItem(prefabType4, new Vector3(18.0f, 3.6f, 51f));   // [19] Hazard: Score -500
            SpawnItem(prefabType3, new Vector3(18.0f, 3.6f, 55f));   // [20] Score +500
            SpawnItem(prefabType3, new Vector3(18.0f, 3.6f, 59f));   // [21] Score +500
            // 3D. Skybridge West to Citadel (3 items)
            SpawnItem(prefabType3, new Vector3(15.0f, 3.6f, 61f));   // [22] Score +500
            SpawnItem(prefabType1, new Vector3(11.5f, 3.6f, 61f));   // [23] Time +3s
            SpawnItem(prefabType3, new Vector3(8.0f, 3.6f, 61f));    // [24] Score +500

            // 4. Path 2: West Sunken Arcade (14 items)
            // 4A. West Traverse (3 items)
            SpawnItem(prefabType3, new Vector3(-8.0f, 1.2f, 28f));   // [25] Score +500
            SpawnItem(prefabType3, new Vector3(-12.0f, 1.2f, 28f));  // [26] Score +500
            SpawnItem(prefabType2, new Vector3(-15.5f, 1.2f, 28f));  // [27] Hazard: Time -5s
            // 4B. Low Arcade (5 items)
            SpawnItem(prefabType3, new Vector3(-18.0f, 1.2f, 33f));  // [28] Score +500
            SpawnItem(prefabType3, new Vector3(-18.0f, 1.2f, 37f));  // [29] Score +500
            SpawnItem(prefabType1, new Vector3(-18.0f, 1.2f, 41f));  // [30] Time +3s
            SpawnItem(prefabType3, new Vector3(-18.0f, 1.2f, 45f));  // [31] Score +500
            SpawnItem(prefabType3, new Vector3(-18.0f, 1.2f, 49f));  // [32] Score +500
            // 4C. West Ascending Stairs (3 items)
            SpawnItem(prefabType3, new Vector3(-18.0f, 1.7f, 53f));  // [33] Score +500
            SpawnItem(prefabType4, new Vector3(-18.0f, 2.3f, 56f));  // [34] Hazard: Score -500
            SpawnItem(prefabType3, new Vector3(-18.0f, 2.9f, 59f));  // [35] Score +500
            // 4D. Skybridge East to Citadel (3 items)
            SpawnItem(prefabType3, new Vector3(-15.0f, 3.6f, 61f));  // [36] Score +500
            SpawnItem(prefabType1, new Vector3(-11.5f, 3.6f, 61f));  // [37] Time +3s
            SpawnItem(prefabType3, new Vector3(-8.0f, 3.6f, 61f));   // [38] Score +500

            // 5. The Citadel Sanctum Goal (2 items)
            SpawnItem(prefabType3, new Vector3(-2.0f, 3.6f, 67f));   // [39] Score +500
            SpawnItem(prefabType3, new Vector3(2.0f, 3.6f, 67f));    // [40] Score +500

            // Verification of Item Values:
            // 25 Type 3 (Score +500) items = 12,500 points available (exceeds 10,000 threshold)
            // 8 Type 1 (Time +3s) items = +24s bonus time
            // 4 Hazard items (Type 2 & 4) to challenge player movement

            // ==========================================
            // 8. FINALIZE & SAVE SCENE
            // ==========================================
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Stage 2 ProBuilder] Rebuilt Stage 2 with ProBuilder 2-path Fortress Causeway, solid colliders, Cinemachine clearance, 40 items, and 1080p HUD!");
        }
    }
}
