namespace SeededRun
{
    internal static class NewGameSeedContext
    {
        private static bool _nextArchiveCreatesNewGame;

        internal static bool IsCreatingNewGameSave { get; private set; }

        internal static void MarkNextArchiveAsNewGame()
        {
            _nextArchiveCreatesNewGame = true;
        }

        internal static bool BeginArchive()
        {
            if (!_nextArchiveCreatesNewGame)
                return false;

            _nextArchiveCreatesNewGame = false;
            IsCreatingNewGameSave = true;
            return true;
        }

        internal static void EndArchive()
        {
            IsCreatingNewGameSave = false;
        }
    }
}
