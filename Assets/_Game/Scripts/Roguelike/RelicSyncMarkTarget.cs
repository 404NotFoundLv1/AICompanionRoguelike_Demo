using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class RelicSyncMarkTarget : MonoBehaviour
    {
        [SerializeField] private bool isMarked;

        public bool IsMarked => isMarked;

        public void MarkByCompanion()
        {
            isMarked = true;
        }

        public void ClearMark()
        {
            isMarked = false;
        }
    }
}
