using System;
using System.Collections;
using System.IO;
using System.Reflection;
using AICompanionRoguelike.Memory;
using NUnit.Framework;

namespace AICompanionRoguelike.Tests
{
    public sealed class SaveGameFrameworkTests
    {
        private string temporaryDirectory;
        private string savePath;

        [SetUp]
        public void SetUp()
        {
            CompanionRelationshipState.Clear();
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "AICompanionRoguelike_SaveTests",
                Guid.NewGuid().ToString("N"));
            savePath = Path.Combine(temporaryDirectory, "save_slot_0.json");
        }

        [TearDown]
        public void TearDown()
        {
            CompanionRelationshipState.Clear();

            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        [Test]
        public void JsonStoreRoundTripsVersionedRelationshipData()
        {
            object store = CreateStore();
            object saveData = CreateSaveData(
                saveVersion: 1,
                trust: 73,
                affection: 61,
                tag: RelationshipMemoryTag.Reliable,
                tagScore: 4);

            Invoke(store, "Save", saveData);
            object loadResult = Invoke(store, "Load");

            Assert.AreEqual("Loaded", ReadProperty(loadResult, "Status").ToString());
            object loadedData = ReadProperty(loadResult, "Data");
            Assert.AreEqual(1, ReadField<int>(loadedData, "saveVersion"));

            object relationship = ReadField<object>(loadedData, "relationship");
            Assert.True(ReadField<bool>(relationship, "hasData"));
            Assert.AreEqual(73, ReadField<int>(relationship, "trust"));
            Assert.AreEqual(61, ReadField<int>(relationship, "affection"));

            IList tags = ReadField<IList>(relationship, "memoryTags");
            Assert.AreEqual(1, tags.Count);
            RelationshipMemoryTagScore storedTag = (RelationshipMemoryTagScore)tags[0];
            Assert.AreEqual(RelationshipMemoryTag.Reliable, storedTag.tag);
            Assert.AreEqual(4, storedTag.score);
        }

        [Test]
        public void JsonStoreReportsCorruptedDataWithoutThrowing()
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(savePath, "{ definitely not valid json");
            object store = CreateStore();

            object loadResult = Invoke(store, "Load");

            Assert.AreEqual("Corrupted", ReadProperty(loadResult, "Status").ToString());
            Assert.IsNull(ReadProperty(loadResult, "Data"));
        }

        [Test]
        public void SaveServiceRestoresRelationshipIntoRuntimeState()
        {
            object store = CreateStore();
            object saveData = CreateSaveData(
                saveVersion: 1,
                trust: 82,
                affection: 69,
                tag: RelationshipMemoryTag.Protected,
                tagScore: 3);
            Invoke(store, "Save", saveData);

            object service = CreateService(store);
            object loadResult = Invoke(service, "LoadIntoSession");

            Assert.AreEqual("Loaded", ReadProperty(loadResult, "Status").ToString());
            Assert.True(CompanionRelationshipState.HasState);
            Assert.AreEqual(82, CompanionRelationshipState.Trust);
            Assert.AreEqual(69, CompanionRelationshipState.Affection);
            Assert.AreEqual(3, ReadRuntimeTagScore(RelationshipMemoryTag.Protected));
        }

        [Test]
        public void SaveServiceCapturesCurrentRelationshipForNextProcess()
        {
            RelationshipMemoryTagScore[] tags =
            {
                new RelationshipMemoryTagScore
                {
                    tag = RelationshipMemoryTag.Brave,
                    score = 2
                }
            };
            CompanionRelationshipState.SaveSnapshot(76, 67, tags);
            object service = CreateService(CreateStore());

            Assert.True((bool)Invoke(service, "SaveSession"));
            CompanionRelationshipState.Clear();
            object loadResult = Invoke(service, "LoadIntoSession");

            Assert.AreEqual("Loaded", ReadProperty(loadResult, "Status").ToString());
            Assert.AreEqual(76, CompanionRelationshipState.Trust);
            Assert.AreEqual(67, CompanionRelationshipState.Affection);
            Assert.AreEqual(2, ReadRuntimeTagScore(RelationshipMemoryTag.Brave));
        }

        [Test]
        public void SaveServiceRejectsNewerSaveVersion()
        {
            object store = CreateStore();
            object saveData = CreateSaveData(
                saveVersion: 999,
                trust: 90,
                affection: 90,
                tag: RelationshipMemoryTag.Reliable,
                tagScore: 9);
            Invoke(store, "Save", saveData);
            CompanionRelationshipState.Clear();

            object service = CreateService(store);
            object loadResult = Invoke(service, "LoadIntoSession");

            Assert.AreEqual("UnsupportedVersion", ReadProperty(loadResult, "Status").ToString());
            Assert.False(CompanionRelationshipState.HasState);
        }

