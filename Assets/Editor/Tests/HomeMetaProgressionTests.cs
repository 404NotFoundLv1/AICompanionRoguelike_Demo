using System;
using System.Collections;
using System.IO;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class HomeMetaProgressionTests
    {
        private string temporaryDirectory;
        private string savePath;

        [SetUp]
        public void SetUp()
        {
            ClearMetaProgressionIfAvailable();
            CompanionRelationshipState.Clear();
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "AICompanionRoguelike_MetaTests",
                Guid.NewGuid().ToString("N"));
            savePath = Path.Combine(temporaryDirectory, "save_slot_0.json");

            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }
        }

        [TearDown]
        public void TearDown()
        {
            ClearMetaProgressionIfAvailable();
            CompanionRelationshipState.Clear();

            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }

            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        [Test]
        public void SaveServiceRoundTripsMetaProgression()
        {
            Type metaStateType = RequireRuntimeType("AICompanionRoguelike.Progression.MetaProgressionState");
            InvokeStatic(metaStateType, "SaveSnapshot", 25, 2, 1, 3);
            object service = CreateService(CreateStore());

            Assert.True((bool)Invoke(service, "SaveSession"));
            InvokeStatic(metaStateType, "Clear");
            object loadResult = Invoke(service, "LoadIntoSession");

            Assert.AreEqual("Loaded", ReadProperty(loadResult, "Status").ToString());
            Assert.AreEqual(25, ReadStaticProperty<int>(metaStateType, "CoreFragments"));
            Assert.AreEqual(2, ReadStaticProperty<int>(metaStateType, "PlayerMaxHealthLevel"));
            Assert.AreEqual(1, ReadStaticProperty<int>(metaStateType, "PlayerDamageLevel"));
            Assert.AreEqual(3, ReadStaticProperty<int>(metaStateType, "CompanionCooldownLevel"));
        }

        [Test]
        public void RunEndAwardsCoreFragmentsAndRecordsSummary()
        {
            Type metaStateType = RequireRuntimeType("AICompanionRoguelike.Progression.MetaProgressionState");
            InvokeStatic(metaStateType, "Clear");

            RunSessionState.EnsureRunStartedFromBattleScene("Assets/Scenes/TestBattleScene.unity");
            RunSessionState.RecordRoomCleared(RoomType.BattleRoom, 1);
            RunSessionState.RecordRoomCleared(RoomType.EliteRoom, 2);
            InvokeStatic(
                typeof(RunSessionState),
                "RecordGrowthRouteSummary",
                "Player Lv3",
                "Damage x1.24",
                1,
                3);
            RunSessionState.EndRun(RunEndReason.Victory, 50, 50);

            RunSessionSummary summary = RunSessionState.LastSummary;
            int earned = ReadProperty<int>(summary, "MetaFragmentsEarned");
            Assert.That(earned, Is.GreaterThan(0));
            Assert.AreEqual(earned, ReadStaticProperty<int>(metaStateType, "CoreFragments"));
            Assert.AreEqual(earned, ReadProperty<int>(summary, "MetaFragmentsTotal"));
            Assert.That(ReadProperty<string>(summary, "MetaProgressionSummaryLine"), Does.Contain("Core Fragments"));
        }

        [Test]
        public void PurchasingMetaUpgradeSpendsFragmentsAndRaisesLevel()
        {
            Type metaStateType = RequireRuntimeType("AICompanionRoguelike.Progression.MetaProgressionState");
            Type upgradeType = RequireRuntimeType("AICompanionRoguelike.Progression.MetaUpgradeType");
            object maxHealthUpgrade = Enum.Parse(upgradeType, "PlayerMaxHealth");
            InvokeStatic(metaStateType, "RestoreSnapshot", 20, 0, 0, 0);

            int cost = (int)InvokeStatic(metaStateType, "GetUpgradeCost", maxHealthUpgrade);
            bool purchased = (bool)InvokeStatic(metaStateType, "TryPurchaseUpgrade", maxHealthUpgrade);

            Assert.True(purchased);
            Assert.AreEqual(20 - cost, ReadStaticProperty<int>(metaStateType, "CoreFragments"));
            Assert.AreEqual(1, ReadStaticProperty<int>(metaStateType, "PlayerMaxHealthLevel"));
        }

        [Test]
        public void RunStartAppliesSavedMetaUpgradesToPlayerAndCompanion()
        {
            Type metaStateType = RequireRuntimeType("AICompanionRoguelike.Progression.MetaProgressionState");
            InvokeStatic(metaStateType, "RestoreSnapshot", 50, 2, 1, 1);
            GameObject player = CreatePlayer("Player");
            GameObject companion = CreateCompanion("MetaProgressionCompanion");
            GameObject runObject = CreateRunManagerObject("MetaProgressionRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                HealthComponent playerHealth = player.GetComponent<HealthComponent>();
                PlayerCombat2D playerCombat = player.GetComponent<PlayerCombat2D>();
                CompanionCombat companionCombat = companion.GetComponent<CompanionCombat>();
                float baseHealth = playerHealth.MaxHealth;
                float baseDamage = playerCombat.Damage;
                float baseCooldown = companionCombat.Cooldown;

                runManager.StartRun();

                Assert.That(playerHealth.MaxHealth, Is.GreaterThan(baseHealth));
                Assert.That(playerCombat.Damage, Is.GreaterThan(baseDamage));
                Assert.That(companionCombat.Cooldown, Is.LessThan(baseCooldown));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(companion);
                UnityEngine.Object.DestroyImmediate(player);
            }
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

        private static GameObject CreateRunManagerObject(string objectName)
        {
            GameObject runObject = new GameObject(objectName);
            runObject.AddComponent<RoomManager>();
            runObject.AddComponent<RunManager>();
            return runObject;
        }

        private static GameObject CreatePlayer(string objectName)
        {
            GameObject player = new GameObject(objectName);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerCombat2D>();
            return player;
        }

        private static GameObject CreateCompanion(string objectName)
        {
            GameObject companion = new GameObject(objectName);
            companion.AddComponent<HealthComponent>().SetMaxHealth(80f, true);
            companion.AddComponent<CompanionSensor>();
            companion.AddComponent<CompanionCombat>();
            return companion;
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

        private static object InvokeStatic(Type type, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(null, parameters);
                }
            }

            Assert.Fail($"{type.Name} should expose static {methodName} with {parameters.Length} parameters.");
            return null;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            return (T)ReadProperty(target, propertyName);
        }

        private static T ReadStaticProperty<T>(Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, $"{type.Name} should expose static property {propertyName}.");
            return (T)property.GetValue(null);
        }

        private static void ClearMetaProgressionIfAvailable()
        {
            Type type = Type.GetType("AICompanionRoguelike.Progression.MetaProgressionState, Assembly-CSharp");
            if (type != null)
            {
                InvokeStatic(type, "Clear");
            }
        }
    }
}
