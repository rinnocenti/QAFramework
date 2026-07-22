using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using UnityEngine;
namespace ImmersiveFrameworkQA.InputMode.Internal.Editor.ImmersiveFrameworkQA.Player.InternalEditor
{
    internal static class QaInputModeFrameworkRuntimeHostResolver
    {
        internal static bool TryResolveUniqueHost(
            out FrameworkRuntimeHost host)
        {
            host = null;
            FrameworkRuntimeHost[] candidates =
                Resources.FindObjectsOfTypeAll<
                    FrameworkRuntimeHost>();

            var loaded =
                new List<FrameworkRuntimeHost>();
            var seen =
                new HashSet<FrameworkRuntimeHost>();

            for (int index = 0;
                 index < candidates.Length;
                 index++)
            {
                FrameworkRuntimeHost candidate =
                    candidates[index];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded ||
                    !seen.Add(candidate))
                {
                    continue;
                }

                loaded.Add(candidate);
            }

            if (loaded.Count != 1)
            {
                return false;
            }

            host = loaded[0];
            return true;
        }
    }
}
