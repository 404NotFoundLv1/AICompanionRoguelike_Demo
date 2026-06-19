using System;
using System.IO;
using AICompanionRoguelike.Memory;

namespace AICompanionRoguelike.Save
{
    public sealed class RelationshipAutosaveSession : IDisposable
    {
        private readonly GameSaveService service;
        private bool isStarted;
        private bool isDisposed;

        public RelationshipAutosaveSession(GameSaveService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public event Action<GameSaveLoadResult> Loaded;
        public event Action Saved;
        public event Action<string> SaveFailed;

        public GameSaveLoadResult LastLoadResult { get; private set; }

        public GameSaveLoadResult Start()
        {
            ThrowIfDisposed();
            if (isStarted)
            {
                return LastLoadResult;
            }

            LastLoadResult = service.LoadIntoSession();
            CompanionRelationshipState.StateChanged += HandleRelationshipStateChanged;
            isStarted = true;
            Loaded?.Invoke(LastLoadResult);
            return LastLoadResult;
        }

        public bool SaveNow()
        {
            ThrowIfDisposed();

            try
            {
                bool saved = service.SaveSession();
                if (saved)
                {
                    Saved?.Invoke();
                }

                return saved;
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is NotSupportedException)
            {
                SaveFailed?.Invoke(exception.Message);
                return false;
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (isStarted)
            {
                CompanionRelationshipState.StateChanged -= HandleRelationshipStateChanged;
            }

            isStarted = false;
            isDisposed = true;
        }

        private void HandleRelationshipStateChanged()
        {
            SaveNow();
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(RelationshipAutosaveSession));
            }
        }
    }
}
