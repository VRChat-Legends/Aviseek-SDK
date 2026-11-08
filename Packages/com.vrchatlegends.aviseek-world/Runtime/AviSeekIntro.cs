using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // the wall's entrance when a player joins: the aviseek logo fills the view, then the view
    // dives into its magnifying glass and the world shows through the lens. after that each
    // panel flips up from flat and grows into place one after another with a small overshoot,
    // its contents fading in, a soft click as it lands, then a welcome banner slides in with
    // the player's name and fades away. it only plays for the player who just joined
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

        [Header("Logo")]
        [Tooltip("Shows the logo over the whole view on joining, then zooms into the magnifying glass.")]
        public bool showLogo = true;
        [Tooltip("The quad held in front of the player's eyes while the logo plays.")]
        public Transform overlay;
        public MeshRenderer overlayRenderer;
        [Tooltip("Seconds the logo stays still before the zoom.")]
        public float logoSeconds = 1.4f;
        [Tooltip("Seconds the zoom into the glass takes.")]
        public float zoomSeconds = 1.8f;
        [Tooltip("How far the zoom goes. The lens fills the view long before the end.")]
        public float zoomTo = 12f;

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
        // the intro's own clock. the first frames after joining can take a whole second each,
        // and the logo would skip straight to the end on real time
        private float _clock;
        private Material _overlayMaterial;
        private bool _overlayOn;
        private Vector2 _lens = new Vector2(0.4974f, 0.6567f);

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
            _clock = 0f;
            _start = startDelay;
            _overlayOn = showLogo && overlay != null && overlayRenderer != null;
            if (_overlayOn)
            {
                // held in front of the eyes in world space, whatever the wall's own scale is
                overlay.SetParent(null, false);
                overlay.localScale = Vector3.one * 2.4f;
                _overlayMaterial = overlayRenderer.material;
                Vector4 lens = _overlayMaterial.GetVector("_LensCentre");
                _lens = new Vector2(lens.x, lens.y);
                overlay.gameObject.SetActive(true);
                Logo(0f);
                // the panels begin as the glass opens up around the player
                _start = 0.3f + logoSeconds + zoomSeconds * 0.8f;
            }
            else if (overlay != null) overlay.gameObject.SetActive(false);
            _running = true;
            SendCustomEventDelayedFrames(nameof(_Step), 1);
        }

        // t is seconds since the logo came up
        private void Logo(float t)
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null)
            {
                VRCPlayerApi.TrackingData head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                overlay.SetPositionAndRotation(head.position + head.rotation * Vector3.forward * 0.4f, head.rotation);
            }
            float k = Mathf.Clamp01((t - logoSeconds) / Mathf.Max(zoomSeconds, 0.01f));
            // slow at first and faster into the glass, like falling in
            float dive = k * k * (1.4f - 0.4f * k);
            float zoom = Mathf.Exp(Mathf.Log(Mathf.Max(zoomTo, 1f)) * dive);
            float aim = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.35f));
            Vector2 anchor = Vector2.Lerp(new Vector2(0.5f, 0.5f), _lens, aim);
            _overlayMaterial.SetVector("_Anchor", new Vector4(anchor.x, anchor.y, 0f, 0f));
            _overlayMaterial.SetFloat("_Zoom", zoom);
            _overlayMaterial.SetFloat("_Hole", 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.05f) / 0.35f)));
            _overlayMaterial.SetFloat("_Cat", 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.3f)));
            _overlayMaterial.SetFloat("_Back", 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.7f) / 0.3f)));
            // the logo fades up over the dark backdrop as the world finishes loading
            float fadeIn = Mathf.Clamp01(t / 0.5f);
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.85f) / 0.15f));
            _overlayMaterial.SetFloat("_Fade", fadeOut);
            _overlayMaterial.SetFloat("_LogoAlpha", fadeIn);
        }

        public void _Step()
        {
            if (!_running) return;
            _clock += Mathf.Min(Time.deltaTime, 1f / 30f);
            float now = _clock;
            bool busy = false;
            if (_overlayOn)
            {
                float t = now - 0.3f;
                if (t < logoSeconds + zoomSeconds)
                {
                    Logo(Mathf.Max(t, 0f));
                    busy = true;
                }
                else
                {
                    _overlayOn = false;
                    overlay.gameObject.SetActive(false);
                }
            }
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
            _overlayOn = false;
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
    }
}