        [Test]
        public void RelationshipSnapshotNotifiesAutosaveButRestoreIsSilent()
        {
            Type stateType = typeof(CompanionRelationshipState);
            EventInfo stateChangedEvent = stateType.GetEvent("StateChanged", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(stateChangedEvent, "CompanionRelationshipState should expose StateChanged.");

            int notificationCount = 0;
            Action handler = () => notificationCount++;
            stateChangedEvent.AddEventHandler(null, handler);

            try
            {
                CompanionRelationshipState.SaveSnapshot(64, 58, Array.Empty<RelationshipMemoryTagScore>());
                Assert.AreEqual(1, notificationCount);

                MethodInfo restoreMethod = stateType.GetMethod(
                    "RestoreSnapshot",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(restoreMethod, "CompanionRelationshipState should expose RestoreSnapshot.");
                restoreMethod.Invoke(
                    null,
                    new object[] { 70, 62, Array.Empty<RelationshipMemoryTagScore>() });

                Assert.AreEqual(1, notificationCount);
                Assert.AreEqual(70, CompanionRelationshipState.Trust);
                Assert.AreEqual(62, CompanionRelationshipState.Affection);
            }
            finally
            {
                stateChangedEvent.RemoveEventHandler(null, handler);
            }
        }

        [Test]
        public void AutosaveSessionPersistsRelationshipChangesAndCanUnsubscribe()
        {
            object service = CreateService(CreateStore());
            Type sessionType = RequireRuntimeType("AICompanionRoguelike.Save.RelationshipAutosaveSession");
            object session = Activator.CreateInstance(sessionType, service);

            Invoke(session, "Start");
            CompanionRelationshipState.SaveSnapshot(
                79,
                71,
                new[]
                {
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Brave,
                        score = 5
                    }
                });

            Assert.True(File.Exists(savePath));
            CompanionRelationshipState.Clear();
            Invoke(service, "LoadIntoSession");
            Assert.AreEqual(79, CompanionRelationshipState.Trust);
            Assert.AreEqual(71, CompanionRelationshipState.Affection);
            Assert.AreEqual(5, ReadRuntimeTagScore(RelationshipMemoryTag.Brave));

            Invoke(session, "Dispose");
            File.Delete(savePath);
            CompanionRelationshipState.SaveSnapshot(20, 20, Array.Empty<RelationshipMemoryTagScore>());
            Assert.False(File.Exists(savePath), "Disposed autosave session should unsubscribe from state changes.");
        }

        [Test]
        public void RuntimeCoordinatorReservesStableDefaultSlotPath()
        {
            Type coordinatorType = RequireRuntimeType("AICompanionRoguelike.Save.SaveGameCoordinator");
            PropertyInfo slotProperty = coordinatorType.GetProperty(
                "DefaultSlotId",
                BindingFlags.Public | BindingFlags.Static);
            PropertyInfo pathProperty = coordinatorType.GetProperty(
                "DefaultSavePath",
                BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(slotProperty);
            Assert.NotNull(pathProperty);
            Assert.AreEqual("slot_0", slotProperty.GetValue(null));
            Assert.AreEqual("save_slot_0.json", Path.GetFileName((string)pathProperty.GetValue(null)));
        }

        private object CreateStore()
        {
            Type storeType = RequireRuntimeType("AICompanionRoguelike.Save.JsonGameSaveStore");
            return Activator.CreateInstance(storeType, savePath);
        }

        private static object CreateService(object store)
        {
            Type serviceType = RequireRuntimeType("AICompanionRoguelike.Save.GameSaveService");
            return Activator.CreateInstance(serviceType, store);
        }

        private static object CreateSaveData(
            int saveVersion,
            int trust,
            int affection,
            RelationshipMemoryTag tag,
            int tagScore)
        {
            Type saveDataType = RequireRuntimeType("AICompanionRoguelike.Save.GameSaveData");
            object saveData = Activator.CreateInstance(saveDataType);
            WriteField(saveData, "saveVersion", saveVersion);
            WriteField(saveData, "savedAtUtc", "2026-06-20T00:00:00.0000000Z");

            object relationship = ReadField<object>(saveData, "relationship");
            Assert.NotNull(relationship, "GameSaveData should initialize its relationship section.");
            WriteField(relationship, "hasData", true);
            WriteField(relationship, "trust", trust);
            WriteField(relationship, "affection", affection);

            IList memoryTags = ReadField<IList>(relationship, "memoryTags");
            memoryTags.Add(new RelationshipMemoryTagScore
            {
                tag = tag,
                score = tagScore
            });
            return saveData;
        }

        private static int ReadRuntimeTagScore(RelationshipMemoryTag tag)
        {
            for (int i = 0; i < CompanionRelationshipState.MemoryTags.Count; i++)
            {
                RelationshipMemoryTagScore entry = CompanionRelationshipState.MemoryTags[i];
                if (entry.tag == tag)
                {
                    return entry.score;
                }
            }

            return 0;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }

        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static T ReadField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"{target.GetType().Name} should expose field {fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void WriteField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"{target.GetType().Name} should expose field {fieldName}.");
            field.SetValue(target, value);
        }
    }
}
