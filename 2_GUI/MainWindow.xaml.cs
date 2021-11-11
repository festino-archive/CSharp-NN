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
        private readonly string RecogniseButton_textToStart = "Классифицировать";
        private readonly string RecogniseButton_textToStop = "Прервать";
        private readonly string RecogniseButton_textStopping = "Прерывается...";
        private enum RecognisionState { PROCESSING, STOPPING, READY }
        RecognisionState recognising = RecognisionState.READY;

        internal readonly RecogniserWrapper recogniser = new RecogniserWrapper();
        ClassificationCollection Result = new ClassificationCollection();

        private PersistentRecognisionStorage storage = new PersistentRecognisionStorage(); // TODO move unnecessary code to another class


        public MainWindow()
        {
            InitializeComponent();

            recogniser.RecognisionFinished += RecognisingButtonStopSync;
            recogniser.ResultUpdated += UpdateResultSync;
            // may be sorted using CollectionViewSource
            listBox_ObjectList.ItemsSource = Result;
            Result.ChildChanged += ReplaceWorkaround;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            foreach (ImageObject obj in storage.LoadAll()) // TODO async
                Result.Add(obj.Category, obj);
        }

        private void SetRecognisingState(RecognisionState isRecognising)
        {
            if (recognising == isRecognising)
                return;
            recognising = isRecognising;

            if (recognising == RecognisionState.STOPPING)
                button_RecogniseButton.Content = RecogniseButton_textStopping;
            else if (recognising == RecognisionState.PROCESSING)
                button_RecogniseButton.Content = RecogniseButton_textToStop;
            else if (recognising == RecognisionState.READY)
                button_RecogniseButton.Content = RecogniseButton_textToStart;

            if (recognising == RecognisionState.READY)
                progressBar_RecognisionProgress.Visibility = Visibility.Hidden;
            else
                progressBar_RecognisionProgress.Visibility = Visibility.Visible;

            UpdateRecogniseButton();
        }

        private void RecognisingButtonStopSync()
        {
            Dispatcher.BeginInvoke(new Action<RecognisionState>(SetRecognisingState), RecognisionState.READY);
        }

        private void UpdateResult(string[] labels, ImageObject[] imageResult)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (!storage.Contains(imageResult[i]))
                {
                    Result.Add(labels[i], imageResult[i]); // TODO async
                    storage.Add(imageResult[i]);
                }
            }

            if (recogniser.ImageCount > 0)
                progressBar_RecognisionProgress.Value = Result.ObjectCount / (double)recogniser.ImageCount;
        }
        private void UpdateResultSync(string[] labels, ImageObject[] imageResult)
        {
            Dispatcher.BeginInvoke(new Action<string[], ImageObject[]>(UpdateResult), labels, imageResult);
        }
        private void UpdateRecogniseButton()
        {
            bool canStart = Directory.Exists(textBox_ImagesDir.Text) && recognising == RecognisionState.READY;
            button_RecogniseButton.IsEnabled = canStart || recognising == RecognisionState.PROCESSING;
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

        private void button_ChooseImagesDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                textBox_ImagesDir.Text = dialog.SelectedPath;
            }
        }

        private void button_RecogniseButton_Click(object sender, RoutedEventArgs e)
        {
            if (recognising == RecognisionState.READY)
            {
                SetRecognisingState(RecognisionState.PROCESSING);
                string fileDir = textBox_ImagesDir.Text;
                Task.Run(() =>
                {
                    recogniser.Recognise(fileDir);
                }
                );
            }
            else
            {
                SetRecognisingState(RecognisionState.STOPPING);
                recogniser.StopRecognision();
            }
        }

        private void button_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            storage.Clear();
            ResetDisplay();
            Result.Clear();
        }

        private void textBox_ImagesDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRecogniseButton();
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
