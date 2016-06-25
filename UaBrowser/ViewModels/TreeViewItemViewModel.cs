// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Workstation.UaBrowser.ViewModels
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class TreeViewItemViewModel : INotifyPropertyChanged
    {
        private static readonly TreeViewItemViewModel DummyChild = new TreeViewItemViewModel();

        private readonly ObservableCollection<TreeViewItemViewModel> children;
        private readonly TreeViewItemViewModel parent;

        private bool isExpanded;
        private bool isLoading;
        private bool isSelected;

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren)
        {
            this.parent = parent;
            this.children = new ObservableCollection<TreeViewItemViewModel>();
            if (lazyLoadChildren)
            {
                this.children.Add(DummyChild);
            }
        }

        // This is used to create the DummyChild instance.
        private TreeViewItemViewModel()
        {
        }

        /// <summary>
        /// Gets the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get { return this.children; }
        }

        /// <summary>
        /// Gets a value indicating whether this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the TreeViewItem
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                if (value != this.isExpanded)
                {
                    this.isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                if (this.isExpanded && this.parent != null)
                {
                    this.parent.IsExpanded = true;
                }

                if (this.isExpanded && this.HasDummyChild)
                {
                    this.LoadChildren();
                }
            }
        }

        private async void LoadChildren()
        {
            this.IsLoading = true;
            this.Children.Remove(DummyChild);
            await this.LoadChildrenAsync();
            this.IsLoading = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the TreeViewItem
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                if (value != this.isSelected)
                {
                    this.isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether gets whether the TreeViewItem
        /// associated with this object is loading children.
        /// </summary>
        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }

            private set
            {
                if (value != this.isLoading)
                {
                    this.isLoading = value;
                    this.OnPropertyChanged("IsLoading");
                }
            }
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual Task LoadChildrenAsync()
        {
            return Task.FromResult(true);
        }

        public TreeViewItemViewModel Parent
        {
            get { return this.parent; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}