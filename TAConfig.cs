using ColossalFramework;

namespace TreeAnarchy
{
    internal static class TAConfig
    {
        internal const float MaxScaleFactor = 6.0f;
        internal const float MinScaleFactor = 1.5f;
        internal const int DefaultTreeLimit = 262144;
        internal const int DefaultTreeUpdateCount = 4096;

        internal static bool isInGame = false;
        internal static bool PreviousSaveWasOldFormat = false;
        private static float m_ScaleFactor = 4f;
        internal static bool TreeEffectOnWind = true;
        internal static bool UseTreeSnapping = true;
        internal static bool DebugMode = false;
        internal static int LastMaxTreeLimit = DefaultTreeLimit;

        internal static int MaxTreeLimit => (int)(DefaultTreeLimit * TreeScaleFactor);
        internal static float TreeScaleFactor
        {
            get => m_ScaleFactor;
            set
            {
                {
                    LastTreeScaleFactor = m_ScaleFactor;
                    m_ScaleFactor = value;
                }
            }
        }
        internal static float LastTreeScaleFactor { get; private set; } = m_ScaleFactor;
        internal static int MaxTreeUpdateLimit => (int)(DefaultTreeUpdateCount * TreeScaleFactor);
        internal static bool UseModifiedTreeCap
        {
            get
            {
                switch(Singleton<SimulationManager>.instance.m_metaData.m_updateMode)
                {
                    case SimulationManager.UpdateMode.LoadGame:
                    case SimulationManager.UpdateMode.LoadMap:
                    case SimulationManager.UpdateMode.NewGameFromMap:
                    case SimulationManager.UpdateMode.NewGameFromScenario:
                    case SimulationManager.UpdateMode.NewMap:
                    case SimulationManager.UpdateMode.LoadScenario:
                    case SimulationManager.UpdateMode.NewScenarioFromGame:
                    case SimulationManager.UpdateMode.NewScenarioFromMap:
                    case SimulationManager.UpdateMode.UpdateScenarioFromGame:
                    case SimulationManager.UpdateMode.UpdateScenarioFromMap:
                        return true;
                }
                return false;
            }
        }
    }
}
