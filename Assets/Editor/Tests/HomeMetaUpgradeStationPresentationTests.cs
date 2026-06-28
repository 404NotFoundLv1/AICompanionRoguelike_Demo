using System.Reflection;
using AICompanionRoguelike.Home;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Tests
{
    public sealed class HomeMetaUpgradeStationPresentationTests
    {
        private string originalScenePath;

        [SetUp]
        public void SetUp()
        {
            originalScenePath = SceneManager.GetActiveScene().path;
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(originalScenePath)
                && SceneManager.GetActiveScene().path != originalScenePath)
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                return;
            }

            if (string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void PromptRectIsClampedInsideSmallGameView()
        {
            MethodInfo method = typeof(HomeMetaUpgradeStation).GetMethod(
                "GetClampedPromptRect",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "HomeMetaUpgradeStation should expose a reusable prompt rect clamp.");

            Rect clamped = (Rect)method.Invoke(
                null,
                new object[] { new Rect(16f, 300f, 440f, 170f), 512f, 360f });

            Assert.That(clamped.xMin, Is.GreaterThanOrEqualTo(8f));
            Assert.That(clamped.yMin, Is.GreaterThanOrEqualTo(8f));
            Assert.That(clamped.xMax, Is.LessThanOrEqualTo(504f));
            Assert.That(clamped.yMax, Is.LessThanOrEqualTo(352f));
        }

        [Test]
        public void HomeUpgradeStationIsProminentAndNearPlayer()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/HomeScene.unity", OpenSceneMode.Single);

            GameObject station = GameObject.Find("HomeMetaUpgradeStation");
            GameObject player = GameObject.Find("Player");

            Assert.NotNull(station, "HomeScene should contain the home meta upgrade station.");
            Assert.NotNull(player, "HomeScene should contain Player.");
            Assert.That(station.transform.position.y, Is.GreaterThan(-2f));
            Assert.That(station.transform.localScale.y, Is.GreaterThanOrEqualTo(2.2f));
            Assert.That(
                Mathf.Abs(station.transform.position.x - player.transform.position.x),
                Is.LessThanOrEqualTo(2.5f));
        }
    }
}
