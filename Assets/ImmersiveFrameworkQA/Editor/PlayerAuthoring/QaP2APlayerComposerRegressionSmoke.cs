using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerAuthoring;
using Immersive.Framework.PlayerAuthoring;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PlayerAuthoring.Editor
{
    /// <summary>
    /// P2A-QA0 regression smoke for the public PlayerComposer Editor product surface.
    /// It uses transient Preview Scene objects, explicit references and public APIs only.
    /// P2B regression cases verify the corrected PlayerComposer authoring behavior.
    /// </summary>
    public static class QaP2APlayerComposerRegressionSmoke
    {
        private const string MenuPath = "Immersive Framework/QA/Player/P2A-QA0 PlayerComposer Regression Smoke";
        private const string PlayerMap = "Player";
        private const string OtherMap = "Other";
        private const string ExpectedActorId = "qa.p2a.player.actor";
        private const string ExpectedSlotId = "qa.p2a.player.1";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            RunForRegression();
        }

        public static bool RunForRegression()
        {
            var results = new List<SmokeCaseResult>();
            var transientAssets = new List<UnityEngine.Object>();
            Scene previewScene = default;

            try
            {
                previewScene = EditorSceneManager.NewPreviewScene();
                RunValidBaseline(previewScene, transientAssets, results);
                RunRecipeDefaults(previewScene, transientAssets, results);
                RunRequiredReferenceNegatives(previewScene, transientAssets, results);
                RunRenameIdentityCase(previewScene, transientAssets, results);
                RunSourceSlotRepairCase(previewScene, transientAssets, results);
                RunDuplicateOwnerBlockingCase(previewScene, transientAssets, results);
            }
            catch (Exception exception)
            {
                results.Add(SmokeCaseResult.BaselineFailure(
                    "UnexpectedException",
                    $"{exception.GetType().Name}: {exception.Message}"));
            }
            finally
            {
                for (int i = transientAssets.Count - 1; i >= 0; i--)
                {
                    if (transientAssets[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(transientAssets[i]);
                    }
                }

                if (previewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(previewScene);
                }
            }

            return Report(results);
        }

        private static void RunValidBaseline(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture fixture = CreateFixture(
                scene,
                transientAssets,
                "ValidBaseline",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: PlayerMap);

            PlayerComposerApplyRebuildResult validation =
                PlayerComposerApplyRebuildUtility.Validate(fixture.Composer, false);
            results.Add(Baseline(
                "ValidValidate",
                validation.Succeeded
                && validation.Status == "ValidationSucceeded"
                && validation.BlockedCount == 0,
                ResultDetail(validation)));

            PlayerInput explicitInput = fixture.PlayerInput;
            Transform explicitCameraTarget = fixture.CameraTarget;
            Transform explicitLookAtTarget = fixture.LookAtTarget;

            PlayerComposerApplyRebuildResult first =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            results.Add(Baseline(
                "FirstApplyRebuild",
                first.Succeeded && first.BlockedCount == 0 && first.CreatedCount > 0,
                ResultDetail(first)));

            results.Add(Baseline(
                "ConcreteReferencesPreserved",
                fixture.Composer.PlayerInput == explicitInput
                && fixture.Composer.CameraTarget == explicitCameraTarget
                && fixture.Composer.LookAtTarget == explicitLookAtTarget,
                $"playerInputSame='{fixture.Composer.PlayerInput == explicitInput}' " +
                $"cameraTargetSame='{fixture.Composer.CameraTarget == explicitCameraTarget}' " +
                $"lookAtTargetSame='{fixture.Composer.LookAtTarget == explicitLookAtTarget}'"));

            PlayerActorDeclaration actor = fixture.Root.GetComponent<PlayerActorDeclaration>();
            PlayerSlotDeclaration slot = fixture.Root.GetComponent<PlayerSlotDeclaration>();
            results.Add(Baseline(
                "DeclarationsMaterialized",
                actor != null
                && slot != null
                && actor.ActorId.Value.Value == ExpectedActorId
                && slot.PlayerSlotId.Value.Value == ExpectedSlotId
                && actor.PlayerInput == explicitInput
                && slot.PlayerInputEvidence == explicitInput,
                $"actor='{Format(actor != null ? actor.ActorId.Value.Value : null)}' " +
                $"slot='{Format(slot != null ? slot.PlayerSlotId.Value.Value : null)}'"));

            UnityPlayerInputGateAdapter gate = fixture.Root.GetComponent<UnityPlayerInputGateAdapter>();
            results.Add(Baseline(
                "GateAdapterMaterialized",
                gate != null
                && gate.PlayerInput == explicitInput
                && gate.GameplayActionMapName == PlayerMap,
                $"gate='{FormatObject(gate)}' playerInputSame='{(gate != null && gate.PlayerInput == explicitInput)}'"));

            Transform bindingsRoot = fixture.Composer.FrameworkBindingsRoot;
            bool bindingsShape = bindingsRoot != null
                && bindingsRoot.name == "_Bindings"
                && bindingsRoot.parent != null
                && bindingsRoot.parent.name == "_Framework";
            results.Add(Baseline(
                "CanonicalBindingsRootMaterialized",
                bindingsShape,
                $"root='{FormatObject(bindingsRoot)}' parent='{FormatObject(bindingsRoot != null ? bindingsRoot.parent : null)}'"));

            PlayerControlBindingTargetBehaviour controlTarget =
                bindingsRoot != null ? bindingsRoot.GetComponent<PlayerControlBindingTargetBehaviour>() : null;
            UnityPlayerInputBridgeTargetBehaviour bridgeTarget =
                bindingsRoot != null ? bindingsRoot.GetComponent<UnityPlayerInputBridgeTargetBehaviour>() : null;
            UnityPlayerInputActivationTargetBehaviour activationTarget =
                bindingsRoot != null ? bindingsRoot.GetComponent<UnityPlayerInputActivationTargetBehaviour>() : null;
            results.Add(Baseline(
                "F52TargetsMaterialized",
                controlTarget != null
                && bridgeTarget != null
                && activationTarget != null
                && bridgeTarget.HasUnityPlayerInput
                && activationTarget.HasUnityPlayerInput
                && activationTarget.ConfiguredActionMapName == PlayerMap,
                $"control='{FormatObject(controlTarget)}' bridge='{FormatObject(bridgeTarget)}' " +
                $"activation='{FormatObject(activationTarget)}'"));

            PlayerComposerApplyRebuildResult second =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            results.Add(Baseline(
                "SecondApplyIsIdempotent",
                second.Succeeded
                && second.CreatedCount == 0
                && second.RepairedCount == 0
                && second.BlockedCount == 0
                && CountCanonicalTargets(fixture) == 3,
                $"{ResultDetail(second)} canonicalTargets='{CountCanonicalTargets(fixture)}'"));

            PlayerComposerApplyRebuildResult third =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            bool deterministic = SameDiagnostics(second, third)
                && string.Equals(
                    fixture.Composer.LastMaterializationSummary,
                    third.Summary,
                    StringComparison.Ordinal);
            results.Add(Baseline(
                "DeterministicDiagnostics",
                deterministic,
                $"second='{ResultDetail(second)}' third='{ResultDetail(third)}' " +
                $"summarySame='{string.Equals(second.Summary, third.Summary, StringComparison.Ordinal)}'"));
        }

        private static void RunRecipeDefaults(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture fixture = CreateFixture(
                scene,
                transientAssets,
                "RecipeDefaults",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: "RecipeGameplay",
                configureComposer: false);

            PlayerRecipe recipe = ScriptableObject.CreateInstance<PlayerRecipe>();
            recipe.name = "QA_P2A_PlayerRecipe";
            transientAssets.Add(recipe);
            ConfigureRecipe(recipe, "qa.recipe.actor", "qa.recipe.slot", "RecipeGameplay");

            ConfigureComposer(
                fixture.Composer,
                recipe,
                "local.actor.before-recipe",
                "local.slot.before-recipe",
                fixture.PlayerInput,
                "LocalBeforeRecipe",
                fixture.CameraTarget,
                fixture.LookAtTarget);

            PlayerInput explicitInput = fixture.PlayerInput;
            Transform explicitCameraTarget = fixture.CameraTarget;
            Transform explicitLookAtTarget = fixture.LookAtTarget;

            bool applied = fixture.Composer.EditorApplyRecipeDefaults(
                overwriteExisting: true,
                out string issue);
            bool defaultsApplied = applied
                && string.IsNullOrEmpty(issue)
                && fixture.Composer.ActorId == "qa.recipe.actor"
                && fixture.Composer.PlayerSlotId == "qa.recipe.slot"
                && fixture.Composer.GameplayActionMap == "RecipeGameplay";
            results.Add(Baseline(
                "RecipeDefaultsApplied",
                defaultsApplied,
                $"applied='{applied}' issue='{Format(issue)}' actor='{fixture.Composer.ActorId}' " +
                $"slot='{fixture.Composer.PlayerSlotId}' map='{fixture.Composer.GameplayActionMap}'"));

            bool concreteReferencesPreserved = fixture.Composer.PlayerInput == explicitInput
                && fixture.Composer.CameraTarget == explicitCameraTarget
                && fixture.Composer.LookAtTarget == explicitLookAtTarget;
            results.Add(Baseline(
                "RecipePreservesConcreteReferences",
                concreteReferencesPreserved,
                $"playerInputSame='{fixture.Composer.PlayerInput == explicitInput}' " +
                $"cameraTargetSame='{fixture.Composer.CameraTarget == explicitCameraTarget}' " +
                $"lookAtTargetSame='{fixture.Composer.LookAtTarget == explicitLookAtTarget}'"));

            PlayerComposerApplyRebuildResult validation =
                PlayerComposerApplyRebuildUtility.Validate(fixture.Composer, false);
            results.Add(Baseline(
                "RecipeConfiguredComposerValidates",
                validation.Succeeded,
                ResultDetail(validation)));
        }

        private static void RunRequiredReferenceNegatives(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture noInput = CreateFixture(
                scene,
                transientAssets,
                "MissingPlayerInput",
                includePlayerInput: false,
                includeActionAsset: false,
                actionMapName: PlayerMap);
            PlayerComposerApplyRebuildResult missingInput =
                PlayerComposerApplyRebuildUtility.Validate(noInput.Composer, false);
            results.Add(Baseline(
                "MissingPlayerInputBlocks",
                missingInput.Failed
                && missingInput.BlockedCount == 1
                && Contains(missingInput.Issue, "PlayerInput"),
                ResultDetail(missingInput)));

            ComposerFixture noActions = CreateFixture(
                scene,
                transientAssets,
                "MissingActionAsset",
                includePlayerInput: true,
                includeActionAsset: false,
                actionMapName: PlayerMap);

            if (noActions.PlayerInput == null || noActions.PlayerInput.actions != null)
            {
                results.Add(SmokeCaseResult.BaselineFailure(
                    "MissingActionAssetFixtureInvalid",
                    $"playerInput='{FormatObject(noActions.PlayerInput)}' " +
                    $"actions='{FormatObject(noActions.PlayerInput != null ? noActions.PlayerInput.actions : null)}'"));
                return;
            }

            PlayerComposerApplyRebuildResult missingActionAsset =
                PlayerComposerApplyRebuildUtility.Validate(noActions.Composer, false);
            results.Add(Baseline(
                "MissingActionAssetBlocks",
                missingActionAsset.Failed
                && string.Equals(
                    missingActionAsset.Status,
                    "ValidationFailed",
                    StringComparison.Ordinal)
                && missingActionAsset.BlockedCount == 1
                && Contains(missingActionAsset.Issue, "InputActionAsset"),
                ResultDetail(missingActionAsset)));

            ComposerFixture missingMap = CreateFixture(
                scene,
                transientAssets,
                "MissingActionMap",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: PlayerMap,
                createdActionMapName: OtherMap);
            PlayerComposerApplyRebuildResult missingActionMap =
                PlayerComposerApplyRebuildUtility.Validate(missingMap.Composer, false);
            results.Add(Baseline(
                "MissingActionMapBlocks",
                missingActionMap.Failed
                && missingActionMap.BlockedCount == 1
                && Contains(missingActionMap.Issue, PlayerMap),
                ResultDetail(missingActionMap)));
        }

        private static void RunRenameIdentityCase(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture fixture = CreateFixture(
                scene,
                transientAssets,
                "RenameIdentity",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: PlayerMap);

            PlayerComposerApplyRebuildResult first =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            PlayerActorDeclaration actor = fixture.Root.GetComponent<PlayerActorDeclaration>();
            PlayerSlotDeclaration slot = fixture.Root.GetComponent<PlayerSlotDeclaration>();

            fixture.Root.name = "QA_Renamed_Player_Without_Identity_Change";
            PlayerComposerApplyRebuildResult validation =
                PlayerComposerApplyRebuildUtility.Validate(fixture.Composer, false);
            PlayerComposerApplyRebuildResult afterRename =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);

            bool passed = first.Succeeded
                && validation.Succeeded
                && afterRename.Succeeded
                && afterRename.CreatedCount == 0
                && actor != null
                && slot != null
                && actor.ActorId.Value.Value == ExpectedActorId
                && slot.PlayerSlotId.Value.Value == ExpectedSlotId
                && fixture.Composer.PlayerInput == fixture.PlayerInput;
            results.Add(Baseline(
                "GameObjectRenameDoesNotChangeIdentity",
                passed,
                $"actor='{Format(actor != null ? actor.ActorId.Value.Value : null)}' " +
                $"slot='{Format(slot != null ? slot.PlayerSlotId.Value.Value : null)}' " +
                $"createdAfterRename='{afterRename.CreatedCount}'"));
        }

        private static void RunSourceSlotRepairCase(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture fixture = CreateFixture(
                scene,
                transientAssets,
                "SourceSlotRepair",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: PlayerMap);

            UnityPlayerInputGateAdapter gate = fixture.Root.AddComponent<UnityPlayerInputGateAdapter>();
            SetObject(gate, "playerInput", fixture.PlayerInput);
            SetObject(gate, "sourceSlot", null);

            PlayerComposerApplyRebuildResult apply =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            PlayerSlotDeclaration slot = fixture.Root.GetComponent<PlayerSlotDeclaration>();
            bool repaired = apply.Succeeded
                && apply.BlockedCount == 0
                && slot != null
                && gate.SourceSlot == slot;
            results.Add(Baseline(
                "GateSourceSlotRepaired",
                repaired,
                $"{ResultDetail(apply)} sourceSlot='{FormatObject(gate.SourceSlot)}'"));
        }

        private static void RunDuplicateOwnerBlockingCase(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            List<SmokeCaseResult> results)
        {
            ComposerFixture fixture = CreateFixture(
                scene,
                transientAssets,
                "DuplicateOwnersBlocked",
                includePlayerInput: true,
                includeActionAsset: true,
                actionMapName: PlayerMap);

            var externalOwner = new GameObject("QA_P2A_ExternalDuplicateOwners");
            externalOwner.transform.SetParent(fixture.Root.transform, false);
            PlayerActorDeclaration externalActor =
                externalOwner.AddComponent<PlayerActorDeclaration>();
            PlayerSlotDeclaration externalSlot =
                externalOwner.AddComponent<PlayerSlotDeclaration>();
            ConfigureExternalOwners(externalActor, externalSlot, fixture.PlayerInput);

            PlayerControlBindingTargetBehaviour externalControl =
                fixture.Root.AddComponent<PlayerControlBindingTargetBehaviour>();
            UnityPlayerInputBridgeTargetBehaviour externalBridge =
                fixture.Root.AddComponent<UnityPlayerInputBridgeTargetBehaviour>();
            UnityPlayerInputActivationTargetBehaviour externalActivation =
                fixture.Root.AddComponent<UnityPlayerInputActivationTargetBehaviour>();
            ConfigureExternalDuplicates(
                externalControl,
                externalBridge,
                externalActivation,
                fixture.PlayerInput);

            PlayerComposerApplyRebuildResult apply =
                PlayerComposerApplyRebuildUtility.ApplyOrRebuild(fixture.Composer, false, false);
            Transform canonicalRoot = fixture.Composer.FrameworkBindingsRoot;
            bool canonicalCreated = canonicalRoot != null
                && canonicalRoot.GetComponent<PlayerControlBindingTargetBehaviour>() != null
                && canonicalRoot.GetComponent<UnityPlayerInputBridgeTargetBehaviour>() != null
                && canonicalRoot.GetComponent<UnityPlayerInputActivationTargetBehaviour>() != null;
            bool externalRetained = externalControl != null
                && externalBridge != null
                && externalActivation != null;
            bool duplicateOwnersRetained = externalActor != null
                && externalSlot != null
                && fixture.Root.GetComponentsInChildren<PlayerActorDeclaration>(true).Length == 2
                && fixture.Root.GetComponentsInChildren<PlayerSlotDeclaration>(true).Length == 2;
            bool blocked = apply.Failed
                && apply.BlockedCount == 1
                && !canonicalCreated
                && externalRetained
                && !duplicateOwnersRetained
                && CountAllTargets(fixture.Root) == 3
                && Contains(apply.Issue, "duplicate or non-canonical");

            results.Add(Baseline(
                "ExternalDuplicatesBlocked",
                blocked,
                $"{ResultDetail(apply)} allTargets='{CountAllTargets(fixture.Root)}' " +
                $"canonicalCreated='{canonicalCreated}' externalRetained='{externalRetained}' " +
                $"duplicateOwnersRetained='{duplicateOwnersRetained}'"));
        }

        private static ComposerFixture CreateFixture(
            Scene scene,
            List<UnityEngine.Object> transientAssets,
            string suffix,
            bool includePlayerInput,
            bool includeActionAsset,
            string actionMapName,
            string createdActionMapName = null,
            bool configureComposer = true)
        {
            var root = new GameObject($"QA_P2A_{suffix}");
            SceneManager.MoveGameObjectToScene(root, scene);

            PlayerInput input = includePlayerInput ? root.AddComponent<PlayerInput>() : null;
            InputActionAsset actions = null;
            if (includeActionAsset)
            {
                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                actions.name = $"QA_P2A_{suffix}_Actions";
                string mapName = string.IsNullOrWhiteSpace(createdActionMapName)
                    ? actionMapName
                    : createdActionMapName;
                InputActionMap map = actions.AddActionMap(mapName);
                map.AddAction("Move", InputActionType.Value);
                transientAssets.Add(actions);
                input.actions = actions;
                input.defaultActionMap = mapName;
            }
            else if (input != null)
            {
                input.actions = null;
            }

            var cameraTargetObject = new GameObject($"QA_P2A_{suffix}_CameraTarget");
            cameraTargetObject.transform.SetParent(root.transform, false);
            var lookAtTargetObject = new GameObject($"QA_P2A_{suffix}_LookAtTarget");
            lookAtTargetObject.transform.SetParent(root.transform, false);

            PlayerComposer composer = root.AddComponent<PlayerComposer>();
            if (configureComposer)
            {
                ConfigureComposer(
                    composer,
                    null,
                    ExpectedActorId,
                    ExpectedSlotId,
                    input,
                    actionMapName,
                    cameraTargetObject.transform,
                    lookAtTargetObject.transform);
            }

            return new ComposerFixture(
                root,
                composer,
                input,
                actions,
                cameraTargetObject.transform,
                lookAtTargetObject.transform);
        }

        private static void ConfigureComposer(
            PlayerComposer composer,
            PlayerRecipe recipe,
            string actorId,
            string slotId,
            PlayerInput input,
            string actionMap,
            Transform cameraTarget,
            Transform lookAtTarget)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetObject(serialized, "recipe", recipe);
            SetString(serialized, "actorId", actorId);
            SetString(serialized, "playerSlotId", slotId);
            SetObject(serialized, "playerInput", input);
            SetString(serialized, "gameplayActionMap", actionMap);
            SetObject(serialized, "cameraTarget", cameraTarget);
            SetObject(serialized, "lookAtTarget", lookAtTarget);
            SetBool(serialized, "inputBindingRequired", true);
            SetBool(serialized, "cameraBindingRequired", true);
            SetBool(serialized, "createBindingsRootIfMissing", true);
            SetBool(serialized, "createAnchorsIfMissing", true);
            SetBool(serialized, "materializeSlotOccupancy", true);
            SetBool(serialized, "materializePassiveEntryViewControl", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRecipe(
            PlayerRecipe recipe,
            string actorId,
            string slotId,
            string actionMap)
        {
            var serialized = new SerializedObject(recipe);
            serialized.Update();
            SetString(serialized, "actorId", actorId);
            SetString(serialized, "playerSlotId", slotId);
            SetString(serialized, "gameplayActionMap", actionMap);
            SetBool(serialized, "inputBindingRequired", true);
            SetBool(serialized, "cameraBindingRequired", true);
            SetBool(serialized, "createBindingsRootIfMissing", true);
            SetBool(serialized, "createAnchorsIfMissing", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureExternalDuplicates(
            PlayerControlBindingTargetBehaviour control,
            UnityPlayerInputBridgeTargetBehaviour bridge,
            UnityPlayerInputActivationTargetBehaviour activation,
            PlayerInput input)
        {
            SetString(control, "bindingTargetName", "External Duplicate Control Target");
            SetString(bridge, "bridgeTargetName", "External Duplicate Bridge Target");
            SetString(bridge, "expectedPlayerSlotId", "external.duplicate.slot");
            SetObject(bridge, "playerInput", input);
            SetString(activation, "activationTargetName", "External Duplicate Activation Target");
            SetString(activation, "expectedPlayerSlotId", "external.duplicate.slot");
            SetObject(activation, "playerInput", input);
            SetString(activation, "actionMapName", OtherMap);
        }

        private static void ConfigureExternalOwners(
            PlayerActorDeclaration actor,
            PlayerSlotDeclaration slot,
            PlayerInput input)
        {
            SetString(actor, "actorId", ExpectedActorId);
            SetObject(actor, "playerInput", input);
            SetString(slot, "slotId", ExpectedSlotId);
            SetObject(slot, "playerInput", input);
        }

        private static int CountCanonicalTargets(ComposerFixture fixture)
        {
            Transform root = fixture.Composer.FrameworkBindingsRoot;
            if (root == null)
            {
                return 0;
            }

            int count = 0;
            count += root.GetComponents<PlayerControlBindingTargetBehaviour>().Length;
            count += root.GetComponents<UnityPlayerInputBridgeTargetBehaviour>().Length;
            count += root.GetComponents<UnityPlayerInputActivationTargetBehaviour>().Length;
            return count;
        }

        private static int CountAllTargets(GameObject root)
        {
            int count = 0;
            count += root.GetComponentsInChildren<PlayerControlBindingTargetBehaviour>(true).Length;
            count += root.GetComponentsInChildren<UnityPlayerInputBridgeTargetBehaviour>(true).Length;
            count += root.GetComponentsInChildren<UnityPlayerInputActivationTargetBehaviour>(true).Length;
            return count;
        }

        private static bool SameDiagnostics(
            PlayerComposerApplyRebuildResult first,
            PlayerComposerApplyRebuildResult second)
        {
            return first.Succeeded == second.Succeeded
                && string.Equals(first.Status, second.Status, StringComparison.Ordinal)
                && string.Equals(first.Issue, second.Issue, StringComparison.Ordinal)
                && string.Equals(first.Summary, second.Summary, StringComparison.Ordinal)
                && first.CreatedCount == second.CreatedCount
                && first.RepairedCount == second.RepairedCount
                && first.AlreadyValidCount == second.AlreadyValidCount
                && first.SkippedByPolicyCount == second.SkippedByPolicyCount
                && first.BlockedCount == second.BlockedCount;
        }

        private static bool Report(IReadOnlyList<SmokeCaseResult> results)
        {
            int passed = 0;
            int failed = 0;

            for (int i = 0; i < results.Count; i++)
            {
                SmokeCaseResult result = results[i];
                if (result.Passed)
                {
                    passed++;
                    Debug.Log(
                        $"[P2A_QA0_PLAYER_COMPOSER] case='{result.Name}' status='Passed' detail='{result.Detail}'");
                }
                else
                {
                    failed++;
                    Debug.LogError(
                        $"[P2A_QA0_PLAYER_COMPOSER] case='{result.Name}' status='Failed' detail='{result.Detail}'");
                }
            }

            bool succeeded = failed == 0;
            string status = succeeded ? "Succeeded" : "Failed";
            string summary =
                $"[P2A_QA0_PLAYER_COMPOSER] status='{status}' passed='{passed}' " +
                $"failed='{failed}' cases='{results.Count}'.";

            if (succeeded)
            {
                Debug.Log(summary);
            }
            else
            {
                Debug.LogError(summary);
            }

            return succeeded;
        }

        private static SmokeCaseResult Baseline(string name, bool passed, string detail)
        {
            return passed
                ? SmokeCaseResult.BaselinePass(name, detail)
                : SmokeCaseResult.BaselineFailure(name, detail);
        }

        private static string ResultDetail(PlayerComposerApplyRebuildResult result)
        {
            return $"succeeded='{result.Succeeded}' status='{Format(result.Status)}' " +
                $"created='{result.CreatedCount}' repaired='{result.RepairedCount}' " +
                $"alreadyValid='{result.AlreadyValidCount}' skipped='{result.SkippedByPolicyCount}' " +
                $"blocked='{result.BlockedCount}' issue='{Format(result.Issue)}'";
        }

        private static bool Contains(string value, string expected)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(expected, StringComparison.Ordinal) >= 0;
        }

        private static string Format(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value;
        }

        private static string FormatObject(UnityEngine.Object value)
        {
            return value != null ? value.name : "<none>";
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SetString(serialized, propertyName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SetObject(serialized, propertyName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
            }
        }

        private sealed class ComposerFixture
        {
            public ComposerFixture(
                GameObject root,
                PlayerComposer composer,
                PlayerInput playerInput,
                InputActionAsset actions,
                Transform cameraTarget,
                Transform lookAtTarget)
            {
                Root = root;
                Composer = composer;
                PlayerInput = playerInput;
                Actions = actions;
                CameraTarget = cameraTarget;
                LookAtTarget = lookAtTarget;
            }

            public GameObject Root { get; }
            public PlayerComposer Composer { get; }
            public PlayerInput PlayerInput { get; }
            public InputActionAsset Actions { get; }
            public Transform CameraTarget { get; }
            public Transform LookAtTarget { get; }
        }

        private readonly struct SmokeCaseResult
        {
            private SmokeCaseResult(string name, bool passed, string detail)
            {
                Name = name ?? string.Empty;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            public string Name { get; }
            public bool Passed { get; }
            public string Detail { get; }

            public static SmokeCaseResult BaselinePass(string name, string detail)
            {
                return new SmokeCaseResult(name, true, detail);
            }

            public static SmokeCaseResult BaselineFailure(string name, string detail)
            {
                return new SmokeCaseResult(name, false, detail);
            }
        }
    }
}
