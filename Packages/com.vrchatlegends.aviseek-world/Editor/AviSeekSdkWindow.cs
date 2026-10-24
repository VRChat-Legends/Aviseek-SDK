using System.Linq;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using VRC.SDK3.Components;

namespace VRChatLegends.AviSeek.Editor
{
    // the sdk's home: set up the scene, brand the wall, check the api and find help
    public class AviSeekSdkWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "Setup", "Branding", "Status", "Help" };
        private int _tab;
        private Vector2 _scroll;

        private UnityWebRequest _ping;
        private UnityWebRequest _listing;
        private string _apiResult = "";
        private string _latest = "";

        [MenuItem("AviSeek/SDK Panel", false, -100)]
        public static void Open()
        {
            var window = GetWindow<AviSeekSdkWindow>("AviSeek SDK");
            window.minSize = new Vector2(380f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("AviSeek SDK", AviSeekEditorUI.Logo);
        }

        private void OnGUI()
        {
            AviSeekEditorUI.Banner("AviSeek SDK", "Avatar search for VRChat worlds");
            _tab = GUILayout.Toolbar(_tab, Tabs, GUILayout.Height(24f));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: SetupTab(); break;
                case 1: BrandingTab(); break;
                case 2: StatusTab(); break;
                default: HelpTab(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (_ping != null && _ping.isDone) { FinishPing(); Repaint(); }
            if (_listing != null && _listing.isDone) { FinishListing(); Repaint(); }
        }

        /* ───────────── setup ───────────── */

        private void SetupTab()
        {
            var walls = FindObjectsOfType<AviSeekSearch>(true);
            AviSeekEditorUI.BeginSection("Your scene");
            AviSeekEditorUI.Check(FindObjectOfType<VRCSceneDescriptor>() != null, "VRChat world descriptor found", "No VRC Scene Descriptor. Add the VRCWorld prefab first.");
            AviSeekEditorUI.Check(TMP_Settings.instance != null, "TextMesh Pro is set up", "TextMesh Pro essentials are missing. Use Window > TextMeshPro > Import TMP Essential Resources.");
            AviSeekEditorUI.Check(!UdonSharpProgramAsset.AnyUdonSharpScriptHasError(), "UdonSharp scripts compile", "An UdonSharp script has errors. Check the Console.");
            AviSeekEditorUI.Check(walls.Length > 0, walls.Length == 1 ? "One AviSeek wall in the scene" : walls.Length + " AviSeek walls in the scene", "No AviSeek wall yet");
            if (walls.Length > 0)
            {
                bool layers = walls.All(wall => wall.GetComponentsInChildren<Canvas>(true).All(canvas => canvas.gameObject.layer == 0));
                AviSeekEditorUI.Check(layers, "Panels are on the Default layer, so players can click them", "A panel is on another layer. VRChat only lets players click world UI on Default.");
            }
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Add the wall");
            AviSeekEditorUI.Hint("The wall is about 7 metres wide with every panel on. It is placed where the Scene view is looking, facing the -Z side. Turn it to face your spawn.");
            if (AviSeekEditorUI.AccentButton("Add AviSeek wall to the scene")) AviSeekPrefabBuilder.AddToScene();
            if (walls.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select wall")) Selection.activeGameObject = walls[0].gameObject;
                if (GUILayout.Button("Frame in Scene view"))
                {
                    Selection.activeGameObject = walls[0].gameObject;
                    SceneView.FrameLastActiveSceneView();
                }
                EditorGUILayout.EndHorizontal();
            }
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Before you upload");
            AviSeekEditorUI.Hint("Players need Allow Untrusted URLs turned on in VRChat for the downloads, unless the AviSeek domain is on VRChat's trusted list. The wall tells them when it is blocked.");
            AviSeekEditorUI.Hint("Test in Play mode with ClientSim: searches, favorites and the promoted list all work there. Avatar switching and group pages only work in VRChat.");
            AviSeekEditorUI.EndSection();
        }

        /* ───────────── branding ───────────── */

        private void BrandingTab()
        {
            var wall = AviSeekEditorUI.FindWall();
            if (wall == null)
            {
                EditorGUILayout.HelpBox("Add an AviSeek wall to the scene first.", MessageType.Info);
                if (AviSeekEditorUI.AccentButton("Add AviSeek wall to the scene")) AviSeekPrefabBuilder.AddToScene();
                return;
            }
            EditorGUILayout.ObjectField("Editing", wall, typeof(AviSeekSearch), true);
            var serialized = new SerializedObject(wall);
            AviSeekSearchEditor.DrawBranding(serialized, wall);
            AviSeekSearchEditor.DrawPanels(wall);
        }

        /* ───────────── status ───────────── */

        private void StatusTab()
        {
            AviSeekEditorUI.BeginSection("AviSeek API");
            AviSeekEditorUI.Hint("Asks the public world API for its catalog numbers, the same way the wall does in game.");
            EditorGUI.BeginDisabledGroup(_ping != null);
            if (AviSeekEditorUI.AccentButton(_ping != null ? "Checking..." : "Check the API")) StartPing();
            EditorGUI.EndDisabledGroup();
            if (_apiResult.Length > 0) EditorGUILayout.HelpBox(_apiResult, _apiResult.StartsWith("Online") ? MessageType.Info : MessageType.Warning);
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Updates");
            EditorGUILayout.LabelField("Installed", AviSeekEditorUI.Version);
            if (_latest.Length > 0) EditorGUILayout.LabelField("Latest", _latest);
            EditorGUI.BeginDisabledGroup(_listing != null);
            if (GUILayout.Button(_listing != null ? "Checking..." : "Check for updates")) StartListing();
            EditorGUI.EndDisabledGroup();
            AviSeekEditorUI.Hint("Updates arrive through the VRChat Creator Companion once the AviSeek listing is added.");
            if (GUILayout.Button("Add the listing to the Creator Companion")) Application.OpenURL("vcc://vpm/addRepo?url=" + UnityWebRequest.EscapeURL(AviSeekEditorUI.ListingUrl));
            AviSeekEditorUI.EndSection();
        }

        private void StartPing()
        {
            _apiResult = "";
            _ping = UnityWebRequest.Get("https://aviseek.vrchatlegends.com/api/world/client?q=&offset=0");
            _ping.SendWebRequest();
        }

        private void FinishPing()
        {
            if (_ping.result != UnityWebRequest.Result.Success)
            {
                _apiResult = "Couldn't reach AviSeek: " + _ping.error;
            }
            else
            {
                var data = JsonUtility.FromJson<ClientReply>(_ping.downloadHandler.text);
                int pictures = data.avatars == null ? 0 : data.avatars.Count(a => !string.IsNullOrEmpty(a.thumb));
                _apiResult = "Online. " + data.stats.active.ToString("N0") + " public avatars from " + data.stats.creators.ToString("N0") + " creators.\n"
                    + "First batch: " + (data.avatars == null ? 0 : data.avatars.Length) + " avatars, " + pictures + " with pictures ready.";
            }
            _ping.Dispose();
            _ping = null;
        }

        private void StartListing()
        {
            _latest = "";
            _listing = UnityWebRequest.Get(AviSeekEditorUI.ListingUrl);
            _listing.SendWebRequest();
        }

        // the listing is a vpm index; the newest version is the last "version" in the package's block
        private void FinishListing()
        {
            if (_listing.result != UnityWebRequest.Result.Success)
            {
                _latest = "Listing not reachable yet (" + _listing.error + ")";
            }
            else
            {
                string text = _listing.downloadHandler.text;
                var versions = System.Text.RegularExpressions.Regex.Matches(text, "\"version\"\\s*:\\s*\"([0-9.]+)\"")
                    .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value)
                    .OrderBy(v => new System.Version(v)).ToList();
                _latest = versions.Count == 0 ? "No releases listed yet" : versions.Last() + (versions.Last() == AviSeekEditorUI.Version ? "  (you're up to date)" : "  (update in the Creator Companion)");
            }
            _listing.Dispose();
            _listing = null;
        }

        [System.Serializable] private class ClientReply { public Stats stats; public Avatar[] avatars; }
        [System.Serializable] private class Stats { public int active; public int creators; }
        [System.Serializable] private class Avatar { public string thumb; }

        /* ───────────── help ───────────── */

        private void HelpTab()
        {
            AviSeekEditorUI.BeginSection("Learn");
            if (GUILayout.Button("World API docs")) Application.OpenURL(AviSeekEditorUI.DocsUrl);
            if (GUILayout.Button("SDK on GitHub")) Application.OpenURL(AviSeekEditorUI.RepoUrl);
            if (GUILayout.Button("SDK listing page")) Application.OpenURL(AviSeekEditorUI.ListingPage);
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Get help");
            if (GUILayout.Button("AviSeek Discord")) Application.OpenURL(AviSeekEditorUI.DiscordUrl);
            if (GUILayout.Button("Report a bug")) Application.OpenURL(AviSeekEditorUI.RepoUrl + "/issues/new");
            if (GUILayout.Button("Email support")) Application.OpenURL("mailto:support@vrchatlegends.com");
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Promote");
            AviSeekEditorUI.Hint("Promoted avatars and groups show on every AviSeek wall's Promoted panel.");
            if (GUILayout.Button("Promote an avatar or group")) Application.OpenURL(AviSeekEditorUI.PromoteUrl);
            AviSeekEditorUI.EndSection();

            AviSeekEditorUI.BeginSection("Credits");
            AviSeekEditorUI.Hint("Made by VRChat Legends. Avatar index powered by VRC-NEXUS (search.vrc-nexus.online). Icons from Lucide (ISC) and Simple Icons (CC0). Text in Inter (OFL). Platform badges from the VRChat Wiki.");
            if (GUILayout.Button("VRC-NEXUS")) Application.OpenURL("https://search.vrc-nexus.online");
            AviSeekEditorUI.EndSection();
        }
    }
}
