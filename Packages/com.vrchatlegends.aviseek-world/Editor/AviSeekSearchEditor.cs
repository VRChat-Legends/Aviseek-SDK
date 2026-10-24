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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("promotedRefreshSeconds"), new GUIContent("Promoted refresh (s)"));
            AviSeekEditorUI.Hint("The wall downloads at most once every 5.5 seconds, as VRChat requires. Searches always go before the promoted list.");
            AviSeekEditorUI.EndSection();

            _colours = EditorGUILayout.Foldout(_colours, "Colours", true);
            if (_colours)
            {
                EditorGUI.indentLevel++;
                foreach (string name in new[] { "tabIdle", "heartOn", "heartOff", "frameOff", "rateExcellent", "rateGood", "rateMedium", "ratePoor", "rateVeryPoor", "rateUnknown", "platformOff" })
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
            PanelToggle(wall, "Supporters Panel", "Supporters", "The people keeping AviSeek running, by tier. The list comes with every search.");
            PanelToggle(wall, "Socials Panel", "Socials", "Your community links. Edit them on the Socials Panel object.");
            PanelToggle(wall, "Promoted Panel", "Promoted", "Avatars and groups promoted on AviSeek. Hidden panels skip their download.");
            PanelToggle(wall, "Store Panel", "Creator store (VRChat Creator Economy)", "Your own UdonProducts, with Get buttons, owned badges and perks. Off until you switch it on.");
            AviSeekEditorUI.Hint("Panels close up when one is switched off. Hang them on any wall in your world.");
            var touch = wall.GetComponent<AviSeekTouch>();
            if (touch != null)
            {
                bool on = EditorGUILayout.ToggleLeft(new GUIContent("Touch screen for VR", "VR players can press the panels with their fingers. A ring shows where the finger is."), touch.touchEnabled);
                if (on != touch.touchEnabled)
                {
                    Undo.RecordObject(touch, "AviSeek touch");
                    touch.touchEnabled = on;
                    EditorUtility.SetDirty(touch);
                    if (PrefabUtility.IsPartOfPrefabInstance(touch)) PrefabUtility.RecordPrefabInstancePropertyModifications(touch);
                }
            }
            EditorGUILayout.BeginHorizontal();
            var socials = wall.GetComponentInChildren<AviSeekSocials>(true);
            if (socials != null && GUILayout.Button("Edit social links")) Selection.activeObject = socials.gameObject;
            var store = wall.GetComponentInChildren<AviSeekStore>(true);
            if (store != null && GUILayout.Button("Edit store products")) Selection.activeObject = store.gameObject;
            EditorGUILayout.EndHorizontal();
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
