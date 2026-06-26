using System;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RoomModifierVisualFeedbackTests
    {
        [Test]
        public void ModifiedCombatRoomsAttachReadableWorldMarkers()
        {
            GameObject reinforcedObject = new GameObject("ReinforcedVisualRoomManagerTest");
            GameObject ambushObject = new GameObject("AmbushVisualRoomManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager reinforcedManager = reinforcedObject.AddComponent<RoomManager>();
                RoomManager ambushManager = ambushObject.AddComponent<RoomManager>();
                WritePrivateField(reinforcedManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(ambushManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(reinforcedManager, "logRoomMessages", false);
                WritePrivateField(ambushManager, "logRoomMessages", false);

                reinforcedManager.EnterRoom(RoomType.BattleRoom, 1, RoomModifierType.Reinforced);
                ambushManager.EnterRoom(RoomType.BattleRoom, 1, RoomModifierType.Ambush);

                Type markerType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RoomModifierVisualMarker2D");

                AssertRoomMarker(
                    reinforcedManager.ActiveEnemies[0].gameObject,
                    markerType,
                    "Reinforced",
                    "orange");

                Assert.AreEqual(2, ambushManager.ActiveEnemies.Count);
                for (int i = 0; i < ambushManager.ActiveEnemies.Count; i++)
                {
                    AssertRoomMarker(
                        ambushManager.ActiveEnemies[i].gameObject,
                        markerType,
                        "Ambush",
                        "violet");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(ambushObject);
                UnityEngine.Object.DestroyImmediate(reinforcedObject);
            }
        }

        [Test]
        public void EntryModifierFeedbackIncludesVisualLanguageAndBondSpeech()
        {
            GameObject runObject = new GameObject("RunManagerModifierVisualFeedbackTest");
            GameObject playerObject = new GameObject("Player");
            GameObject companionObject = new GameObject("CompanionModifierSpeechTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 4);
                WritePrivateField(
                    runManager,
                    "selectableRoomTypes",
                    new[]
                    {
                        RoomType.BattleRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom,
                        RoomType.EliteRoom
                    });

                HealthComponent health = playerObject.AddComponent<HealthComponent>();
                health.SetMaxHealth(100f, true);
                health.TakeDamage(new DamageInfo(60f, DamageSourceType.Environment, null));

                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);
                CompanionSpeechBubbleUI speechBubble = companionObject.AddComponent<CompanionSpeechBubbleUI>();

                Invoke(runManager, "PrepareNextRoomChoices");
                WritePrivateField(runManager, "waitingForNextRoom", true);
                Invoke(runManager, "AdvanceToSelectedRoom", FindChoiceIndex(runManager, RoomType.SafeRoom));

                Assert.That(ReadStringProperty(runManager, "LastRoomModifierFeedbackTitle"), Does.Contain("Recovery"));
                Assert.That(ReadStringProperty(runManager, "LastRoomModifierFeedbackLine"), Does.Contain("healing field"));
                Color recoveryColor = (Color)ReadProperty(runManager, "LastRoomModifierFeedbackColor");
                Assert.Greater(recoveryColor.g, recoveryColor.r);

                Invoke(runManager, "PrepareNextRoomChoices");
                WritePrivateField(runManager, "waitingForNextRoom", true);
                Invoke(runManager, "AdvanceToSelectedRoom", FindChoiceIndex(runManager, RoomType.ShopRoom));

                Assert.That(ReadStringProperty(runManager, "LastRoomModifierFeedbackTitle"), Does.Contain("Bond Signal"));
                Assert.That(ReadStringProperty(runManager, "LastRoomModifierFeedbackLine"), Does.Contain("Trust +1"));
                Assert.That(ReadStringProperty(runManager, "LastRoomModifierFeedbackLine"), Does.Contain("Affection +1"));
                Assert.That(speechBubble.CurrentMessage, Does.Contain("Bond Signal"));
                Assert.That(relationship.Trust, Is.EqualTo(51));
                Assert.That(relationship.Affection, Is.EqualTo(51));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static void AssertRoomMarker(
            GameObject enemyObject,
            Type markerType,
            string expectedModifierName,
            string expectedColorWord)
        {
            Component marker = enemyObject.GetComponent(markerType);
            Assert.NotNull(marker, $"{enemyObject.name} should expose a modifier marker component.");

            Assert.That(ReadProperty(marker, "ModifierType").ToString(), Is.EqualTo(expectedModifierName));
            Assert.That(ReadStringProperty(marker, "Label"), Does.Contain(expectedModifierName));
            Assert.That(ReadStringProperty(marker, "ReadableVisualHint"), Does.Contain(expectedColorWord));
            Assert.True((bool)ReadProperty(marker, "HasMarkerVisual"));

            Color markerColor = (Color)ReadProperty(marker, "VisualColor");
            Assert.Greater(markerColor.a, 0.5f);
        }

        private static Type RequireRuntimeType(string fullTypeName)
        {
            Type type = Type.GetType($"{fullTypeName}, Assembly-CSharp");
            Assert.NotNull(type, $"{fullTypeName} should exist.");
            return type;
        }

        private static int FindChoiceIndex(RunManager runManager, RoomType roomType)
        {
            for (int i = 0; i < runManager.CurrentRoomChoices.Count; i++)
            {
                if (runManager.CurrentRoomChoices[i] == roomType)
                {
                    return i;
                }
            }

            Assert.Fail($"Expected {roomType} in current room choices.");
            return -1;
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("ModifierVisualEnemyPrefab");
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<HealthComponent>();
            enemy.AddComponent<EnemyAttack2D>();
            enemy.AddComponent<EnemyController2D>();
            enemy.AddComponent<SpriteRenderer>();
            return enemy;
        }

        private static string ReadStringProperty(object target, string propertyName)
        {
            return ReadProperty(target, propertyName) as string;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, $"{type.Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define field {fieldName}.");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].GetType();
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }
    }
}
