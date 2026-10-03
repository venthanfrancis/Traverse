using UnityEngine;

namespace Drift
{
    // Stable identity for references between the three additive scenes.
    [DisallowMultipleComponent]
    public sealed class SceneLinkId : MonoBehaviour
    {
        [HideInInspector]
        public string Id;
    }
}
