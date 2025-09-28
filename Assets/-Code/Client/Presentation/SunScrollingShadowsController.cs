using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Client.Presentation
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class SunScrollingShadowsController : MonoBehaviour
    {
        [SerializeField] Vector2 _step = new Vector2(1,1);
        UniversalAdditionalLightData _lightData;

        void OnEnable()
        {
            _lightData = GetComponent<UniversalAdditionalLightData>();
        }

        void Update()
        {
            _lightData.lightCookieOffset = _step * Time.time;
        }
    }
}
