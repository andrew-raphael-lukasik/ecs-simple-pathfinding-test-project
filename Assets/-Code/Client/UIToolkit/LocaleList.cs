// void* src = https://gist.github.com/andrew-raphael-lukasik/e4ae9b45a2c24672c0d1218f77235948
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Client.UIToolkit
{
    [UnityEngine.Scripting.Preserve]
    [UxmlElement]
    public partial class LocaleList : VisualElement
    {
        #region fields
        
        [UxmlAttribute]
        public int itemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                _listView.fixedItemHeight = value;
            }
        }
        int _itemHeight = k_itemHeight;
        const int k_itemHeight = 16;
        
        public List<Locale> locales = new List<Locale>();
        ListView _listView = null;

        #endregion
        #region constructors


        public LocaleList ()
        {
            _listView = new ListView(
                itemsSource:    locales ,
                itemHeight:        itemHeight ,
                makeItem:        OnMakeItem ,
                bindItem:        OnBindItem
            );
            _listView.selectionType = SelectionType.Single;
            {
                var style = _listView.style;
                style.minHeight = itemHeight;
                style.flexGrow = 1f;
            }
            this.Add( _listView );
            _listView.itemsChosen += OnItemsChosen;
            // _listview.onSelectionChange += (objects)=> Debug.Log($"selection change: {objects}");

            if( Application.isPlaying )
            {
                var op = LocalizationSettings.SelectedLocaleAsync;
                if( op.IsDone ) InitializeCompleted( op );
                else op.Completed += InitializeCompleted;
            }
            else
            {
                locales.Add(null);
                locales.Add(null);
                locales.Add(null);
                _listView.Rebuild();
            }
        }

        #endregion
        #region private methods

        void InitializeCompleted ( AsyncOperationHandle<Locale> op )
        {
            locales.Clear();
            locales.AddRange( LocalizationSettings.AvailableLocales.Locales );
            _listView.Rebuild();

            LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
        }

        VisualElement OnMakeItem ()
        {
            return new Label();
        }
        void OnBindItem ( VisualElement visualElement , int index )
        {
            Label label = (Label) visualElement;
            Locale locale = locales[index];
            label.text = locale!=null ?  locale.name : "...";
        }

        void OnItemsChosen ( IEnumerable<object> objects ) => ChangeLocale( (Locale) objects.FirstOrDefault() );
        void ChangeLocale ( Locale locale )
        {
            if( locale!=null )
            {
                LocalizationSettings.SelectedLocaleChanged -= LocalizationSettings_SelectedLocaleChanged;
                LocalizationSettings.SelectedLocale = locale;
                LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
            }
            else Debug.LogWarning("locale object is null");
        }

        void LocalizationSettings_SelectedLocaleChanged ( Locale locale )
        {
            _listView.ScrollToItem( locales.IndexOf(locale) );
        }

        #endregion
    }
}
