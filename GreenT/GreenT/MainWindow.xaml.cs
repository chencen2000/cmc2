using GreenT.Common;
using GreenT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        //List<UserControl> taskControls = new List<UserControl>();
        Dictionary<int, UserControl> taskControls = new Dictionary<int, UserControl>();
        //int ll = 0;
        internal MainWindowViewModel mwModels = null;
        //public Action<string> onUIUdateCallback = null;

        public MainWindow()
        {
            InitializeComponent();

            //onUIUdateCallback = new Action<string>(this.onUIUpdate);

            Width -= Constant.BlankWidth;
            Height -= Constant.BlankHeight;

            mwModels = new MainWindowViewModel();
            DataContext = mwModels;

            //LocUtil.SetDefaultLanguage(this);

            int labels = (int)Application.Current.Properties["labels"];
            int rows = (int)Application.Current.Properties["rows"];
            int cols = (int)Application.Current.Properties["cols"];
            for (int i = 0; i < cols; i++)
                grdMain.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < rows; i++)
                grdMain.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < labels; i++)
            {
                Dictionary<string, object> d = (Dictionary<string, object>)Application.Current.Properties[(i + 1).ToString()];
                Views.viewIdle v = new Views.viewIdle(i+1);
                Grid.SetColumn(v, (int)d["col"]);
                Grid.SetRow(v, (int)d["row"]);
                d["view"] = v;
                grdMain.Children.Add(v);
            }

            /*
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
                    int ll = i * 3 + col + 1;
                    task.SetLabel(ll);
                    //task.SetConnect(ll%2==0);
                    
                    Grid.SetColumn(task, col);
                    Grid.SetRow(task, i);
                    grdMain.Children.Add(task);
                    taskControls.Add(ll, task);

                }
            }
            */
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


            //ProcessControl processControl = new ProcessControl();
            //grdMain.Children.RemoveAt(0);
            //taskControls.RemoveAt(0);
            //processControl.SetBackGround(Colors.DarkOrange);
            //Grid.SetColumn(processControl, 0);
            //Grid.SetRow(processControl, 0);
            //grdMain.Children.Insert(0, processControl);
            //taskControls.Insert(0, processControl);


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

        Tuple<int,int> getRowColByLabel(int label, int row, int col)
        {
            int x = (label-1) / col;
            int y = (label - 1) % col;
            return new Tuple<int, int>(x, y);
        }
        UserControl loadUserControlByName(string id)
        {
            UserControl ret = null;
            try
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                foreach(var v in a.ExportedTypes)
                {
                    if(string.Compare(id, v.Name, true) == 0)
                    {
                        ret = Activator.CreateInstance(v) as UserControl;
                        break;
                    }
                }
            }
            catch (Exception) { }
            return ret;
        }
        void updateUI(int label, string id)
        {
            if (taskControls.ContainsKey(label))
            {
                this.Dispatcher.Invoke(() => 
                {
                    UserControl uc = taskControls[label];
                    Type t = uc.GetType();
                    if (string.Compare(t.Name, id, true) != 0)
                    {
                        grdMain.Children.Remove(uc);
                        int row = grdMain.RowDefinitions.Count;
                        int col = grdMain.ColumnDefinitions.Count;
                        UserControl uc1 = loadUserControlByName(id);
                        if (uc1 != null)
                        {
                            Tuple<int, int> pos = getRowColByLabel(label, row, col);
                            Grid.SetRow(uc1, pos.Item1);
                            Grid.SetColumn(uc1, pos.Item2);
                            grdMain.Children.Add(uc1);
                            taskControls[label] = uc1;
                        }
                    }
                    // call updateUI in UserControl
                    uc = taskControls[label];
                    t = uc.GetType();
                    MethodInfo mi = t.GetMethod("updateUI");
                    if (mi != null)
                    {
                        mi.Invoke(uc, null);
                    }
                });
            }
        }
        void onUIUpdate(string str)
        {
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            try
            {
                Dictionary<string, object> data = jss.Deserialize<Dictionary<string, object>>(str);
                if (data.ContainsKey("error") && data["error"].GetType() == typeof(int) && (int)data["error"] == 0)
                {
                    foreach (KeyValuePair<string, object> kvp in data)
                    {
                        Match m = Regex.Match(kvp.Key, @"label_(\d+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            int label = 0;
                            if(Int32.TryParse(m.Groups[1].Value, out label))
                            {
                                updateUI(label, kvp.Value.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }
        public void updateView(Type t, Dictionary<string,object> args)
        {
            this.Dispatcher.Invoke(() =>
            {
                UserControl olduc = (UserControl)args["view"];
                int label = (int)args["label"];
                int row = (int)args["row"];
                int col = (int)args["col"];
                UserControl ret = Activator.CreateInstance(t, new object[] { label }) as UserControl;
                grdMain.Children.Remove(olduc);
                Grid.SetRow(ret, row);
                Grid.SetColumn(ret, col);
                grdMain.Children.Add(ret);
                args["view"] = ret;
            });
        }
    }
}
