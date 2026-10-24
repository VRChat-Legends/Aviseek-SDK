using System;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace VRChatLegends.AviSeek.Editor
{
    // edits the socials list as cards, one per link, and repaints the panel as you type
    [CustomEditor(typeof(AviSeekSocials))]
    public class AviSeekSocialsEditor : UnityEditor.Editor
    {
        private static readonly string[] IconNames = { "VRChat", "Discord", "Bluesky", "Patreon", "GitHub", "YouTube", "Ko-fi", "Website", "AviSeek", "Email", "Group" };
        private static readonly string[] IconGlyphs =
        {
            AviSeekPrefabBuilder.BrandVrchat, AviSeekPrefabBuilder.BrandDiscord, AviSeekPrefabBuilder.BrandBluesky, AviSeekPrefabBuilder.BrandPatreon,
            AviSeekPrefabBuilder.BrandGithub, AviSeekPrefabBuilder.BrandYoutube, AviSeekPrefabBuilder.BrandKofi,
            AviSeekPrefabBuilder.IconGlobe, AviSeekPrefabBuilder.IconSearch, AviSeekPrefabBuilder.IconMail, AviSeekPrefabBuilder.IconUsers,
        };
        private static readonly bool[] IconBrand = { true, true, true, true, true, true, true, false, false, false, false };

        private bool _wiring;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var socials = (AviSeekSocials)target;
            int slots = socials.rows == null ? 0 : socials.rows.Length;
            Normalise(socials, slots);
            // recorded before any field is drawn, so every edit below can be undone
            Undo.RecordObject(socials, "Edit social links");

            AviSeekEditorUI.Banner("Social links", "Up to " + slots + " links on the Socials panel");
            AviSeekEditorUI.Hint("Players select a link and copy it. A VRChat group id (grp_...) adds an Open button that opens the group in game.");

            int remove = -1, up = -1;
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < slots; i++)
            {
                if (string.IsNullOrEmpty(socials.labels[i])) continue;
                AviSeekEditorUI.BeginSection((i + 1) + ". " + socials.labels[i]);
                socials.labels[i] = EditorGUILayout.TextField("Label", socials.labels[i]);
                socials.links[i] = EditorGUILayout.TextField("Link", socials.links[i]);
                EditorGUILayout.BeginHorizontal();
                int current = Array.FindIndex(IconGlyphs, glyph => glyph == socials.icons[i] && IconBrand[Array.IndexOf(IconGlyphs, glyph)] == socials.brandIcons[i]);
                int chosen = EditorGUILayout.Popup("Icon", Mathf.Max(current, 0), IconNames);
                if (chosen != current)
                {
                    socials.icons[i] = IconGlyphs[chosen];
                    socials.brandIcons[i] = IconBrand[chosen];
                }
                socials.iconColors[i] = EditorGUILayout.ColorField(GUIContent.none, socials.iconColors[i], GUILayout.Width(60f));
                EditorGUILayout.EndHorizontal();
                socials.groupIds[i] = EditorGUILayout.TextField(new GUIContent("VRChat group id", "grp_... to show an Open button"), socials.groupIds[i]);
                if (!string.IsNullOrEmpty(socials.groupIds[i]) && !socials.groupIds[i].StartsWith("grp_"))
                    EditorGUILayout.HelpBox("Group ids start with grp_. Find it in the group's vrchat.com address.", MessageType.Warning);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (i > 0 && GUILayout.Button("Move up", GUILayout.Width(80f))) up = i;
                if (GUILayout.Button("Remove", GUILayout.Width(80f))) remove = i;
                EditorGUILayout.EndHorizontal();
                AviSeekEditorUI.EndSection();
            }
            bool edited = EditorGUI.EndChangeCheck();

            int used = socials.labels.Count(label => !string.IsNullOrEmpty(label));
            EditorGUI.BeginDisabledGroup(used >= slots);
            if (AviSeekEditorUI.AccentButton(used >= slots ? "The panel is full" : "Add a link"))
            {
                Undo.RecordObject(socials, "Add social link");
                socials.labels[used] = "New link";
                socials.links[used] = "https://";
                socials.icons[used] = AviSeekPrefabBuilder.IconGlobe;
                socials.brandIcons[used] = false;
                socials.iconColors[used] = Color.white;
                socials.groupIds[used] = "";
                edited = true;
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Restore the VRChat Legends links"))
            {
                Undo.RecordObject(socials, "Restore social links");
                for (int i = 0; i < slots; i++)
                {
                    bool has = i < AviSeekPrefabBuilder.DefaultSocials.Length;
                    var entry = has ? AviSeekPrefabBuilder.DefaultSocials[i] : default;
                    socials.labels[i] = has ? entry.label : "";
                    socials.links[i] = has ? entry.link : "";
                    socials.icons[i] = has ? entry.icon : "";
                    socials.brandIcons[i] = has && entry.brand;
                    socials.iconColors[i] = has ? entry.color : Color.white;
                    socials.groupIds[i] = has ? entry.groupId : "";
                }
                edited = true;
            }

            if (remove >= 0) { Shift(socials, remove); edited = true; }
            if (up > 0) { Swap(socials, up, up - 1); edited = true; }
            if (edited)
            {
                Undo.RecordObject(socials, "Edit social links");
                EditorUtility.SetDirty(socials);
                if (PrefabUtility.IsPartOfPrefabInstance(socials)) PrefabUtility.RecordPrefabInstancePropertyModifications(socials);
                AviSeekPrefabBuilder.ApplySocials(socials);
            }

            _wiring = EditorGUILayout.Foldout(_wiring, "Wiring (advanced)", true);
            if (_wiring) DrawDefaultInspector();
        }

        // keeps every list as long as the panel has lines
        private static void Normalise(AviSeekSocials s, int size)
        {
            s.labels = Resize(s.labels, size, "");
            s.links = Resize(s.links, size, "");
            s.icons = Resize(s.icons, size, "");
            s.brandIcons = Resize(s.brandIcons, size, false);
            s.iconColors = Resize(s.iconColors, size, Color.white);
            s.groupIds = Resize(s.groupIds, size, "");
            for (int i = 0; i < size; i++)
            {
                if (s.labels[i] == null) s.labels[i] = "";
                if (s.links[i] == null) s.links[i] = "";
                if (s.icons[i] == null) s.icons[i] = "";
                if (s.groupIds[i] == null) s.groupIds[i] = "";
            }
        }

        private static T[] Resize<T>(T[] array, int size, T fill)
        {
            if (array != null && array.Length == size) return array;
            var next = new T[size];
            for (int i = 0; i < size; i++) next[i] = array != null && i < array.Length ? array[i] : fill;
            return next;
        }

        private static void Shift(AviSeekSocials s, int from)
        {
            for (int i = from; i < s.labels.Length - 1; i++) Swap(s, i, i + 1);
            int last = s.labels.Length - 1;
            s.labels[last] = "";
            s.links[last] = "";
            s.icons[last] = "";
            s.groupIds[last] = "";
        }

        private static void Swap(AviSeekSocials s, int a, int b)
        {
            (s.labels[a], s.labels[b]) = (s.labels[b], s.labels[a]);
            (s.links[a], s.links[b]) = (s.links[b], s.links[a]);
            (s.icons[a], s.icons[b]) = (s.icons[b], s.icons[a]);
            (s.brandIcons[a], s.brandIcons[b]) = (s.brandIcons[b], s.brandIcons[a]);
            (s.iconColors[a], s.iconColors[b]) = (s.iconColors[b], s.iconColors[a]);
            (s.groupIds[a], s.groupIds[b]) = (s.groupIds[b], s.groupIds[a]);
        }
    }
}
