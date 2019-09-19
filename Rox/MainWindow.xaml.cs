﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rox
{
    public class DelegateCommand<T> : System.Windows.Input.ICommand where T : class
    {
        private readonly Predicate<T> _canExecute;
        private readonly Action<T> _execute;
        public DelegateCommand(Action<T> execute)            : this(execute, null)
        {
        }
        public DelegateCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) { return true; }
            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public interface INode
    {
        NodeTypes NodeType { get; }
        string Name { get; set; }
        string Description();
        List<INode> Items { get; set; }
        List<NodeTypes> AllowedNodes { get; }
    }
    public enum NodeTypes
    {
        General = 0,
        Mode = 1,
        Condition = 2,
        Timer = 3,
        Initialized = 4,
        Continuous = 5
    }
    public class IteMode : INode
    {
        public NodeTypes NodeType { get; } = NodeTypes.Mode;
        public string Name { get; set; }
        public List<NodeTypes> AllowedNodes { get; } = new List<NodeTypes>() { NodeTypes.Condition };
        public List<INode> Items { get; set; } = new List<INode>();
        public string Description() { return "{ MODE } A mode can be created to easily abort a running mode and start new sequencing. Stop and Auto modes will run with Start/Stop button. No items can be added directly. Add items to Initialize or Continuous branches."; }
        public IteMode(string name)
        {
            Name = name;
        }
    }
    public class IteFirstScan : INode
    {
        public NodeTypes NodeType { get; } = NodeTypes.Initialized;
        public string Name { get; set; }
        public List<NodeTypes> AllowedNodes { get; } = new List<NodeTypes>() { NodeTypes.Condition };
        public List<INode> Items { get; set; } = new List<INode>();
        public string Description() { return "{ First Scan branch } This sequence will run one time when the program is loaded and can accept any item."; }
        public IteFirstScan(string name)
        {
            Name = name;
        }
    }
    public class IteIntialize : INode
    {
        public NodeTypes NodeType { get; } = NodeTypes.Initialized;
        public string Name { get; set; }
        public List<NodeTypes> AllowedNodes { get; } = new List<NodeTypes>() { NodeTypes.Condition };
        public List<INode> Items { get; set; } = new List<INode>();
        public string Description() { return "{ Initialize branch } This sequence will only run one time when the mode is first started and before the continuous branch. This node can accept any item."; }
        public IteIntialize(string name)
        {
            Name = name;
        }
    }
    public class IteContinuous : INode
    {
        public NodeTypes NodeType { get; } = NodeTypes.Continuous;
        public string Name { get; set; }
        public List<NodeTypes> AllowedNodes { get; } = new List<NodeTypes>() { NodeTypes.Condition };
        public List<INode> Items { get; set; } = new List<INode>();
        public string Description() { return "{ Continuous branch } This sequence will run repeatedly after the initalize branch has completed and while the mode is active."; }
        public IteContinuous(string name)
        {
            Name = name;
        }
    }
    public class IteNodeViewModel : INotifyPropertyChanged
    {
        public DelegateCommand<string> ButtonClickCommand
        {
            get { return _clickCommand; }
        }
        private readonly DelegateCommand<string> _clickCommand;
        private INode Node { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isSelected;
        private bool _isExpanded = true;
        public IteNodeViewModel Parent { get; set; }
        public Collection<IteNodeViewModel> Items { get; set; }
        public IteNodeViewModel(INode node) : this(node, null) { }
        public IteNodeViewModel(INode node, IteNodeViewModel parent)
        {
            Node = node;
            Parent = parent;
            Items = new Collection<IteNodeViewModel>();
            foreach (var item in node.Items)
            {
                switch (item.NodeType)
                {
                    case NodeTypes.General:
                        Items.Add(new IteNodeViewModel(item));
                        break;
                    case NodeTypes.Mode:
                        Items.Add(new IteMODE_VM(item));
                        break;
                    case NodeTypes.Condition:
                        Items.Add(new IteNodeViewModel(item));
                        break;
                    case NodeTypes.Timer:
                        Items.Add(new IteTIMER_VM(item));
                        break;
                    case NodeTypes.Initialized:
                        Items.Add(new IteFIRST_VM(item));
                        break;
                    case NodeTypes.Continuous:
                        Items.Add(new IteCONTINUOUS_VM(item));
                        break;
                    default:
                        break;
                }
            }
            //Items = new Collection<IteNodeViewModel>((from item in node.Items select new IteNodeViewModel(item, this)).ToList<IteNodeViewModel>());
            _clickCommand = new DelegateCommand<string>(
                       (s) => 
                       {
                           Console.WriteLine(node.Description());
                       }, //Execute
                       (s) => { return true; } //CanExecute
                       );
        }
        public string Name
        {
            get { return Node.Name; }
        }
        public string Description
        {
            get { return Node.Description(); }
        }
        public NodeTypes NodeType
        {
            get { return Node.NodeType; }
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                    Parent.IsExpanded = true;
            }
        }
    }
    public class IteCONTINUOUS_VM : IteNodeViewModel
    {
        public IteCONTINUOUS_VM(INode node) : base(node) { }
    }
    public class IteMODE_VM : IteNodeViewModel
    {
        public IteMODE_VM(INode node) : base(node) { }
    }
    public class IteFIRST_VM : IteNodeViewModel
    {
        public IteFIRST_VM(INode node) : base(node) { }
    }
    public class IteTIMER_VM : IteNodeViewModel
    {
        public IteTIMER_VM(INode node) : base(node) { }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<IteNodeViewModel> Modes;
        System.Threading.Timer closeMnu;
        private bool? _running;
        public bool? Running
        {
            get { return _running; }
            private set
            {
                if (!value.HasValue)
                {
                    // no file
                    btnToggleRun.Content = txtblkRunDefault;
                    brdrRun.Background = new SolidColorBrush(Color.FromRgb(41, 41, 41));
                    btnToggleRun.BorderBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
                    btnToggleRun.Foreground = new SolidColorBrush(Colors.White);
                }
                else if (value.Value)
                {
                    // running
                    btnToggleRun.Content = "STOP";
                    brdrRun.Background = new SolidColorBrush(Colors.OrangeRed);
                    btnToggleRun.BorderBrush = new SolidColorBrush(Colors.OrangeRed);
                    btnToggleRun.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    // stopped
                    btnToggleRun.Content = "START";
                    brdrRun.Background = new SolidColorBrush(Colors.LightGreen);
                    btnToggleRun.BorderBrush = new SolidColorBrush(Colors.LightGreen);
                    btnToggleRun.Foreground = new SolidColorBrush(Colors.LawnGreen);
                }
                _running = value;
            }
        }
        string loadedFile;
        string fileToBeLoaded;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //App.splashScreen.LoadComplete();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            gridMenu.Visibility = Visibility.Visible;
            gridFiles.Visibility = Visibility.Collapsed;
            btnSaveAs.Visibility = Visibility.Collapsed;
            ApplySettings();
            txtVer.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            closeMnu = new System.Threading.Timer(new System.Threading.TimerCallback(closeMenu), null, 10000, System.Threading.Timeout.Infinite);
            PopulateFilelist();
            //System.Threading.Thread.Sleep(12000); // << to test splash screen
            resetForm(true);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileUnload(false);
            SaveSettings();
        }
        private void resetCloseMenuTimer(int interval = 10000)
        {
            closeMnu.Change(interval, System.Threading.Timeout.Infinite);
        }
        private void _MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) { this.DragMove(); }
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ApplySettings()
        {
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
            this.WindowState = Properties.Settings.Default.LastWindowState;
            this.Width = Properties.Settings.Default.LastWindowRect.Width;
            this.Height = Properties.Settings.Default.LastWindowRect.Height;
            this.Top = Properties.Settings.Default.LastWindowRect.Top;
            this.Left = Properties.Settings.Default.LastWindowRect.Left;
        }
        private void SaveSettings()
        {
            Properties.Settings.Default.LastWindowState = this.WindowState;
            Properties.Settings.Default.LastWindowRect = this.RestoreBounds;
            Properties.Settings.Default.Save();
        }
        private void toggleMenu(object sender, RoutedEventArgs e)
        {
            if (gridMenu.Visibility == Visibility.Visible)
            {
                closeFiles();
                gridMenu.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridMenu.Visibility = Visibility.Visible;
                //closeMnu = new System.Threading.Timer(new System.Threading.TimerCallback(closeMenu), null, 10000, System.Threading.Timeout.Infinite);
            }
        }
        private void closeFiles()
        {
            Dispatcher.Invoke(() =>
            {
                gridMenu.Visibility = Visibility.Collapsed;
                gridFiles.Visibility = Visibility.Collapsed;
                btnLoadFileText.Text = "select file";
            });
            //if (this.Dispatcher.CheckAccess())
            //{
            //    //gridMenu.Visibility = Visibility.Collapsed;
            //    //gridFiles.Visibility = Visibility.Collapsed;
            //    //btnLoadFileText.Text = "select file";
            //}
            //else
            //{
            //    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(closeFiles));
            //}
        }
        private void closeMenu(object state)
        {
            closeFiles();
        }
        private void Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            resetCloseMenuTimer();
            if (((ListBox)sender).SelectedItem == null) { return; }

            if (((ListBox)sender).SelectedItem.ToString() == "> unload <")
            {
                // unload file
                btnLoadFileText.Text = "select file";
                FileUnload(false);
                fileToBeLoaded = null;
            }
            else
            {
                fileToBeLoaded = (string)((ListBox)sender).SelectedItem;
                btnLoadFileText.Text = string.Format("load [{0}]", fileToBeLoaded);
            }
        }
        private void Files_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((ListBox)sender).SelectedItem == null) { return; }
            Files_SelectionChanged(sender, null);
        }
        private void Files_LostFocus(object sender, RoutedEventArgs e)
        {
            ((ListBox)sender).SelectedItem = null;
        }
        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            //var d = new SaveAs(loadedFile) { Owner = this };
            //if (d.ShowDialog() == true)
            //{
            //    if (SharedMethods.SaveJobAs(pluginsCore, d.Filename))
            //    {
            //        FileLoad(d.Filename);
            //        PopulateFilelist();
            //    }
            //}
        }
        private void FileLoad(string filename)
        {
            FileUnload(false);
        }
        private void FileUnload(bool unselectProgram)
        {
            btnSaveAs.Visibility = Visibility.Collapsed;
            Running = null;
            resetForm(unselectProgram);
        }
        private void resetForm(bool unselectProgram = false)
        {
            loadedFile = null;
            UpdateHeader("Load a program.");
            if (unselectProgram)
            {
                listFiles.SelectedItem = null;
                listRecentFiles.SelectedItem = null;
                SetDefaultGuiElements();
            }
        }
        private void UpdateHeader(string s)
        {
            txtTitle.Text = s;
        }
        private void SetDefaultGuiElements()
        {
            Modes = new List<IteNodeViewModel>()
            {
                new IteFIRST_VM( new IteFirstScan("1st scan")),
                new IteMODE_VM( new IteMode("Stop"){Items={new IteIntialize("Initialize"),new IteContinuous("Continuous") } }),
                new IteMODE_VM( new IteMode("Auto"){Items={new IteIntialize("Initialize"),new IteContinuous("Continuous") } }),
            };
            tree.DataContext = new
            {
                Modes
            };
        }
        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            if (btnLoadFileText.Text == "select file" || fileToBeLoaded == null)
            {
                return;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(fileToBeLoaded))
                {
                    FileLoad(fileToBeLoaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void toggleRun(object sender, RoutedEventArgs e)
        {
            toggleRun();
        }
        private void toggleRun()
        {
            if (string.IsNullOrEmpty(loadedFile))
            {
                showFiles();
            }
            else
            {
                Running = !Running;
            }
        }
        private void showFiles()
        {
            resetCloseMenuTimer();
            gridFiles.Visibility = Visibility.Visible;
        }
        private void PopulateFilelist()
        {
            //listRecentFiles.Items.Clear();
            //foreach (var item in SharedMethods.GetVisionFiles(true, false).Take(3))
            //{
            //    listRecentFiles.Items.Add(item);
            //}
            //listFiles.ItemsSource = SharedMethods.GetVisionFiles();
            //if (listRecentFiles.Items.Count > 0)
            //{
            //    listRecentFiles.SelectedItem = listRecentFiles.Items[0];
            //}
        }
        private void SetHelperText(string t)
        {
            txtSelectedNodeInfo.Text = t;
        }
        private void SetAvailableAddButtons(NodeTypes t)
        {
            switch (t)
            {
                case NodeTypes.General:
                    break;
                case NodeTypes.Mode:
                    break;
                case NodeTypes.Condition:
                    break;
                case NodeTypes.Timer:
                    break;
                default:
                    break;
            }
        }

        private void tvi_GotFocus(object sender, RoutedEventArgs e)
        {
            //Dispatcher.Invoke(() => {
            var s = ((IteNodeViewModel)((TreeViewItem)sender).DataContext);
            SetHelperText(s.Name + " - " + s.Description);
            SetAvailableAddButtons(((IteNodeViewModel)((TreeViewItem)sender).DataContext).NodeType);
            //});
            e.Handled = true;
        }

        //private void tvi_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    e.Handled = true;
        //}
        //private void tvi_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    e.Handled = true;
        //}
    }
}
