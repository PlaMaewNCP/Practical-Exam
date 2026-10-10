using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace NatchapholAunjai
{
    public class Stage1Builder
    {
        [MenuItem("Tools/Build Stage 1 Map")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage 1.unity", OpenSceneMode.Single);

            // 1. Create CaveStone Material
            Material caveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CaveStone_Mat.mat");
            if (caveMat == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                    
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                caveMat = new Material(shader);
                if (caveMat.HasProperty("_BaseColor"))
                    caveMat.SetColor("_BaseColor", new Color(0.25f, 0.2f, 0.15f)); // URP
                else
                    caveMat.color = new Color(0.25f, 0.2f, 0.15f); // Standard

                AssetDatabase.CreateAsset(caveMat, "Assets/Materials/CaveStone_Mat.mat");
            }

            // 2. Add Tag "DeathZone"
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("DeathZone")) { found = true; break; }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "DeathZone";
                tagManager.ApplyModifiedProperties();
            }

            // 3. Environment Lighting
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) l.intensity = 0.4f; // Dim the main light
            }

            GameObject mapRoot = GameObject.Find("Map");
            if (mapRoot == null) mapRoot = new GameObject("Map");

            // 4. ProBuilder Generation using Reflection (so it compiles even if ProBuilder is missing in this assembly)
            System.Type shapeGen = System.Type.GetType("UnityEngine.ProBuilder.ShapeGenerator, Unity.ProBuilder");
            System.Type pivotLoc = System.Type.GetType("UnityEngine.ProBuilder.PivotLocation, Unity.ProBuilder");
            
            System.Func<string, Vector3, Vector3, GameObject> CreateCube = (name, position, size) => {
                GameObject go = null;
                if (shapeGen != null && pivotLoc != null)
                {
                    var method = shapeGen.GetMethod("GenerateCube", new System.Type[] { pivotLoc, typeof(Vector3) });
                    if (method != null)
                    {
                        object pivotObj = System.Enum.ToObject(pivotLoc, 0); // PivotLocation.Center
                        var pbMesh = method.Invoke(null, new object[] { pivotObj, size });
                        if (pbMesh != null)
                        {
                            var prop = pbMesh.GetType().GetProperty("gameObject");
                            if (prop != null) go = prop.GetValue(pbMesh) as GameObject;
                        }
                    }
                }
                
                // Fallback to Primitive if ProBuilder API is missing or fails
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
                
                return go;
            };

            // Entrance (Wide)
            CreateCube("Entrance", new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // Ruined Bridge (Narrow)
            CreateCube("RuinedBridge", new Vector3(0, 0, 12.5f), new Vector3(2, 1, 15));

            // Tomb Chamber (Wide)
            CreateCube("TombChamber", new Vector3(0, 0, 30), new Vector3(20, 1, 20));

            // Pillars in Tomb Chamber
            CreateCube("Pillar1", new Vector3(-5, 3.5f, 25), new Vector3(2, 6, 2));
            CreateCube("Pillar2", new Vector3(5, 3.5f, 25), new Vector3(2, 6, 2));
            CreateCube("Pillar3", new Vector3(-5, 3.5f, 35), new Vector3(2, 6, 2));
            CreateCube("Pillar4", new Vector3(5, 3.5f, 35), new Vector3(2, 6, 2));
            CreateCube("Debris", new Vector3(0, 1.5f, 30), new Vector3(3, 2, 3));

            // Treasure Ledges (Narrow)
            CreateCube("Ledge1", new Vector3(-15, 0, 30), new Vector3(10, 1, 2));
            CreateCube("Ledge2", new Vector3(15, 0, 30), new Vector3(10, 1, 2));
            CreateCube("Ledge3", new Vector3(0, 0, 45), new Vector3(2, 1, 10));

            // 5. Warm torch-like point light
            GameObject torchLightGo = new GameObject("TorchLight");
            torchLightGo.transform.position = new Vector3(0, 5, 30);
            torchLightGo.transform.SetParent(mapRoot.transform);
            Light torchLight = torchLightGo.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.range = 25;
            torchLight.color = new Color(1f, 0.6f, 0.2f);
            torchLight.intensity = 2f;

            // 6. Death Zone
            GameObject deathZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deathZone.name = "DeathZone";
            deathZone.transform.position = new Vector3(0, -10, 20);
            deathZone.transform.localScale = new Vector3(100, 2, 100);
            deathZone.tag = "DeathZone";
            Object.DestroyImmediate(deathZone.GetComponent<MeshRenderer>()); // Invisible
            Collider col = deathZone.GetComponent<Collider>();
            col.isTrigger = true;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Stage 1 Map Built Successfully!");
        }
    }
}
