using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Lab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum RecognisionState { LOADING, PROCESSING, STOPPING, READY }
        private RecognisionState recognising = RecognisionState.LOADING;

        internal readonly ClassificationCollection mainCollection;

        public MainWindow()
        {
            /*
        Приложение WPF обращается для выполнения распознавания и хранения результатов к сервису из п.1. 

        Вся функциональность приложения из задания 3 сохранена, но работа идёт с сервисом, а не с базой данных. 

        Приложение корректно обрабатывает ситуацию недоступности сервиса 

        Обращение к сервису происходит асинхронно 
             */
            InitializeComponent();

            mainCollection = new ClassificationCollection(Dispatcher);

            mainCollection.RecognisionFinished += RecognisingButtonStopSync;
            mainCollection.ResultUpdated += UpdateResultSync;
            // may be sorted using CollectionViewSource
            listBox_ObjectList.ItemsSource = mainCollection;
            mainCollection.ChildChanged += ReplaceWorkaround;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            mainCollection.SetHost(textBox_Host.Text);

            progressBar_RecognisionProgress.Visibility = Visibility.Visible;
            Task.Run(async () =>
            {
                await mainCollection.LoadAllAsync(
                    (percent) => Dispatcher.Invoke(() => progressBar_RecognisionProgress.Value = percent)
                );
                Dispatcher.Invoke(Storage_Loaded);
            });
        }

        private void Storage_Loaded()
        {
            button_ClearButton.IsEnabled = true;
            progressBar_RecognisionProgress.Visibility = Visibility.Hidden;
            SetRecognisingState(RecognisionState.READY);
        }

        private void SetRecognisingState(RecognisionState isRecognising)
        {
            if (recognising == isRecognising)
                return;
            recognising = isRecognising;

            if (recognising == RecognisionState.READY)
                progressBar_RecognisionProgress.Visibility = Visibility.Hidden;
            else
                progressBar_RecognisionProgress.Visibility = Visibility.Visible;
        }

        private void RecognisingButtonStopSync()
        {
            Dispatcher.BeginInvoke(new Action<RecognisionState>(SetRecognisingState), RecognisionState.READY);
        }

        private void UpdateResult(double progress)
        {
            progressBar_RecognisionProgress.Value = progress;
        }
        private void UpdateResultSync(double progress)
        {
            Dispatcher.BeginInvoke(new Action<double>(UpdateResult), progress);
        }

        private void ResetDisplay()
        {
            progressBar_RecognisionProgress.Value = 0.0;
            scrollViewer_ObjectImages.ScrollToVerticalOffset(0);
            listBox_ObjectList.SelectedIndex = -1;
            wrapPanel_ObjectImages.ItemsSource = null;
        }

        private void ReplaceWorkaround()
        {
            // TO REWORK? ListBox just can't
            // the problem is that the most frequently used change(Replace)
            // doen't work or cause list view rebuilding
            listBox_ObjectList.Items.Refresh();
        }

        private void button_ChooseImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string[] filenames = dialog.FileNames;
                SetRecognisingState(RecognisionState.PROCESSING);
                Task.Run(() => StartClassificationAsync(filenames));
            }
        }
        private async Task StartClassificationAsync(string[] filenames)
        {
            await mainCollection.Classify(filenames);
        }

        private void button_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDisplay();
            mainCollection.Clear();
        }

        private void listBox_ObjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox_ObjectList.SelectedItem != null)
            {
                wrapPanel_ObjectImages.ItemsSource = ((ClassificationCategory)listBox_ObjectList.SelectedItem).FoundObjects;
                scrollViewer_ObjectImages.ScrollToVerticalOffset(0);
            }
        }
    }
}
