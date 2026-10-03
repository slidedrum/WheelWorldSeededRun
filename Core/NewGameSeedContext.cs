namespace SeededRun
{
    internal static class NewGameSeedContext
    {
        private static bool _nextArchiveCreatesNewGame;

        internal static void MarkNextArchiveAsNewGame()
        {
            _nextArchiveCreatesNewGame = true;
        }

        internal static bool ConsumeNewGameArchiveMarker()
        {
            if (!_nextArchiveCreatesNewGame)
                return false;

            _nextArchiveCreatesNewGame = false;
            return true;
        }
    }
}
