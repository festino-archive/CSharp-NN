using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Lab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string RecogniseButton_textBuffer = "Прервать";
        bool recognising = false;

        internal readonly ViewModel viewModel = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();

            viewModel.RecognisionFinished += RecognisingButtonStopAsync;
            viewModel.ResultUpdated += UpdateResultAsync;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
        }

        private void SetRecognisingState(bool isRecognising)
        {
            if (recognising == isRecognising)
                return;
            recognising = !recognising;
            string s = (string)button_RecogniseButton.Content;
            button_RecogniseButton.Content = RecogniseButton_textBuffer;
            RecogniseButton_textBuffer = s;
            
            button_RecogniseButton.IsEnabled = Directory.Exists(textBox_ImagesDir.Text) || recognising;
        }

        private void RecognisingButtonStopAsync()
        {
            Dispatcher.BeginInvoke(new Action<bool>(SetRecognisingState), false);
        }

        private void UpdateResult()
        {
            listBox_ObjectList.ItemsSource = null;
            listBox_ObjectList.ItemsSource = viewModel.Result; // cause exceptions
        }
        private void UpdateResultAsync()
        {
            Dispatcher.BeginInvoke(new Action(UpdateResult));
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
            SetRecognisingState(!recognising);
            if (recognising)
            {
                string fileDir = textBox_ImagesDir.Text;
                Thread thread = new Thread(() =>
                {
                    viewModel.Recognise(fileDir);
                }
                );
                thread.Start();
            }
            else
            {
                viewModel.StopRecognision();
            }
        }

        private void textBox_ImagesDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            button_RecogniseButton.IsEnabled = Directory.Exists(textBox_ImagesDir.Text) || recognising;
        }

        private void listBox_ObjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scrollViewer_ObjectImages.ScrollToVerticalOffset(0);
            wrapPanel_ObjectImages.Children.Clear();
            string selected = ((KeyValuePair<string, List<ImageObject>>) listBox_ObjectList.SelectedItem).Key;
            foreach (ImageObject obj in viewModel.Result[selected])
            {
                Int32Rect rect = new Int32Rect(obj.X1, obj.Y1, obj.X2 - obj.X1, obj.Y2 - obj.Y1);
                CroppedBitmap cropped = new CroppedBitmap(obj.Image, rect);
                Image img = new Image();
                img.Source = cropped; // label filename below
                wrapPanel_ObjectImages.Children.Add(img);
            }
        }
    }
}
