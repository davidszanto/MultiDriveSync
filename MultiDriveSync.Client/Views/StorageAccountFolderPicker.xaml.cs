using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiDriveSync.Client.Views
{
    /// <summary>
    /// Interaction logic for StorageAccountFilePicker.xaml
    /// </summary>
    public partial class StorageAccountFolderPicker : Window
    {
        private readonly TaskCompletionSource<Folder> tcs;
        private readonly Func<string, Task<IEnumerable<Folder>>> getChildrenFunc;
        private readonly string rootId;

        public StorageAccountFolderPicker(Func<string, Task<IEnumerable<Folder>>> getChildrenFunc, string rootId)
        {
            tcs = new TaskCompletionSource<Folder>();
            this.getChildrenFunc = getChildrenFunc;
            this.rootId = rootId;

            InitializeComponent();
        }

        public Task<Folder> GetResult()
        {
            return tcs.Task;
        }

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedFolder = (folderTree.SelectedItem as TreeViewItem).Tag as Folder;
            tcs.SetResult(selectedFolder);
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            tcs.SetResult(null);
            Close();
        }

        private async void folderTree_Initialized(object sender, EventArgs e)
        {
            folderTree.Items.Clear();

            var rootFolder = new Folder
            {
                Id = rootId,
                Name = "Google Drive"
            };

            var rootFolderVisual = new TreeViewItem
            {
                Header = rootFolder.Name,
                Tag = rootFolder
            };

            folderTree.Items.Add(rootFolderVisual);

            await LoadChildren(rootFolderVisual);

            rootFolderVisual.IsExpanded = true;
        }

        private async void folderTreeItem_Expanded(object sender, RoutedEventArgs e)
        {
            var expandedTreeViewItem = e.OriginalSource as TreeViewItem;

            var tasks = new List<Task>();
            foreach (var child in expandedTreeViewItem.Items)
            {
                var childTreeViewItem = child as TreeViewItem;
                tasks.Add(LoadChildren(childTreeViewItem));
            }

            await Task.WhenAll(tasks);
        }

        private async Task LoadChildren(TreeViewItem treeViewItem)
        {
            var folder = treeViewItem.Tag as Folder;

            treeViewItem.Items.Clear();

            foreach (var child in await getChildrenFunc(folder.Id))
            {
                treeViewItem.Items.Add(new TreeViewItem
                {
                    Header = child.Name,
                    Tag = child
                });
            }
        }

        private void folderTreeItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectBtn_Click(sender, e);
        }
    }
}
