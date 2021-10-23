using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        internal readonly ViewModel viewModel = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();

            viewModel.RecognisionFinished += RecognisingButtonStopAsync;
            viewModel.ResultUpdated += UpdateResultAsync;
            listBox_ObjectList.ItemsSource = viewModel.Result;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
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

        private void RecognisingButtonStopAsync()
        {
            Dispatcher.BeginInvoke(new Action<RecognisionState>(SetRecognisingState), RecognisionState.READY);
        }

        private void UpdateResult()
        {
            if (viewModel.ImageCount > 0)
                progressBar_RecognisionProgress.Value = viewModel.Result.Count / (double)viewModel.ImageCount;
        }
        private void UpdateResultAsync()
        {
            Dispatcher.BeginInvoke(new Action(UpdateResult));
        }
        private void UpdateRecogniseButton()
        {
            button_RecogniseButton.IsEnabled = Directory.Exists(textBox_ImagesDir.Text) || recognising != RecognisionState.STOPPING;
        }

        private void ResetDisplay()
        {
            progressBar_RecognisionProgress.Value = 0.0;
            scrollViewer_ObjectImages.ScrollToVerticalOffset(0);
            //listBox_ObjectList.ItemsSource = null;
            listBox_ObjectList.SelectedIndex = -1;
            wrapPanel_ObjectImages.ItemsSource = null;
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
                ResetDisplay();
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
                SetRecognisingState(RecognisionState.STOPPING);
                viewModel.StopRecognision();
            }
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
