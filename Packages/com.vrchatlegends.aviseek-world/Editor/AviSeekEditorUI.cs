using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRChatLegends.AviSeek.Editor
{
    // shared look for the sdk window and the inspectors: a violet banner, card sections
    // and the links everyone needs
    public static class AviSeekEditorUI
    {
        public const string RepoUrl = "https://github.com/VRChat-Legends/Aviseek-SDK";
        public const string ListingPage = "https://vrchat-legends.github.io/Aviseek-SDK/";
        public const string ListingUrl = "https://vrchat-legends.github.io/Aviseek-SDK/index.json";
        public const string DocsUrl = "https://aviseek.vrchatlegends.com/world";
        public const string PromoteUrl = "https://aviseek.vrchatlegends.com/promote";
        public const string DiscordUrl = "https://discord.gg/UkvKMG8eUY";

        public static readonly Color Accent = new Color(0.518f, 0.478f, 0.988f);
        private static readonly Color Ink = new Color(0.07f, 0.05f, 0.12f);
        private static readonly Color Card = new Color(0f, 0f, 0f, 0.18f);

        private static Texture2D _logo;
        private static GUIStyle _title, _subtitle, _section, _hint, _pill;

        public static Texture2D Logo
        {
            get
            {
                if (_logo == null) _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(AviSeekPrefabBuilder.Root + "/Runtime/Textures/AviSeekLogo.png");
                return _logo;
            }
        }

        public static string Version
        {
            get
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(AviSeekPrefabBuilder.Root + "/package.json");
                return info != null ? info.version : "dev";
            }
        }

        private static void Styles()
        {
            if (_title != null) return;
            _title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, normal = { textColor = Color.white } };
            _subtitle = new GUIStyle(EditorStyles.label) { fontSize = 11, normal = { textColor = new Color(1f, 1f, 1f, 0.65f) } };
            _section = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, normal = { textColor = new Color(0.66f, 0.55f, 0.98f) } };
            _hint = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.65f) } };
            _pill = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }

        public static void Banner(string title, string subtitle)
        {
            Styles();
            Rect rect = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, Ink);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Accent);
            if (Logo != null) GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + 8f, 64f, 48f), Logo, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 84f, rect.y + 12f, rect.width - 160f, 24f), title, _title);
            GUI.Label(new Rect(rect.x + 85f, rect.y + 36f, rect.width - 160f, 18f), subtitle, _subtitle);
            Rect pill = new Rect(rect.xMax - 70f, rect.y + 20f, 60f, 20f);
            EditorGUI.DrawRect(pill, new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            GUI.Label(pill, "v" + Version, _pill);
            GUILayout.Space(6f);
        }

        public static void BeginSection(string title)
        {
            Styles();
            GUILayout.Space(4f);
            Rect start = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(start.x - 2f, start.y - 2f, start.width + 4f, start.height + 4f), Card);
            GUILayout.Label(title, _section);
        }

        public static void EndSection()
        {
            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        public static void Hint(string text)
        {
            Styles();
            GUILayout.Label(text, _hint);
        }

        public static void Check(bool ok, string good, string bad)
        {
            var icon = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.warnicon.sml");
            EditorGUILayout.LabelField(new GUIContent(" " + (ok ? good : bad), icon.image));
        }

        public static bool AccentButton(string label, params GUILayoutOption[] options)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = Accent;
            bool pressed = GUILayout.Button(label, GUILayout.Height(26f));
            GUI.backgroundColor = old;
            return pressed;
        }

        public static void LinkRow()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("World API docs")) Application.OpenURL(DocsUrl);
            if (GUILayout.Button("GitHub")) Application.OpenURL(RepoUrl);
            if (GUILayout.Button("AviSeek Discord")) Application.OpenURL(DiscordUrl);
            EditorGUILayout.EndHorizontal();
        }

        public static AviSeekSearch FindWall()
        {
            if (Selection.activeGameObject != null)
            {
                var selected = Selection.activeGameObject.GetComponentInParent<AviSeekSearch>();
                if (selected != null) return selected;
            }
            return Object.FindObjectOfType<AviSeekSearch>();
        }

        public static bool PackageFileExists(string relative)
        {
            return File.Exists(Path.GetFullPath(AviSeekPrefabBuilder.Root + "/" + relative));
        }
    }
}
