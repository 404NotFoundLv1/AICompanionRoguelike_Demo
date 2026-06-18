using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Roguelike;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Home
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class ReturnHomeOnDeath : MonoBehaviour
    {
        [SerializeField] private string homeScenePath = "Assets/_Game/Scenes/HomeScene.unity";
        [SerializeField] private bool logTransition = true;

        private HealthComponent health;
        private bool isReturningHome;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            health.Died += HandleDeath;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void HandleDeath(HealthComponent deadHealth, DamageInfo damageInfo)
        {
            if (isReturningHome)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(homeScenePath))
            {
                Debug.LogWarning("ReturnHomeOnDeath cannot load home scene because homeScenePath is empty.", this);
                return;
            }

            isReturningHome = true;
            RunSessionState.EndRun(RunEndReason.PlayerDeath);

            if (logTransition)
            {
                Debug.Log($"Player died. Returning to home scene: {homeScenePath}", this);
            }

            SceneManager.LoadScene(homeScenePath, LoadSceneMode.Single);
        }
    }
}
