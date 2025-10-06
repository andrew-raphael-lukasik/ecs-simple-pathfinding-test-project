using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Client.UIToolkit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public abstract class BaseUIController : MonoBehaviour
    {
        static List<UIDocument> _activeDocuments = new ();

        [SerializeField] protected UIDocument _document;
        [SerializeField] protected UIDocumentLocalization _localization;

        public UIDocument document => _document;

        void OnEnable()
        {
            _activeDocuments.Add(_document);
            _localization.onCompleted += Bind;
        }

        void OnDisable()
        {
            _activeDocuments.Remove(_document);
            _localization.onCompleted -= Bind;
        }

        protected abstract void Bind(VisualElement root);

        public static bool IsPointerOverUI(Vector2 screenPos)
        {
            Vector2 pointerUiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            foreach (var baseUIController in _activeDocuments)
            {
                VisualElement picked = baseUIController.rootVisualElement.panel.Pick(pointerUiPos);
                if (picked!=null) return true;
            }
            return false;
        }

    }
}
