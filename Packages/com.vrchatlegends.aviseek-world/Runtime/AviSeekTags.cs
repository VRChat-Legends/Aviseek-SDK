using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // a small tag over each AviSeek supporter's head with their tier, in the tier's colour.
    // the tier comes from the summary every signed in player keeps in their PlayerData, which
    // everyone in the instance can read. it floats above vrchat's own name tag, never inside it,
    // keeps one size in the world so it does not grow with distance, fades out far away, and a
    // player never sees their own. each player can switch the tags off, and that is remembered
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Supporter Tags")]
    public class AviSeekTags : UdonSharpBehaviour
    {
        private const string ProfileKey = "aviseek.profile";
        private const string ShowKey = "aviseek.tags";

        [Tooltip("Shows the tags until a player switches them off for themselves.")]
        public bool showByDefault = true;
        [Tooltip("Metres above the head, for an avatar of VRChat's default height. Taller and shorter avatars scale it.")]
        public float heightAboveHead = 0.5f;
        [Tooltip("Tags start to fade from here and are gone at Hide Distance.")]
        public float fadeDistance = 16f;
        public float hideDistance = 24f;

        [Header("Built by the prefab")]
        public Transform holder;
        public Transform[] tags;
        public CanvasGroup[] groups;
        public TextMeshProUGUI[] labels;
        public TextMeshProUGUI[] icons;
        public Image[] rims;
        public Color accent = new Color(0.52f, 0.48f, 0.99f, 1f);

        [HideInInspector] public bool shown = true;

        private VRCPlayerApi[] _owners;
        private int[] _ids;
        private bool _restored;

        private void Start()
        {
            int count = tags == null ? 0 : tags.Length;
            _owners = new VRCPlayerApi[count];
            _ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                _ids[i] = -1;
                if (tags[i] != null) tags[i].gameObject.SetActive(false);
            }
            shown = showByDefault;
            // world space, whatever the wall's own scale is
            if (holder != null) holder.SetParent(null, true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null) return;
            if (player.isLocal)
            {
                _restored = true;
                bool on;
                if (PlayerData.TryGetBool(player, ShowKey, out on)) shown = on;
                return;
            }
            Read(player);
        }

        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
        {
            if (player == null || player.isLocal) return;
            Read(player);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player == null) return;
            Free(player.playerId);
        }

        // the menu's Tags button
        public void _Toggle()
        {
            shown = !shown;
            if (_restored) PlayerData.SetBool(ShowKey, shown);
            if (!shown) HideAll();
        }

        private void Read(VRCPlayerApi player)
        {
            string tier = "";
            string saved;
            if (PlayerData.TryGetString(player, ProfileKey, out saved) && saved != null && saved.Length > 0)
            {
                DataToken root;
                DataToken value;
                if (VRCJson.TryDeserializeFromJson(saved, out root) && root.TokenType == TokenType.DataDictionary
                    && root.DataDictionary.TryGetValue("tier", TokenType.String, out value)) tier = value.String.Trim();
            }
            if (tier.Length == 0 || tier.ToLower() == "free")
            {
                Free(player.playerId);
                return;
            }
            int slot = SlotOf(player.playerId);
            if (slot < 0) slot = SlotOf(-1);
            if (slot < 0) return;
            _owners[slot] = player;
            _ids[slot] = player.playerId;
            Color color = TierColor(tier);
            if (labels != null && labels[slot] != null)
            {
                labels[slot].text = Clip(tier, 22);
                labels[slot].color = color;
            }
            if (icons != null && icons[slot] != null) icons[slot].color = color;
            if (rims != null && rims[slot] != null) rims[slot].color = new Color(color.r, color.g, color.b, 0.85f);
        }

        private int SlotOf(int id)
        {
            if (_ids == null) return -1;
            for (int i = 0; i < _ids.Length; i++) if (_ids[i] == id) return i;
            return -1;
        }

        private void Free(int id)
        {
            int slot = SlotOf(id);
            if (slot < 0) return;
            _ids[slot] = -1;
            _owners[slot] = null;
            if (tags[slot] != null) tags[slot].gameObject.SetActive(false);
        }

        private void HideAll()
        {
            if (tags == null) return;
            for (int i = 0; i < tags.Length; i++) if (tags[i] != null) tags[i].gameObject.SetActive(false);
        }

        public override void PostLateUpdate()
        {
            if (!shown || tags == null || _ids == null) return;
            VRCPlayerApi local = Networking.LocalPlayer;
            if (local == null) return;
            Vector3 eye = local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            for (int i = 0; i < tags.Length; i++)
            {
                Transform tag = tags[i];
                if (tag == null) continue;
                VRCPlayerApi owner = _owners[i];
                if (_ids[i] < 0 || !Utilities.IsValid(owner))
                {
                    if (tag.gameObject.activeSelf) tag.gameObject.SetActive(false);
                    continue;
                }
                Vector3 head = owner.GetBonePosition(HumanBodyBones.Head);
                if (head == Vector3.zero) head = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                // taller avatars carry vrchat's name tag higher, so the tag rides above it the same way
                float tall = Mathf.Clamp(owner.GetAvatarEyeHeightAsMeters() / 1.6f, 0.4f, 4f);
                Vector3 at = head + Vector3.up * (heightAboveHead * tall);
                float distance = Vector3.Distance(eye, at);
                if (distance > hideDistance)
                {
                    if (tag.gameObject.activeSelf) tag.gameObject.SetActive(false);
                    continue;
                }
                if (!tag.gameObject.activeSelf) tag.gameObject.SetActive(true);
                tag.position = at;
                Vector3 look = at - eye;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f) tag.rotation = Quaternion.LookRotation(look);
                if (groups != null && groups[i] != null)
                {
                    groups[i].alpha = 1f - Mathf.Clamp01((distance - fadeDistance) / Mathf.Max(0.01f, hideDistance - fadeDistance));
                }
            }
        }

        private Color TierColor(string tier)
        {
            string t = tier.ToLower();
            if (t.IndexOf("platinum") >= 0) return new Color(0.62f, 0.93f, 1f, 1f);
            if (t.IndexOf("gold") >= 0) return new Color(1f, 0.8f, 0.3f, 1f);
            if (t.IndexOf("silver") >= 0) return new Color(0.82f, 0.85f, 0.92f, 1f);
            if (t.IndexOf("bronze") >= 0) return new Color(0.88f, 0.58f, 0.36f, 1f);
            return accent;
        }

        private string Clip(string text, int max)
        {
            text = text.Replace("<", "");
            return text.Length > max ? text.Substring(0, max) : text;
        }
    }
}
