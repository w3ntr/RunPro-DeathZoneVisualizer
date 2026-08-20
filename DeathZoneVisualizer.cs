using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(RunProTools.Core), "Death Zone Visualizer", "1.4.0", "w3ntr")]
[assembly: MelonGame(null, null)]

namespace RunProTools
{
    public class Core : MelonMod
    {
        private bool showMenu = false;
        private int windowID = 8888;
        private Rect windowRect = new Rect(100, 100, 260, 260);

        private bool isBindingToggle = false;
        private bool isBindingMenu = false;

        public override void OnUpdate()
        {
            // Если переназначаем клавишу, не реагируем на нажатия вызова
            if (isBindingToggle || isBindingMenu) return;

            if (Input.GetKeyDown(DeathZoneSettings.ToggleKey))
            {
                DeathZoneVisualizer.ToggleVisuals();
            }

            if (Input.GetKeyDown(DeathZoneSettings.MenuKey) || Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;

                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showMenu;
            }
        }

        public override void OnGUI()
        {
            if (!showMenu) return;

            // Перехват нажатия клавиш для смены биндов
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                if (isBindingToggle)
                {
                    DeathZoneSettings.ToggleKey = e.keyCode;
                    isBindingToggle = false;
                }
                else if (isBindingMenu)
                {
                    DeathZoneSettings.MenuKey = e.keyCode;
                    isBindingMenu = false;
                }
            }

            GUI.Box(windowRect, "");
            windowRect = GUI.Window(windowID, windowRect, (GUI.WindowFunction)DrawWindow, "DeathZone Settings");
        }

        private void DrawWindow(int id)
        {
            // 1. Visuals Toggle Button
            string btnText = DeathZoneVisualizer.IsEnabled ? "Visuals: ON" : "Visuals: OFF";
            if (GUI.Button(new Rect(15, 30, 230, 30), btnText))
            {
                DeathZoneVisualizer.ToggleVisuals();
            }

            // 2. X-Ray Toggle
            bool newXRay = GUI.Toggle(new Rect(15, 65, 230, 20), DeathZoneSettings.XRayMode, " X-Ray (Through Walls)");
            if (newXRay != DeathZoneSettings.XRayMode)
            {
                DeathZoneSettings.XRayMode = newXRay;
                if (DeathZoneVisualizer.IsEnabled) DeathZoneVisualizer.RefreshVisuals();
            }

            // 3. Wireframe Toggle
            bool newWire = GUI.Toggle(new Rect(15, 90, 230, 20), DeathZoneSettings.WireframeMode, " Wireframe (Outline Only)");
            if (newWire != DeathZoneSettings.WireframeMode)
            {
                DeathZoneSettings.WireframeMode = newWire;
                if (DeathZoneVisualizer.IsEnabled) DeathZoneVisualizer.RefreshVisuals();
            }

            // 4. Transparency Slider
            GUI.Label(new Rect(15, 115, 230, 20), $"Transparency: {Mathf.RoundToInt(DeathZoneSettings.Alpha * 100)}%");
            float newAlpha = GUI.HorizontalSlider(new Rect(15, 135, 230, 20), DeathZoneSettings.Alpha, 0.05f, 1.0f);
            if (Mathf.Abs(newAlpha - DeathZoneSettings.Alpha) > 0.01f)
            {
                DeathZoneSettings.Alpha = newAlpha;
                if (DeathZoneVisualizer.IsEnabled) DeathZoneVisualizer.UpdateAlpha();
            }

            // 5. Keybind Buttons
            string toggleBtnText = isBindingToggle ? "Press Key..." : $"Toggle Key: [{DeathZoneSettings.ToggleKey}]";
            if (GUI.Button(new Rect(15, 165, 230, 25), toggleBtnText))
            {
                isBindingToggle = true;
                isBindingMenu = false;
            }

            string menuBtnText = isBindingMenu ? "Press Key..." : $"Menu Key: [{DeathZoneSettings.MenuKey}]";
            if (GUI.Button(new Rect(15, 195, 230, 25), menuBtnText))
            {
                isBindingMenu = true;
                isBindingToggle = false;
            }

            GUI.DragWindow();
        }
    }

    public static class DeathZoneSettings
    {
        public static float Alpha = 0.6f;
        public static bool XRayMode = true;
        public static bool WireframeMode = false;
        public static KeyCode ToggleKey = KeyCode.F2;
        public static KeyCode MenuKey = KeyCode.F4;
    }

    public static class DeathZoneVisualizer
    {
        private static List<GameObject> overlays = new List<GameObject>();
        private static Mesh wireframeMesh;
        public static bool IsEnabled = false;
        public static float MinYCutoff = -10f;

        public static void ToggleVisuals()
        {
            IsEnabled = !IsEnabled;
            RefreshVisuals();
        }

        public static void RefreshVisuals()
        {
            ClearOverlays();
            if (IsEnabled) RenderDeathZones();
        }

        public static void UpdateAlpha()
        {
            foreach (GameObject obj in overlays)
            {
                if (obj != null)
                {
                    Renderer ren = obj.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        Color c = ren.material.color;
                        c.a = DeathZoneSettings.Alpha;
                        ren.material.color = c;
                    }
                }
            }
        }

        private static void ClearOverlays()
        {
            foreach (GameObject obj in overlays)
            {
                if (obj != null) Object.Destroy(obj);
            }
            overlays.Clear();
        }

        private static Mesh GetWireframeMesh()
        {
            if (wireframeMesh != null) return wireframeMesh;

            wireframeMesh = new Mesh();
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
            wireframeMesh.vertices = verts;
            wireframeMesh.SetIndices(lines, MeshTopology.Lines, 0);
            return wireframeMesh;
        }

        private static void RenderDeathZones()
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

            foreach (GameObject go in allObjects)
            {
                if (go.name.ToLower().Contains("death") && go.transform.position.y >= MinYCutoff)
                {
                    Collider col = go.GetComponent<Collider>();
                    GameObject overlayObj;

                    if (DeathZoneSettings.WireframeMode)
                    {
                        overlayObj = new GameObject("DeathZone_Wireframe");
                        MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
                        mf.sharedMesh = GetWireframeMesh();
                        overlayObj.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        overlayObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(overlayObj.GetComponent<Collider>());
                    }

                    if (col != null)
                    {
                        overlayObj.transform.position = col.bounds.center;
                        overlayObj.transform.localScale = col.bounds.size + new Vector3(0.01f, 0.01f, 0.01f);
                    }
                    else
                    {
                        overlayObj.transform.position = go.transform.position;
                        overlayObj.transform.localScale = go.transform.lossyScale;
                        overlayObj.transform.rotation = go.transform.rotation;
                    }

                    Renderer ren = overlayObj.GetComponent<Renderer>();
                    ren.material.shader = Shader.Find("Hidden/Internal-Colored");

                    ren.material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    ren.material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    ren.material.SetInt("_ZWrite", 0);

                    if (DeathZoneSettings.XRayMode)
                    {
                        ren.material.SetInt("_ZTest", (int)CompareFunction.Always);
                    }

                    ren.material.color = new Color(1f, 0f, 0f, DeathZoneSettings.Alpha);
                    overlays.Add(overlayObj);
                }
            }
        }
    }
}