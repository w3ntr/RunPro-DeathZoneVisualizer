using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(RunProTools.Core), "Death Zone Visualizer", "2.8.0", "w3ntr")]
[assembly: MelonGame(null, null)]

namespace RunProTools
{
    public class Core : MelonMod
    {
        private static Material vizMaterial;
        private static List<GameObject> overlays = new List<GameObject>();

        // Настройки
        private static bool isVisualsOn = true;
        private static bool xRay = true;
        private static bool wireframeMode = false;
        private static float transparency = 0.5f;
        private static float colorR = 1.0f;
        private static float colorG = 0.0f;
        private static float colorB = 0.0f;

        private static KeyCode toggleKey = KeyCode.F2;
        private static KeyCode menuKey = KeyCode.F4;
        private bool showMenu = false;

        private bool isBindingToggle = false;
        private bool isBindingMenu = false;

        private Rect windowRect = new Rect(100, 100, 260, 450);

        // MelonPreferences (.cfg)
        private MelonPreferences_Category prefCategory;
        private MelonPreferences_Entry<bool> prefVisualsOn;
        private MelonPreferences_Entry<bool> prefXRay;
        private MelonPreferences_Entry<bool> prefWireframe;
        private MelonPreferences_Entry<float> prefTransparency;
        private MelonPreferences_Entry<float> prefColorR;
        private MelonPreferences_Entry<float> prefColorG;
        private MelonPreferences_Entry<float> prefColorB;
        private MelonPreferences_Entry<KeyCode> prefToggleKey;
        private MelonPreferences_Entry<KeyCode> prefMenuKey;

        public override void OnInitializeMelon()
        {
            InitConfig();
            CreateMaterial();
        }

        private void InitConfig()
        {
            prefCategory = MelonPreferences.CreateCategory("DeathZoneVisualizer", "Death Zone Visualizer");
            prefVisualsOn = prefCategory.CreateEntry("VisualsOn", true);
            prefXRay = prefCategory.CreateEntry("XRay", true);
            prefWireframe = prefCategory.CreateEntry("Wireframe", false);
            prefTransparency = prefCategory.CreateEntry("Transparency", 0.5f);
            prefColorR = prefCategory.CreateEntry("ColorR", 1.0f);
            prefColorG = prefCategory.CreateEntry("ColorG", 0.0f);
            prefColorB = prefCategory.CreateEntry("ColorB", 0.0f);
            prefToggleKey = prefCategory.CreateEntry("ToggleKey", KeyCode.F2);
            prefMenuKey = prefCategory.CreateEntry("MenuKey", KeyCode.F4);

            isVisualsOn = prefVisualsOn.Value;
            xRay = prefXRay.Value;
            wireframeMode = prefWireframe.Value;
            transparency = prefTransparency.Value;
            colorR = prefColorR.Value;
            colorG = prefColorG.Value;
            colorB = prefColorB.Value;
            toggleKey = prefToggleKey.Value;
            menuKey = prefMenuKey.Value;
        }

        private void SaveConfig()
        {
            prefVisualsOn.Value = isVisualsOn;
            prefXRay.Value = xRay;
            prefWireframe.Value = wireframeMode;
            prefTransparency.Value = transparency;
            prefColorR.Value = colorR;
            prefColorG.Value = colorG;
            prefColorB.Value = colorB;
            prefToggleKey.Value = toggleKey;
            prefMenuKey.Value = menuKey;

            prefCategory.SaveToFile();
        }

        private static void CreateMaterial()
        {
            // Используем гарантированно работающий внутренний шейдер Unity
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("GUI/Text Shader");

            vizMaterial = new Material(shader);
            UpdateMaterialProperties();
        }

        private static void UpdateMaterialProperties()
        {
            if (vizMaterial == null) return;

            vizMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            vizMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            vizMaterial.SetInt("_Cull", (int)CullMode.Off);
            vizMaterial.SetInt("_ZWrite", 0);

            // X-Ray и глубина отрисовки
            vizMaterial.SetInt("_ZTest", xRay ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);
            vizMaterial.renderQueue = xRay ? 5000 : 3000;

            vizMaterial.color = new Color(colorR, colorG, colorB, transparency);
        }

