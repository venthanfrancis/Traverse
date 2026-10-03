using UnityEngine;

namespace Drift
{
    public sealed class AnchorPresentation : MonoBehaviour
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField]
        private CameraShake shake;
        [SerializeField]
        private GameFlowManager flow;
        private bool restored;
        private void Update()
        {
            if (flow.IsPaused)
                return;
            if (anchor.IsRestored && !restored)
            {
                restored = true;
                shake.Shake(.5f, .04f);
            }
        }
    }
}
