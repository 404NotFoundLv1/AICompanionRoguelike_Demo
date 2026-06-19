using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AICompanionRoguelike.Save
{
    public enum GameSaveLoadStatus
    {
        Missing,
        Loaded,
        Corrupted,
        UnsupportedVersion
    }

    public readonly struct GameSaveLoadResult
    {
        public GameSaveLoadResult(
            GameSaveLoadStatus status,
            GameSaveData data,
            string errorMessage = "")
        {
            Status = status;
            Data = data;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public GameSaveLoadStatus Status { get; }
        public GameSaveData Data { get; }
        public string ErrorMessage { get; }
        public bool IsLoaded => Status == GameSaveLoadStatus.Loaded && Data != null;
    }

    public sealed class JsonGameSaveStore
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public JsonGameSaveStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Save file path cannot be empty.", nameof(filePath));
            }

            FilePath = Path.GetFullPath(filePath);
        }

        public string FilePath { get; }
        public string TemporaryFilePath => FilePath + ".tmp";
        public string BackupFilePath => FilePath + ".bak";

        public void Save(GameSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.Normalize();
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(TemporaryFilePath, json, Utf8WithoutBom);
            CommitTemporaryFile();
        }

        public GameSaveLoadResult Load()
        {
            if (!File.Exists(FilePath))
            {
                return new GameSaveLoadResult(GameSaveLoadStatus.Missing, null);
            }

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null || data.saveVersion <= 0)
                {
                    return new GameSaveLoadResult(
                        GameSaveLoadStatus.Corrupted,
                        null,
                        "Save file does not contain a valid schema version.");
                }

                if (data.saveVersion > GameSaveData.CurrentVersion)
                {
                    return new GameSaveLoadResult(
                        GameSaveLoadStatus.UnsupportedVersion,
                        null,
                        $"Save version {data.saveVersion} is newer than supported version {GameSaveData.CurrentVersion}.");
                }

                data.Normalize();
                return new GameSaveLoadResult(GameSaveLoadStatus.Loaded, data);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                return new GameSaveLoadResult(
                    GameSaveLoadStatus.Corrupted,
                    null,
                    exception.Message);
            }
        }

        public bool Delete()
        {
            bool hadSaveFiles = false;
            hadSaveFiles |= DeleteIfPresent(FilePath);
            hadSaveFiles |= DeleteIfPresent(TemporaryFilePath);
            hadSaveFiles |= DeleteIfPresent(BackupFilePath);
            return hadSaveFiles;
        }

        private void CommitTemporaryFile()
        {
            if (!File.Exists(FilePath))
            {
                File.Move(TemporaryFilePath, FilePath);
                return;
            }

            try
            {
                File.Replace(TemporaryFilePath, FilePath, BackupFilePath, ignoreMetadataErrors: true);
            }
            catch (Exception exception) when (
                exception is PlatformNotSupportedException
                || exception is IOException)
            {
                File.Copy(FilePath, BackupFilePath, overwrite: true);
                File.Copy(TemporaryFilePath, FilePath, overwrite: true);
                File.Delete(TemporaryFilePath);
            }
        }

        private static bool DeleteIfPresent(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
    }
}
