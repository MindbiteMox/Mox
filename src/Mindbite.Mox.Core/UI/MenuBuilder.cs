using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindbite.Mox.UI.MenuBuilder
{
    public interface IMenuItem
    {
        string Title { get; set; }
        string Id { get; set; }
        string Url { get; set; }
        int Depth { get; }
        bool Selected { get; set; }
        IEnumerable<IMenuItem> Children { get; }
        IMenuItem Parent { get; }
        IEnumerable<IMenuItem> Parents { get; }
    }

    public interface IMenu
    {
        IEnumerable<IMenuItem> Hierarchy { get; }
        IEnumerable<IMenuItem> Flat { get; }
        IMenu Prune(Func<IMenuItem, bool> predicate);
        IMenu Select(Func<IMenuItem, bool> predicate);
    }

    internal class MenuItem : IMenuItem
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
        public int Depth { get; set; }
        public List<MenuItem> Children { get; set; } = new List<MenuItem>();
        public MenuItem Parent { get; set; }
        public bool Selected { get; set; }

        IEnumerable<IMenuItem> IMenuItem.Children => this.Children;
        IMenuItem IMenuItem.Parent => this.Parent;
        IEnumerable<IMenuItem> IMenuItem.Parents => this.Parent == null ? new IMenuItem[0] : ((IMenuItem)this.Parent).Parents.Append(this.Parent);

        string IMenuItem.Url
        {
            get => this.Url ?? this.Children.FirstOrDefault()?.Url;
            set => this.Url = value;
        }

        bool IMenuItem.Selected
        {
            get => this.Selected;
            set
            {
                this.Selected = value;
                foreach(var parent in ((IMenuItem)this).Parents)
                {
                    parent.Selected = value;
                }
            }
        }
    }

    internal class Menu : IMenu
    {
        public List<MenuItem> MenuItems { get; set; }

        IEnumerable<IMenuItem> IMenu.Hierarchy => this.MenuItems;
        IEnumerable<IMenuItem> IMenu.Flat
        {
            get
            {
                var result = new List<IMenuItem>();

                void addChildren(IEnumerable<MenuItem> menuItems)
                {
                    foreach(var item in menuItems)
                    {
                        result.Add(item);
                        addChildren(item.Children);
                    }
                }

                addChildren(this.MenuItems);

                return result;
            }
        }

        public Menu Clone()
        {
            var builder = MenuBuilder.Create();

            void addItem(MenuItem menuItem)
            {
                var itembuilder = builder.Add(menuItem.Title)
                    .Url(menuItem.Url)
                    .Id(menuItem.Id)
                    .Children(items =>
                    {
                        foreach (var item in menuItem.Children)
                        {
                            addItem(item);
                        }
                    });
                itembuilder._item.Selected = menuItem.Selected;
            }

            
            foreach(var item in this.MenuItems)
            {
                addItem(item);
            }

            return (Menu)builder.Build();
        }

        /// <summary>
        /// A predicate returning false will remove the menu item and it's children
        /// </summary>
        /// <param name="predicate"></param>
        public IMenu Prune(Func<IMenuItem, bool> predicate)
        {
            var clone = this.Clone();

            void prune(List<MenuItem> menuItems)
            {
                menuItems.RemoveAll(x => predicate(x));
                foreach(var menuItem in menuItems)
                {
                    prune(menuItem.Children);
                }
            }

            prune(clone.MenuItems);

            return clone;
        }

        /// <summary>
        /// Every selected item is put at the root level, with its children intact.
        /// Select something like: 
        ///     x => x.Depth == 2
        /// Or:
        ///     x => x.Parent?.Id == "someid"
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IMenu Select(Func<IMenuItem, bool> predicate)
        {

            void addItem(MenuBuilder _builder, IMenuItem item)
            {
                var itemBuilder = _builder.Add(item.Title)
                    .Url(item.Url)
                    .Id(item.Id)
                    .Children(childBuilder =>
                    {
                        foreach(var child in item.Children)
                        {
                            addItem(childBuilder, child);
                        }
                    });
                itemBuilder._item.Selected = item.Selected;
            }

            var builder = MenuBuilder.Create();

            foreach(var item in ((IMenu)this).Flat)
            {
                addItem(builder, item);
            }

            return builder.Build();
        }
    }

    public class MenuBuilder
    {
        public class MenuItemBuilder
        {
            internal readonly MenuItem _item;

            internal MenuItemBuilder(MenuItem item)
            {
                this._item = item;
            }

            public MenuItemBuilder Url(string url)
            {
                this._item.Url = url;
                return this;
            }

            public MenuItemBuilder Id(string id)
            {
                this._item.Id = id;
                return this;
            }

            public MenuItemBuilder Children(Action<MenuBuilder> children)
            {
                var builder = new MenuBuilder(this._item.Children, this._item);
                children(builder);
                return this;
            }
        }

        private readonly List<MenuItem> _menuItems;
        private readonly MenuItem _parent;

        private MenuBuilder(List<MenuItem> menuItems, MenuItem parent)
        {
            this._menuItems = menuItems;
            this._parent = parent;
        }

        public static MenuBuilder Create()
        {
            return new MenuBuilder(new List<MenuItem>(), null);
        }

        public IMenu Build()
        {
            return new Menu
            {
                MenuItems = this._menuItems
            };
        }

        public MenuItemBuilder Add(string title)
        {
            var newItem = new MenuItem
            {
                Title = title,
                Parent = this._parent,
                Depth = ((this._parent?.Depth ?? -1) + 1)
            };
            this._menuItems.Add(newItem);
            return new MenuItemBuilder(newItem);
        }
    }
}
