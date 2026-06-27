using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Deferred. Early ContentFlow marker kept only for historical compatibility during F8.
    /// Runtime materialization adapters must use Immersive.Framework.RuntimeContent.IRuntimeMaterializationAdapter.
    /// </summary>
    [Obsolete("Use Immersive.Framework.RuntimeContent.IRuntimeMaterializationAdapter for F8 runtime materialization boundaries. This marker is not the canonical materialization contract.", false)]
    [FrameworkApiStatus(FrameworkApiStatus.Deferred, "Superseded by RuntimeContent.IRuntimeMaterializationAdapter in F8I; do not use for new materialization code.")]
    public interface IFrameworkContentMaterializer
    {
        FrameworkContentScope Scope { get; }

        FrameworkContentKind Kind { get; }
    }
}
