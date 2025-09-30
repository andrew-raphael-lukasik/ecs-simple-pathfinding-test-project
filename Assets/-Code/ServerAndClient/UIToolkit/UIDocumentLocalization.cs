/// void* src = https://gist.github.com/andrew-raphael-lukasik/72a4d3d14dd547a1d61ae9dc4c4513da
///
/// Copyright (C) 2022 Andrzej Rafał Łukasik (also known as: Andrew Raphael Lukasik)
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU General Public License as published by
/// the Free Software Foundation, version 3 of the License.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
/// See the GNU General Public License for details https://www.gnu.org/licenses/
///
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

// NOTE: this class assumes that you designate StringTable keys in label fields (as seen in Label, Button, etc)
// and start them all with '#' char (so other labels will be left be)
// example: https://i.imgur.com/H5RUIej.gif

[HelpURL("https://gist.github.com/andrew-raphael-lukasik/72a4d3d14dd547a1d61ae9dc4c4513da")]
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public class UIDocumentLocalization : MonoBehaviour
{

    [SerializeField] LocalizedStringTable _table = null;
    UIDocument _uiDocument;

    /// <summary> Executed after hierarchy is cloned fresh and translated. </summary>
    public event System.Action<VisualElement> onCompleted = ( VisualElement root ) =>
    {
#if DEBUG
        Debug.Log($"{nameof(UIDocumentLocalization)}: {nameof(UIDocument)} translated");
#endif
    };


    void OnEnable ()
    {
        if( _uiDocument == null )
            _uiDocument = GetComponent<UIDocument>();
        _table.TableChanged += OnTableChanged;
    }

    void OnDisable ()
    {
        _table.TableChanged -= OnTableChanged;
    }


    void OnTableChanged ( StringTable table )
    {
        _uiDocument.rootVisualElement.Clear();
        _uiDocument.visualTreeAsset.CloneTree(_uiDocument.rootVisualElement);

#if DEBUG
        Debug.Log($"{nameof(UIDocumentLocalization)}: {nameof(StringTable)} changed, {nameof(VisualTreeAsset)} has been cloned anew" , _uiDocument);
#endif

        var op = _table.GetTableAsync();
        if( op.IsDone )
        {
            OnTableLoaded(op);
        }
        else
        {
            op.Completed -= OnTableLoaded;
            op.Completed += OnTableLoaded;
        }
    }

    void OnTableLoaded ( AsyncOperationHandle<StringTable> op )
    {
        StringTable table = op.Result;
        LocalizeChildrenRecursively(_uiDocument.rootVisualElement , table);
        _uiDocument.rootVisualElement.MarkDirtyRepaint();
        onCompleted(_uiDocument.rootVisualElement);
    }

    void LocalizeChildrenRecursively ( VisualElement element , StringTable table )
    {
        VisualElement.Hierarchy elementHierarchy = element.hierarchy;
        int numChildren = elementHierarchy.childCount;
        for( int i = 0 ; i < numChildren ; i++ )
        {
            VisualElement child = elementHierarchy.ElementAt(i);
            Localize(child , table);
        }
        for( int i = 0 ; i < numChildren ; i++ )
        {
            VisualElement child = elementHierarchy.ElementAt(i);
            VisualElement.Hierarchy childHierarchy = child.hierarchy;
            int numGrandChildren = childHierarchy.childCount;
            if( numGrandChildren != 0 )
                LocalizeChildrenRecursively(child , table);
        }
    }

    void Localize ( VisualElement next , StringTable table )
    {
        if( typeof(TextElement).IsInstanceOfType(next) )
        {
            TextElement textElement = (TextElement)next;
            string key = textElement.text;
            if( !string.IsNullOrEmpty(key) && key[0] == '#' )
            {
                key = key.TrimStart('#');
                StringTableEntry entry = table[key];
                if( entry != null )
                    textElement.text = entry.LocalizedValue;
                else
                    Debug.LogWarning($"No {table.LocaleIdentifier.Code} translation for key: '{key}'");
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UIDocumentLocalization))]
    public class MyEditor : Editor
    {
        public override VisualElement CreateInspectorGUI ()
        {
            var ROOT = new VisualElement();

            var LABEL = new Label($"- Remember -<br> Use <color=\"yellow\">{nameof(onCompleted)}</color> event instead of <color=\"yellow\">OnEnable()</color><br>to localize and bind this document correctly.");
            {
                var style = LABEL.style;
                style.minHeight = EditorGUIUtility.singleLineHeight * 3;
                style.backgroundColor = new Color(1f , 0.121f , 0 , 0.2f);
                style.borderBottomLeftRadius = style.borderBottomRightRadius = style.borderTopLeftRadius = style.borderTopRightRadius = 6;
                style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            ROOT.Add(LABEL);

            InspectorElement.FillDefaultInspector(ROOT , this.serializedObject , this);

            return ROOT;
        }
    }
#endif

}
