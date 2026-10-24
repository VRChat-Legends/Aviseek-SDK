using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace VRChatLegends.AviSeek.Editor
{
    // the wall's inspector: branding and panels up front, the wiring folded away
    [CustomEditor(typeof(AviSeekSearch))]
    public class AviSeekSearchEditor : UnityEditor.Editor
    {
        private bool _colours;
        private bool _wiring;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var wall = (AviSeekSearch)target;

            AviSeekEditorUI.Banner("AviSeek Avatar Search", "Search, favorites, promoted and socials for your world");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open SDK Panel")) AviSeekSdkWindow.Open();
            if (GUILayout.Button("World API docs")) Application.OpenURL(AviSeekEditorUI.DocsUrl);
            EditorGUILayout.EndHorizontal();

            serializedObject.Update();
            DrawBranding(serializedObject, wall);
            DrawPanels(wall);

            AviSeekEditorUI.BeginSection("Downloads");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailRetries"), new GUIContent("Picture retries"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promotedRefreshSeconds"), new GUIContent("Promoted refresh (s)"));
            AviSeekEditorUI.Hint("The wall downloads at most once every 5.5 seconds, as VRChat requires. Searches always go before the promoted list.");
            AviSeekEditorUI.EndSection();

            _colours = EditorGUILayout.Foldout(_colours, "Colours", true);
            if (_colours)
            {
                EditorGUI.indentLevel++;
                foreach (string name in new[] { "tabIdle", "heartOn", "heartOff", "frameOff", "rateExcellent", "rateGood", "rateMedium", "ratePoor", "rateVeryPoor", "rateUnknown" })
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
                EditorGUI.indentLevel--;
            }

            _wiring = EditorGUILayout.Foldout(_wiring, "Wiring (advanced)", true);
            if (_wiring)
            {
                EditorGUI.indentLevel++;
                AviSeekEditorUI.Hint("These links are set up by the prefab. You only need them if you rebuild the panels yourself.");
                var property = serializedObject.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false))
                {
                    if (property.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(property, true);
                }
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }

        // also drawn by the sdk window, so both edit the same fields the same way
        public static void DrawBranding(SerializedObject serialized, AviSeekSearch wall)
        {
            AviSeekEditorUI.BeginSection("Branding");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.FindProperty("brandTitle"), new GUIContent("Title"));
            EditorGUILayout.PropertyField(serialized.FindProperty("brandHighlight"), new GUIContent("Highlighted word"));
            EditorGUILayout.PropertyField(serialized.FindProperty("brandLogo"), new GUIContent("Logo"));
            EditorGUILayout.PropertyField(serialized.FindProperty("accent"), new GUIContent("Accent colour"));
            bool changed = EditorGUI.EndChangeCheck();
            serialized.ApplyModifiedProperties();
            if (changed) AviSeekBranding.Apply(wall);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply again")) AviSeekBranding.Apply(wall);
            if (GUILayout.Button("Reset to AviSeek"))
            {
                Undo.RecordObject(wall, "Reset AviSeek branding");
                wall.brandTitle = "AVATAR";
                wall.brandHighlight = "SEARCH";
                wall.brandLogo = AssetDatabase.LoadAssetAtPath<Sprite>(AviSeekPrefabBuilder.Root + "/Runtime/Textures/AviSeekLogo.png");
                wall.accent = AviSeekEditorUI.Accent;
                EditorUtility.SetDirty(wall);
                AviSeekBranding.Apply(wall);
                serialized.Update();
            }
            EditorGUILayout.EndHorizontal();
            AviSeekEditorUI.Hint("Changes show on the wall straight away: the title, the logo, the accent buttons, the panel glow and the rims.");
            AviSeekEditorUI.EndSection();
        }

        public static void DrawPanels(AviSeekSearch wall)
        {
            AviSeekEditorUI.BeginSection("Panels");
            PanelToggle(wall, "Socials Panel", "Socials", "Your community links. Edit them on the Socials Panel object.");
            PanelToggle(wall, "Promoted Panel", "Promoted", "Avatars and groups promoted on AviSeek. Hidden panels skip their download.");
            PanelToggle(wall, "Backdrop", "Backdrop wall", "The stone wall behind the panels. Turn it off to hang the panels on your own wall.");
            var socials = wall.GetComponentInChildren<AviSeekSocials>(true);
            if (socials != null && GUILayout.Button("Edit social links")) Selection.activeObject = socials.gameObject;
            AviSeekEditorUI.EndSection();
        }

        private static void PanelToggle(AviSeekSearch wall, string child, string label, string tip)
        {
            bool on = AviSeekBranding.PanelOn(wall, child);
            bool next = EditorGUILayout.ToggleLeft(new GUIContent(label, tip), on);
            if (next != on) AviSeekBranding.SetPanel(wall, child, next);
        }
    }
}
