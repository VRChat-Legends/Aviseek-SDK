using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VRChatLegends.AviSeek
{
    // touch screen for vr players. it follows the very tip of both index fingers, outlines
    // whatever button the finger is over, draws a short trail as the finger slides along a
    // panel and presses a button when the fingertip reaches that button's own surface, with a
    // click and a haptic tap. the laser pointer keeps working as well. the prefab builder
    // fills the list of things that can be pressed
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Touch")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekTouch : UdonSharpBehaviour
    {
        [Tooltip("Lets vr players press the panels with their fingers.")]
        public bool touchEnabled = true;
        [Tooltip("How close a fingertip has to be before its cursor shows, in metres.")]
        public float hoverDistance = 0.12f;
        [Tooltip("A press lands this far in front of a button's surface, in metres, so a light touch counts.")]
        public float pressDistance = 0.006f;
        [Tooltip("How far a finger has to pull back before it can press again, in metres.")]
        public float releaseDistance = 0.022f;
        [Tooltip("The trail only draws while the fingertip is this close to the panel, in metres.")]
        public float trailDistance = 0.035f;
        [Tooltip("Only players this close to the wall are tracked, in metres.")]
        public float nearDistance = 7f;

        [Header("Panels")]
        public RectTransform[] panels;
        [Tooltip("A panel that goes with the player, like the profile menu. Fingers are tracked while it is open, however far from the wall.")]
        public RectTransform followPanel;

        [Header("Things to press")]
        public RectTransform[] targets;
        [Tooltip("The panel each target is on, by its place in Panels.")]
        public int[] targetPanels;
        public UdonBehaviour[] targetBehaviours;
        public string[] targetEvents;
        [Tooltip("Text boxes are selected instead, which opens the VRChat keyboard.")]
        public Selectable[] targetFields;

        [Header("Cursors, left hand then right")]
        public Transform[] cursors;
        public RectTransform[] rings;
        public Image[] ringImages;
        public Image[] dots;
        public RectTransform[] ripples;
        public Image[] rippleImages;
        public TrailRenderer[] trails;
        [Tooltip("The outline that hugs the button under each finger.")]
        public RectTransform[] highlights;
        public Image[] highlightImages;

        [Header("Sounds")]
        public AudioSource[] voices;
        public AudioClip hoverClip;
        public AudioClip pressClip;
        public AudioClip releaseClip;
        [Range(0f, 1f)] public float volume = 0.45f;

        [Header("Colours")]
        public Color accent = new Color(0.525f, 0.486f, 0.996f, 1f);

        private bool[] _pressed = new bool[2];
        private float[] _ripple = { 9f, 9f };
        private float[] _spin = new float[2];
        private float[] _lastPress = { -9f, -9f };
        private int[] _hover = { -1, -1 };
        private float[] _hoverFade = new float[2];
        private float[] _flash = new float[2];
        private Vector3[] _smooth = new Vector3[2];
        private bool[] _shown = new bool[2];
        // the cursor is built for the wall's panels. on a smaller panel, like the profile menu,
        // it shrinks with it so it never covers the buttons
        private float[] _cursorScale = new float[2];
        private float[] _trailWidth = new float[2];
        private float _wallScale = 1f;
        private float[] _lastTick = { -9f, -9f };
        private bool _vr;
        private float _nextCheck;

        private void Start()
        {
            for (int hand = 0; hand < 2; hand++)
            {
                if (cursors != null && hand < cursors.Length && cursors[hand] != null)
                {
                    _cursorScale[hand] = cursors[hand].localScale.x;
                    _wallScale = cursors[hand].lossyScale.x;
                }
                if (trails != null && hand < trails.Length && trails[hand] != null) _trailWidth[hand] = trails[hand].widthMultiplier;
            }
            HideCursor(0);
            HideCursor(1);
        }

        private void Update()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + 2f;
                _vr = player.IsUserInVR();
            }
            bool near = Vector3.Distance(player.GetPosition(), transform.position) <= nearDistance
                || (followPanel != null && followPanel.gameObject.activeInHierarchy);
            if (!touchEnabled || !_vr || panels == null || !near)
            {
                HideCursor(0);
                HideCursor(1);
                return;
            }
            Track(player, 0, HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate, VRCPlayerApi.TrackingDataType.LeftHand);
            Track(player, 1, HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate, VRCPlayerApi.TrackingDataType.RightHand);
        }

        // the distal bone sits at the last knuckle, so the tip is that bone carried on along the finger
        private Vector3 Fingertip(VRCPlayerApi player, HumanBodyBones distal, HumanBodyBones middle, VRCPlayerApi.TrackingDataType tracking)
        {
            Vector3 end = player.GetBonePosition(distal);
            if (end != Vector3.zero)
            {
                Vector3 joint = player.GetBonePosition(middle);
                if (joint != Vector3.zero)
                {
                    Vector3 along = end - joint;
                    // the last finger bone is a little shorter than the one before it
                    if (along.sqrMagnitude > 0.000001f) return end + along * 0.8f;
                }
                return end;
            }
            // avatars without finger bones: a point just past the hand
            VRCPlayerApi.TrackingData data = player.GetTrackingData(tracking);
            return data.position + data.rotation * (Vector3.forward * 0.1f);
        }

        private void Track(VRCPlayerApi player, int hand, HumanBodyBones distal, HumanBodyBones middle, VRCPlayerApi.TrackingDataType tracking)
        {
            Vector3 tip = Fingertip(player, distal, middle, tracking);
            // each hand's sounds come from its own fingertip
            if (voices != null && hand < voices.Length && voices[hand] != null) voices[hand].transform.position = tip;

            // the panel this finger is closest to, in front of it and inside its edges
            int best = -1;
            float bestDistance = 0f;
            Vector3 bestLocal = Vector3.zero;
            for (int i = 0; i < panels.Length; i++)
            {
                RectTransform panel = panels[i];
                if (panel == null || !panel.gameObject.activeInHierarchy) continue;
                Vector3 local = panel.InverseTransformPoint(tip);
                Rect rect = panel.rect;
                if (local.x < rect.xMin || local.x > rect.xMax || local.y < rect.yMin || local.y > rect.yMax) continue;
                // panels face -z, so a finger in front has a negative z, in canvas units
                float distance = -local.z * panel.lossyScale.z;
                if (distance < -0.06f || distance > hoverDistance) continue;
                if (best >= 0 && Mathf.Abs(distance) >= Mathf.Abs(bestDistance)) continue;
                best = i;
                bestDistance = distance;
                bestLocal = local;
            }
            if (best < 0)
            {
                _pressed[hand] = false;
                HideCursor(hand);
                return;
            }

            // the button under the finger, and how far the fingertip is from its raised face
            int target = TargetAt(best, tip);
            float surface = bestDistance;
            if (target >= 0)
            {
                RectTransform t = targets[target];
                surface = -t.InverseTransformPoint(tip).z * t.lossyScale.z;
            }
            if (target != _hover[hand])
            {
                _hover[hand] = target;
                // a soft tick for each new button, never a buzz while a finger slides along a row
                if (target >= 0 && Time.time - _lastTick[hand] > 0.07f)
                {
                    _lastTick[hand] = Time.time;
                    Play(hand, hoverClip, 0.5f);
                }
            }

            if (!_pressed[hand] && target >= 0 && surface <= pressDistance && Time.time - _lastPress[hand] > 0.22f)
            {
                _pressed[hand] = true;
                _lastPress[hand] = Time.time;
                _ripple[hand] = 0f;
                _flash[hand] = 1f;
                if (Press(target))
                {
                    Play(hand, pressClip, 1f);
                    player.PlayHapticEventInHand(hand == 0 ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, 0.05f, 0.6f, 220f);
                }
            }
            else if (_pressed[hand] && surface > releaseDistance)
            {
                _pressed[hand] = false;
                Play(hand, releaseClip, 0.6f);
            }

            DrawCursor(hand, panels[best], bestLocal, bestDistance);
            DrawHighlight(hand, panels[best], target);
        }

        // the topmost thing under the finger on that panel: later entries sit on top of earlier ones
        private int TargetAt(int panel, Vector3 tip)
        {
            if (targets == null) return -1;
            for (int i = targets.Length - 1; i >= 0; i--)
            {
                if (targetPanels != null && i < targetPanels.Length && targetPanels[i] != panel) continue;
                RectTransform target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                Vector3 local = target.InverseTransformPoint(tip);
                if (target.rect.Contains(new Vector2(local.x, local.y))) return i;
            }
            return -1;
        }

        private bool Press(int index)
        {
            Selectable field = targetFields != null && index < targetFields.Length ? targetFields[index] : null;
            if (field != null)
            {
                field.Select();
                return true;
            }
            UdonBehaviour behaviour = targetBehaviours != null && index < targetBehaviours.Length ? targetBehaviours[index] : null;
            string eventName = targetEvents != null && index < targetEvents.Length ? targetEvents[index] : "";
            if (behaviour == null || string.IsNullOrEmpty(eventName)) return false;
            behaviour.SendCustomEvent(eventName);
            return true;
        }

        private void DrawCursor(int hand, RectTransform panel, Vector3 local, float distance)
        {
            if (cursors == null || hand >= cursors.Length || cursors[hand] == null) return;
            Transform cursor = cursors[hand];
            // the spot under the fingertip, just in front of the raised cards so it is never hidden
            Vector3 spot = panel.TransformPoint(new Vector3(local.x, local.y, -22f));
            bool fresh = !_shown[hand];
            if (fresh)
            {
                _shown[hand] = true;
                cursor.gameObject.SetActive(true);
                _smooth[hand] = spot;
            }
            // a light smoothing takes the tremble out of tracking without any lag you can feel
            _smooth[hand] = Vector3.Lerp(_smooth[hand], spot, 1f - Mathf.Exp(-Time.deltaTime * 40f));
            cursor.position = _smooth[hand];
            cursor.rotation = panel.rotation;
            float ratio = _wallScale > 0f ? Mathf.Clamp(panel.lossyScale.x / _wallScale, 0.2f, 1.5f) : 1f;
            cursor.localScale = Vector3.one * (_cursorScale[hand] * ratio);

            TrailRenderer trail = trails != null && hand < trails.Length ? trails[hand] : null;
            if (trail != null)
            {
                if (fresh) trail.Clear();
                trail.widthMultiplier = _trailWidth[hand] * ratio;
                trail.emitting = distance <= trailDistance;
            }

            float near = 1f - Mathf.Clamp01(distance / hoverDistance);
            float pressed = _pressed[hand] ? 1f : 0f;
            _spin[hand] = (_spin[hand] + Time.deltaTime * (50f + near * 260f)) % 360f;
            if (rings != null && hand < rings.Length && rings[hand] != null)
            {
                float size = Mathf.Lerp(2.4f, 0.8f, near * near) - pressed * 0.2f;
                rings[hand].localScale = new Vector3(size, size, 1f);
                rings[hand].localRotation = Quaternion.Euler(0f, 0f, _spin[hand]);
            }
            bool over = _hover[hand] >= 0;
            SetColor(ringImages, hand, over ? accent : Color.white, Mathf.Lerp(0.12f, 0.95f, near));
            if (dots != null && hand < dots.Length && dots[hand] != null)
            {
                float dot = Mathf.Lerp(0.7f, 1.25f, near) + pressed * 0.35f;
                dots[hand].rectTransform.localScale = new Vector3(dot, dot, 1f);
            }
            SetColor(dots, hand, Color.white, Mathf.Lerp(0.3f, 1f, near));

            // a ring that spreads out and fades every time the finger presses
            _ripple[hand] += Time.deltaTime;
            float k = Mathf.Clamp01(_ripple[hand] / 0.45f);
            if (ripples != null && hand < ripples.Length && ripples[hand] != null)
            {
                float size = 0.8f + (1f - (1f - k) * (1f - k)) * 3f;
                ripples[hand].localScale = new Vector3(size, size, 1f);
            }
            SetColor(rippleImages, hand, accent, (1f - k) * 0.9f);
        }

        // an outline that slides to the button under the finger and flashes when it is pressed
        private void DrawHighlight(int hand, RectTransform panel, int target)
        {
            if (highlights == null || hand >= highlights.Length || highlights[hand] == null) return;
            RectTransform frame = highlights[hand];
            _hoverFade[hand] = Mathf.MoveTowards(_hoverFade[hand], target >= 0 ? 1f : 0f, Time.deltaTime * 8f);
            _flash[hand] = Mathf.MoveTowards(_flash[hand], 0f, Time.deltaTime * 4f);
            if (_hoverFade[hand] <= 0f)
            {
                if (frame.gameObject.activeSelf) frame.gameObject.SetActive(false);
                return;
            }
            if (!frame.gameObject.activeSelf) frame.gameObject.SetActive(true);
            if (target >= 0)
            {
                RectTransform t = targets[target];
                Rect rect = t.rect;
                float scale = t.lossyScale.x;
                float pad = 8f;
                frame.position = t.TransformPoint(rect.center) - panel.forward * 0.003f;
                frame.rotation = t.rotation;
                frame.localScale = Vector3.one * (scale / (frame.parent == null ? 1f : frame.parent.lossyScale.x));
                float grow = 1f + _flash[hand] * 0.06f;
                frame.sizeDelta = new Vector2((rect.width + pad * 2f) * grow, (rect.height + pad * 2f) * grow);
            }
            Color color = Color.Lerp(accent, Color.white, _flash[hand] * 0.7f);
            SetColor(highlightImages, hand, color, _hoverFade[hand] * (0.75f + _flash[hand] * 0.25f));
        }

        private void HideCursor(int hand)
        {
            _shown[hand] = false;
            _hover[hand] = -1;
            _hoverFade[hand] = 0f;
            if (cursors != null && hand < cursors.Length && cursors[hand] != null && cursors[hand].gameObject.activeSelf) cursors[hand].gameObject.SetActive(false);
            if (highlights != null && hand < highlights.Length && highlights[hand] != null && highlights[hand].gameObject.activeSelf) highlights[hand].gameObject.SetActive(false);
        }

        private void Play(int hand, AudioClip clip, float loudness)
        {
            if (clip == null || voices == null || hand >= voices.Length || voices[hand] == null) return;
            voices[hand].PlayOneShot(clip, volume * loudness);
        }

        private void SetColor(Image[] images, int hand, Color color, float alpha)
        {
            if (images == null || hand >= images.Length || images[hand] == null) return;
            color.a = alpha;
            images[hand].color = color;
        }
    }
}
