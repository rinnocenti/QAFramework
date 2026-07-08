using System;
using UnityEngine;

namespace ImmersiveFrameworkQA.Hub
{
    /// <summary>
    /// Obsolete compatibility stub for a previous F49D QA repair attempt.
    /// The real F49D fixture lives at ImmersiveFrameworkQA.Player.QaPlayerEntryPassiveFixture.
    /// This class intentionally contains no smoke logic.
    /// </summary>
    [Obsolete("Use ImmersiveFrameworkQA.Player.QaPlayerEntryPassiveFixture instead. This stub only prevents stale local files from breaking compilation.")]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class QaPlayerEntryPassiveFixture : MonoBehaviour
    {
    }
}
