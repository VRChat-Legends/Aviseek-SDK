using System.Collections.Generic;
using System.IO;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VRChatLegends.AviSeek.Editor
{
    // builds everything the wall needs from the package's own files: the udon
    // programs, the lucide icon font, the panel sprites and the prefab itself.
    // change the look here and rebuild instead of hand editing the prefab
    public static class AviSeekPrefabBuilder
    {
        public const string Root = "Packages/com.vrchatlegends.aviseek-world";
        public const string PrefabPath = Root + "/Runtime/Prefabs/AviSeek Avatar Search.prefab";
        private const string Generated = Root + "/Runtime/Textures/Generated";
        private const string IconFontPath = Root + "/Runtime/Fonts/Lucide SDF.asset";
        // thumbs=0: the wall draws vrchat's own pictures, so the text comes down alone
        private const string ApiBase = "https://aviseek.vrchatlegends.com/api/world/client?thumbs=0&";
        // icons=1: every promoted group's icon and every supporter's picture come in this one download
        private const string PromotedUrl = "https://aviseek.vrchatlegends.com/api/world/bundle?include=promoted,promotedGroups,supporters&limit=100&icons=1";
        // the short addresses players see in vrchat's keyboard, used once /w/ping answers
        private const string ShortBase = "https://aviapi.vrchatlegends.com/w";
        // one address per picture sheet slot on the server, /w/p/0 to /w/p/1023
        private const int PictureSlots = 1024;
        private const string BrandFontPath = Root + "/Runtime/Fonts/Simple Icons SDF.asset";
        private const int BrowseBatches = 10;
        private const int Columns = 6;
        private const int Rows = 2;
        private const string TextFontPath = Root + "/Runtime/Fonts/Inter SDF.asset";
        private const string BoldFontPath = Root + "/Runtime/Fonts/Inter Bold SDF.asset";

        // canvas units to metres: a 900 unit panel stands 1.125 m tall
        public const float PanelScale = 0.00125f;
        private const float PanelCentreY = 1.6f;
        private const float PanelGap = 0.035f;
        // left to right along the wall. Layout spaces out whichever are switched on
        public static readonly string[] PanelOrder = { "Supporters Panel", "Socials Panel", "Search Panel", "Results Panel", "Folders Panel", "Promoted Panel", "Mirror Panel", "Store Panel" };

        // lucide icon codepoints (lucide-static font/info.json)
        public const string IconSearch = "\ue151";
        public const string IconHeart = "\ue0f2";
        public const string IconPrev = "\ue072";
        public const string IconNext = "\ue073";
        public const string IconChevronDown = "\ue06d";
        public const string IconHome = "\ue0f5";
        public const string IconRefresh = "\ue145";
        public const string IconUser = "\ue468";
        public const string IconShirt = "\ue1ca";
        public const string IconSort = "\ue047";
        public const string IconGauge = "\ue1bf";
        public const string IconLayers = "\ue529";
        public const string IconInfo = "\ue0f9";
        public const string IconGlobe = "\ue0e8";
        public const string IconSparkles = "\ue412";
        public const string IconMinus = "\ue11c";
        public const string IconX = "\ue1b2";
        public const string IconFilter = "\ue460";
        public const string IconUsers = "\ue1a4";
        public const string IconExternal = "\ue0b9";
        public const string IconMail = "\ue10f";
        public const string IconShoppingBag = "\ue15b";
        public const string IconStore = "\ue3e4";
        public const string IconBadgeCheck = "\ue241";
        public const string IconHeartHandshake = "\ue2d7";
        public const string IconGift = "\ue0e1";
        public const string IconFolder = "\ue0d7";
        public const string IconFolderHeart = "\ue333";
        public const string IconCoins = "\ue097";
        public const string IconHeadphones = "\ue0f1";
        public const string IconVolume = "\ue1ab";
        public const string IconLogIn = "\ue10d";
        public const string IconLogOut = "\ue10e";
        public const string IconKey = "\ue4a3";
        public const string IconCrown = "\ue1d6";
        public const string IconTrophy = "\ue373";
        public const string IconTrash = "\ue18e";
        public const string IconPlus = "\ue13d";
        public const string IconFolderPlus = "\ue0d9";
        public const string IconFolderMinus = "\ue0d8";
        public const string IconGrip = "\ue0ea";
        public const string IconScaling = "\ue2ec";
        public const string IconZoomIn = "\ue1b6";
        public const string IconZoomOut = "\ue1b7";
        public const string IconCircleUser = "\ue461";
        public const string IconMirror = "\ue6c5";
        public const string IconEyeOff = "\ue0bb";

        // simple icons brand logos (simple-icons-font, font/simple-icons.json)
        public const string BrandBluesky = "\ueb74";
        public const string BrandDiscord = "\ued0c";
        public const string BrandGithub = "\uee64";
        public const string BrandKofi = "\uf039";
        public const string BrandPatreon = "\uf29f";
        public const string BrandVrchat = "\uf6c2";
        public const string BrandYoutube = "\uf751";
        private const string AllBrands = BrandBluesky + BrandDiscord + BrandGithub + BrandKofi + BrandPatreon + BrandVrchat + BrandYoutube;
        private const string AllIcons = IconSearch + IconHeart + IconPrev + IconNext + IconChevronDown + IconHome + IconRefresh
            + IconUser + IconShirt + IconSort + IconGauge + IconLayers + IconInfo + IconGlobe + IconSparkles + IconMinus + IconX + IconFilter + IconUsers + IconExternal + IconMail
            + IconShoppingBag + IconStore + IconBadgeCheck + IconHeartHandshake + IconGift
            + IconFolder + IconFolderHeart + IconCoins + IconHeadphones + IconVolume + IconLogIn + IconLogOut + IconKey + IconCrown + IconTrophy
            + IconTrash + IconPlus + IconFolderPlus + IconFolderMinus + IconGrip + IconScaling + IconZoomIn + IconZoomOut + IconCircleUser + IconMirror + IconEyeOff;

        private static readonly Color Ink = Hex("0a0714");
        private static readonly Color Panel = Hex("120c1e");
        private static readonly Color PanelLight = Hex("1b1430");
        private static readonly Color Accent = Hex("847afc");
        private static readonly Color AccentSoft = Hex("a78bfa");
        private static readonly Color Muted = new Color(1f, 1f, 1f, 0.55f);
        // solid colours, not see-through white: vrchat renders in linear colour space, where a
        // faint white overlay comes out as a flat grey
        private static readonly Color Field = Hex("2a2244");
        private static readonly Color ButtonSoft = Hex("3a3060");
        private static readonly Color RowBg = Hex("1d1731");
        private static readonly Color CardOff = Hex("2b2442");
        private static readonly Color Well = Hex("0b0816");
        // the profile menu wears the wall's own colours
        private static readonly Color MenuBack = Panel;
        private static readonly Color MenuPanel = RowBg;
        private static readonly Color MenuTile = Field;
        private static readonly Color MenuAccent = AccentSoft;
        private static readonly Color MenuMuted = Muted;
        private const float MenuScale = 0.00045f;
        private const float MenuW = 1200f;
        private const float MenuH = 820f;
        private const string SignInPrefix = "https://aviapi.vrchatlegends.com/w/l?c=";

        private static Sprite _rounded;
        private static Sprite _circle;
        private static Sprite _glow;
        private static Sprite _fade;
        private static Sprite _shadow;
        private static Sprite _ring;
        private static Sprite _ringDashed;
        private static Sprite _outline;
        private static TMP_FontAsset _brand;
        private static readonly List<Image> _accentImages = new List<Image>();

        public struct SocialLink
        {
            public string label, link, icon, groupId, picture;
            public bool brand;
            public Color color;
            public SocialLink(string label, string link, string icon, bool brand, Color color, string groupId = "", string picture = "")
            {
                this.label = label;
                this.link = link;
                this.icon = icon;
                this.brand = brand;
                this.color = color;
                this.groupId = groupId;
                this.picture = picture;
            }

            // the logo picture, from the package's textures, or null for the icon character
            public Sprite Picture => string.IsNullOrEmpty(picture) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/" + picture + ".png");
        }

        // the vrchat legends and aviseek links, as listed on vrchatlegends.com and aviseek.vrchatlegends.com
        public static readonly SocialLink[] DefaultSocials =
        {
            new SocialLink("VRChat Legends group", "https://vrc.group/LEGEND.0850", BrandVrchat, true, Color.white, "grp_e80398ca-3725-4529-b104-9fe5ef71ab91", "VRChatLegendsIcon"),
            new SocialLink("AviSeek group", "https://vrchat.com/home/group/grp_f5dda886-1636-46a7-86c3-49256fab4fc6", BrandVrchat, true, Hex("a78bfa"), "grp_f5dda886-1636-46a7-86c3-49256fab4fc6", "AviSeekIcon"),
            new SocialLink("VRChat Legends Discord", "https://discord.gg/6xPkZ7Dxp9", BrandDiscord, true, Hex("5865f2")),
            new SocialLink("AviSeek Discord", "https://discord.gg/UkvKMG8eUY", BrandDiscord, true, Hex("8b8ff7")),
            new SocialLink("Bluesky", "https://bsky.app/profile/vrchatlegends.com", BrandBluesky, true, Hex("0085ff")),
            new SocialLink("Patreon", "https://www.patreon.com/vrchatlegends", BrandPatreon, true, Hex("ff424d")),
            new SocialLink("GitHub", "https://github.com/VRChat-Legends", BrandGithub, true, Color.white),
            new SocialLink("AviSeek", "https://aviseek.vrchatlegends.com", IconSearch, false, Hex("a78bfa"), "", "AviSeekIcon"),
            new SocialLink("VRChat Legends", "https://vrchatlegends.com", IconGlobe, false, Color.white, "", "VRChatLegendsIcon"),
            new SocialLink("Support", "support@vrchatlegends.com", IconMail, false, Hex("6ee7b7")),
        };
        private static Material _bodyMaterial;
        private static Material _rimMaterial;
        private static TMP_FontAsset _text;
        private static TMP_FontAsset _bold;
        private static TMP_FontAsset _icons;
        private static Font _legacy;

        [MenuItem("AviSeek/Add Avatar Search Wall", false, 0)]
        [MenuItem("GameObject/AviSeek/Avatar Search Wall", false, 10)]
        public static void AddToScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("AviSeek", "The AviSeek prefab is missing. Run AviSeek > Rebuild Prefab first.", "OK");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Vector3 pivot = view.pivot;
                instance.transform.position = new Vector3(pivot.x, 0f, pivot.z);
            }
            Undo.RegisterCreatedObjectUndo(instance, "Add AviSeek Avatar Search");
            Selection.activeGameObject = instance;
        }

        [MenuItem("AviSeek/Open AviSeek World API Docs", false, 20)]
        public static void OpenDocs()
        {
            Application.OpenURL("https://aviseek.vrchatlegends.com/world");
        }

        [MenuItem("AviSeek/Maintainers/Rebuild Prefab", false, 100)]
        public static void Rebuild()
        {
            PrepareAssets();
            BuildPrefab();
        }

        /* ───────────────────────────── assets ───────────────────────────── */

        public static void PrepareAssets()
        {
            Directory.CreateDirectory(Generated);
            _rounded = MakeSprite("Rounded", 128, 32, RoundedPixels(128, 32));
            _circle = MakeSprite("Circle", 64, 0, RoundedPixels(64, 32));
            _glow = MakeSprite("Glow", 256, 0, GlowPixels(256));
            _fade = MakeSprite("Fade", 64, 0, FadePixels(64));
            _shadow = MakeSprite("Shadow", 128, 48, ShadowPixels(128));
            _ring = MakeSprite("Ring", 128, 0, RingPixels(128, false));
            _ringDashed = MakeSprite("Ring Dashed", 128, 0, RingPixels(128, true));
            _outline = MakeSprite("Outline", 128, 48, OutlinePixels(128, 30, 5f));
            _bodyMaterial = MakeMaterial("Panel Body", Hex("17112a"), Color.black, 0.55f);
            _rimMaterial = MakeMaterial("Panel Rim", Accent, Accent * 1.1f, 0.5f);
            foreach (string name in new[] { "AviSeekLogo", "Windows", "Android", "Apple", "Windows Glyph", "Android Glyph", "Apple Glyph", "AviSeekIcon", "VRChatLegendsIcon" })
                ImportAsSprite(Root + "/Runtime/Textures/" + name + ".png", 0);
            _text = MakeTextFonts();
            _icons = MakeFont(IconFontPath, Root + "/Runtime/Fonts/Lucide.ttf", "Lucide SDF", AllIcons, IconMinus);
            _legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _brand = MakeFont(BrandFontPath, Root + "/Runtime/Fonts/SimpleIcons-Brands.ttf", "Simple Icons SDF", AllBrands, BrandDiscord);
            MakeProgram<AviSeekSearch>("AviSeekSearch");
            MakeProgram<AviSeekCard>("AviSeekCard");
            MakeProgram<AviSeekRow>("AviSeekRow");
            MakeProgram<AviSeekPromoted>("AviSeekPromoted");
            MakeProgram<AviSeekSocials>("AviSeekSocials");
            MakeProgram<AviSeekSupporters>("AviSeekSupporters");
            MakeProgram<AviSeekStore>("AviSeekStore");
            MakeProgram<AviSeekTouch>("AviSeekTouch");
            MakeProgram<AviSeekIntro>("AviSeekIntro");
            MakeProgram<AviSeekMenu>("AviSeekMenu");
            MakeProgram<AviSeekFolders>("AviSeekFolders");
            MakeProgram<AviSeekMirror>("AviSeekMirror");
            AssetDatabase.SaveAssets();
        }

        private static void MakeProgram<T>(string name) where T : UdonSharpBehaviour
        {
            string path = Root + "/Runtime/Programs/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) != null) return;
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "/Runtime/" + name + ".cs");
            var program = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            program.sourceCsScript = script;
            AssetDatabase.CreateAsset(program, path);
        }

        public static void MakeUdonSharpAssembly()
        {
            string path = Root + "/Runtime/VRChatLegends.AviSeek.Runtime.UdonSharp.asset";
            if (AssetDatabase.LoadAssetAtPath<UdonSharpAssemblyDefinition>(path) != null) return;
            var definition = ScriptableObject.CreateInstance<UdonSharpAssemblyDefinition>();
            definition.sourceAssembly = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(Root + "/Runtime/VRChatLegends.AviSeek.Runtime.asmdef");
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
        }

        // inter with a real bold face. tmp's stand in bold smears the letters and spaces them out
        private static TMP_FontAsset MakeTextFonts()
        {
            var bold = MakeDynamicFont(BoldFontPath, Root + "/Runtime/Fonts/Inter-Bold.ttf", "Inter Bold SDF");
            var regular = MakeDynamicFont(TextFontPath, Root + "/Runtime/Fonts/Inter-Regular.ttf", "Inter SDF");
            _bold = bold;
            if (regular.fontWeightTable[7].regularTypeface != bold)
            {
                regular.fontWeightTable[7].regularTypeface = bold;
                EditorUtility.SetDirty(regular);
            }
            // anything inter lacks falls back to the project's default font, then vrchat's own
            var fallback = TMP_Settings.defaultFontAsset;
            foreach (var asset in new[] { regular, bold })
            {
                if (asset.fallbackFontAssetTable == null) asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (fallback == null || asset.fallbackFontAssetTable.Contains(fallback)) continue;
                asset.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            return regular;
        }

        // dynamic, because avatar and creator names can hold any letter
        private static TMP_FontAsset MakeDynamicFont(string path, string ttf, string name)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            string missing;
            asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u00b7\u2026", out missing);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        // static atlas holding only the glyphs the wall uses
        private static TMP_FontAsset MakeFont(string path, string ttf, string name, string glyphs, string underscore)
        {
            // made once, rebuilding would give it a new guid and break saved references
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                // already holds every glyph: leave it alone so its sub asset ids stay put too
                bool complete = true;
                foreach (char glyph in glyphs) complete &= existing.characterLookupTable.ContainsKey(glyph);
                if (complete) return existing;
                existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                string absent;
                bool fits = existing.TryAddCharacters(glyphs, out absent) && string.IsNullOrEmpty(absent);
                existing.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                if (fits) return existing;
                // a full atlas, start again with every glyph so they share one bigger sheet
                AssetDatabase.DeleteAsset(path);
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            var asset = TMP_FontAsset.CreateFontAsset(font, 96, 10, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            string missing;
            asset.TryAddCharacters(glyphs, out missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("[AviSeek] " + name + " is missing glyphs: " + missing);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            // tmp wants an underscore in every font for underlines, icon fonts have none, lend it a glyph
            TMP_Character stand;
            if (asset.characterLookupTable.TryGetValue(underscore[0], out stand))
            {
                asset.characterTable.Add(new TMP_Character('_', stand.glyph));
                asset.ReadFontAssetDefinition();
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        private static Sprite MakeSprite(string name, int size, int border, Color32[] pixels)
        {
            string path = Generated + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return ImportAsSprite(path, border);
        }

        private static Sprite ImportAsSprite(string path, int border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // a rounded frame line with a soft glow on both sides, sliced so it fits any button
        private static Color32[] OutlinePixels(int size, int radius, float thickness)
        {
            var pixels = new Color32[size * size];
            float half = size * 0.5f - 12f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var q = new Vector2(Mathf.Abs(x + 0.5f - size * 0.5f), Mathf.Abs(y + 0.5f - size * 0.5f)) - new Vector2(half - radius, half - radius);
                    float edge = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                    float line = Mathf.Clamp01(thickness * 0.5f - Mathf.Abs(edge) + 0.5f);
                    float glow = Mathf.Exp(-Mathf.Abs(edge) / 4f) * 0.45f;
                    float alpha = Mathf.Max(line, glow);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        private static Color32[] RoundedPixels(int size, int radius)
        {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, size - radius);
                    float cy = Mathf.Clamp(py, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        private static Color32[] GlowPixels(int size)
        {
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float alpha = Mathf.Clamp01(1f - d);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        // a rounded block blurred out to nothing at the edges, sliced under raised things
        private static Color32[] ShadowPixels(int size)
        {
            var pixels = new Color32[size * size];
            const float inset = 40f, radius = 12f, sigma = 13f;
            float half = size * 0.5f;
            float box = half - inset - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - box;
                    float qy = Mathf.Abs(y + 0.5f - half) - box;
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float alpha = outside <= 0f ? 1f : Mathf.Exp(-(outside * outside) / (2f * sigma * sigma));
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        // a thin ring for the touch cursor, with four gaps when dashed
        private static Color32[] RingPixels(int size, bool dashed)
        {
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half, dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / half;
                    float alpha = Mathf.Clamp01((0.07f - Mathf.Abs(d - 0.85f)) * half * 0.5f);
                    if (dashed)
                    {
                        float m = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 90f);
                        alpha *= Mathf.Clamp01((m - 16f) / 3f) * Mathf.Clamp01((88f - m) / 3f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        // clear at the top, solid at the bottom, for text over pictures
        private static Color32[] FadePixels(int size)
        {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float t = 1f - y / (float)(size - 1);
                byte alpha = (byte)(Mathf.SmoothStep(0f, 1f, t) * 255f);
                for (int x = 0; x < size; x++) pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            return pixels;
        }

        /* ───────────────────────────── prefab ───────────────────────────── */

        public static GameObject BuildPrefab()
        {
            if (_rounded == null) PrepareAssets();
            _accentImages.Clear();

            var root = new GameObject("AviSeek Avatar Search");

            var search = UdonSharpUndo.AddComponent<AviSeekSearch>(root);
            var searchUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(search);

            // the panels hang side by side, flat on the wall. Layout spaces them out
            var supportersPanel = PanelCanvas(root.transform, "Supporters Panel", 720, 900);
            var socials = PanelCanvas(root.transform, "Socials Panel", 720, 900);
            var left = PanelCanvas(root.transform, "Search Panel", 1100, 900);
            var right = PanelCanvas(root.transform, "Results Panel", 1980, 900);
            var foldersPanel = PanelCanvas(root.transform, "Folders Panel", 720, 900);
            var promotedPanel = PanelCanvas(root.transform, "Promoted Panel", 760, 900);
            var mirrorPanel = PanelCanvas(root.transform, "Mirror Panel", 760, 900);
            var storePanel = PanelCanvas(root.transform, "Store Panel", 760, 900);

            BuildSearchPanel(left, search, searchUdon);
            BuildResults(right, search, searchUdon);
            BuildSocials(socials);
            search.supporters = BuildSupporters(supportersPanel);
            search.promoted = BuildPromoted(promotedPanel, search);
            search.promoted.supporters = search.supporters;
            UdonSharpEditorUtility.CopyProxyToUdon(search.promoted);
            BuildStore(storePanel);
            BuildMirror(mirrorPanel);
            // the creator store is opt in
            storePanel.parent.gameObject.SetActive(false);

            search.pedestalObject = BuildPedestal(root.transform);

            search.searchPrefixUrl = new VRCUrl(ApiBase + "q=");
            search.creatorPrefixUrl = new VRCUrl(ApiBase + "author=");
            search.shortSearchPrefixUrl = new VRCUrl(ShortBase + "?q=");
            search.shortCreatorPrefixUrl = new VRCUrl(ShortBase + "?a=");
            search.shortPingUrl = new VRCUrl(ShortBase + "/ping");
            search.promotedUrl = new VRCUrl(PromotedUrl);
            search.pictureUrls = new VRCUrl[PictureSlots];
            for (int i = 0; i < PictureSlots; i++) search.pictureUrls[i] = new VRCUrl(ShortBase + "/p/" + i);
            search.browseUrls = new VRCUrl[BrowseBatches];
            for (int i = 0; i < BrowseBatches; i++) search.browseUrls[i] = new VRCUrl(ApiBase + "q=&offset=" + (i * 120));
            search.accent = Accent;
            search.tabIdle = Field;
            search.frameOff = CardOff;
            search.accentImages = _accentImages.ToArray();
            UdonSharpEditorUtility.CopyProxyToUdon(search);

            Layout(root.transform, false);
            var menu = BuildMenu(root.transform, search);
            BuildFolders(foldersPanel, search, menu);
            UdonSharpEditorUtility.CopyProxyToUdon(search);
            BuildTouch(root.transform);
            BuildIntro(root.transform);

            SetLayerRecursive(root, 0);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[AviSeek] Prefab rebuilt at " + PrefabPath);
            return prefab;
        }

        // spaces the switched on panels evenly along the wall, centred.
        // the inspector runs it too when a panel is switched on or off
        public static void Layout(Transform root, bool record)
        {
            var shown = new List<RectTransform>();
            foreach (string name in PanelOrder)
            {
                var panel = root.Find(name) as RectTransform;
                if (panel != null && panel.gameObject.activeSelf) shown.Add(panel);
            }
            float total = -PanelGap;
            foreach (var panel in shown) total += panel.sizeDelta.x * panel.localScale.x + PanelGap;
            float x = -total * 0.5f;
            foreach (var panel in shown)
            {
                float w = panel.sizeDelta.x * panel.localScale.x;
                Move(panel, new Vector3(x + w * 0.5f, panel.localPosition.y, panel.localPosition.z), record);
                x += w + PanelGap;
            }
        }

        private static void Move(Transform part, Vector3 position, bool record)
        {
            if (part.localPosition == position) return;
            if (record) Undo.RecordObject(part, "AviSeek layout");
            part.localPosition = position;
            if (record) RecordChange(part);
        }

        private static void RecordChange(Object changed)
        {
            EditorUtility.SetDirty(changed);
            if (PrefabUtility.IsPartOfPrefabInstance(changed)) PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
        }

        // a world space canvas mounted flat on the wall, with a solid body and glowing rim behind it
        private static RectTransform PanelCanvas(Transform root, string name, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            go.AddComponent<GraphicRaycaster>();
            var rect = (RectTransform)go.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            // the body is 45 mm deep and its back sits on the wall's face. Layout sets x
            rect.localPosition = new Vector3(0f, PanelCentreY, -0.026f);
            rect.localScale = Vector3.one * PanelScale;
            go.AddComponent<VRCUiShape>();

            // the bodies are built in canvas units too
            Slab(rect, "Body", new Vector3(0f, 0f, 2f), w, h, 44f, 34f, _bodyMaterial);
            Slab(rect, "Rim", new Vector3(0f, 0f, 18f), w + 14f, h + 14f, 50f, 8f, _rimMaterial);

            return PanelBox(rect, "Panel", 0, 0, w, h);
        }

        private static void BuildSocials(RectTransform panel)
        {
            var socials = UdonSharpUndo.AddComponent<AviSeekSocials>(panel.parent.gameObject);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(socials);

            Lift(Icon(panel, "Icon", 36, 34, 60, 64, IconUsers, 44, AccentSoft), 10f);
            Lift(Txt(panel, "Title", "COMMUNITY", 104, 32, 580, 64, 48, Color.white, TextAlignmentOptions.Left, FontStyles.Bold), 10f);
            Txt(panel, "Subtitle", "VRChat Legends and AviSeek", 106, 92, 580, 30, 21, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            var hint = Txt(panel, "Hint", "Click a link to open the keyboard and copy it. Groups open in VRChat.", 40, 128, 640, 30, 18, AccentSoft, TextAlignmentOptions.Left, FontStyles.Italic);
            hint.overflowMode = TextOverflowModes.Ellipsis;

            int count = DefaultSocials.Length;
            var rows = new GameObject[count];
            var icons = new TextMeshProUGUI[count];
            var brands = new TextMeshProUGUI[count];
            var pictures = new Image[count];
            var labels = new TextMeshProUGUI[count];
            var links = new TMP_InputField[count];
            var opens = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                float y = 170f + i * 70f;
                var row = Img(panel, "Link " + (i + 1), 36, y, 648, 64, _rounded, RowBg);
                Round(row, 14);
                var r = row.rectTransform;
                rows[i] = row.gameObject;

                var well = Inset(r, "Icon Box", 6, 6, 52, 52, 12);
                icons[i] = Icon(well.rectTransform, "Icon", 0, 0, 52, 52, "", 26, Color.white);
                brands[i] = Icon(well.rectTransform, "Brand", 0, 0, 52, 52, "", 26, Color.white);
                brands[i].font = _brand;
                pictures[i] = Img(well.rectTransform, "Picture", 5, 5, 42, 42, null, Color.white);
                pictures[i].preserveAspect = true;
                pictures[i].enabled = false;

                labels[i] = Txt(r, "Label", "", 70, 3, 440, 30, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                labels[i].overflowMode = TextOverflowModes.Ellipsis;
                links[i] = CopyField(r, "Link", 66, 31, 446, 30, 17);

                var rowScript = UdonSharpUndo.AddComponent<AviSeekRow>(row.gameObject);
                var rowUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(rowScript);
                rowScript.target = socials;
                rowScript.index = i;
                rowScript.primaryEvent = nameof(AviSeekSocials._OpenGroup);
                rowScript.secondaryEvent = nameof(AviSeekSocials._RestoreLink);
                UdonSharpEditorUtility.CopyProxyToUdon(rowScript);
                UnityEventTools.AddStringPersistentListener(links[i].onValueChanged, rowUdon.SendCustomEvent, nameof(AviSeekRow._Secondary));
                UnityEventTools.AddStringPersistentListener(links[i].onEndEdit, rowUdon.SendCustomEvent, nameof(AviSeekRow._Secondary));
                // the button and its shadow share a holder, so hiding the holder hides both
                var holder = new GameObject("Open Holder", typeof(RectTransform));
                Place((RectTransform)holder.transform, r, 0, 0, 648, 64);
                var open = Btn((RectTransform)holder.transform, "Open", 520, 10, 118, 44, Accent, rowUdon, nameof(AviSeekRow._Primary));
                _accentImages.Add(open.GetComponent<Image>());
                Icon((RectTransform)open.transform, "Icon", 8, 0, 34, 44, IconExternal, 20, Color.white);
                Txt((RectTransform)open.transform, "Label", "Open", 40, 0, 72, 44, 20, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                opens[i] = holder;
            }

            socials.labels = new string[count];
            socials.links = new string[count];
            socials.icons = new string[count];
            socials.brandIcons = new bool[count];
            socials.iconColors = new Color[count];
            socials.groupIds = new string[count];
            socials.iconImages = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var entry = DefaultSocials[i];
                socials.labels[i] = entry.label;
                socials.links[i] = entry.link;
                socials.icons[i] = entry.icon;
                socials.brandIcons[i] = entry.brand;
                socials.iconColors[i] = entry.color;
                socials.groupIds[i] = entry.groupId;
                socials.iconImages[i] = entry.Picture;
            }
            socials.rows = rows;
            socials.rowIcons = icons;
            socials.rowBrandIcons = brands;
            socials.rowPictures = pictures;
            socials.rowLabels = labels;
            socials.rowLinks = links;
            socials.rowOpenButtons = opens;
            UdonSharpEditorUtility.CopyProxyToUdon(socials);
            ApplySocials(socials);
        }

        // paints the socials list onto its lines in the editor, the same way Refresh does in game
        public static void ApplySocials(AviSeekSocials socials)
        {
            if (socials == null || socials.rows == null) return;
            for (int i = 0; i < socials.rows.Length; i++)
            {
                bool used = socials.labels != null && i < socials.labels.Length && !string.IsNullOrEmpty(socials.labels[i]);
                if (socials.rows[i] != null) socials.rows[i].SetActive(used);
                if (!used) continue;
                bool brand = socials.brandIcons != null && i < socials.brandIcons.Length && socials.brandIcons[i];
                string icon = socials.icons != null && i < socials.icons.Length ? socials.icons[i] : "";
                Color color = socials.iconColors != null && i < socials.iconColors.Length ? socials.iconColors[i] : Color.white;
                Sprite picture = socials.iconImages != null && i < socials.iconImages.Length ? socials.iconImages[i] : null;
                if (picture != null) icon = "";
                if (socials.rowPictures != null && i < socials.rowPictures.Length && socials.rowPictures[i] != null)
                {
                    socials.rowPictures[i].sprite = picture;
                    socials.rowPictures[i].enabled = picture != null;
                    EditorUtility.SetDirty(socials.rowPictures[i]);
                }
                SetText(socials.rowIcons, i, brand ? "" : icon, color);
                SetText(socials.rowBrandIcons, i, brand ? icon : "", color);
                SetText(socials.rowLabels, i, socials.labels[i], Color.white);
                if (socials.rowLinks != null && i < socials.rowLinks.Length && socials.rowLinks[i] != null)
                {
                    socials.rowLinks[i].text = socials.links != null && i < socials.links.Length ? socials.links[i] : "";
                    EditorUtility.SetDirty(socials.rowLinks[i]);
                }
                bool group = socials.groupIds != null && i < socials.groupIds.Length && !string.IsNullOrEmpty(socials.groupIds[i]);
                if (socials.rowOpenButtons != null && i < socials.rowOpenButtons.Length && socials.rowOpenButtons[i] != null) socials.rowOpenButtons[i].SetActive(group);
            }
        }

        private static void SetText(TextMeshProUGUI[] labels, int i, string text, Color color)
        {
            if (labels == null || i >= labels.Length || labels[i] == null) return;
            labels[i].text = text;
            labels[i].color = color;
            EditorUtility.SetDirty(labels[i]);
        }

        private static AviSeekPromoted BuildPromoted(RectTransform panel, AviSeekSearch search)
        {
            var promoted = UdonSharpUndo.AddComponent<AviSeekPromoted>(panel.parent.gameObject);
            promoted.search = search;

            Lift(Icon(panel, "Icon", 36, 34, 60, 64, IconSparkles, 44, AccentSoft), 10f);
            Lift(Txt(panel, "Title", "PROMOTED", 104, 32, 620, 64, 48, Color.white, TextAlignmentOptions.Left, FontStyles.Bold), 10f);
            Txt(panel, "Subtitle", "Featured on AviSeek", 106, 92, 620, 30, 21, Muted, TextAlignmentOptions.Left, FontStyles.Normal);

            Txt(panel, "Avatars Heading", "AVATARS", 40, 140, 300, 30, 18, AccentSoft, TextAlignmentOptions.Left, FontStyles.Bold);
            var promotedUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(promoted);
            promoted.avatarPager = Pager(panel, "Avatar Pager", 534, 132, promotedUdon, nameof(AviSeekPromoted._PrevAvatars), nameof(AviSeekPromoted._NextAvatars), out promoted.avatarPageLabel);
            const int avatarSlots = 3;
            promoted.avatarRows = new GameObject[avatarSlots];
            promoted.avatarNames = new TextMeshProUGUI[avatarSlots];
            promoted.avatarAuthors = new TextMeshProUGUI[avatarSlots];
            for (int i = 0; i < avatarSlots; i++)
            {
                var row = ListRow(panel, "Avatar " + (i + 1), 176f + i * 90f, IconShirt, promoted, i, nameof(AviSeekPromoted._SelectAvatar), nameof(AviSeekPromoted._UseAvatar), out var rowUdon);
                promoted.avatarRows[i] = row.gameObject;
                promoted.avatarNames[i] = Txt(row, "Name", "Avatar", 84, 10, 380, 32, 23, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                promoted.avatarNames[i].richText = false;
                promoted.avatarNames[i].overflowMode = TextOverflowModes.Ellipsis;
                promoted.avatarAuthors[i] = Txt(row, "Author", "by creator", 84, 42, 380, 28, 18, AccentSoft, TextAlignmentOptions.Left, FontStyles.Normal);
                promoted.avatarAuthors[i].richText = false;
                promoted.avatarAuthors[i].overflowMode = TextOverflowModes.Ellipsis;
                var view = Btn(row, "View", 476, 14, 92, 54, Field, rowUdon, nameof(AviSeekRow._Primary));
                Txt((RectTransform)view.transform, "Label", "View", 0, 0, 92, 54, 21, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                var use = Btn(row, "Use", 578, 14, 92, 54, Accent, rowUdon, nameof(AviSeekRow._Secondary));
                _accentImages.Add(use.GetComponent<Image>());
                Txt((RectTransform)use.transform, "Label", "Use", 0, 0, 92, 54, 21, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            }
            promoted.avatarsEmpty = Txt(panel, "No Avatars", "No promoted avatars right now.", 40, 176, 680, 82, 20, Muted, TextAlignmentOptions.Center, FontStyles.Italic).gameObject;
            promoted.avatarsEmpty.SetActive(false);

            Txt(panel, "Groups Heading", "GROUPS", 40, 456, 300, 30, 18, AccentSoft, TextAlignmentOptions.Left, FontStyles.Bold);
            promoted.groupPager = Pager(panel, "Group Pager", 534, 448, promotedUdon, nameof(AviSeekPromoted._PrevGroups), nameof(AviSeekPromoted._NextGroups), out promoted.groupPageLabel);
            const int groupSlots = 4;
            promoted.groupRows = new GameObject[groupSlots];
            promoted.groupNames = new TextMeshProUGUI[groupSlots];
            promoted.groupDetails = new TextMeshProUGUI[groupSlots];
            promoted.groupPictures = new RawImage[groupSlots];
            for (int i = 0; i < groupSlots; i++)
            {
                var row = ListRow(panel, "Group " + (i + 1), 492f + i * 90f, IconUsers, promoted, i, nameof(AviSeekPromoted._OpenGroup), "", out var rowUdon);
                promoted.groupRows[i] = row.gameObject;
                // the group's own icon covers the glyph once it arrives with the list
                var well = (RectTransform)row.Find("Icon Box");
                well.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                promoted.groupPictures[i] = Raw(well, "Group Icon", 0, 0, 62, 62);
                promoted.groupPictures[i].enabled = false;
                promoted.groupNames[i] = Txt(row, "Name", "Group", 84, 10, 470, 32, 23, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                promoted.groupNames[i].richText = false;
                promoted.groupNames[i].overflowMode = TextOverflowModes.Ellipsis;
                promoted.groupDetails[i] = Txt(row, "Details", "", 84, 42, 470, 28, 17, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
                promoted.groupDetails[i].overflowMode = TextOverflowModes.Ellipsis;
                var open = Btn(row, "Open", 566, 14, 104, 54, Accent, rowUdon, nameof(AviSeekRow._Primary));
                _accentImages.Add(open.GetComponent<Image>());
                Icon((RectTransform)open.transform, "Icon", 8, 0, 32, 54, IconExternal, 20, Color.white);
                Txt((RectTransform)open.transform, "Label", "Open", 38, 0, 62, 54, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            }
            promoted.groupsEmpty = Txt(panel, "No Groups", "No promoted groups right now.", 40, 492, 680, 82, 20, Muted, TextAlignmentOptions.Center, FontStyles.Italic).gameObject;
            promoted.groupsEmpty.SetActive(false);

            var footer = Txt(panel, "Footer", "Promote yours at <color=#a78bfa>aviseek.vrchatlegends.com/promote</color>", 40, 852, 680, 32, 18, Muted, TextAlignmentOptions.Center, FontStyles.Normal);
            footer.overflowMode = TextOverflowModes.Ellipsis;

            UdonSharpEditorUtility.CopyProxyToUdon(promoted);
            return promoted;
        }

        // a raised line with an icon well and an AviSeekRow that reports its index to the panel
        private static RectTransform ListRow(RectTransform panel, string name, float y, string glyph, UdonSharpBehaviour target, int index, string primary, string secondary, out UdonBehaviour rowUdon)
        {
            var row = Img(panel, name, 36, y, 688, 82, _rounded, RowBg);
            Round(row, 16);
            var r = row.rectTransform;
            var well = Inset(r, "Icon Box", 10, 10, 62, 62, 14);
            Icon(well.rectTransform, "Icon", 0, 0, 62, 62, glyph, 28, AccentSoft);
            var script = UdonSharpUndo.AddComponent<AviSeekRow>(row.gameObject);
            rowUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(script);
            script.target = target;
            script.index = index;
            script.primaryEvent = primary;
            script.secondaryEvent = secondary;
            UdonSharpEditorUtility.CopyProxyToUdon(script);
            return r;
        }

        // text that opens the vrchat keyboard for copying; the owning script undoes edits
        private static TMP_InputField CopyField(RectTransform parent, string name, float x, float y, float w, float h, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            Place(rect, parent, x, y, w, h);
            var field = go.AddComponent<TMP_InputField>();
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var areaRect = (RectTransform)area.transform;
            Place(areaRect, rect, 4, 0, w - 8, h);
            var text = Txt(areaRect, "Text", "", 0, 0, w - 8, h, size, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            text.richText = false;
            field.textViewport = areaRect;
            field.textComponent = text;
            // vrchat only opens its keyboard for editable fields, so this stays editable
            // and the panel's script puts the text back whenever a player changes it
            field.readOnly = false;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.richText = false;
            var hit = go.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            field.targetGraphic = hit;
            return field;
        }

        private static void BuildSearchPanel(RectTransform panel, AviSeekSearch search, UdonBehaviour udon)
        {
            // header
            var logo = Img(panel, "Logo", 50, 34, 140, 108, null, Color.white);
            logo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/AviSeekLogo.png");
            logo.preserveAspect = true;
            Lift(logo, 10f);
            search.logoImage = logo;
            search.brandLogo = logo.sprite;
            search.titleLabel = Txt(panel, "Title", "AVATAR <color=#847afc>SEARCH</color>", 208, 38, 660, 64, 56, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Lift(search.titleLabel, 10f);
            search.titleCountLabel = Txt(panel, "Count", "Loading...", 210, 102, 660, 34, 22, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            var profile = Btn(panel, "Profile Button", 880, 44, 170, 60, Field, udon, nameof(AviSeekSearch._OpenProfile));
            Icon((RectTransform)profile.transform, "Icon", 12, 0, 40, 60, IconCircleUser, 26, AccentSoft);
            Txt((RectTransform)profile.transform, "Label", "Profile", 56, 0, 80, 60, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var key = Img((RectTransform)profile.transform, "Key", 132, 16, 28, 28, _rounded, ButtonSoft);
            Round(key, 7);
            Txt(key.rectTransform, "Letter", "P", 0, 0, 28, 28, 16, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            search.titleCountLabel.overflowMode = TextOverflowModes.Ellipsis;

            // search row: what to search, the address box, then go / home / refresh
            var modes = new Image[2];
            modes[0] = Tab(panel, "Avatar Mode", 50, 162, 126, 64, IconShirt, null, "Avatar", udon, nameof(AviSeekSearch._ModeAvatar));
            modes[1] = Tab(panel, "Creator Mode", 184, 162, 136, 64, IconUser, null, "Creator", udon, nameof(AviSeekSearch._ModeCreator));
            search.searchModeTabs = modes;

            // the box holds the whole AviSeek address, which vrchat needs, but its own text is
            // hidden: the display on top shows only what the player typed
            var fieldBg = Inset(panel, "Search Field", 330, 162, 492, 64, 16);
            fieldBg.raycastTarget = true;
            var field = fieldBg.gameObject.AddComponent<VRCUrlInputField>();
            var fieldText = LegacyTxt(fieldBg.rectTransform, "Text", "", 22, new Color(1f, 1f, 1f, 0f));
            field.textComponent = fieldText;
            field.lineType = VRCUrlInputField.LineType.SingleLine;
            field.characterLimit = 200;
            field.targetGraphic = fieldBg;
            field.customCaretColor = true;
            field.caretColor = new Color(0f, 0f, 0f, 0f);
            field.selectionColor = new Color(0f, 0f, 0f, 0f);
            UnityEventTools.AddStringPersistentListener(field.onEndEdit, udon.SendCustomEvent, nameof(AviSeekSearch._Search));
            search.searchField = field;
            Icon(fieldBg.rectTransform, "Icon", 12, 0, 40, 64, IconSearch, 24, AccentSoft);
            search.searchDisplay = Txt(fieldBg.rectTransform, "Typed", "", 58, 0, 420, 64, 24, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
            search.searchDisplay.richText = false;
            search.searchDisplay.overflowMode = TextOverflowModes.Ellipsis;
            search.searchHint = Txt(fieldBg.rectTransform, "Hint", "Search avatars", 58, 0, 420, 64, 24, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.Left, FontStyles.Normal);

            _accentImages.Add(IconButton(panel, "Search Button", 832, 162, 64, 64, IconSearch, Accent, Color.white, udon, nameof(AviSeekSearch._Search)).GetComponent<Image>());
            IconButton(panel, "Home Button", 908, 162, 64, 64, IconHome, Field, Color.white, udon, nameof(AviSeekSearch._Home));
            IconButton(panel, "Refresh Button", 984, 162, 64, 64, IconRefresh, Field, Color.white, udon, nameof(AviSeekSearch._Refresh));

            // platform, sort and rank
            var tabs = new Image[4];
            tabs[0] = Tab(panel, "All Tab", 50, 242, 100, 56, IconLayers, null, "All", udon, nameof(AviSeekSearch._PlatformAll));
            tabs[1] = Tab(panel, "PC Tab", 156, 242, 100, 56, null, "Windows", "PC", udon, nameof(AviSeekSearch._PlatformPc));
            tabs[2] = Tab(panel, "Quest Tab", 262, 242, 124, 56, null, "Android", "Quest", udon, nameof(AviSeekSearch._PlatformQuest));
            tabs[3] = Tab(panel, "iOS Tab", 392, 242, 100, 56, null, "Apple", "iOS", udon, nameof(AviSeekSearch._PlatformIos));
            search.platformTabs = tabs;
            search.sortLabel = CycleButton(panel, "Sort Button", 504, 242, 268, IconSort, "Popular", udon, nameof(AviSeekSearch._CycleSort));
            search.rankLabel = CycleButton(panel, "Rank Button", 782, 242, 268, IconGauge, "Any Rank", udon, nameof(AviSeekSearch._CycleRank));

            // creator filter, favorites, pages
            var filterBg = Inset(panel, "Creator Filter", 50, 314, 360, 56, 14);
            filterBg.raycastTarget = true;
            Icon(filterBg.rectTransform, "Icon", 8, 0, 40, 56, IconUser, 22, AccentSoft);
            var filter = filterBg.gameObject.AddComponent<TMP_InputField>();
            var filterArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var filterRect = (RectTransform)filterArea.transform;
            Place(filterRect, filterBg.rectTransform, 50, 0, 300, 56);
            var filterText = Txt(filterRect, "Text", "", 0, 0, 300, 56, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
            filterText.richText = false;
            var filterHint = Txt(filterRect, "Placeholder", "Filter by creator...", 0, 0, 300, 56, 21, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.Left, FontStyles.Normal);
            filter.textViewport = filterRect;
            filter.textComponent = filterText;
            filter.placeholder = filterHint;
            filter.richText = false;
            filter.characterLimit = 40;
            filter.lineType = TMP_InputField.LineType.SingleLine;
            filter.targetGraphic = filterBg;
            UnityEventTools.AddStringPersistentListener(filter.onValueChanged, udon.SendCustomEvent, nameof(AviSeekSearch._CreatorFilterChanged));
            search.creatorFilterField = filter;
            // beside the box, not inside it, so pressing it never opens the keyboard
            IconButton(panel, "Clear Filter", 416, 314, 56, 56, IconX, Field, Color.white, udon, nameof(AviSeekSearch._ClearCreatorFilter));

            var favorites = Btn(panel, "Favorites Tab", 480, 314, 250, 56, Field, udon, nameof(AviSeekSearch._ToggleFavorites));
            search.favoritesTab = favorites.GetComponent<Image>();
            search.favoritesIcon = Icon((RectTransform)favorites.transform, "Icon", 12, 0, 40, 56, IconHeart, 24, Color.white);
            search.favoritesLabel = Txt((RectTransform)favorites.transform, "Label", "Favorites  0", 56, 0, 186, 56, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            IconButton(panel, "Prev Page", 740, 314, 60, 56, IconPrev, Field, Color.white, udon, nameof(AviSeekSearch._PrevPage));
            search.pageLabel = Txt(panel, "Page", "Page 1 / 1", 806, 314, 178, 56, 21, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            IconButton(panel, "Next Page", 990, 314, 60, 56, IconNext, Field, Color.white, udon, nameof(AviSeekSearch._NextPage));

            search.statusLabel = Txt(panel, "Status", "", 52, 376, 996, 46, 20, AccentSoft, TextAlignmentOptions.Left, FontStyles.Normal);
            search.statusLabel.enableWordWrapping = true;

            BuildDetails(panel, search, udon);

            // footer
            Icon(panel, "Globe", 50, 812, 30, 30, IconGlobe, 24, Muted);
            Txt(panel, "Site", "aviseek.vrchatlegends.com", 88, 808, 360, 38, 22, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            search.statsLabel = Txt(panel, "Stats", "", 450, 808, 600, 38, 22, Muted, TextAlignmentOptions.Right, FontStyles.Normal);
            Txt(panel, "Credits", "Avatar index powered by <color=#a78bfa>VRC-NEXUS</color>  ·  search.vrc-nexus.online", 50, 846, 1000, 32, 19, new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Left, FontStyles.Normal);
        }

        private static void BuildDetails(RectTransform panel, AviSeekSearch search, UdonBehaviour udon)
        {
            var box = Inset(panel, "Selected Avatar", 50, 428, 1000, 364, 24);
            box.color = new Color(0f, 0f, 0f, 0.3f);
            var rect = box.rectTransform;

            var empty = new GameObject("Empty", typeof(RectTransform));
            Place((RectTransform)empty.transform, rect, 0, 0, 1000, 364);
            Icon((RectTransform)empty.transform, "Icon", 470, 116, 60, 60, IconSparkles, 52, AccentSoft);
            Txt((RectTransform)empty.transform, "Hint", "Pick an avatar on the wall to see it here.\nPress <color=#ffffff>Use</color> to switch straight into it, or the heart to save it.", 60, 192, 880, 90, 24, Muted, TextAlignmentOptions.Center, FontStyles.Normal);
            search.detailsEmpty = empty;

            var details = new GameObject("Details", typeof(RectTransform));
            var d = (RectTransform)details.transform;
            Place(d, rect, 0, 0, 1000, 364);
            search.detailsRoot = details;
            details.SetActive(false);

            var thumbBg = Img(d, "Picture", 20, 20, 324, 324, _rounded, PanelLight);
            Round(thumbBg, 18);
            thumbBg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var placeholder = LoadingCard(thumbBg.rectTransform, 324, 324, 0.5f);
            search.detailPlaceholder = placeholder;
            // the picture sheet's tiles are 4:3, centred in the square
            search.detailPicture = Raw(thumbBg.rectTransform, "Picture", 0, 40, 324, 243);

            search.detailName = Txt(d, "Name", "Avatar", 368, 24, 466, 52, 40, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            search.detailName.richText = false;
            search.detailName.overflowMode = TextOverflowModes.Ellipsis;

            // the creator line doubles as "show me only this creator"
            var author = Btn(d, "Author", 362, 78, 470, 40, new Color(1f, 1f, 1f, 0.001f), udon, nameof(AviSeekSearch._FilterBySelectedCreator), false);
            search.detailAuthor = Txt((RectTransform)author.transform, "Label", "by creator", 6, 0, 420, 40, 24, AccentSoft, TextAlignmentOptions.Left, FontStyles.Normal);
            search.detailAuthor.richText = false;
            search.detailAuthor.overflowMode = TextOverflowModes.Ellipsis;
            Icon((RectTransform)author.transform, "Filter", 430, 0, 36, 40, IconFilter, 20, Muted);

            search.detailPcBadge = Badge(d, "PC", 846, 28, 40, "Windows").gameObject;
            search.detailQuestBadge = Badge(d, "Quest", 892, 28, 40, "Android").gameObject;
            search.detailIosBadge = Badge(d, "iOS", 938, 28, 40, "Apple").gameObject;

            search.detailRatings = Txt(d, "Ratings", "", 370, 128, 600, 120, 25, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            search.detailRatings.lineSpacing = 6f;

            var idBg = Inset(d, "Avatar ID", 366, 276, 362, 68, 14);
            idBg.raycastTarget = true;
            var idField = idBg.gameObject.AddComponent<TMP_InputField>();
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var areaRect = (RectTransform)area.transform;
            Place(areaRect, idBg.rectTransform, 14, 0, 334, 68);
            var idText = Txt(areaRect, "Text", "", 0, 0, 334, 68, 16, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            idText.richText = false;
            idField.textViewport = areaRect;
            idField.textComponent = idText;
            // editable so the keyboard opens for copying, _RestoreId undoes any change
            idField.readOnly = false;
            idField.lineType = TMP_InputField.LineType.SingleLine;
            idField.targetGraphic = idBg;
            idField.richText = false;
            UnityEventTools.AddStringPersistentListener(idField.onValueChanged, udon.SendCustomEvent, nameof(AviSeekSearch._RestoreId));
            UnityEventTools.AddStringPersistentListener(idField.onEndEdit, udon.SendCustomEvent, nameof(AviSeekSearch._RestoreId));
            search.detailId = idField;

            var heart = IconButton(d, "Favorite Button", 740, 276, 76, 68, IconHeart, Field, Color.white, udon, nameof(AviSeekSearch._FavoriteSelected));
            search.detailHeart = heart.GetComponentInChildren<TextMeshProUGUI>();

            var use = Btn(d, "Use Button", 828, 276, 152, 68, Accent, udon, nameof(AviSeekSearch._UseSelected));
            _accentImages.Add(use.GetComponent<Image>());
            Icon((RectTransform)use.transform, "Icon", 22, 0, 40, 68, IconShirt, 30, Color.white);
            Txt((RectTransform)use.transform, "Label", "Use", 62, 0, 80, 68, 30, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void BuildResults(RectTransform panel, AviSeekSearch search, UdonBehaviour udon)
        {
            const float cardW = 300f, cardH = 390f, gap = 24f;
            float startX = (1980f - (Columns * cardW + (Columns - 1) * gap)) * 0.5f;
            float startY = (900f - (Rows * cardH + (Rows - 1) * gap)) * 0.5f;
            var cards = new AviSeekCard[Columns * Rows];
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    int slot = r * Columns + c;
                    cards[slot] = BuildCard(panel, search, slot, startX + c * (cardW + gap), startY + r * (cardH + gap), cardW, cardH);
                }
            }
            search.cards = cards;
        }

        private static AviSeekCard BuildCard(RectTransform parent, AviSeekSearch search, int slot, float x, float y, float w, float h)
        {
            // the holder carries the script so hiding a card also hides its shadow
            var holder = new GameObject("Card " + (slot + 1), typeof(RectTransform));
            var holderRect = (RectTransform)holder.transform;
            Place(holderRect, parent, x, y, w, h);
            var shadow = Img(holderRect, "Shadow", -26, -14, w + 52, h + 52, _shadow, new Color(0f, 0f, 0f, 0.75f));
            shadow.type = Image.Type.Sliced;
            // and the card itself floats 16mm off the panel
            var frame = Img(holderRect, "Body", 0, 0, w, h, _rounded, CardOff);
            Round(frame, 22);
            Lift(frame, 16f);
            var root = frame.rectTransform;
            var card = UdonSharpUndo.AddComponent<AviSeekCard>(holder);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(card);
            card.search = search;
            card.slot = slot;
            card.frame = frame;

            var inner = Img(root, "Inner", 3, 3, w - 6, h - 6, _rounded, Panel);
            Round(inner, 20);
            inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var r = inner.rectTransform;
            float iw = w - 6f;

            // the picture is 4:3, so the text sits under it
            float ph = Mathf.Round(iw * 0.75f);
            var area = new GameObject("Picture Area", typeof(RectTransform));
            var areaRect = (RectTransform)area.transform;
            Place(areaRect, r, 0, 0, iw, ph);
            card.placeholder = LoadingCard(areaRect, iw, ph, slot / 12f);
            Sheen(r, iw, 120f, 0.045f);

            // the picture itself selects the card
            var select = Btn(areaRect, "Select", 0, 0, iw, ph, new Color(1f, 1f, 1f, 0f), udon, nameof(AviSeekCard._Select), false);
            select.transition = Selectable.Transition.None;
            card.picture = Raw(areaRect, "Picture", 0, 0, iw, ph);

            card.nameLabel = Txt(r, "Name", "Avatar", 14, ph + 6, iw - 28, 38, 26, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            card.nameLabel.richText = false;
            card.nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            card.authorLabel = Txt(r, "Author", "by creator", 14, ph + 42, iw - 28, 28, 18, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Left, FontStyles.Bold);
            card.authorLabel.richText = false;
            card.authorLabel.overflowMode = TextOverflowModes.Ellipsis;

            // bottom bar
            float barY = iw + 8f;
            var heart = IconButton(r, "Favorite", 10, barY, 64, 64, IconHeart, Field, Color.white, udon, nameof(AviSeekCard._Favorite));
            card.heartIcon = heart.GetComponentInChildren<TextMeshProUGUI>();

            // the platform logos, each tinted with that platform's performance rank
            string[] platforms = { "Windows", "Android", "Apple" };
            var dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                dots[i] = Img(r, platforms[i] + " Rating", 82 + i * 36, barY + 16, 31, 31, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/" + platforms[i] + " Glyph.png"), Color.gray);
                dots[i].preserveAspect = true;
            }
            card.pcDot = dots[0];
            card.questDot = dots[1];
            card.iosDot = dots[2];

            var use = Btn(r, "Use", iw - 104, barY, 94, 64, ButtonSoft, udon, nameof(AviSeekCard._Use));
            Txt((RectTransform)use.transform, "Label", "Use", 0, 0, 94, 64, 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

            UdonSharpEditorUtility.CopyProxyToUdon(card);
            return card;
        }

        // what a picture spot shows while vrchat fetches the picture: drifting stripes and a
        // shimmer from the loading shader, with the aviseek logo floating in the middle. phase
        // (0 to 1) puts each card out of step, carried to the shaders in the alpha
        private static GameObject LoadingCard(RectTransform area, float w, float h, float phase)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            Place(rect, area, 0, 0, w, h);
            float alpha = 1f - Mathf.Repeat(phase, 1f) * 0.1f;
            var back = Img(rect, "Loading", 0, 0, w, h, null, new Color(1f, 1f, 1f, alpha));
            back.material = ShaderMaterial("Loading Card", "AviSeek/Loading Card");
            back.raycastTarget = false;
            float size = Mathf.Min(w, h) * 0.42f;
            var logo = Img(rect, "Logo", (w - size) * 0.5f, (h - size) * 0.5f - h * 0.04f, size, size,
                AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/AviSeekIcon.png"), new Color(1f, 1f, 1f, alpha));
            logo.preserveAspect = true;
            logo.material = ShaderMaterial("Loading Logo", "AviSeek/UI Float");
            logo.raycastTarget = false;
            var label = Txt(rect, "Label", "Picture on the way", 0, h - 40f, w, 30, 17, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center, FontStyles.Bold);
            label.raycastTarget = false;
            return go;
        }

        private static Material ShaderMaterial(string name, string shader)
        {
            string path = Generated + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = Shader.Find(shader);
            EditorUtility.SetDirty(material);
            return material;
        }

        // the pedestal only switches avatars. vrchat paints a big picture of the avatar above it,
        // so the placement sits far under the floor where nobody sees that picture
        private static GameObject BuildPedestal(Transform root)
        {
            var go = new GameObject("Avatar Pedestal");
            go.transform.SetParent(root, false);
            var placement = new GameObject("Placement");
            placement.transform.SetParent(go.transform, false);
            placement.transform.localPosition = new Vector3(0f, -200f, 0f);
            var pedestal = go.AddComponent<VRCAvatarPedestal>();
            pedestal.blueprintId = "";
            pedestal.Placement = placement.transform;
            pedestal.ChangeAvatarsOnUse = true;
            pedestal.scale = 0.01f;
            return go;
        }

        private static Material MakeMaterial(string name, Color color, Color emission, float smoothness)
        {
            string path = Generated + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /* ───────────────────────────── supporters ───────────────────────────── */

        private static AviSeekSupporters BuildSupporters(RectTransform panel)
        {
            var supporters = UdonSharpUndo.AddComponent<AviSeekSupporters>(panel.parent.gameObject);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(supporters);

            Lift(Icon(panel, "Icon", 36, 34, 60, 64, IconHeartHandshake, 44, AccentSoft), 10f);
            Lift(Txt(panel, "Title", "SUPPORTERS", 104, 32, 580, 64, 48, Color.white, TextAlignmentOptions.Left, FontStyles.Bold), 10f);
            Txt(panel, "Subtitle", "Thank you for keeping AviSeek running", 106, 92, 580, 30, 21, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            supporters.countLabel = Txt(panel, "Count", "", 40, 132, 440, 30, 18, AccentSoft, TextAlignmentOptions.Left, FontStyles.Bold);
            supporters.countLabel.richText = true;
            supporters.pager = Pager(panel, "Pager", 494, 126, udon, nameof(AviSeekSupporters._Prev), nameof(AviSeekSupporters._Next), out supporters.pageLabel);

            const int slots = 8;
            // the lines live in one group so a page turn can fade them together
            var lines = new GameObject("Rows", typeof(RectTransform), typeof(CanvasGroup));
            var linesRect = (RectTransform)lines.transform;
            Place(linesRect, panel, 0, 0, 720, 900);
            supporters.rowsGroup = lines.GetComponent<CanvasGroup>();
            supporters.rows = new GameObject[slots];
            supporters.rowBacks = new Image[slots];
            supporters.rowNames = new TextMeshProUGUI[slots];
            supporters.rowTiers = new TextMeshProUGUI[slots];
            supporters.rowTierBacks = new Image[slots];
            supporters.rowRanks = new TextMeshProUGUI[slots];
            supporters.rowPictures = new RawImage[slots];
            supporters.rowCircles = new Image[slots];
            supporters.rowInitials = new TextMeshProUGUI[slots];
            for (int i = 0; i < slots; i++)
            {
                var row = Img(linesRect, "Supporter " + (i + 1), 36, 172f + i * 68f, 648, 60, _rounded, RowBg);
                Round(row, 14);
                var r = row.rectTransform;
                supporters.rows[i] = row.gameObject;
                supporters.rowBacks[i] = row;
                supporters.rowRanks[i] = Txt(r, "Rank", (i + 1).ToString(), 0, 0, 58, 60, 24, AccentSoft, TextAlignmentOptions.Center, FontStyles.Bold);
                // their picture in a circle, or the first letter of their name until it arrives
                var circle = Img(r, "Picture", 58, 8, 44, 44, _circle, Field);
                circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                supporters.rowCircles[i] = circle;
                supporters.rowInitials[i] = Txt(circle.rectTransform, "Initial", "S", 0, 0, 44, 44, 21, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                supporters.rowPictures[i] = Raw(circle.rectTransform, "Icon", 0, 0, 44, 44);
                supporters.rowNames[i] = Txt(r, "Name", "Supporter", 114, 0, 280, 60, 23, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                supporters.rowNames[i].overflowMode = TextOverflowModes.Ellipsis;
                var tier = Img(r, "Tier", 398, 12, 238, 36, _rounded, Field);
                Round(tier, 18);
                supporters.rowTierBacks[i] = tier;
                supporters.rowTiers[i] = Txt(tier.rectTransform, "Label", "", 8, 0, 222, 36, 17, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                supporters.rowTiers[i].overflowMode = TextOverflowModes.Ellipsis;
                row.gameObject.SetActive(false);
            }
            supporters.rowBase = RowBg;
            supporters.emptyLabel = Txt(panel, "Empty", "Supporters show up here.", 40, 172, 640, 60, 21, Muted, TextAlignmentOptions.Center, FontStyles.Normal).gameObject;

            Txt(panel, "Become", "Become a supporter", 40, 732, 640, 30, 20, AccentSoft, TextAlignmentOptions.Left, FontStyles.Bold);
            Txt(panel, "Become Hint", "Click the link to open the keyboard and copy it.", 40, 762, 640, 28, 17, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            Round(Img(panel, "Support Link Box", 36, 796, 648, 64, _rounded, RowBg), 16);
            var link = CopyField(panel, "Support Link", 52, 800, 616, 56, 21);
            link.textComponent.color = Color.white;
            link.text = supporters.supportLink;
            UnityEventTools.AddStringPersistentListener(link.onValueChanged, udon.SendCustomEvent, nameof(AviSeekSupporters._RestoreLink));
            UnityEventTools.AddStringPersistentListener(link.onEndEdit, udon.SendCustomEvent, nameof(AviSeekSupporters._RestoreLink));
            supporters.linkField = link;

            UdonSharpEditorUtility.CopyProxyToUdon(supporters);
            return supporters;
        }

        // previous, "1 / 3", next. hidden until a list runs past one page
        private static GameObject Pager(RectTransform panel, string name, float x, float y, UdonBehaviour udon, string prev, string next, out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            Place(rect, panel, x, y, 190, 44);
            IconButton(rect, "Prev", 0, 0, 48, 44, IconPrev, Field, Color.white, udon, prev);
            label = Txt(rect, "Page", "1 / 1", 50, 0, 90, 44, 18, Muted, TextAlignmentOptions.Center, FontStyles.Bold);
            IconButton(rect, "Next", 142, 0, 48, 44, IconNext, Field, Color.white, udon, next);
            go.SetActive(false);
            return go;
        }

        /* ───────────────────────────── creator store ───────────────────────────── */

        private static void BuildStore(RectTransform panel)
        {
            var store = UdonSharpUndo.AddComponent<AviSeekStore>(panel.parent.gameObject);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(store);

            Lift(Icon(panel, "Icon", 36, 34, 60, 64, IconShoppingBag, 44, AccentSoft), 10f);
            Lift(Txt(panel, "Title", "STORE", 104, 32, 620, 64, 48, Color.white, TextAlignmentOptions.Left, FontStyles.Bold), 10f);
            Txt(panel, "Subtitle", "Support this world", 106, 92, 400, 30, 21, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            store.pager = Pager(panel, "Pager", 534, 88, udon, nameof(AviSeekStore._Prev), nameof(AviSeekStore._Next), out store.pageLabel);
            store.announceLabel = Txt(panel, "Announcement", "", 40, 132, 680, 30, 19, AccentSoft, TextAlignmentOptions.Left, FontStyles.Bold);
            store.announceLabel.overflowMode = TextOverflowModes.Ellipsis;

            const int slots = 4;
            store.rows = new GameObject[slots];
            store.rowNames = new TextMeshProUGUI[slots];
            store.rowDescriptions = new TextMeshProUGUI[slots];
            store.rowPictures = new RawImage[slots];
            store.rowOwned = new GameObject[slots];
            store.rowGetButtons = new GameObject[slots];
            for (int i = 0; i < slots; i++)
            {
                var row = ListRow(panel, "Product " + (i + 1), 172f + i * 122f, IconGift, store, i, nameof(AviSeekStore._Get), "", out var rowUdon);
                row.sizeDelta = new Vector2(688, 112);
                store.rows[i] = row.gameObject;
                // the product picture covers the icon well when the product has one
                var well = row.Find("Icon Box") as RectTransform;
                well.sizeDelta = new Vector2(92, 92);
                var iconLabel = well.Find("Icon") as RectTransform;
                iconLabel.sizeDelta = new Vector2(92, 92);
                well.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                store.rowPictures[i] = Raw(well, "Picture", 0, 0, 92, 92);
                store.rowNames[i] = Txt(row, "Name", "Product", 116, 12, 410, 34, 24, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                store.rowNames[i].overflowMode = TextOverflowModes.Ellipsis;
                store.rowDescriptions[i] = Txt(row, "Description", "", 116, 48, 410, 54, 17, Muted, TextAlignmentOptions.TopLeft, FontStyles.Normal);
                store.rowDescriptions[i].enableWordWrapping = true;
                store.rowDescriptions[i].overflowMode = TextOverflowModes.Ellipsis;

                var owned = Img(row, "Owned", 538, 34, 136, 44, _rounded, Hex("143a2c"));
                Round(owned, 22);
                Icon(owned.rectTransform, "Icon", 8, 0, 34, 44, IconBadgeCheck, 22, Hex("6ee7b7"));
                Txt(owned.rectTransform, "Label", "Owned", 44, 0, 86, 44, 19, Hex("6ee7b7"), TextAlignmentOptions.Left, FontStyles.Bold);
                owned.gameObject.SetActive(false);
                store.rowOwned[i] = owned.gameObject;

                var holder = new GameObject("Get Holder", typeof(RectTransform));
                Place((RectTransform)holder.transform, row, 0, 0, 688, 112);
                var get = Btn((RectTransform)holder.transform, "Get", 548, 30, 126, 52, Accent, rowUdon, nameof(AviSeekRow._Primary));
                _accentImages.Add(get.GetComponent<Image>());
                Icon((RectTransform)get.transform, "Icon", 10, 0, 36, 52, IconShoppingBag, 22, Color.white);
                Txt((RectTransform)get.transform, "Label", "Get", 48, 0, 70, 52, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                store.rowGetButtons[i] = holder;
                row.gameObject.SetActive(false);
            }
            store.emptyLabel = Txt(panel, "Empty", "Add your UdonProducts to the Store Panel\nand they show up here.", 40, 172, 680, 90, 21, Muted, TextAlignmentOptions.Center, FontStyles.Normal).gameObject;

            store.groupStoreButton = StoreButton(panel, "Group Store", 36, IconUsers, "Group store", udon, nameof(AviSeekStore._OpenGroupStore));
            store.worldStoreButton = StoreButton(panel, "World Store", 388, IconStore, "World store", udon, nameof(AviSeekStore._OpenWorldStore));

            var footer = Txt(panel, "Footer", "Purchases open in VRChat's own store window.", 40, 852, 680, 32, 18, Muted, TextAlignmentOptions.Center, FontStyles.Normal);
            footer.overflowMode = TextOverflowModes.Ellipsis;

            UdonSharpEditorUtility.CopyProxyToUdon(store);
        }

        // in a holder with its shadow, so the store can hide the pair
        private static GameObject StoreButton(RectTransform panel, string name, float x, string glyph, string text, UdonBehaviour udon, string eventName)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            Place(rect, panel, x, 676, 336, 60);
            var button = Btn(rect, "Button", 0, 0, 336, 60, Field, udon, eventName);
            Icon((RectTransform)button.transform, "Icon", 14, 0, 40, 60, glyph, 24, AccentSoft);
            Txt((RectTransform)button.transform, "Label", text, 58, 0, 270, 60, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            return holder;
        }

        /* ───────────────────────────── profile menu ───────────────────────────── */

        // the profile menu, the wrist button that opens it and the world's music
        private static AviSeekMenu BuildMenu(Transform root, AviSeekSearch search)
        {
            var go = new GameObject("Profile Menu", typeof(RectTransform));
            go.transform.SetParent(root, false);
            // its own object: the menu canvas switches off while closed, this never does
            var logic = new GameObject("AviSeek Menu");
            logic.transform.SetParent(root, false);
            var menuScript = UdonSharpUndo.AddComponent<AviSeekMenu>(logic);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(menuScript);
            menuScript.search = search;
            menuScript.supporters = search.supporters;
            search.menu = menuScript;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            go.AddComponent<GraphicRaycaster>();
            var rect = (RectTransform)go.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MenuW, MenuH);
            rect.localScale = Vector3.one * MenuScale;
            rect.localPosition = new Vector3(0f, PanelCentreY, -0.6f);
            go.AddComponent<VRCUiShape>();
            // the pointer still finds it, but it never pushes the player around
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(MenuW, MenuH, 2f);
            menuScript.menu = rect;

            Slab(rect, "Body", new Vector3(0f, 0f, 2f), MenuW, MenuH, 36f, 34f, _bodyMaterial);
            Slab(rect, "Rim", new Vector3(0f, 0f, 18f), MenuW + 14f, MenuH + 14f, 42f, 8f, _rimMaterial);
            var back = Img(rect, "Back", 0, 0, MenuW, MenuH, _rounded, MenuBack);
            Round(back, 36);
            back.raycastTarget = true;
            back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var panel = back.rectTransform;
            var glow = Img(panel, "Glow", 200, -440, 800, 760, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.42f));
            glow.raycastTarget = false;
            var floor = Img(panel, "Floor", 0, MenuH - 420, MenuW, 420, _fade, new Color(Ink.r, Ink.g, Ink.b, 0.85f));
            floor.raycastTarget = false;

            // header: sync, size and close on the right
            var logo = Img(panel, "Logo", 30, 22, 56, 56, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/AviSeekIcon.png"), Color.white);
            logo.preserveAspect = true;
            Txt(panel, "Title", "AviSeek", 92, 18, 400, 40, 34, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Txt(panel, "Subtitle", "Your profile", 93, 56, 400, 26, 19, MenuMuted, TextAlignmentOptions.Left, FontStyles.Normal);
            var sync = Btn(panel, "Sync Button", 770, 22, 170, 56, MenuTile, udon, nameof(AviSeekMenu._Sync));
            Icon((RectTransform)sync.transform, "Icon", 16, 0, 40, 56, IconRefresh, 24, MenuAccent);
            Txt((RectTransform)sync.transform, "Label", "Sync", 62, 0, 96, 56, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            IconButton(panel, "Smaller Button", 956, 22, 56, 56, IconZoomOut, MenuTile, Color.white, udon, nameof(AviSeekMenu._Smaller));
            IconButton(panel, "Bigger Button", 1022, 22, 56, 56, IconZoomIn, MenuTile, Color.white, udon, nameof(AviSeekMenu._Bigger));
            IconButton(panel, "Close Button", 1114, 22, 56, 56, IconX, MenuTile, Color.white, udon, nameof(AviSeekMenu._Close));
            Img(panel, "Header Line", 30, 96, 1140, 2, null, new Color(1f, 1f, 1f, 0.08f));

            // the profile card, signed in or not: the picture glows in the tier's colour
            var card = Img(panel, "Profile Card", 30, 116, 380, 618, _rounded, MenuPanel);
            Round(card, 24);
            var cardRect = card.rectTransform;
            menuScript.avatarGlow = Img(cardRect, "Halo", 30, -30, 320, 300, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.5f));
            menuScript.avatarRing = Img(cardRect, "Ring", 100, 26, 180, 180, _ring, Accent);
            var circle = Img(cardRect, "Avatar", 115, 41, 150, 150, _circle, MenuTile);
            circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            menuScript.avatarCircle = circle;
            menuScript.initialLabel = Txt(circle.rectTransform, "Initial", "?", 0, 0, 150, 150, 68, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            menuScript.avatarPicture = Raw(circle.rectTransform, "Picture", 0, 0, 150, 150);
            menuScript.nameLabel = Txt(cardRect, "Name", "Not signed in", 20, 214, 340, 44, 30, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            menuScript.nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            menuScript.playerLabel = Txt(cardRect, "Player", "", 20, 258, 340, 28, 19, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            menuScript.playerLabel.overflowMode = TextOverflowModes.Ellipsis;
            // centred under the name, the menu sizes it to the tier's name
            var pill = Img(cardRect, "Tier", 0, 298, 160, 40, _rounded, MenuTile);
            Round(pill, 20);
            pill.rectTransform.pivot = new Vector2(0.5f, 1f);
            pill.rectTransform.anchoredPosition = new Vector2(190f, -298f);
            menuScript.tierPill = pill;
            menuScript.tierLabel = Txt(pill.rectTransform, "Label", "", 0, 0, 160, 40, 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var tierRect = menuScript.tierLabel.rectTransform;
            tierRect.anchorMin = Vector2.zero;
            tierRect.anchorMax = Vector2.one;
            tierRect.pivot = new Vector2(0.5f, 0.5f);
            tierRect.offsetMin = Vector2.zero;
            tierRect.offsetMax = Vector2.zero;
            menuScript.rankLabel = Txt(cardRect, "Rank", "", 20, 348, 340, 28, 20, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);

            // signed in: how full the collection is, stats, recent favorites, folders, sign out
            var signedIn = new GameObject("Signed In", typeof(RectTransform));
            var inRect = (RectTransform)signedIn.transform;
            Place(inRect, panel, 0, 0, MenuW, MenuH);
            menuScript.signedInRoot = signedIn;
            menuScript.limitLabel = Txt(inRect, "Limit", "0 / 5 favorites", 60, 510, 320, 30, 19, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var track = Img(inRect, "Limit Track", 60, 546, 320, 14, _rounded, MenuTile);
            Round(track, 7);
            var fill = Img(track.rectTransform, "Fill", 0, 0, 320, 14, _rounded, Accent);
            Round(fill, 7);
            _accentImages.Add(fill);
            menuScript.limitFill = fill.rectTransform;
            var more = Txt(inRect, "Limit Hint", "Supporters get more slots at aviseek.vrchatlegends.com", 60, 568, 320, 44, 15, MenuMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            more.enableWordWrapping = true;
            menuScript.signedInAtLabel = Txt(inRect, "Signed In At", "", 60, 616, 320, 30, 16, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            menuScript.signedInAtLabel.enableWordWrapping = true;
            var signOut = Btn(inRect, "Sign Out Button", 80, 662, 280, 56, MenuTile, udon, nameof(AviSeekMenu._SignOut));
            Icon((RectTransform)signOut.transform, "Icon", 20, 0, 40, 56, IconLogOut, 24, MenuAccent);
            Txt((RectTransform)signOut.transform, "Label", "Sign out", 66, 0, 200, 56, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            menuScript.creditsValue = StatTile(inRect, "Credits", 430, IconCoins, "Credits");
            menuScript.favoritesValue = StatTile(inRect, "Favorites", 620, IconHeart, "Favorites");
            menuScript.foldersValue = StatTile(inRect, "Folders", 810, IconFolder, "Folders");
            menuScript.tierValue = StatTile(inRect, "Tier", 1000, IconCrown, "Tier");

            Icon(inRect, "Recent Icon", 430, 268, 40, 48, IconHeart, 24, MenuAccent);
            Txt(inRect, "Recent Title", "Recently saved", 476, 268, 400, 48, 26, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var noRecent = Txt(inRect, "Recent Empty", "Press the heart on any avatar on the wall and it shows up here.", 430, 340, 740, 60, 19, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            menuScript.recentEmpty = noRecent.gameObject;
            const int recent = 5;
            menuScript.recentSlots = new GameObject[recent];
            menuScript.recentPictures = new RawImage[recent];
            menuScript.recentNames = new TextMeshProUGUI[recent];
            string[] recentEvents = { nameof(AviSeekMenu._Recent0), nameof(AviSeekMenu._Recent1), nameof(AviSeekMenu._Recent2), nameof(AviSeekMenu._Recent3), nameof(AviSeekMenu._Recent4) };
            for (int i = 0; i < recent; i++)
            {
                var slot = Btn(inRect, "Recent " + (i + 1), 430 + i * 151, 318, 136, 134, MenuPanel, udon, recentEvents[i], false);
                var slotRect = (RectTransform)slot.transform;
                slot.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                Icon(slotRect, "Placeholder", 0, 0, 136, 102, IconSparkles, 30, new Color(1f, 1f, 1f, 0.15f));
                menuScript.recentPictures[i] = Raw(slotRect, "Picture", 0, 0, 136, 102);
                menuScript.recentNames[i] = Txt(slotRect, "Name", "", 8, 104, 120, 28, 15, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                menuScript.recentNames[i].overflowMode = TextOverflowModes.Ellipsis;
                menuScript.recentNames[i].richText = false;
                menuScript.recentSlots[i] = slot.gameObject;
            }

            Icon(inRect, "Folders Icon", 430, 466, 40, 48, IconFolderHeart, 26, MenuAccent);
            Txt(inRect, "Folders Title", "Your folders", 476, 466, 300, 48, 26, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var all = Btn(inRect, "All Favorites Button", 846, 468, 170, 44, MenuTile, udon, nameof(AviSeekMenu._AllFavorites));
            Txt((RectTransform)all.transform, "Label", "Show all", 0, 0, 170, 44, 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            IconButton(inRect, "Folders Prev", 1026, 468, 44, 44, IconPrev, MenuTile, Color.white, udon, nameof(AviSeekMenu._FolderPrev));
            IconButton(inRect, "Folders Next", 1126, 468, 44, 44, IconNext, MenuTile, Color.white, udon, nameof(AviSeekMenu._FolderNext));
            menuScript.folderPageLabel = Txt(inRect, "Folders Page", "", 1070, 468, 56, 44, 16, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            var noFolders = Txt(inRect, "Folders Empty", "No folders yet. Make one on the Folders panel by the wall, or on the website.", 430, 540, 740, 90, 19, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            noFolders.enableWordWrapping = true;
            menuScript.foldersEmpty = noFolders.gameObject;
            const int folderRows = 4;
            menuScript.folderRows = new GameObject[folderRows];
            menuScript.folderNames = new TextMeshProUGUI[folderRows];
            menuScript.folderCounts = new TextMeshProUGUI[folderRows];
            string[] folderEvents = { nameof(AviSeekMenu._Folder0), nameof(AviSeekMenu._Folder1), nameof(AviSeekMenu._Folder2), nameof(AviSeekMenu._Folder3) };
            for (int i = 0; i < folderRows; i++)
            {
                float x = 430 + (i % 2) * 375;
                float y = 526 + (i / 2) * 72;
                var row = Btn(inRect, "Folder " + (i + 1), x, y, 365, 62, MenuPanel, udon, folderEvents[i], false);
                var rowRect = (RectTransform)row.transform;
                Icon(rowRect, "Icon", 14, 0, 40, 62, IconFolder, 24, MenuAccent);
                menuScript.folderNames[i] = Txt(rowRect, "Name", "Folder", 60, 0, 240, 62, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                menuScript.folderNames[i].overflowMode = TextOverflowModes.Ellipsis;
                menuScript.folderCounts[i] = Txt(rowRect, "Count", "0", 295, 0, 56, 62, 20, MenuMuted, TextAlignmentOptions.Right, FontStyles.Normal);
                menuScript.folderRows[i] = row.gameObject;
            }

            // signed out: how to get a code, the box to type it in, and what signing in gives
            var signedOut = new GameObject("Signed Out", typeof(RectTransform));
            var outRect = (RectTransform)signedOut.transform;
            Place(outRect, panel, 0, 0, MenuW, MenuH);
            menuScript.signedOutRoot = signedOut;
            var how = Img(outRect, "Sign In Card", 430, 116, 740, 400, _rounded, MenuPanel);
            Round(how, 24);
            var howRect = how.rectTransform;
            Icon(howRect, "Icon", 30, 26, 48, 52, IconKey, 30, MenuAccent);
            Txt(howRect, "Title", "Sign in with VRChat Legends", 88, 26, 620, 52, 28, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var steps = Txt(howRect, "Steps",
                "1  On your phone or PC, open <color=#a78bfa>aviseek.vrchatlegends.com/account</color>\n" +
                "2  Under <b>VRChat worlds</b>, enter your VRChat name and get a code\n" +
                "3  Tap the box below and type the code after the address",
                30, 96, 680, 130, 20, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            steps.enableWordWrapping = true;
            steps.lineSpacing = 18f;
            steps.richText = true;

            var fieldBg = Img(howRect, "Code Field", 30, 250, 520, 72, _rounded, MenuTile);
            Round(fieldBg, 18);
            fieldBg.raycastTarget = true;
            var field = fieldBg.gameObject.AddComponent<VRCUrlInputField>();
            var fieldText = LegacyTxt(fieldBg.rectTransform, "Text", "", 22, new Color(1f, 1f, 1f, 0f));
            field.textComponent = fieldText;
            field.lineType = VRCUrlInputField.LineType.SingleLine;
            field.characterLimit = 80;
            field.targetGraphic = fieldBg;
            field.customCaretColor = true;
            field.caretColor = new Color(0f, 0f, 0f, 0f);
            field.selectionColor = new Color(0f, 0f, 0f, 0f);
            UnityEventTools.AddStringPersistentListener(field.onEndEdit, udon.SendCustomEvent, nameof(AviSeekMenu._SignIn));
            menuScript.signInField = field;
            Icon(fieldBg.rectTransform, "Icon", 14, 0, 40, 72, IconKey, 24, MenuAccent);
            menuScript.signInDisplay = Txt(fieldBg.rectTransform, "Typed", "", 62, 0, 440, 72, 30, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            menuScript.signInDisplay.richText = false;
            menuScript.signInDisplay.characterSpacing = 12f;
            menuScript.signInHint = Txt(fieldBg.rectTransform, "Hint", "ABCD-EFGH", 62, 0, 440, 72, 26, new Color(1f, 1f, 1f, 0.3f), TextAlignmentOptions.Left, FontStyles.Normal);
            var signIn = Btn(howRect, "Sign In Button", 566, 250, 144, 72, Accent, udon, nameof(AviSeekMenu._SignIn));
            _accentImages.Add(signIn.GetComponent<Image>());
            Icon((RectTransform)signIn.transform, "Icon", 12, 0, 40, 72, IconLogIn, 24, Color.white);
            Txt((RectTransform)signIn.transform, "Label", "Go", 52, 0, 80, 72, 24, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var safety = Txt(howRect, "Safety", "The code only works for your VRChat name, once, for ten minutes. This world can save favorites and folders for you, nothing else, and you can unlink it on the website any time.",
                30, 336, 680, 52, 16, MenuMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            safety.enableWordWrapping = true;
            menuScript.signInPrefixUrl = new VRCUrl(SignInPrefix);

            var perks = Img(outRect, "Perks", 430, 530, 740, 126, _rounded, MenuPanel);
            Round(perks, 24);
            string[] perkIcons = { IconHeart, IconFolderHeart, IconCrown };
            string[] perkText = { "Favorites save to your account", "Folders sync with the website", "Your name shines gold on the board" };
            for (int i = 0; i < 3; i++)
            {
                Icon(perks.rectTransform, "Perk Icon " + (i + 1), 20 + i * 240, 18, 220, 40, perkIcons[i], 28, MenuAccent);
                var line = Txt(perks.rectTransform, "Perk " + (i + 1), perkText[i], 20 + i * 240, 62, 220, 50, 17, Color.white, TextAlignmentOptions.Top, FontStyles.Normal);
                line.enableWordWrapping = true;
            }

            // music, for everyone
            var bar = Img(panel, "Music Bar", 430, 672, 740, 62, _rounded, MenuPanel);
            Round(bar, 18);
            var barRect = bar.rectTransform;
            var toggle = Btn(barRect, "Music Button", 8, 7, 270, 48, MenuTile, udon, nameof(AviSeekMenu._ToggleMusic), false);
            menuScript.musicIcon = Icon((RectTransform)toggle.transform, "Icon", 10, 0, 40, 48, IconHeadphones, 24, MenuAccent);
            menuScript.musicLabel = Txt((RectTransform)toggle.transform, "Label", "Music on", 56, 0, 200, 48, 20, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Icon(barRect, "Volume Icon", 300, 0, 40, 62, IconVolume, 22, MenuMuted);
            IconButton(barRect, "Volume Down", 350, 7, 72, 48, IconMinus, MenuTile, Color.white, udon, nameof(AviSeekMenu._MusicDown));
            var vtrack = Img(barRect, "Volume Track", 436, 24, 140, 14, _rounded, MenuTile);
            Round(vtrack, 7);
            var vfill = Img(vtrack.rectTransform, "Fill", 0, 0, 140, 14, _rounded, MenuAccent);
            Round(vfill, 7);
            menuScript.volumeFill = vfill.rectTransform;
            menuScript.volumeLabel = Txt(barRect, "Volume", "25%", 580, 0, 70, 62, 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var up = Btn(barRect, "Volume Up", 660, 7, 72, 48, MenuTile, udon, nameof(AviSeekMenu._MusicUp));
            Icon((RectTransform)up.transform, "Icon", 0, 0, 72, 48, IconPlus, 22, Color.white);

            menuScript.statusLabel = Txt(panel, "Status", "", 30, 748, 1140, 56, 18, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            menuScript.statusLabel.enableWordWrapping = true;
            menuScript.statusLabel.richText = true;

            // vr handles, outside the menu's edge: the bar moves it, a corner sizes it
            var handles = new List<Image>();
            var bottom = Handle(rect, "Move Bar", MenuW * 0.5f, MenuH + 34f, 300f, 30f, _rounded);
            Round(bottom, 15);
            Icon(bottom.rectTransform, "Grip", 0, 0, 300, 30, IconGrip, 26, new Color(0f, 0f, 0f, 0.55f));
            menuScript.moveBar = bottom.rectTransform;
            handles.Add(bottom);
            menuScript.corners = new RectTransform[4];
            Vector2[] spots = { new Vector2(-14f, -14f), new Vector2(MenuW + 14f, -14f), new Vector2(-14f, MenuH + 14f), new Vector2(MenuW + 14f, MenuH + 14f) };
            for (int i = 0; i < 4; i++)
            {
                var corner = Handle(rect, "Corner " + (i + 1), spots[i].x, spots[i].y, 52f, 52f, _circle);
                Icon(corner.rectTransform, "Icon", 0, 0, 52, 52, IconScaling, 26, new Color(0f, 0f, 0f, 0.6f));
                menuScript.corners[i] = corner.rectTransform;
                handles.Add(corner);
            }
            menuScript.handleImages = handles.ToArray();

            // throwing it: one picture of the menu, then the same picture in pieces
            var snapshot = new RenderTexture(1024, 700, 24, RenderTextureFormat.ARGB32);
            string snapshotPath = Generated + "/Menu Snapshot.renderTexture";
            var oldSnapshot = AssetDatabase.LoadAssetAtPath<RenderTexture>(snapshotPath);
            if (oldSnapshot != null) snapshot = oldSnapshot;
            else
            {
                snapshot.name = "Menu Snapshot";
                AssetDatabase.CreateAsset(snapshot, snapshotPath);
            }
            var cameraGo = new GameObject("Menu Snapshot");
            cameraGo.transform.SetParent(root, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = 1;
            camera.targetTexture = snapshot;
            camera.depth = -50f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.enabled = false;
            menuScript.snapshotCamera = camera;

            var shardsGo = new GameObject("Menu Shards");
            shardsGo.transform.SetParent(root, false);
            shardsGo.AddComponent<MeshFilter>().sharedMesh = ShardMesh(MenuW, MenuH);
            var shardRenderer = shardsGo.AddComponent<MeshRenderer>();
            var shatter = ShaderMaterial("Menu Shatter", "AviSeek/Shatter");
            shatter.SetTexture("_MainTex", snapshot);
            shatter.SetColor("_Accent", Accent);
            shatter.SetVector("_Size", new Vector4(MenuW, MenuH, 36f, 0f));
            shardRenderer.sharedMaterial = shatter;
            shardRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shardRenderer.receiveShadows = false;
            shardsGo.SetActive(false);
            menuScript.shards = shardRenderer;
            // kenney's impact sounds, cc0
            menuScript.shatterClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Menu Shatter.ogg");

            // the wrist button: a ring around the aviseek logo, sized and placed at runtime
            var wristGo = new GameObject("Wrist Button", typeof(RectTransform));
            wristGo.transform.SetParent(root, false);
            var wristCanvas = wristGo.AddComponent<Canvas>();
            wristCanvas.renderMode = RenderMode.WorldSpace;
            var wristRect = (RectTransform)wristGo.transform;
            wristRect.sizeDelta = new Vector2(100f, 100f);
            wristRect.localScale = Vector3.one * 0.0004f;
            Centred(wristRect, "Glow", 170f, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.42f));
            // a real button too, so the vrchat laser can click it
            wristGo.AddComponent<GraphicRaycaster>();
            var disc = Centred(wristRect, "Disc", 100f, _circle, Panel);
            disc.raycastTarget = true;
            var wristPress = disc.gameObject.AddComponent<Button>();
            wristPress.targetGraphic = disc;
            var wristNav = wristPress.navigation;
            wristNav.mode = Navigation.Mode.None;
            wristPress.navigation = wristNav;
            UnityEventTools.AddStringPersistentListener(wristPress.onClick, udon.SendCustomEvent, nameof(AviSeekMenu._WristPressed));
            menuScript.wristRing = Centred(wristRect, "Ring", 100f, _ring, Accent);
            Centred(wristRect, "Logo", 58f, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/AviSeekIcon.png"), Color.white).preserveAspect = true;
            wristGo.AddComponent<VRCUiShape>();
            var wristBox = wristGo.GetComponent<BoxCollider>();
            if (wristBox == null) wristBox = wristGo.AddComponent<BoxCollider>();
            wristBox.isTrigger = true;
            wristBox.size = new Vector3(100f, 100f, 2f);
            menuScript.wristButton = wristRect;
            wristGo.SetActive(false);

            // the music plays for this player only, not from a spot in the world
            var musicGo = new GameObject("Music");
            musicGo.transform.SetParent(root, false);
            var music = musicGo.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.priority = 64;
            // "chill lofi inspired" by omfgdude, loop edit by qubodup, cc0
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Background Music.ogg");
            menuScript.music = music;

            var voiceGo = new GameObject("Menu Sound");
            voiceGo.transform.SetParent(root, false);
            var voice = voiceGo.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            menuScript.voice = voice;
            // kenney's ui audio, cc0
            menuScript.openClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Panel Land.ogg");
            menuScript.closeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Touch Release.ogg");
            menuScript.accent = Accent;

            // the linked world addresses, all baked: udon can not build one
            menuScript.refreshUrl = new VRCUrl(ShortBase + "/r");
            menuScript.signOutUrl = new VRCUrl(ShortBase + "/o");
            menuScript.selectSlotUrls = new VRCUrl[PictureSlots];
            for (int i = 0; i < PictureSlots; i++) menuScript.selectSlotUrls[i] = new VRCUrl(ShortBase + "/s/" + i);
            const int tiles = 120;
            menuScript.favoriteUrls = new VRCUrl[tiles * 2];
            menuScript.pickUrls = new VRCUrl[tiles];
            for (int i = 0; i < tiles; i++)
            {
                menuScript.favoriteUrls[i * 2] = new VRCUrl(ShortBase + "/f/" + i + "/0");
                menuScript.favoriteUrls[i * 2 + 1] = new VRCUrl(ShortBase + "/f/" + i + "/1");
                menuScript.pickUrls[i] = new VRCUrl(ShortBase + "/t/" + i);
            }
            const int folders = 21;
            menuScript.moveUrls = new VRCUrl[folders];
            menuScript.deleteFolderUrls = new VRCUrl[folders];
            for (int i = 0; i < folders; i++)
            {
                menuScript.moveUrls[i] = new VRCUrl(ShortBase + "/m/" + i);
                menuScript.deleteFolderUrls[i] = new VRCUrl(ShortBase + "/fd/" + i);
            }
            menuScript.createFolderPrefixUrl = new VRCUrl(ShortBase + "/fc?n=");
            UdonSharpEditorUtility.CopyProxyToUdon(menuScript);
            return menuScript;
        }

        // a mirror in a frame with three buttons under it. each player picks their own
        private static void BuildMirror(RectTransform panel)
        {
            var mirror = UdonSharpUndo.AddComponent<AviSeekMirror>(panel.parent.gameObject);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(mirror);

            Icon(panel, "Icon", 40, 34, 56, 56, IconMirror, 38, AccentSoft);
            Txt(panel, "Title", "Mirror", 106, 28, 560, 52, 40, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Txt(panel, "Subtitle", "Only you see what you pick here", 108, 80, 600, 30, 19, Muted, TextAlignmentOptions.Left, FontStyles.Normal);

            var frame = Img(panel, "Frame", 30, 120, 700, 620, _rounded, Well);
            Round(frame, 24);
            var frameRect = frame.rectTransform;
            var cover = new GameObject("Off", typeof(RectTransform));
            Place((RectTransform)cover.transform, frameRect, 0, 0, 700, 620);
            Icon((RectTransform)cover.transform, "Icon", 0, 220, 700, 90, IconEyeOff, 70, new Color(1f, 1f, 1f, 0.25f));
            Txt((RectTransform)cover.transform, "Label", "Mirror is off", 0, 320, 700, 50, 28, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center, FontStyles.Bold);
            mirror.offCover = cover;

            var material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.vrchat.base/Runtime/VRCSDK/Sample Assets/Materials/MirrorReflection.mat");
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            // players only: remote players and the local player's mirror copy
            mirror.playersMirror = MirrorQuad(frameRect, "Players Mirror", quad, material, (1 << 9) | (1 << 18));
            // everything but the local player's own body and vrchat's menus
            mirror.fullMirror = MirrorQuad(frameRect, "Full Mirror", quad, material, ~((1 << 10) | (1 << 12)));

            mirror.buttons = new Image[3];
            mirror.buttonIcons = new TextMeshProUGUI[3];
            string[] labels = { "Off", "Players", "Full" };
            string[] icons = { IconEyeOff, IconUsers, IconSparkles };
            string[] events = { nameof(AviSeekMirror._Off), nameof(AviSeekMirror._Players), nameof(AviSeekMirror._Full) };
            for (int i = 0; i < 3; i++)
            {
                var button = Btn(panel, labels[i] + " Button", 30 + i * 240, 760, 220, 80, Field, udon, events[i]);
                mirror.buttons[i] = button.GetComponent<Image>();
                mirror.buttonIcons[i] = Icon((RectTransform)button.transform, "Icon", 26, 0, 44, 80, icons[i], 28, Color.white);
                Txt((RectTransform)button.transform, "Label", labels[i], 78, 0, 130, 80, 26, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            }
            mirror.hintLabel = Txt(panel, "Hint", "", 30, 852, 700, 34, 18, Muted, TextAlignmentOptions.Center, FontStyles.Normal);
            mirror.accent = Accent;
            mirror.idle = Field;
            UdonSharpEditorUtility.CopyProxyToUdon(mirror);
        }

        private static GameObject MirrorQuad(RectTransform frame, string name, Mesh quad, Material material, int layers)
        {
            var go = new GameObject(name);
            go.transform.SetParent(frame, false);
            // the frame's middle, in canvas units, just in front of it. the quad faces -z like the canvas
            go.transform.localPosition = new Vector3(350f, -310f, -2f);
            go.transform.localScale = new Vector3(676f, 596f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var reflection = go.AddComponent<VRC.SDK3.Components.VRCMirrorReflection>();
            var serialized = new SerializedObject(reflection);
            var mask = serialized.FindProperty("m_ReflectLayers");
            if (mask != null)
            {
                mask.intValue = layers;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[AviSeek] Mirror layers could not be set on " + name);
            go.SetActive(false);
            return go;
        }

        // a grab handle centred on a spot in the menu's canvas, top left origin
        private static Image Handle(RectTransform parent, string name, float cx, float cy, float w, float h, Sprite sprite)
        {
            var image = Img(parent, name, 0, 0, w, h, sprite, new Color(1f, 1f, 1f, 0.35f));
            var rect = image.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(cx, -cy);
            return image;
        }

        // the menu cut into jittered triangles, each with its own corners so it can fly off alone.
        // uv2 holds the piece's middle and two random numbers, the colour its corner's weights
        private static Mesh ShardMesh(float w, float h)
        {
            const int cols = 13, rows = 9;
            var random = new System.Random(5150);
            var grid = new Vector2[(cols + 1) * (rows + 1)];
            for (int y = 0; y <= rows; y++)
            {
                for (int x = 0; x <= cols; x++)
                {
                    float jx = x == 0 || x == cols ? 0f : ((float)random.NextDouble() - 0.5f) * 0.7f;
                    float jy = y == 0 || y == rows ? 0f : ((float)random.NextDouble() - 0.5f) * 0.7f;
                    grid[y * (cols + 1) + x] = new Vector2(((x + jx) / cols - 0.5f) * w, ((y + jy) / rows - 0.5f) * h);
                }
            }
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var pieces = new List<Vector4>();
            var weights = new List<Color>();
            var triangles = new List<int>();
            Color[] corner = { new Color(1f, 0f, 0f, 1f), new Color(0f, 1f, 0f, 1f), new Color(0f, 0f, 1f, 1f) };
            void Piece(Vector2 a, Vector2 b, Vector2 c)
            {
                Vector2 middle = (a + b + c) / 3f;
                var piece = new Vector4(middle.x, middle.y, (float)random.NextDouble(), (float)random.NextDouble());
                Vector2[] points = { a, b, c };
                for (int i = 0; i < 3; i++)
                {
                    triangles.Add(vertices.Count);
                    vertices.Add(points[i]);
                    uvs.Add(new Vector2(points[i].x / w + 0.5f, points[i].y / h + 0.5f));
                    pieces.Add(piece);
                    weights.Add(corner[i]);
                }
            }
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Vector2 p00 = grid[y * (cols + 1) + x], p10 = grid[y * (cols + 1) + x + 1];
                    Vector2 p01 = grid[(y + 1) * (cols + 1) + x], p11 = grid[(y + 1) * (cols + 1) + x + 1];
                    if (random.NextDouble() < 0.5)
                    {
                        Piece(p00, p01, p11);
                        Piece(p00, p11, p10);
                    }
                    else
                    {
                        Piece(p00, p01, p10);
                        Piece(p10, p01, p11);
                    }
                }
            }
            string path = Generated + "/Menu Shards.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.name = "Menu Shards";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, pieces);
            mesh.SetColors(weights);
            mesh.SetTriangles(triangles, 0);
            // the pieces fly far from where the mesh sits, so it must never be culled early
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 40000f);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // the wall's folders panel: the same folders as the website, kept through the menu
        private static void BuildFolders(RectTransform panel, AviSeekSearch search, AviSeekMenu menu)
        {
            var folders = UdonSharpUndo.AddComponent<AviSeekFolders>(panel.parent.gameObject);
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(folders);
            folders.menu = menu;
            folders.search = search;
            menu.folders = folders;

            Icon(panel, "Icon", 50, 40, 56, 56, IconFolderHeart, 40, AccentSoft);
            Txt(panel, "Title", "Your folders", 116, 34, 420, 52, 40, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Txt(panel, "Subtitle", "The same as on aviseek.vrchatlegends.com", 118, 86, 560, 30, 19, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            var sync = Btn(panel, "Sync Button", 540, 42, 130, 52, Field, udon, nameof(AviSeekFolders._Sync));
            Icon((RectTransform)sync.transform, "Icon", 12, 0, 36, 52, IconRefresh, 22, AccentSoft);
            Txt((RectTransform)sync.transform, "Label", "Sync", 52, 0, 70, 52, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            var signedIn = new GameObject("Signed In", typeof(RectTransform));
            var inRect = (RectTransform)signedIn.transform;
            Place(inRect, panel, 0, 0, 720, 900);
            folders.signedInRoot = signedIn;

            folders.selectedLabel = Txt(inRect, "Selected", "", 50, 128, 620, 44, 19, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
            folders.selectedLabel.enableWordWrapping = true;
            folders.selectedLabel.overflowMode = TextOverflowModes.Ellipsis;
            folders.statusLabel = Txt(inRect, "Status", "", 50, 174, 620, 34, 17, AccentSoft, TextAlignmentOptions.Left, FontStyles.Normal);
            folders.statusLabel.enableWordWrapping = true;
            folders.statusLabel.richText = true;

            var all = Btn(inRect, "All Favorites", 50, 214, 620, 62, RowBg, udon, nameof(AviSeekFolders._ShowAll), false);
            folders.allBack = all.GetComponent<Image>();
            Icon((RectTransform)all.transform, "Icon", 14, 0, 40, 62, IconHeart, 24, AccentSoft);
            Txt((RectTransform)all.transform, "Name", "All favorites", 62, 0, 400, 62, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            folders.allCount = Txt((RectTransform)all.transform, "Count", "0", 520, 0, 84, 62, 21, Muted, TextAlignmentOptions.Right, FontStyles.Normal);

            const int rows = 6;
            folders.rows = new GameObject[rows];
            folders.rowBacks = new Image[rows];
            folders.rowNames = new TextMeshProUGUI[rows];
            folders.rowCounts = new TextMeshProUGUI[rows];
            folders.deleteLabels = new TextMeshProUGUI[rows];
            string[] show = { nameof(AviSeekFolders._Show0), nameof(AviSeekFolders._Show1), nameof(AviSeekFolders._Show2), nameof(AviSeekFolders._Show3), nameof(AviSeekFolders._Show4), nameof(AviSeekFolders._Show5) };
            string[] add = { nameof(AviSeekFolders._Add0), nameof(AviSeekFolders._Add1), nameof(AviSeekFolders._Add2), nameof(AviSeekFolders._Add3), nameof(AviSeekFolders._Add4), nameof(AviSeekFolders._Add5) };
            string[] delete = { nameof(AviSeekFolders._Delete0), nameof(AviSeekFolders._Delete1), nameof(AviSeekFolders._Delete2), nameof(AviSeekFolders._Delete3), nameof(AviSeekFolders._Delete4), nameof(AviSeekFolders._Delete5) };
            for (int i = 0; i < rows; i++)
            {
                float y = 290 + i * 68;
                var row = new GameObject("Folder " + (i + 1), typeof(RectTransform));
                var rowRect = (RectTransform)row.transform;
                Place(rowRect, inRect, 50, y, 620, 58);
                var open = Btn(rowRect, "Open", 0, 0, 470, 58, RowBg, udon, show[i], false);
                folders.rowBacks[i] = open.GetComponent<Image>();
                Icon((RectTransform)open.transform, "Icon", 14, 0, 40, 58, IconFolder, 24, AccentSoft);
                folders.rowNames[i] = Txt((RectTransform)open.transform, "Name", "Folder", 62, 0, 330, 58, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                folders.rowNames[i].overflowMode = TextOverflowModes.Ellipsis;
                folders.rowCounts[i] = Txt((RectTransform)open.transform, "Count", "0", 390, 0, 64, 58, 20, Muted, TextAlignmentOptions.Right, FontStyles.Normal);
                IconButton(rowRect, "Add", 480, 0, 64, 58, IconPlus, Field, Color.white, udon, add[i]);
                var bin = Btn(rowRect, "Delete", 554, 0, 66, 58, Field, udon, delete[i]);
                folders.deleteLabels[i] = Icon((RectTransform)bin.transform, "Icon", 0, 0, 66, 58, IconTrash, 22, new Color(1f, 1f, 1f, 0.55f));
                folders.deleteLabels[i].font = _icons;
                folders.rows[i] = row;
            }
            IconButton(inRect, "Prev", 50, 702, 56, 48, IconPrev, Field, Color.white, udon, nameof(AviSeekFolders._Prev));
            folders.pageLabel = Txt(inRect, "Page", "", 110, 702, 260, 48, 18, Muted, TextAlignmentOptions.Center, FontStyles.Normal);
            IconButton(inRect, "Next", 374, 702, 56, 48, IconNext, Field, Color.white, udon, nameof(AviSeekFolders._Next));
            var unfile = Btn(inRect, "Take Out", 444, 702, 226, 48, Field, udon, nameof(AviSeekFolders._Unfile));
            Icon((RectTransform)unfile.transform, "Icon", 10, 0, 36, 48, IconFolderMinus, 20, AccentSoft);
            Txt((RectTransform)unfile.transform, "Label", "Out of folder", 48, 0, 170, 48, 18, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            // the box holds the whole address, the display shows only the typed name
            var fieldBg = Inset(inRect, "Name Field", 50, 766, 470, 58, 16);
            fieldBg.raycastTarget = true;
            var field = fieldBg.gameObject.AddComponent<VRCUrlInputField>();
            var fieldText = LegacyTxt(fieldBg.rectTransform, "Text", "", 22, new Color(1f, 1f, 1f, 0f));
            field.textComponent = fieldText;
            field.lineType = VRCUrlInputField.LineType.SingleLine;
            field.characterLimit = 120;
            field.targetGraphic = fieldBg;
            field.customCaretColor = true;
            field.caretColor = new Color(0f, 0f, 0f, 0f);
            field.selectionColor = new Color(0f, 0f, 0f, 0f);
            UnityEventTools.AddStringPersistentListener(field.onEndEdit, udon.SendCustomEvent, nameof(AviSeekFolders._Create));
            folders.nameField = field;
            Icon(fieldBg.rectTransform, "Icon", 12, 0, 40, 58, IconFolderPlus, 22, AccentSoft);
            folders.nameDisplay = Txt(fieldBg.rectTransform, "Typed", "", 58, 0, 400, 58, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
            folders.nameDisplay.overflowMode = TextOverflowModes.Ellipsis;
            folders.nameHint = Txt(fieldBg.rectTransform, "Hint", "New folder name", 58, 0, 400, 58, 22, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.Left, FontStyles.Normal);
            var make = Btn(inRect, "Create", 530, 766, 140, 58, Accent, udon, nameof(AviSeekFolders._Create));
            _accentImages.Add(make.GetComponent<Image>());
            Icon((RectTransform)make.transform, "Icon", 12, 0, 36, 58, IconFolderPlus, 22, Color.white);
            Txt((RectTransform)make.transform, "Label", "Make", 52, 0, 80, 58, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            folders.limitLabel = Txt(inRect, "Limit", "", 50, 836, 620, 26, 17, Muted, TextAlignmentOptions.Left, FontStyles.Normal);
            var track = Img(inRect, "Limit Track", 50, 868, 620, 8, _rounded, Field);
            Round(track, 4);
            var fill = Img(track.rectTransform, "Fill", 0, 0, 620, 8, _rounded, Accent);
            Round(fill, 4);
            _accentImages.Add(fill);
            folders.limitFill = fill.rectTransform;

            var signedOut = new GameObject("Signed Out", typeof(RectTransform));
            var outRect = (RectTransform)signedOut.transform;
            Place(outRect, panel, 0, 0, 720, 900);
            folders.signedOutRoot = signedOut;
            Icon(outRect, "Icon", 0, 260, 720, 90, IconFolderHeart, 80, AccentSoft);
            Txt(outRect, "Title", "Sign in to use folders", 0, 370, 720, 56, 34, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var why = Txt(outRect, "Text", "Favorites and folders live on your AviSeek account, so they are the same in this world, in every AviSeek world and on the website.", 80, 436, 560, 100, 21, Muted, TextAlignmentOptions.Top, FontStyles.Normal);
            why.enableWordWrapping = true;
            var openProfile = Btn(outRect, "Open Profile", 220, 560, 280, 68, Accent, udon, nameof(AviSeekFolders._OpenProfile));
            _accentImages.Add(openProfile.GetComponent<Image>());
            Icon((RectTransform)openProfile.transform, "Icon", 22, 0, 40, 68, IconCircleUser, 28, Color.white);
            Txt((RectTransform)openProfile.transform, "Label", "Open profile", 70, 0, 200, 68, 24, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            Txt(outRect, "Key", "or press <b>P</b> on desktop, or hover your left arm in VR", 0, 646, 720, 32, 18, Muted, TextAlignmentOptions.Center, FontStyles.Normal).richText = true;
            signedOut.SetActive(true);
            signedIn.SetActive(false);

            folders.accent = Accent;
            folders.rowIdle = RowBg;
            UdonSharpEditorUtility.CopyProxyToUdon(folders);
            UdonSharpEditorUtility.CopyProxyToUdon(menu);
        }

        private static TextMeshProUGUI StatTile(RectTransform parent, string name, float x, string glyph, string caption)
        {
            var tile = Img(parent, name + " Tile", x, 116, 170, 136, _rounded, MenuTile);
            Round(tile, 20);
            var rect = tile.rectTransform;
            Icon(rect, "Icon", 0, 14, 170, 36, glyph, 26, MenuAccent);
            var value = Txt(rect, "Value", "0", 6, 54, 158, 44, 32, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            value.enableAutoSizing = true;
            value.fontSizeMin = 16f;
            value.fontSizeMax = 32f;
            value.overflowMode = TextOverflowModes.Ellipsis;
            Txt(rect, "Caption", caption, 0, 98, 170, 26, 17, MenuMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            return value;
        }

        /* ───────────────────────────── touch ───────────────────────────── */

        // lists every button and text box on the panels for the touch script, in drawing order
        private static void BuildTouch(Transform root)
        {
            var touch = UdonSharpUndo.AddComponent<AviSeekTouch>(root.gameObject);
            var panels = new List<RectTransform>();
            foreach (string name in PanelOrder)
            {
                var panel = root.Find(name) as RectTransform;
                if (panel != null) panels.Add(panel);
            }
            var menuPanel = root.Find("Profile Menu") as RectTransform;
            if (menuPanel != null)
            {
                panels.Add(menuPanel);
                touch.followPanel = menuPanel;
                touch.menu = root.GetComponentInChildren<AviSeekMenu>(true);
            }
            var targets = new List<RectTransform>();
            var owners = new List<int>();
            var behaviours = new List<UdonBehaviour>();
            var events = new List<string>();
            var fields = new List<Selectable>();
            foreach (var panel in panels)
            {
                foreach (var selectable in panel.GetComponentsInChildren<Selectable>(true))
                {
                    if (selectable is Button button)
                    {
                        if (button.onClick.GetPersistentEventCount() == 0) continue;
                        var call = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls.Array.data[0]");
                        var behaviour = call.FindPropertyRelative("m_Target").objectReferenceValue as UdonBehaviour;
                        string eventName = call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;
                        if (behaviour == null || string.IsNullOrEmpty(eventName)) continue;
                        targets.Add((RectTransform)button.transform);
                        owners.Add(panels.IndexOf(panel));
                        behaviours.Add(behaviour);
                        events.Add(eventName);
                        fields.Add(null);
                    }
                    else if (selectable is TMP_InputField || selectable is VRCUrlInputField)
                    {
                        targets.Add((RectTransform)selectable.transform);
                        owners.Add(panels.IndexOf(panel));
                        behaviours.Add(null);
                        events.Add("");
                        fields.Add(selectable);
                    }
                }
            }
            touch.panels = panels.ToArray();
            touch.targets = targets.ToArray();
            touch.targetPanels = owners.ToArray();
            touch.targetBehaviours = behaviours.ToArray();
            touch.targetEvents = events.ToArray();
            touch.targetFields = fields.ToArray();

            touch.cursors = new Transform[2];
            touch.rings = new RectTransform[2];
            touch.ringImages = new Image[2];
            touch.dots = new Image[2];
            touch.ripples = new RectTransform[2];
            touch.rippleImages = new Image[2];
            touch.trails = new TrailRenderer[2];
            touch.highlights = new RectTransform[2];
            touch.highlightImages = new Image[2];
            touch.voices = new AudioSource[2];
            for (int hand = 0; hand < 2; hand++) BuildCursor(root, touch, hand);
            // kenney's ui audio, cc0
            touch.hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Touch Hover.ogg");
            touch.pressClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Touch Press.ogg");
            touch.releaseClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Touch Release.ogg");
            touch.accent = Accent;
            UdonSharpEditorUtility.CopyProxyToUdon(touch);
        }

        // the entrance a joining player sees, and the welcome banner over the middle of the wall
        private static void BuildIntro(Transform root)
        {
            var intro = UdonSharpUndo.AddComponent<AviSeekIntro>(root.gameObject);
            var panels = new List<RectTransform>();
            var groups = new List<CanvasGroup>();
            foreach (string name in PanelOrder)
            {
                var panel = root.Find(name) as RectTransform;
                if (panel == null) continue;
                panels.Add(panel);
                var group = panel.GetComponent<CanvasGroup>();
                if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }
            intro.panels = panels.ToArray();
            intro.groups = groups.ToArray();

            var go = new GameObject("Welcome", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1000f, 120f);
            rect.localScale = Vector3.one * PanelScale;
            rect.localPosition = new Vector3(0f, PanelCentreY + 900f * PanelScale * 0.5f + 0.12f, -0.06f);
            var pill = new GameObject("Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var pillRect = (RectTransform)pill.transform;
            pillRect.SetParent(rect, false);
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(0.5f, 0.5f);
            pillRect.sizeDelta = new Vector2(1000f, 110f);
            var back = pill.GetComponent<Image>();
            back.sprite = _rounded;
            back.type = Image.Type.Sliced;
            back.color = Hex("1d1731");
            back.raycastTarget = false;
            Round(back, 55);
            var glow = Centred(pillRect, "Glow", 1100f, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            glow.rectTransform.sizeDelta = new Vector2(1200f, 260f);
            glow.transform.SetAsFirstSibling();
            var label = Txt(pillRect, "Label", "Welcome", 0, 0, 1000, 110, 46, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.richText = true;
            intro.welcome = pillRect;
            intro.welcomeGroup = go.GetComponent<CanvasGroup>();
            intro.welcomeGroup.alpha = 0f;
            intro.welcomeLabel = label;

            var voiceGo = new GameObject("Intro Sound");
            voiceGo.transform.SetParent(root, false);
            voiceGo.transform.localPosition = new Vector3(0f, PanelCentreY, 0f);
            var voice = voiceGo.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.6f;
            voice.minDistance = 2f;
            voice.maxDistance = 25f;
            voice.dopplerLevel = 0f;
            intro.voice = voice;
            // kenney's ui audio, cc0
            intro.landClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Runtime/Audio/Panel Land.ogg");
            UdonSharpEditorUtility.CopyProxyToUdon(intro);
        }

        // a glow, a spinning dashed ring that closes in as the finger nears, a dot, and a ripple per press.
        // its own canvas has no raycaster, so it never gets in the way of a click
        private static void BuildCursor(Transform root, AviSeekTouch touch, int hand)
        {
            var go = new GameObject(hand == 0 ? "Touch Cursor Left" : "Touch Cursor Right", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.localScale = Vector3.one * PanelScale;

            Centred(rect, "Glow", 120f, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            var ripple = Centred(rect, "Ripple", 52f, _ring, AccentSoft);
            var ring = Centred(rect, "Ring", 50f, _ringDashed, Color.white);
            var dot = Centred(rect, "Dot", 12f, _circle, Color.white);
            touch.cursors[hand] = go.transform;
            touch.rings[hand] = ring.rectTransform;
            touch.ringImages[hand] = ring;
            touch.dots[hand] = dot;
            touch.ripples[hand] = ripple.rectTransform;
            touch.rippleImages[hand] = ripple;

            // a short trail that follows the fingertip as it slides along a panel
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.minVertexDistance = 0.003f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.widthMultiplier = 0.011f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(AccentSoft, 0.35f), new GradientColorKey(Accent, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 2;
            trail.alignment = LineAlignment.TransformZ;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.sharedMaterial = TrailMaterial();
            touch.trails[hand] = trail;
            go.SetActive(false);

            // the outline that hugs the button under the finger, on its own canvas so it can go anywhere
            var frameGo = new GameObject(hand == 0 ? "Touch Highlight Left" : "Touch Highlight Right", typeof(RectTransform));
            frameGo.transform.SetParent(root, false);
            var frameCanvas = frameGo.AddComponent<Canvas>();
            frameCanvas.renderMode = RenderMode.WorldSpace;
            var frameRect = (RectTransform)frameGo.transform;
            frameRect.sizeDelta = new Vector2(100f, 100f);
            frameRect.localScale = Vector3.one * PanelScale;
            var line = new GameObject("Outline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lineRect = (RectTransform)line.transform;
            lineRect.SetParent(frameRect, false);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;
            var outline = line.GetComponent<Image>();
            outline.sprite = _outline;
            outline.type = Image.Type.Sliced;
            outline.pixelsPerUnitMultiplier = 2.4f;
            outline.raycastTarget = false;
            outline.color = Accent;
            touch.highlights[hand] = frameRect;
            touch.highlightImages[hand] = outline;
            frameGo.SetActive(false);

            // its sounds play from the fingertip, from an object that is never switched off
            var voiceGo = new GameObject(hand == 0 ? "Touch Sound Left" : "Touch Sound Right");
            voiceGo.transform.SetParent(root, false);
            var voice = voiceGo.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.minDistance = 0.4f;
            voice.maxDistance = 6f;
            voice.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.dopplerLevel = 0f;
            touch.voices[hand] = voice;
        }

        private static Material TrailMaterial()
        {
            string path = Generated + "/Touch Trail.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = null;
            EditorUtility.SetDirty(material);
            return material;
        }

        // centred on its parent, so it scales and spins around its middle
        private static Image Centred(RectTransform parent, string name, float size, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /* ───────────────────────────── ui helpers ───────────────────────────── */

        private static RectTransform PanelBox(Transform parent, string name, float x, float y, float w, float h)
        {
            var bg = Img((RectTransform)parent, name, x, y, w, h, _rounded, Panel);
            Round(bg, 44);
            bg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var glow = Img(bg.rectTransform, "Glow", w * 0.15f, -h * 0.55f, w * 0.7f, h * 0.95f, _glow, new Color(Accent.r, Accent.g, Accent.b, 0.42f));
            glow.raycastTarget = false;
            _accentImages.Add(glow);
            var shade = Img(bg.rectTransform, "Floor", 0, h * 0.45f, w, h * 0.55f, _fade, new Color(Ink.r, Ink.g, Ink.b, 0.85f));
            shade.raycastTarget = false;
            return bg.rectTransform;
        }

        private static void Place(RectTransform rect, RectTransform parent, float x, float y, float w, float h)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Image Img(RectTransform parent, string name, float x, float y, float w, float h, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Place((RectTransform)go.transform, parent, x, y, w, h);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
            return image;
        }

        // the sprite's corners are 32px, scale them to the radius we want
        private static void Round(Image image, float radius)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 32f / radius;
        }

        private static RawImage Raw(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            Place((RectTransform)go.transform, parent, x, y, w, h);
            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;
            return raw;
        }

        private static TextMeshProUGUI Txt(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Place((RectTransform)go.transform, parent, x, y, w, h);
            var label = go.GetComponent<TextMeshProUGUI>();
            // bold labels use the bold face itself: going through the weight table would give
            // every one of them an extra sub mesh, and an extra draw call
            bool bold = (style & FontStyles.Bold) != 0 && _bold != null;
            label.font = bold ? _bold : _text;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.fontStyle = bold ? style & ~FontStyles.Bold : style;
            label.enableWordWrapping = false;
            // inter stands taller than tmp's default font, so a cut off line must not vanish
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.margin = Vector4.zero;
            return label;
        }

        private static TextMeshProUGUI Icon(RectTransform parent, string name, float x, float y, float w, float h, string glyph, float size, Color color)
        {
            var label = Txt(parent, name, glyph, x, y, w, h, size, color, TextAlignmentOptions.Center, FontStyles.Normal);
            label.font = _icons;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static Text LegacyTxt(RectTransform parent, string name, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 4f);
            rect.offsetMax = new Vector2(-22f, -4f);
            var label = go.GetComponent<Text>();
            label.font = _legacy;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button Btn(RectTransform parent, string name, float x, float y, float w, float h, Color color, UdonBehaviour udon, string eventName, bool raised = true)
        {
            if (raised)
            {
                var shadow = Img(parent, name + " Shadow", x - 12, y - 6, w + 24, h + 24, _shadow, new Color(0f, 0f, 0f, 0.55f));
                shadow.type = Image.Type.Sliced;
                shadow.pixelsPerUnitMultiplier = 2f;
            }
            var image = Img(parent, name, x, y, w, h, _rounded, color);
            if (raised) Lift(image, 6f);
            Round(image, Mathf.Min(16f, h * 0.3f));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.4f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            UnityEventTools.AddStringPersistentListener(button.onClick, udon.SendCustomEvent, eventName);
            return button;
        }

        private static Button IconButton(RectTransform parent, string name, float x, float y, float w, float h, string glyph, Color background, Color iconColor, UdonBehaviour udon, string eventName)
        {
            var button = Btn(parent, name, x, y, w, h, background, udon, eventName);
            Icon((RectTransform)button.transform, "Icon", 0, 0, w, h, glyph, Mathf.Min(w, h) * 0.46f, iconColor);
            return button;
        }

        private static Image Tab(RectTransform parent, string name, float x, float y, float w, float h, string glyph, string platform, string text, UdonBehaviour udon, string eventName)
        {
            var button = Btn(parent, name, x, y, w, h, Field, udon, eventName);
            var rect = (RectTransform)button.transform;
            if (glyph != null) Icon(rect, "Icon", 10, 0, 30, h, glyph, 22, Color.white);
            else Badge(rect, "Icon", 10, (h - 30) * 0.5f, 30, platform);
            Txt(rect, "Label", text, 46, 0, w - 50, h, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            return button.GetComponent<Image>();
        }

        private static TextMeshProUGUI CycleButton(RectTransform parent, string name, float x, float y, float w, string glyph, string text, UdonBehaviour udon, string eventName)
        {
            var button = Btn(parent, name, x, y, w, 56, Field, udon, eventName);
            var rect = (RectTransform)button.transform;
            Icon(rect, "Icon", 10, 0, 34, 56, glyph, 22, AccentSoft);
            var label = Txt(rect, "Label", text, 48, 0, w - 84, 56, 21, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            label.overflowMode = TextOverflowModes.Ellipsis;
            Icon(rect, "Chevron", w - 38, 0, 28, 56, IconChevronDown, 20, Muted);
            return label;
        }

        private static Image Badge(RectTransform parent, string name, float x, float y, float size, string platform)
        {
            var image = Img(parent, name, x, y, size, size, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Runtime/Textures/" + platform + ".png"), Color.white);
            image.preserveAspect = true;
            return image;
        }

        // moves a graphic towards the player, in canvas units (mm here)
        private static void Lift(Component graphic, float mm)
        {
            var t = graphic.transform;
            var p = t.localPosition;
            p.z = -mm;
            t.localPosition = p;
        }

        // a darker sunken well for fields and icon boxes
        private static Image Inset(RectTransform parent, string name, float x, float y, float w, float h, float radius)
        {
            var well = Img(parent, name, x, y, w, h, _rounded, Well);
            Round(well, radius);
            return well;
        }

        // light catching the top of a raised surface
        private static void Sheen(RectTransform parent, float w, float h, float alpha)
        {
            var sheen = Img(parent, "Sheen", 0, 0, w, h, _fade, new Color(1f, 1f, 1f, alpha));
            Flip(sheen);
        }

        // turns a fade upside down in place, so it is solid at the top
        private static void Flip(Image image)
        {
            var rect = image.rectTransform;
            Vector2 size = rect.sizeDelta;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition += new Vector2(size.x * 0.5f, -size.y * 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        // a rounded slab in the canvas' own units, front face towards the player
        private static void Slab(RectTransform canvas, string name, Vector3 front, float w, float h, float radius, float depth, Material material)
        {
            const int segments = 10;
            var ring = new List<Vector2>();
            var centres = new List<Vector2>();
            float cx = w * 0.5f - radius, cy = h * 0.5f - radius;
            Vector2[] corners = { new Vector2(cx, cy), new Vector2(-cx, cy), new Vector2(-cx, -cy), new Vector2(cx, -cy) };
            for (int c = 0; c < 4; c++)
            {
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (c * 90f + i * 90f / segments) * Mathf.Deg2Rad;
                    ring.Add(corners[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                    centres.Add(corners[c]);
                }
            }
            int n = ring.Count;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // front, facing -z
            vertices.Add(Vector3.zero); normals.Add(Vector3.back);
            for (int i = 0; i < n; i++) { vertices.Add(ring[i]); normals.Add(Vector3.back); }
            for (int i = 0; i < n; i++) triangles.AddRange(new[] { 0, 1 + (i + 1) % n, 1 + i });

            // back
            int back = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, depth)); normals.Add(Vector3.forward);
            for (int i = 0; i < n; i++) { vertices.Add(new Vector3(ring[i].x, ring[i].y, depth)); normals.Add(Vector3.forward); }
            for (int i = 0; i < n; i++) triangles.AddRange(new[] { back, back + 1 + i, back + 1 + (i + 1) % n });

            // sides, smooth round the corners
            int side = vertices.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 normal = (ring[i] - centres[i]).normalized;
                vertices.Add(ring[i]); normals.Add(normal);
                vertices.Add(new Vector3(ring[i].x, ring[i].y, depth)); normals.Add(normal);
            }
            for (int i = 0; i < n; i++)
            {
                int a = side + i * 2, b = side + ((i + 1) % n) * 2;
                triangles.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
            }

            string path = Generated + "/" + canvas.name + " " + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.name = canvas.name + " " + name;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(canvas, false);
            go.transform.localPosition = front;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            go.transform.SetAsFirstSibling();
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private static Color Hex(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            return color;
        }
    }
}
