using UnityEditor;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaPlayerParticipationAuthoringRegression
    {
        [MenuItem("Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression")]
        internal static void Run()
        {
            QaP3CPlayerProfileAuthoringSmoke.Run();
            QaP3DActivityParticipationAuthoringSmoke.Run();
        }
    }
}
