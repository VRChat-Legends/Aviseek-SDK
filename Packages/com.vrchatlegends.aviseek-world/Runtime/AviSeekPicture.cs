using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace VRChatLegends.AviSeek
{
    // vrchat's own picture of an avatar on the wall. a hidden avatar pedestal draws it, so the
    // picture goes straight from vrchat to each player with no download from aviseek at all.
    // vrchat draws that picture about a metre and a half wide, so the wall measures it once on a
    // pedestal under the floor, then shrinks each picture's pedestal to fit its spot
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Picture")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekPicture : UdonSharpBehaviour
    {
        public AviSeekSearch search;
        [Tooltip("The pedestal that draws the picture. It never switches anyone's avatar.")]
        public VRCAvatarPedestal pedestal;
        [Tooltip("The pedestal's placement. The wall moves and scales it to fit the area.")]
        public Transform placement;
        [Tooltip("The spot on the wall the picture fills.")]
        public RectTransform area;
        [Tooltip("How far the picture floats in front of the area, in metres.")]
        public float lift = 0.004f;

        private string _id = "";
        private bool _probing;
        private int _tries;
        private float _lastWidth;
        private float _lastHeight;

        public void Show(string id)
        {
            if (pedestal == null || id == null || id.Length == 0)
            {
                Clear();
                return;
            }
            GameObject body = pedestal.gameObject;
            if (!body.activeSelf) body.SetActive(true);
            if (id == _id) return;
            _id = id;
            pedestal.SwitchAvatar(id);
        }

        public void Clear()
        {
            _id = "";
            if (pedestal != null && pedestal.gameObject.activeSelf) pedestal.gameObject.SetActive(false);
        }

        // the measuring run. the placement sits under the floor, square to the world and at full
        // size, so the bounds of what vrchat draws give its size and where it sits against the placement
        public void Probe(string id)
        {
            if (pedestal == null || placement == null || id == null || id.Length == 0) return;
            _probing = true;
            _tries = 0;
            _lastWidth = 0f;
            _lastHeight = 0f;
            placement.rotation = Quaternion.identity;
            placement.localScale = Vector3.one;
            placement.position = transform.position - new Vector3(0f, 200f, 0f);
            Show(id);
            SendCustomEventDelayedSeconds(nameof(_Measure), 1f);
        }

        public void _Measure()
        {
            if (!_probing) return;
            _tries++;
            // the join animation may still be turning the panel, so square up again before measuring
            placement.rotation = Quaternion.identity;
            placement.position = transform.position - new Vector3(0f, 200f, 0f);
            Renderer[] parts = placement.GetComponentsInChildren<Renderer>();
            bool found = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < parts.Length; i++)
            {
                Renderer part = parts[i];
                if (part == null || !part.enabled) continue;
                Bounds b = part.bounds;
                if (b.size.sqrMagnitude <= 0f) continue;
                if (!found)
                {
                    bounds = b;
                    found = true;
                }
                else bounds.Encapsulate(b);
            }
            float unit = placement.lossyScale.x;
            if (found && unit > 0f)
            {
                float width = Mathf.Max(bounds.size.x, bounds.size.z) / unit;
                float height = bounds.size.y / unit;
                // the same size twice in a row, so a picture still growing in is not caught half way
                bool steady = Mathf.Abs(width - _lastWidth) < width * 0.01f && Mathf.Abs(height - _lastHeight) < height * 0.01f;
                _lastWidth = width;
                _lastHeight = height;
                if (width > 0.01f && height > 0.01f && steady)
                {
                    _probing = false;
                    Debug.Log("[AviSeek] Pedestal picture is " + width.ToString("F3") + " x " + height.ToString("F3") + ", centre " + ((bounds.center - placement.position) / unit).ToString("F3"));
                    if (search != null) search._PictureMeasured(width, height, (bounds.center - placement.position) / unit);
                    return;
                }
            }
            if (_tries < 40)
            {
                SendCustomEventDelayedSeconds(nameof(_Measure), 1f);
                return;
            }
            // nothing to measure, so the pictures stay off rather than risk a giant one on the wall
            _probing = false;
            Clear();
            Debug.LogWarning("[AviSeek] Could not measure the pedestal picture. Avatar pictures stay off.");
        }

        // shrinks the picture into the area, centred, keeping its shape
        public void Fit(float width, float height, Vector3 offset)
        {
            if (placement == null || area == null || width <= 0f || height <= 0f) return;
            Rect rect = area.rect;
            float scale = Mathf.Min(rect.width * area.lossyScale.x / width, rect.height * area.lossyScale.y / height);
            Transform parent = placement.parent;
            float parentScale = parent == null ? 1f : parent.lossyScale.x;
            placement.rotation = area.rotation * Quaternion.Euler(0f, 180f, 0f);
            placement.localScale = Vector3.one * (scale / parentScale);
            Vector3 centre = area.TransformPoint(rect.center) - area.forward * lift;
            placement.position = centre - placement.rotation * (offset * scale);
        }
    }
}
