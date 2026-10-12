using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace NatchapholAunjai
{
    public class Stage1Builder
    {
        [MenuItem("Tools/Build Stage 1 Map")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage 1.unity", OpenSceneMode.Single);

            // ==========================================
            // 1. CREATE / VERIFY MATERIALS
            // ==========================================
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            System.Func<string, Color, Material> GetOrCreateMaterial = (matPath, color) => {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (m == null)
                {
                    m = new Material(shader);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                    else m.color = color;
                    AssetDatabase.CreateAsset(m, matPath);
                }
                return m;
            };

            Material caveMat = GetOrCreateMaterial("Assets/Materials/CaveStone_Mat.mat", new Color(0.25f, 0.2f, 0.15f));
            Material timePlusMat = GetOrCreateMaterial("Assets/Materials/Item_TimePlus_Mat.mat", new Color(0.1f, 0.9f, 0.3f));     // Type 1: Green (+3s)
            Material timeMinusMat = GetOrCreateMaterial("Assets/Materials/Item_TimeMinus_Mat.mat", new Color(1f, 0.15f, 0.15f));   // Type 2: Red (-5s)
            Material scorePlusMat = GetOrCreateMaterial("Assets/Materials/Item_ScorePlus_Mat.mat", new Color(1f, 0.85f, 0.1f));    // Type 3: Gold (+500pts)
            Material scoreMinusMat = GetOrCreateMaterial("Assets/Materials/Item_ScoreMinus_Mat.mat", new Color(0.65f, 0.15f, 0.85f)); // Type 4: Purple (-500pts)

            // Ensure "DeathZone" Tag exists
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool foundTag = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("DeathZone")) { foundTag = true; break; }
            }
            if (!foundTag)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "DeathZone";
                tagManager.ApplyModifiedProperties();
            }

            // Environment Dim Lighting
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) l.intensity = 0.35f;
            }

            // Clean previous Map geometry
            GameObject oldMap = GameObject.Find("Map");
            if (oldMap != null) Object.DestroyImmediate(oldMap);

            GameObject mapRoot = new GameObject("Map");

            // ProBuilder Reflection Generator
            System.Type shapeGen = System.Type.GetType("UnityEngine.ProBuilder.ShapeGenerator, Unity.ProBuilder");
            System.Type pivotLoc = System.Type.GetType("UnityEngine.ProBuilder.PivotLocation, Unity.ProBuilder");

            System.Func<string, Vector3, Vector3, GameObject> CreatePlatform = (name, position, size) => {
                GameObject go = null;
                if (shapeGen != null && pivotLoc != null)
                {
                    var method = shapeGen.GetMethod("GenerateCube", new System.Type[] { pivotLoc, typeof(Vector3) });
                    if (method != null)
                    {
                        object pivotObj = System.Enum.ToObject(pivotLoc, 0); // Center
                        var pbMesh = method.Invoke(null, new object[] { pivotObj, size });
                        if (pbMesh != null)
                        {
                            var prop = pbMesh.GetType().GetProperty("gameObject");
                            if (prop != null) go = prop.GetValue(pbMesh) as GameObject;
                        }
                    }
                }
                if (go == null)
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = size;
                }
                go.name = name;
                go.transform.position = position;
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = caveMat;
                go.transform.SetParent(mapRoot.transform);

                // Add solid collision so player doesn't fall through the floor
                Collider col = go.GetComponent<Collider>();
                if (col == null)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                    else
                    {
                        BoxCollider bc = go.AddComponent<BoxCollider>();
                        bc.size = size;
                    }
                }

                return go;
            };

            // ==========================================
            // COMPLEX PROBUILDER LEVEL ARCHITECTURE
            // ==========================================
            // 1. Entrance Area
            CreatePlatform("Entrance_Platform", new Vector3(0, 0, 0), new Vector3(8, 1, 8));

            // 2. Ruined Bridge with gaps and stepping island
            CreatePlatform("Bridge_Segment1", new Vector3(0, 0, 7.5f), new Vector3(2.2f, 1, 7f));
            CreatePlatform("Bridge_IslandPillar", new Vector3(1.5f, 0.4f, 13f), new Vector3(3.2f, 1.2f, 3.2f));
            CreatePlatform("Bridge_Segment2", new Vector3(-1f, 0, 18.5f), new Vector3(1.8f, 1, 7f));

            // 3. Main Tomb Chamber (Central Hub)
            CreatePlatform("TombChamber_Floor", new Vector3(0, 0, 28f), new Vector3(16f, 1f, 12f));

            // Pillars & Obstacles
            CreatePlatform("Pillar_SW", new Vector3(-5.5f, 3.5f, 24.5f), new Vector3(2f, 6f, 2f));
            CreatePlatform("Pillar_SE", new Vector3(5.5f, 3.5f, 24.5f), new Vector3(2f, 6f, 2f));
            CreatePlatform("Pillar_NW", new Vector3(-5.5f, 3.5f, 31.5f), new Vector3(2f, 6f, 2f));
            CreatePlatform("Pillar_NE", new Vector3(5.5f, 3.5f, 31.5f), new Vector3(2f, 6f, 2f));
            CreatePlatform("Ruin_DebrisBlock", new Vector3(0f, 1.5f, 28f), new Vector3(3.5f, 2f, 3f));
            CreatePlatform("Collapsed_ArchBlock", new Vector3(-2f, 1.2f, 25.5f), new Vector3(1.6f, 1.4f, 4f));

            // Stairs to North Terrace
            CreatePlatform("Stairs_Step1", new Vector3(0f, 0.6f, 35f), new Vector3(6f, 0.6f, 2f));
            CreatePlatform("Stairs_Step2", new Vector3(0f, 1.2f, 37f), new Vector3(6f, 0.6f, 2f));
            CreatePlatform("Stairs_Step3", new Vector3(0f, 1.8f, 39f), new Vector3(6f, 0.6f, 2f));
            CreatePlatform("NorthTerrace_HighPlatform", new Vector3(0f, 2.4f, 44f), new Vector3(12f, 1f, 8f));

            // Branch 1 (West): Zig-Zag Balance Catwalk
            CreatePlatform("LedgeWest_Beam", new Vector3(-11f, 0f, 26f), new Vector3(6f, 1f, 1.6f));
            CreatePlatform("LedgeWest_Corner", new Vector3(-15f, 0.3f, 29.5f), new Vector3(2.2f, 1f, 6f));
            CreatePlatform("LedgeWest_Pedestal", new Vector3(-18f, 0.8f, 34f), new Vector3(3f, 1.2f, 3f));

            // Branch 2 (East): Precarious Stepping Stones
            CreatePlatform("LedgeEast_Step1", new Vector3(10.5f, 0f, 29f), new Vector3(2.4f, 1f, 2.4f));
            CreatePlatform("LedgeEast_Step2", new Vector3(14.5f, 0.6f, 31.5f), new Vector3(2.2f, 1.2f, 2.2f));
            CreatePlatform("LedgeEast_Step3", new Vector3(18.5f, 1.2f, 34f), new Vector3(2f, 1.4f, 2f));
            CreatePlatform("LedgeEast_Pedestal", new Vector3(22.5f, 1.8f, 37f), new Vector3(3.2f, 1.6f, 3.2f));

            // Branch 3 (North): The High Sanctuary Catwalk
            CreatePlatform("LedgeNorth_Spine1", new Vector3(0f, 2.7f, 50f), new Vector3(1.5f, 0.8f, 4f));
            CreatePlatform("LedgeNorth_Spine2", new Vector3(0f, 3.2f, 54.5f), new Vector3(1.2f, 0.8f, 5f));
            CreatePlatform("HighSanctuary_Pedestal", new Vector3(0f, 3.8f, 59.5f), new Vector3(3.8f, 1.5f, 3.8f));

            // Torches
            System.Action<string, Vector3, float> AddCaveTorch = (name, pos, range) => {
                GameObject tGo = new GameObject(name);
                tGo.transform.position = pos;
                tGo.transform.SetParent(mapRoot.transform);
                Light l = tGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = range;
                l.color = new Color(1f, 0.62f, 0.25f);
                l.intensity = 2.2f;
            };
            AddCaveTorch("Torch_Entrance", new Vector3(0, 3f, 0), 14f);
            AddCaveTorch("Torch_BridgeIsland", new Vector3(1.5f, 3f, 13f), 12f);
            AddCaveTorch("Torch_TombChamber", new Vector3(0, 5.5f, 28f), 22f);
            AddCaveTorch("Torch_LedgeWest", new Vector3(-18f, 3.2f, 34f), 10f);
            AddCaveTorch("Torch_LedgeEast", new Vector3(22.5f, 4f, 37f), 10f);
            AddCaveTorch("Torch_HighSanctuary", new Vector3(0, 6f, 59.5f), 14f);

            // Death Zone
            GameObject deathZone = GameObject.Find("DeathZone");
            if (deathZone == null)
            {
                deathZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deathZone.name = "DeathZone";
                deathZone.tag = "DeathZone";
                Object.DestroyImmediate(deathZone.GetComponent<MeshRenderer>());
                Collider col = deathZone.GetComponent<Collider>();
                col.isTrigger = true;
            }
            deathZone.transform.position = new Vector3(0, -10, 30);
            deathZone.transform.localScale = new Vector3(250, 2, 250);

            // ==========================================
            // 2. CREATE 4 PROBUILDER ITEM PREFABS
            // ==========================================
            System.Func<string, ItemType, Material, GameObject> CreateItemPrefab = (prefabName, type, mat) => {
                string path = "Assets/Prefabs/" + prefabName + ".prefab";
                GameObject tempGo = null;
                if (shapeGen != null && pivotLoc != null)
                {
                    var method = shapeGen.GetMethod("GenerateCube", new System.Type[] { pivotLoc, typeof(Vector3) });
                    if (method != null)
                    {
                        object pivotObj = System.Enum.ToObject(pivotLoc, 0);
                        var pbMesh = method.Invoke(null, new object[] { pivotObj, new Vector3(0.5f, 0.5f, 0.5f) });
                        if (pbMesh != null)
                        {
                            var prop = pbMesh.GetType().GetProperty("gameObject");
                            if (prop != null) tempGo = prop.GetValue(pbMesh) as GameObject;
                        }
                    }
                }
                if (tempGo == null)
                {
                    tempGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tempGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }

                tempGo.name = prefabName;
                tempGo.tag = "Item";

                // Ensure Trigger Collider
                Collider col = tempGo.GetComponent<Collider>();
                if (col == null) col = tempGo.AddComponent<BoxCollider>();
                col.isTrigger = true;

                // Renderer & Material
                MeshRenderer mr = tempGo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;

                // Attach & Configure Item Script
                Item itemComp = tempGo.GetComponent<Item>();
                if (itemComp == null) itemComp = tempGo.AddComponent<Item>();
                itemComp.itemType = type;

                // Save Prefab
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGo, path);
                Object.DestroyImmediate(tempGo);
                return prefab;
            };

            GameObject prefabType1 = CreateItemPrefab("Item_Type1_TimePlus", ItemType.AddTime, timePlusMat);
            GameObject prefabType2 = CreateItemPrefab("Item_Type2_TimeMinus", ItemType.SubtractTime, timeMinusMat);
            GameObject prefabType3 = CreateItemPrefab("Item_Type3_ScorePlus", ItemType.AddScore, scorePlusMat);
            GameObject prefabType4 = CreateItemPrefab("Item_Type4_ScoreMinus", ItemType.SubtractScore, scoreMinusMat);

            // ==========================================
            // 3. CLEAR OLD ITEMS & SCATTER NEW ONES
            // ==========================================
            GameObject oldItemsRoot = GameObject.Find("Items");
            if (oldItemsRoot != null) Object.DestroyImmediate(oldItemsRoot);

            // Also destroy any legacy "Item_1", "Item_2", "Item_3"
            for (int i = 1; i <= 10; i++)
            {
                var legacyItem = GameObject.Find("Item_" + i);
                if (legacyItem != null) Object.DestroyImmediate(legacyItem);
            }

            GameObject itemsRoot = new GameObject("Items");

            System.Action<GameObject, Vector3> SpawnItem = (prefab, pos) => {
                if (prefab == null) return;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.SetParent(itemsRoot.transform);
            };

            // --- STRATEGIC PLACEMENT ---
            // To achieve 10,000 score, we need at least 20 Type 3 items (+500 each).
            // We provide 26 Type 3 items (total 13,000 available score).
            // We provide 8 Type 1 items (+3s time bonus) to help players extend the 30s limit.
            // We provide 6 Type 2 items (-5s hazard) and 6 Type 4 items (-500pts hazard) to navigate around!

            // [Entrance & Bridge]
            SpawnItem(prefabType3, new Vector3(0, 1.2f, 2f));       // Score +500
            SpawnItem(prefabType1, new Vector3(0, 1.2f, 6f));       // Time +3s
            SpawnItem(prefabType3, new Vector3(1.5f, 1.5f, 13f));   // Score +500 (Bridge Island)
            SpawnItem(prefabType4, new Vector3(0.5f, 1.2f, 10f));   // Hazard: Score -500 (Near gap)
            SpawnItem(prefabType2, new Vector3(-1f, 1.2f, 17f));    // Hazard: Time -5s (Narrow beam)
            SpawnItem(prefabType3, new Vector3(-1f, 1.2f, 20f));    // Score +500

            // [Main Tomb Chamber - Floor & Pillars]
            SpawnItem(prefabType3, new Vector3(-4f, 1.2f, 24f));    // Score +500
            SpawnItem(prefabType3, new Vector3(4f, 1.2f, 24f));     // Score +500
            SpawnItem(prefabType1, new Vector3(0f, 1.2f, 25f));     // Time +3s
            SpawnItem(prefabType3, new Vector3(-3f, 1.2f, 28f));    // Score +500
            SpawnItem(prefabType3, new Vector3(3f, 1.2f, 28f));     // Score +500
            SpawnItem(prefabType4, new Vector3(-1f, 1.2f, 27f));    // Hazard: Score -500
            SpawnItem(prefabType2, new Vector3(1.5f, 1.2f, 29f));   // Hazard: Time -5s
            SpawnItem(prefabType3, new Vector3(-4f, 1.2f, 32f));    // Score +500
            SpawnItem(prefabType3, new Vector3(4f, 1.2f, 32f));     // Score +500
            SpawnItem(prefabType1, new Vector3(0f, 1.2f, 31f));     // Time +3s

            // [Stairs & North Terrace]
            SpawnItem(prefabType3, new Vector3(0f, 1.8f, 35f));     // Score +500
            SpawnItem(prefabType3, new Vector3(0f, 2.4f, 37f));     // Score +500
            SpawnItem(prefabType1, new Vector3(0f, 3.0f, 39f));     // Time +3s
            SpawnItem(prefabType3, new Vector3(-3f, 3.6f, 43f));    // Score +500
            SpawnItem(prefabType3, new Vector3(3f, 3.6f, 43f));     // Score +500
            SpawnItem(prefabType4, new Vector3(0f, 3.6f, 44f));     // Hazard: Score -500
            SpawnItem(prefabType3, new Vector3(-4f, 3.6f, 46f));    // Score +500
            SpawnItem(prefabType3, new Vector3(4f, 3.6f, 46f));     // Score +500

            // [Branch 1: West Wing - Zig-Zag Catwalk]
            SpawnItem(prefabType3, new Vector3(-9f, 1.2f, 26f));    // Score +500
            SpawnItem(prefabType2, new Vector3(-12f, 1.2f, 26f));   // Hazard: Time -5s
            SpawnItem(prefabType3, new Vector3(-15f, 1.5f, 28f));   // Score +500
            SpawnItem(prefabType1, new Vector3(-15f, 1.5f, 31f));   // Time +3s
            SpawnItem(prefabType3, new Vector3(-18f, 2.2f, 34f));   // Score +500 (Pedestal)
            SpawnItem(prefabType3, new Vector3(-17.5f, 2.2f, 33.5f));// Score +500 (Bonus on Pedestal)

            // [Branch 2: East Wing - Precarious Stepping Stones]
            SpawnItem(prefabType3, new Vector3(10.5f, 1.2f, 29f));  // Score +500
            SpawnItem(prefabType4, new Vector3(12.5f, 1.5f, 30f));  // Hazard: Score -500
            SpawnItem(prefabType3, new Vector3(14.5f, 1.8f, 31.5f));// Score +500
            SpawnItem(prefabType1, new Vector3(18.5f, 2.4f, 34f));  // Time +3s
            SpawnItem(prefabType3, new Vector3(22.5f, 3.2f, 37f));  // Score +500 (Pedestal)
            SpawnItem(prefabType3, new Vector3(22f, 3.2f, 36.5f));  // Score +500 (Bonus on Pedestal)

            // [Branch 3: North Sanctuary - High Spine over the Void]
            SpawnItem(prefabType3, new Vector3(0f, 3.8f, 49f));     // Score +500
            SpawnItem(prefabType2, new Vector3(0f, 4.0f, 52f));     // Hazard: Time -5s
            SpawnItem(prefabType1, new Vector3(0f, 4.4f, 55f));     // Time +3s
            SpawnItem(prefabType3, new Vector3(0f, 5.2f, 58.5f));   // Score +500
            SpawnItem(prefabType3, new Vector3(0f, 5.2f, 60.5f));   // Score +500 (Sanctuary Altar)
            SpawnItem(prefabType3, new Vector3(-1f, 5.2f, 59.5f));  // Score +500 (Sanctuary Altar)
            SpawnItem(prefabType3, new Vector3(1f, 5.2f, 59.5f));   // Score +500 (Sanctuary Altar)

            // Reset Player Spawn
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = new Vector3(0, 2f, 0);
                if (cc != null) cc.enabled = true;
            }

            // Sync GameManager Settings in Scene
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.timeLimit = 30f;
                gm.winScoreThreshold = 10000;
                EditorUtility.SetDirty(gm);
            }

            // Fix Player Visor and Cinemachine Camera rotation binding
            FixPlayerAndCameraSetup(scene);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 1 Map, 4 ProBuilder Item Prefabs, and Item Scatter generated successfully!");
        }

        [MenuItem("Tools/Fix Player Rotation & Camera")]
        public static void FixPlayerAndCameraMenu()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage 1.unity", OpenSceneMode.Single);
            FixPlayerAndCameraSetup(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage 1] Player rotation and Cinemachine camera setup fixed!");
        }

        public static void FixPlayerAndCameraSetup(Scene scene)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) return;

            // 1. Update CapsulePlayer mouseSensitivity to 50
            CapsulePlayer cp = player.GetComponent<CapsulePlayer>();
            if (cp != null)
            {
                cp.mouseSensitivity = 50f;
                EditorUtility.SetDirty(cp);
            }

            // 2. Add Visual Visor/Face to Player so rotation is clearly visible
            Transform existingVisor = player.transform.Find("Player_Visor");
            if (existingVisor == null)
            {
                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Player_Visor";
                visor.transform.SetParent(player.transform);
                visor.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
                visor.transform.localRotation = Quaternion.identity;
                visor.transform.localScale = new Vector3(0.5f, 0.2f, 0.25f);

                // Remove collider from cosmetic visor
                Collider vc = visor.GetComponent<Collider>();
                if (vc != null) Object.DestroyImmediate(vc);

                // Visor Material (Cyan Glass)
                Material visorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Player_Visor_Mat.mat");
                if (visorMat == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Lit");
                    if (s == null) s = Shader.Find("Standard");
                    visorMat = new Material(s);
                    if (visorMat.HasProperty("_BaseColor")) visorMat.SetColor("_BaseColor", new Color(0.1f, 0.7f, 1f));
                    else visorMat.color = new Color(0.1f, 0.7f, 1f);
                    AssetDatabase.CreateAsset(visorMat, "Assets/Materials/Player_Visor_Mat.mat");
                }
                var vr = visor.GetComponent<MeshRenderer>();
                if (vr != null) vr.sharedMaterial = visorMat;
            }

            // 3. Configure Cinemachine Camera
            GameObject vcamGo = GameObject.Find("PlayerVCam");
            if (vcamGo == null)
            {
                vcamGo = new GameObject("PlayerVCam");
            }

            CinemachineCamera vcam = vcamGo.GetComponent<CinemachineCamera>();
            if (vcam == null) vcam = vcamGo.AddComponent<CinemachineCamera>();

            vcam.Follow = player.transform;
            vcam.LookAt = player.transform;

            // Transposer (Body) - Position behind player with low damping
            CinemachineTransposer transposer = vcamGo.GetComponent<CinemachineTransposer>();
            if (transposer == null) transposer = vcamGo.AddComponent<CinemachineTransposer>();

            transposer.m_BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = new Vector3(0f, 2.5f, -5f);
            transposer.m_XDamping = 0.05f;
            transposer.m_YDamping = 0.05f;
            transposer.m_ZDamping = 0.05f;
            transposer.m_YawDamping = 0f;
            transposer.m_PitchDamping = 0f;
            transposer.m_RollDamping = 0f;

            // Rotation Composer (Aim) - Ensures camera always looks at the player and turns synchronously
            CinemachineRotationComposer composer = vcamGo.GetComponent<CinemachineRotationComposer>();
            if (composer == null) composer = vcamGo.AddComponent<CinemachineRotationComposer>();

            composer.TargetOffset = new Vector3(0f, 1.2f, 0f);
            composer.Damping = Vector2.zero; // Instant aim, no delayed orbit
            composer.CenterOnActivate = true;

            EditorUtility.SetDirty(vcamGo);
            EditorUtility.SetDirty(player);
        }

        [MenuItem("Tools/Add Floor Colliders to Stage 1")]
        public static void AddCollidersToExistingMap()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage 1.unity", OpenSceneMode.Single);
            int count = FixMapColliders();
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"[Stage 1] Successfully verified and added colliders to {count} floor platforms!");
        }

        public static int FixMapColliders()
        {
            GameObject map = GameObject.Find("Map");
            if (map == null) return 0;

            int count = 0;
            foreach (Transform child in map.GetComponentsInChildren<Transform>(true))
            {
                if (child == map.transform) continue;
                if (child.GetComponent<Light>() != null) continue; // Skip light objects

                Collider col = child.GetComponent<Collider>();
                if (col == null)
                {
                    MeshFilter mf = child.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = child.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                    else
                    {
                        child.gameObject.AddComponent<BoxCollider>();
                    }
                    count++;
                }
            }
            return count;
        }
    }
}
