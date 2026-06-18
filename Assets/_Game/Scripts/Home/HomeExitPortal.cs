using AICompanionRoguelike.Character;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Home
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HomeExitPortal : MonoBehaviour
    {
        [SerializeField] private string battleScenePath = "Assets/Scenes/SampleScene.unity";
        [SerializeField] private bool logTransition = true;

        private bool isTransitioning;

        private void Reset()
        {
            Collider2D portalCollider = GetComponent<Collider2D>();
            portalCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isTransitioning || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerMovement2D>() == null)
            {
                return;
            }

            EnterBattle();
        }

        public void EnterBattle()
        {
            if (isTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(battleScenePath))
            {
                Debug.LogWarning("HomeExitPortal cannot load battle scene because battleScenePath is empty.", this);
                return;
            }

            isTransitioning = true;

            if (logTransition)
            {
                Debug.Log($"HomeExitPortal loading battle scene: {battleScenePath}", this);
            }

            SceneManager.LoadScene(battleScenePath, LoadSceneMode.Single);
        }
    }
}
