using System.IO;
using AICompanionRoguelike.Save;
using UnityEditor;
using UnityEngine;

namespace AICompanionRoguelike.EditorTools
{
    public static class SaveGameDebugMenu
    {
        [MenuItem("Tools/AI Companion/Clear Default Save")]
        private static void ClearDefaultSave()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Default Save",
                "Delete the v0.17 default-slot save data?",
                "Clear",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            bool deleted = SaveGameCoordinator.ClearDefaultSave();
            Debug.Log(deleted ? "Default save data cleared." : "No default save data existed.");
        }

        [MenuItem("Tools/AI Companion/Clear Default Save", true)]
        private static bool ValidateClearDefaultSave()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Tools/AI Companion/Reveal Save Folder")]
        private static void RevealSaveFolder()
        {
            string directory = Path.GetDirectoryName(SaveGameCoordinator.DefaultSavePath);
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }
    }
}
