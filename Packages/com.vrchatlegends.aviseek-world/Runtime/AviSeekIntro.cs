using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // the wall's entrance when a player joins: each panel flips up from flat and grows into
    // place one after another with a small overshoot, its contents fading in, a soft click as
    // it lands, then a welcome banner slides in with the player's name and fades away.
    // it only plays for the player who just joined, nobody else sees it again
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Intro")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekIntro : UdonSharpBehaviour
    {
        [Tooltip("Plays the entrance when a player joins the world.")]
        public bool playOnJoin = true;
        [Tooltip("Seconds after joining before the first panel moves, so the world has finished loading in.")]
        public float startDelay = 1.2f;
        [Tooltip("Seconds between one panel and the next.")]
        public float stagger = 0.16f;
        [Tooltip("Seconds each panel takes to land.")]
        public float duration = 0.75f;

        [Header("Panels, left to right")]
        public RectTransform[] panels;
        public CanvasGroup[] groups;

        [Header("Welcome")]
        public RectTransform welcome;
        public CanvasGroup welcomeGroup;
        public TextMeshProUGUI welcomeLabel;
        public float welcomeSeconds = 4.5f;

        [Header("Sound")]
        public AudioSource voice;
        public AudioClip landClip;
        [Range(0f, 1f)] public float volume = 0.35f;

        private Vector3[] _scale;
        private Quaternion[] _rotation;
        private bool[] _landed;
        private float _start;
        private bool _running;
        private Vector2 _welcomeHome;

        private void Start()
        {
            int count = panels == null ? 0 : panels.Length;
            _scale = new Vector3[count];
            _rotation = new Quaternion[count];
            _landed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                if (panels[i] == null) continue;
                _scale[i] = panels[i].localScale;
                _rotation[i] = panels[i].localRotation;
            }
            if (welcome != null) _welcomeHome = welcome.anchoredPosition;
            if (welcomeGroup != null) welcomeGroup.alpha = 0f;
            if (!playOnJoin)
            {
                Finish();
                return;
            }
            VRCPlayerApi player = Networking.LocalPlayer;
            if (welcomeLabel != null && player != null) welcomeLabel.text = "Welcome, <color=#a78bfa>" + player.displayName.Replace("<", "<​") + "</color>";
            for (int i = 0; i < count; i++) Pose(i, 0f);
            _start = Time.time + startDelay;
            _running = true;
            SendCustomEventDelayedFrames(nameof(_Step), 1);
        }

        public void _Step()
        {
            if (!_running) return;
            float now = Time.time;
            bool busy = false;
            for (int i = 0; i < panels.Length; i++)
            {
                float k = Mathf.Clamp01((now - _start - i * stagger) / duration);
                Pose(i, k);
                if (k < 1f) busy = true;
                else if (!_landed[i])
                {
                    _landed[i] = true;
                    if (panels[i] != null && panels[i].gameObject.activeInHierarchy && voice != null && landClip != null)
                    {
                        voice.pitch = 0.9f + i * 0.05f;
                        voice.PlayOneShot(landClip, volume);
                    }
                }
            }

            // the welcome banner drops in after the last panel and leaves on its own
            float w = now - _start - panels.Length * stagger - duration * 0.5f;
            if (welcome != null && welcomeGroup != null)
            {
                float into = Mathf.Clamp01(w / 0.5f);
                float away = Mathf.Clamp01((w - welcomeSeconds) / 0.8f);
                welcomeGroup.alpha = Ease(into) * (1f - away);
                welcome.anchoredPosition = _welcomeHome + new Vector2(0f, (1f - Ease(into)) * 40f + away * 20f);
                if (w < welcomeSeconds + 0.8f) busy = true;
            }

            if (busy)
            {
                SendCustomEventDelayedFrames(nameof(_Step), 1);
                return;
            }
            Finish();
        }

        // k from 0 (flat and tiny) to 1 (in place)
        private void Pose(int i, float k)
        {
            RectTransform panel = panels[i];
            if (panel == null) return;
            float e = Ease(k);
            panel.localScale = _scale[i] * Mathf.Max(0.0001f, e);
            panel.localRotation = _rotation[i] * Quaternion.Euler((1f - e) * 80f, 0f, 0f);
            if (groups != null && i < groups.Length && groups[i] != null) groups[i].alpha = Mathf.Clamp01(k * 1.6f);
        }

        // ease out with a small overshoot
        private float Ease(float k)
        {
            float c = 1.6f;
            return 1f + (c + 1f) * Mathf.Pow(k - 1f, 3f) + c * Mathf.Pow(k - 1f, 2f);
        }

        private void Finish()
        {
            _running = false;
            if (panels == null) return;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                panels[i].localScale = _scale[i];
                panels[i].localRotation = _rotation[i];
                if (groups != null && i < groups.Length && groups[i] != null) groups[i].alpha = 1f;
            }
            if (welcomeGroup != null) welcomeGroup.alpha = 0f;
        }
    }
}
