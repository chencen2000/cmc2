using GreenT.Common;
using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreenT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<UserControl> taskControls = new List<UserControl>();
        int ll = 0;
        internal MainWindowViewModel mwModels = null;
        public MainWindow()
        {
            InitializeComponent();

            Width -= Constant.BlankWidth;
            Height -= Constant.BlankHeight;

            mwModels = new MainWindowViewModel();
            DataContext = mwModels;

            //LocUtil.SetDefaultLanguage(this);
            
            grdMain.ColumnDefinitions.Add(new ColumnDefinition());
            grdMain.ColumnDefinitions.Add(new ColumnDefinition());
            grdMain.ColumnDefinitions.Add(new ColumnDefinition());
            grdMain.RowDefinitions.Add(new RowDefinition());
            grdMain.RowDefinitions.Add(new RowDefinition());
            grdMain.RowDefinitions.Add(new RowDefinition());
            grdMain.RowDefinitions.Add(new RowDefinition());

            for (int i = 0; i < 4; i++)
            {
                for (int col= 0; col < 3; col++)
                {
                    TaskControl task = new TaskControl();
                    ll = i * 3 + col + 1;
                    task.SetLabel(ll);
                    task.SetConnect(ll%2==0);
                    
                    Grid.SetColumn(task, col);
                    Grid.SetRow(task, i);
                    grdMain.Children.Add(task);
                    taskControls.Add(task);

                }
            }
        }

        public void QuitSystem(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MouseMove_Click(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Change_language(String Culture)
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }

            string requestedCulture = string.Format(@"i18N\Resources.{0}.xaml", Culture);
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            if (resourceDictionary == null)
            {
                requestedCulture = @"i18N\Resources.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            }

            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
        }

        private void DrawCircleButton_Click(object sender, RoutedEventArgs e)
        {
            /*            Uri uri = new Uri("/i18N/testUI.xaml", UriKind.Relative);
            Stream stream = Application.GetResourceStream(uri).Stream;
            //FrameworkElement继承自UIElement
            FrameworkElement obj = XamlReader.Load(stream) as FrameworkElement;
            grdMain.Children.RemoveAt(0);
            taskControls.RemoveAt(0);
            grdMain.Children.Insert(0, obj);
//            taskControls.Insert(0, obj);*/


            ProcessControl processControl = new ProcessControl();
            grdMain.Children.RemoveAt(0);
            taskControls.RemoveAt(0);
            processControl.SetBackGround(Colors.DarkOrange);
            Grid.SetColumn(processControl, 0);
            Grid.SetRow(processControl, 0);
            grdMain.Children.Insert(0, processControl);
            taskControls.Insert(0, processControl);


            //MessageBox.Show($"{this.ActualWidth} X {this.ActualHeight}");

            //MessageBox.Show($"{this.grdMain.ActualWidth} X {this.grdMain.ActualHeight}");

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                grdMain.Height = e.NewSize.Height - 64 - 48;
            }
        }

        private void IconImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Pop.IsOpen = true;
        }


    }
}
