using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRChatLegends.AviSeek.Editor
{
    // a wall placed before an update keeps the panel spots it had then. when a panel changes size
    // in a new version, those old spots would leave panels overlapping, so each open scene's
    // walls are spaced out again. nothing changes, and the scene is not marked, when they fit
    [InitializeOnLoad]
    internal static class AviSeekLayoutCheck
    {
        static AviSeekLayoutCheck()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Check(scene);
            EditorApplication.delayCall += () =>
            {
                for (int i = 0; i < SceneManager.sceneCount; i++) Check(SceneManager.GetSceneAt(i));
            };
        }

        private static void Check(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var wall in root.GetComponentsInChildren<AviSeekSearch>(true))
                {
                    AviSeekPrefabBuilder.Layout(wall.transform, true);
                }
            }
        }
    }
}
