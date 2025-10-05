using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

namespace Client.UIToolkit
{
    [UnityEngine.Scripting.Preserve]
    [UxmlElement]
    public partial class EntityList : ListView
    {

        const float k_double_click_threshold = 0.5f;
        List<Entity> items = new List<Entity>();
        public event System.Action<Entity> onItemClicked = null;
        public event System.Action<Entity> onItemDoubleClicked = null;
        public event System.Func<Entity,string> onItemBind = null;

        float _clickTime;

        public EntityList ()
            : base()
        {
            this.itemsSource =new List<Entity>();
            this.makeItem = MakeItem;
            this.bindItem = BindItem;
            this.selectionType = SelectionType.Single;
            this.itemsChosen += OnItemsChosen;
            // listview.onSelectionChange += (objects)=> Debug.Log($"selection change: {objects}");

            this.style.height = fixedItemHeight * 3;

            #if UNITY_EDITOR
            if( !Application.isPlaying )
            {
                items.Add( Entity.Null );
                items.Add( Entity.Null );
                items.Add( Entity.Null );
            }
            #endif
        }

        public EntityList ( System.Func<Entity,string> onItemBind , System.Action<Entity> onItemClicked , System.Action<Entity> onItemDoubleClicked )
            : this()
            => Initialize( new Entity[0] , 3 , onItemBind , onItemClicked , onItemDoubleClicked );

        VisualElement MakeItem () => new Label();
        void BindItem ( VisualElement visualElement , int index )
        {
            Label label = (Label) visualElement;
            Entity entity = items[index];
            label.text = onItemBind!=null ? onItemBind( entity ) : "<null>";
        }

        void OnItemsChosen ( IEnumerable<object> objects )
        {
            Entity entity = (Entity) objects.FirstOrDefault();
            if( (Time.realtimeSinceStartup-_clickTime)>k_double_click_threshold )
                onItemClicked( entity );
            else
                onItemDoubleClicked( entity );
            _clickTime = Time.realtimeSinceStartup;
        }

        /// <summary> Required. Object won't work without these fields filled. </summary>
        public void Initialize ( Entity[] items , int numVisibleItems , System.Func<Entity,string> onItemBind , System.Action<Entity> onItemClicked , System.Action<Entity> onItemDoubleClicked )
        {
            this.items.Clear();
            this.items.AddRange( items );
            SetNumVisibleItems( numVisibleItems );
            this.onItemClicked = onItemClicked;
            this.onItemDoubleClicked = onItemDoubleClicked;
            this.onItemBind = onItemBind;
        }

        public void AddItem ( Entity entity ) => this.items.Add(entity);
        public void AddItems ( Entity[] entities ) => this.items.AddRange( entities );
        public void ClearItems () => this.items.Clear();
        public void SetNumVisibleItems ( int numVisibleItems ) => this.style.height = fixedItemHeight * numVisibleItems;

    }
}