        public override void OnUpdate()
        {
            if (isBindingToggle || isBindingMenu)
            {
                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kcode))
                    {
                        if (isBindingToggle) { toggleKey = kcode; isBindingToggle = false; }
                        else if (isBindingMenu) { menuKey = kcode; isBindingMenu = false; }
                        SaveConfig();
                        break;
                    }
                }
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                isVisualsOn = !isVisualsOn;
                RefreshVisuals();
                SaveConfig();
            }

            if (Input.GetKeyDown(menuKey) || Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showMenu;
                if (!showMenu) SaveConfig();
            }
        }

        public override void OnGUI()
        {
            if (!showMenu) return;

            GUI.Box(windowRect, "");
            windowRect = GUI.Window(8888, windowRect, (GUI.WindowFunction)DrawWindow, "DeathZone Settings (v2.8.0)");
        }

        private void DrawWindow(int id)
        {
            float x = 15;
            float y = 25;
            float w = 230;

            string btnText = isVisualsOn ? "Visuals: ON" : "Visuals: OFF";
            if (GUI.Button(new Rect(x, y, w, 25), btnText))
            {
                isVisualsOn = !isVisualsOn;
                RefreshVisuals();
            }

            y += 30;
            bool newXRay = GUI.Toggle(new Rect(x, y, w, 20), xRay, " X-Ray (Through Walls)");
            if (newXRay != xRay)
            {
                xRay = newXRay;
                UpdateMaterialProperties();
            }

            y += 22;
            bool newWire = GUI.Toggle(new Rect(x, y, w, 20), wireframeMode, " Wireframe Mode");
            if (newWire != wireframeMode)
            {
                wireframeMode = newWire;
                RefreshVisuals();
            }

            y += 25;
            GUI.Label(new Rect(x, y, w, 20), $"Color (R: {(int)(colorR * 255)}, G: {(int)(colorG * 255)}, B: {(int)(colorB * 255)})");
            y += 18;
            float nr = GUI.HorizontalSlider(new Rect(x, y, w, 15), colorR, 0f, 1f);
            y += 16;
            float ng = GUI.HorizontalSlider(new Rect(x, y, w, 15), colorG, 0f, 1f);
            y += 16;
            float nb = GUI.HorizontalSlider(new Rect(x, y, w, 15), colorB, 0f, 1f);

            if (Mathf.Abs(nr - colorR) > 0.01f || Mathf.Abs(ng - colorG) > 0.01f || Mathf.Abs(nb - colorB) > 0.01f)
            {
                colorR = nr; colorG = ng; colorB = nb;
                UpdateMaterialProperties();
            }

            y += 22;
            GUI.Label(new Rect(x, y, w, 20), $"Transparency: {(int)(transparency * 100)}%");
            y += 18;
            float newTrans = GUI.HorizontalSlider(new Rect(x, y, w, 15), transparency, 0.05f, 1.0f);
            if (Mathf.Abs(newTrans - transparency) > 0.01f)
            {
                transparency = newTrans;
                UpdateMaterialProperties();
            }

            y += 22;
            string toggleBtnText = isBindingToggle ? "Press Key..." : $"Toggle Key: [{toggleKey}]";
            if (GUI.Button(new Rect(x, y, w, 22), toggleBtnText))
            {
                isBindingToggle = true;
                isBindingMenu = false;
            }

            y += 25;
            string menuBtnText = isBindingMenu ? "Press Key..." : $"Menu Key: [{menuKey}]";
            if (GUI.Button(new Rect(x, y, w, 22), menuBtnText))
            {
                isBindingMenu = true;
                isBindingToggle = false;
            }

            y += 25;
            if (GUI.Button(new Rect(x, y, w, 22), "Force Rescan Map"))
            {
                RefreshVisuals();
            }

            GUI.DragWindow();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            RefreshVisuals();
        }

        private static void RefreshVisuals()
        {
            ClearOverlays();
            if (isVisualsOn) RenderDeathZones();
        }

        private static void ClearOverlays()
        {
            foreach (GameObject obj in overlays)
            {
                if (obj != null) Object.Destroy(obj);
            }
            overlays.Clear();
        }

        private static void RenderDeathZones()
        {
            if (vizMaterial == null) CreateMaterial();

            Collider[] allColliders = Object.FindObjectsOfType<Collider>();

            foreach (Collider col in allColliders)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

                if (IsDeathZone(col))
                {
                    CreateOverlay(col);
                }
            }
        }

        private static bool IsDeathZone(Collider col)
        {
            GameObject go = col.gameObject;
            string name = go.name.ToLower();
            string tag = go.tag.ToLower();
            string layer = LayerMask.LayerToName(go.layer).ToLower();

            // Исключения (Спавны, Финиш, Камера)
            if (name.Contains("player") || name.Contains("start") || name.Contains("finish") ||
                name.Contains("checkpoint") || name.Contains("spawn") || name.Contains("camera"))
                return false;

            // Расширенный поиск по именам/тегам/слоям
            if (name.Contains("death") || name.Contains("kill") || name.Contains("dead") ||
                name.Contains("hazard") || name.Contains("lava") || name.Contains("void") ||
                name.Contains("fall") || name.Contains("trigger") || name.Contains("out") ||
                tag.Contains("death") || tag.Contains("kill") || tag.Contains("hazard") ||
                layer.Contains("death") || layer.Contains("hazard"))
            {
                return true;
            }

            // Поиск по скриптам внутри объекта
            foreach (var script in go.GetComponents<MonoBehaviour>())
            {
                if (script == null) continue;
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("death") || scriptName.Contains("kill") ||
                    scriptName.Contains("hazard") || scriptName.Contains("fall") || scriptName.Contains("damage"))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateOverlay(Collider col)
        {
            GameObject overlayObj;

            if (wireframeMode)
            {
                overlayObj = new GameObject("[VizWire]");
                MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
                mf.sharedMesh = GetWireframeMesh();
                MeshRenderer mr = overlayObj.AddComponent<MeshRenderer>();
                mr.material = vizMaterial;
            }
            else
            {
                overlayObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(overlayObj.GetComponent<Collider>());
                Renderer ren = overlayObj.GetComponent<Renderer>();
                if (ren != null) ren.material = vizMaterial;
            }

            Bounds bounds = col.bounds;
            overlayObj.transform.position = bounds.center;
            overlayObj.transform.localScale = bounds.size + new Vector3(0.01f, 0.01f, 0.01f);

            overlays.Add(overlayObj);
        }

        private static Mesh wireMesh;
        private static Mesh GetWireframeMesh()
        {
            if (wireMesh != null) return wireMesh;

            wireMesh = new Mesh();
            Vector3[] verts = new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f)
            };
            int[] lines = new int[24] {
                0,1, 1,2, 2,3, 3,0,
                4,5, 5,6, 6,7, 7,4,
                0,4, 1,5, 2,6, 3,7
            };
            wireMesh.vertices = verts;
            wireMesh.SetIndices(lines, MeshTopology.Lines, 0);
            return wireMesh;
        }
    }
}
