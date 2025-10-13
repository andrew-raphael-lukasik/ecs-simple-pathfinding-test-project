using UnityEngine;

namespace Client.Animation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Unit Animation Events Receiver")]
    public class UnitAnimationEventsReceiver : MonoBehaviour
    {
        public void OnFootstep()
        {
            // play footstep sound
        }
    }
}
