using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VRChatLegends.AviSeek.Editor
{
    // puts a wall's branding fields onto its panels. this runs in the editor only,
    // so the uploaded world simply carries the result
    public static class AviSeekBranding
    {
        private const string BrandingFolder = "Assets/AviSeek Branding";

        public static void Apply(AviSeekSearch wall)
        {
            if (wall == null) return;
            string hex = ColorUtility.ToHtmlStringRGB(wall.accent);
            if (wall.titleLabel != null)
            {
                Undo.RecordObject(wall.titleLabel, "AviSeek branding");
                wall.titleLabel.text = wall.brandTitle + " <color=#" + hex + ">" + wall.brandHighlight + "</color>";
                Record(wall.titleLabel);
            }
            if (wall.logoImage != null)
            {
                Undo.RecordObject(wall.logoImage, "AviSeek branding");
                wall.logoImage.sprite = wall.brandLogo;
                wall.logoImage.enabled = wall.brandLogo != null;
                Record(wall.logoImage);
            }
            if (wall.accentImages != null)
            {
                foreach (var image in wall.accentImages)
                {
                    if (image == null) continue;
                    Undo.RecordObject(image, "AviSeek branding");
                    image.color = new Color(wall.accent.r, wall.accent.g, wall.accent.b, image.color.a);
                    Record(image);
                }
            }
            ApplyGlowMaterials(wall);
        }

        // the rims glow in the accent too. a project copy of the material keeps
        // the package's own untouched
        private static void ApplyGlowMaterials(AviSeekSearch wall)
        {
            bool stock = Approximately(wall.accent, AviSeekEditorUI.Accent);
            Material rim = stock ? Package("Panel Rim") : ProjectCopy("Panel Rim", wall.accent);
            foreach (var renderer in wall.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.gameObject.name;
                Material target = name == "Rim" ? rim : null;
                if (target == null || renderer.sharedMaterial == target) continue;
                Undo.RecordObject(renderer, "AviSeek branding");
                renderer.sharedMaterial = target;
                Record(renderer);
            }
        }

        private static Material Package(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(AviSeekPrefabBuilder.Root + "/Runtime/Textures/Generated/" + name + ".mat");
        }

        private static Material ProjectCopy(string name, Color accent)
        {
            Directory.CreateDirectory(BrandingFolder);
            string path = BrandingFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Package(name));
                AssetDatabase.CreateAsset(material, path);
            }
            float boost = name == "Trim" ? 2.4f : 1.1f;
            material.color = accent;
            material.SetColor("_EmissionColor", accent * boost);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        public static void SetPanel(AviSeekSearch wall, string child, bool on)
        {
            var panel = wall.transform.Find(child);
            if (panel == null || panel.gameObject.activeSelf == on) return;
            Undo.RecordObject(panel.gameObject, "AviSeek panels");
            panel.gameObject.SetActive(on);
            Record(panel.gameObject);
            // the others close up or move apart
            AviSeekPrefabBuilder.Layout(wall.transform, true);
        }

        public static bool PanelOn(AviSeekSearch wall, string child)
        {
            var panel = wall.transform.Find(child);
            return panel != null && panel.gameObject.activeSelf;
        }

        private static void Record(Object changed)
        {
            EditorUtility.SetDirty(changed);
            if (PrefabUtility.IsPartOfPrefabInstance(changed)) PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }
    }
}
