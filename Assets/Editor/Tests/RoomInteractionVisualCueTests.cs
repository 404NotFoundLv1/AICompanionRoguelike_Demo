using System;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RoomInteractionVisualCueTests
    {
        [TearDown]
        public void TearDown()
        {
            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }
        }

        [Test]
        public void SharedCueCanConfigureRoleAndMarkerVisual()
        {
            Type cueType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RoomInteractionVisualCue2D");
            GameObject cueObject = new GameObject("SharedCueTest");

            try
            {
                object cue = cueObject.AddComponent(cueType);
                Invoke(cue, "Configure", "Route Portal", Color.cyan, Color.green, Color.gray);
                Invoke(cue, "ApplyState", true, true, true);

                Assert.AreEqual("Route Portal", ReadProperty<string>(cue, "RoleLabel"));
                Assert.True(ReadProperty<bool>(cue, "IsVisible"));
                Assert.True(ReadProperty<bool>(cue, "IsAvailable"));
                Assert.True(ReadProperty<bool>(cue, "IsHighlighted"));
                Assert.True(ReadProperty<bool>(cue, "HasMarkerVisual"));
                Assert.That(ReadProperty<Color>(cue, "CurrentColor"), Is.EqualTo(Color.green));
                Assert.NotNull(cueObject.transform.Find("InteractionCueMarker"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cueObject);
            }
        }

        [Test]
        public void SafeRestInteractableHighlightsWhenRestObjectiveIsActive()
        {
            GameObject runObject = CreateRunManagerObject("RestCueRunManager");
            GameObject restObject = new GameObject("RestCueInteractable");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                SafeRestInteractable rest = restObject.AddComponent<SafeRestInteractable>();
                restObject.AddComponent<SpriteRenderer>();
                rest.Configure(runManager);

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Invoke(rest, "RefreshVisualState");

                object cue = ReadProperty<object>(rest, "VisualCue");
                Assert.AreEqual("Rest Point", ReadProperty<string>(cue, "RoleLabel"));
                Assert.True(ReadProperty<bool>(cue, "IsVisible"));
                Assert.True(ReadProperty<bool>(cue, "IsAvailable"));
                Assert.True(ReadProperty<bool>(cue, "IsHighlighted"));
                Assert.True(ReadProperty<bool>(cue, "HasMarkerVisual"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SupplyShopInteractableHighlightsWhenShopObjectiveIsActive()
        {
            GameObject runObject = CreateRunManagerObject("ShopCueRunManager");
            GameObject shopObject = new GameObject("ShopCueInteractable");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                SupplyShopInteractable shop = shopObject.AddComponent<SupplyShopInteractable>();
                shopObject.AddComponent<SpriteRenderer>();
                shop.Configure(runManager);

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);
                Invoke(shop, "RefreshVisualState");

                object cue = ReadProperty<object>(shop, "VisualCue");
                Assert.AreEqual("Shop", ReadProperty<string>(cue, "RoleLabel"));
                Assert.True(ReadProperty<bool>(cue, "IsVisible"));
                Assert.True(ReadProperty<bool>(cue, "IsAvailable"));
                Assert.True(ReadProperty<bool>(cue, "IsHighlighted"));
                Assert.True(ReadProperty<bool>(cue, "HasMarkerVisual"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shopObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RoutePortalHighlightsWhenChooseRouteObjectiveIsActive()
        {
            GameObject runObject = CreateRunManagerObject("PortalCueRunManager");
            GameObject playerObject = new GameObject("Player");
            GameObject portalObject = new GameObject("PortalCueInteractable");

            try
            {
                playerObject.AddComponent<AICompanionRoguelike.Character.PlayerMovement2D>();
                RunManager runManager = runObject.GetComponent<RunManager>();
                portalObject.AddComponent<BoxCollider2D>();
                portalObject.AddComponent<SpriteRenderer>();
                NextRoomChoicePortal portal = portalObject.AddComponent<NextRoomChoicePortal>();
                WritePrivateField(portal, "runManager", runManager);

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Invoke(runManager, "CloseSafeRestDraft");
                Invoke(
                    portal,
                    "HandleRoomChoicesPrepared",
                    runManager,
                    (IReadOnlyList<RoomType>)runManager.CurrentRoomChoices);

                object cue = ReadProperty<object>(portal, "VisualCue");
                Assert.AreEqual("Route Portal", ReadProperty<string>(cue, "RoleLabel"));
                Assert.True(ReadProperty<bool>(cue, "IsVisible"));
                Assert.True(ReadProperty<bool>(cue, "IsAvailable"));
                Assert.True(ReadProperty<bool>(cue, "IsHighlighted"));
                Assert.True(ReadProperty<bool>(cue, "HasMarkerVisual"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateRunManagerObject(string objectName)
        {
            GameObject runObject = new GameObject(objectName);
            runObject.AddComponent<RoomManager>();
            runObject.AddComponent<RunManager>();
            return runObject;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"{fullName} should exist.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, args.Length);
            Assert.NotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            return method.Invoke(target, args);
        }

        private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, int argumentCount)
        {
            MethodInfo[] methods = type.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == argumentCount)
                {
                    return methods[i];
                }
            }

            return null;
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, $"{target.GetType().Name}.{propertyName} should exist.");
            object value = property.GetValue(target);
            return value is T typed ? typed : default;
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name}.{fieldName} should exist.");
            field.SetValue(target, value);
        }
    }
}
