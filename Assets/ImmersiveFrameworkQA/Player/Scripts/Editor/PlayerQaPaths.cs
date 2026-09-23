namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class PlayerQaPaths
    {
        internal const string Root = "Assets/ImmersiveFrameworkQA/Player";
        internal const string Prefabs = Root + "/Prefabs";
        internal const string Profiles = Root + "/Profiles";
        internal const string Input = Root + "/Input";
        internal const string Activities = Root + "/Activities";
        internal const string Routes = Root + "/Routes";
        internal const string Scenes = Root + "/Scenes";

        internal const string InputActionsPath = Input + "/QA_PlayerInputActions.inputactions";
        internal const string InputActionsName = "QA_PlayerInputActions";

        internal const string DefaultPresentationPath = Prefabs + "/QA_DefaultPresentation.prefab";
        internal const string AlternatePresentationPath = Prefabs + "/QA_AlternatePresentation.prefab";
        internal const string NoGameplayReaderPresentationPath =
            Prefabs + "/QA_NoGameplayReaderPresentation.prefab";
        internal const string AmbiguousGameplayReaderPresentationPath =
            Prefabs + "/QA_AmbiguousGameplayReaderPresentation.prefab";
        internal const string RuntimeHostPath = Prefabs + "/QA_PlayerActorRuntimeHost.prefab";
        internal const string ManagerHostPath = Prefabs + "/QA_ManagerLocalPlayerHost.prefab";
        internal const string SceneHostPath = Prefabs + "/QA_SceneLocalPlayerHost.prefab";

        internal const string DefaultActorPath = Profiles + "/QA_DefaultActor.asset";
        internal const string AlternateActorPath = Profiles + "/QA_AlternateActor.asset";
        internal const string NoGameplayReaderActorPath =
            Profiles + "/QA_NoGameplayReaderActor.asset";
        internal const string AmbiguousGameplayReaderActorPath =
            Profiles + "/QA_AmbiguousGameplayReaderActor.asset";
        internal const string PlayerOneSlotPath = Profiles + "/QA_PlayerSlot_P1.asset";
        internal const string PlayerTwoSlotPath = Profiles + "/QA_PlayerSlot_P2.asset";
        internal const string ManagerSessionPath = Profiles + "/QA_PlayerSession_Manager.asset";
        internal const string SceneSessionPath = Profiles + "/QA_PlayerSession_Scene.asset";
        internal const string ClosedUnresolvedSessionPath =
            Profiles + "/QA_PlayerSession_JoinClosed_LeaveUnresolved.asset";

        internal const string StartupActivityPath = Activities + "/QA_PlayerStartupActivity.asset";
        internal const string RelocateActivityPath = Activities + "/QA_PlayerRelocateActivity.asset";
        internal const string GameplayReadyActivityPath =
            Activities + "/QA_PlayerGameplayReadyActivity.asset";
        internal const string EmptyActivityPath = Activities + "/QA_PlayerEmptyActivity.asset";

        internal const string PrimaryRoutePath = Routes + "/QA_PlayerRoute.asset";
        internal const string SceneProvidedRoutePath = Routes + "/QA_PlayerSceneProvidedRoute.asset";
        internal const string HubRoutePath = "Assets/ImmersiveFrameworkQA/Hub/Routes/QA_HubRoute.asset";

        internal const string PrimaryScenePath = Scenes + "/QA_Player.unity";
        internal const string SceneProvidedScenePath = Scenes + "/QA_PlayerSceneProvided.unity";

        internal const string PlayerOneSlotId = "player.1";
        internal const string PlayerTwoSlotId = "player.2";
        internal const string DefaultActorId = "actor-profile.qa.default";
        internal const string AlternateActorId = "actor-profile.qa.alternate";
        internal const string NoGameplayReaderActorId = "actor-profile.qa.no-gameplay-reader";
        internal const string AmbiguousGameplayReaderActorId =
            "actor-profile.qa.ambiguous-gameplay-reader";
    }
}
