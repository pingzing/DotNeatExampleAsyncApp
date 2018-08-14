using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ExampleAsyncApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsButtonEnabled));
            }
        }

        public bool IsButtonEnabled => !IsRunning;


        private DispatcherTimer _counterTimer = new DispatcherTimer();
        private uint _frameCount = 0;
        public MainPage()
        {
            this.InitializeComponent();
            _counterTimer.Interval = TimeSpan.FromMilliseconds(100);
            _counterTimer.Tick += (s, e) =>
            {
                FrameCounterText.Text = $"Frame: {_frameCount}";
                _frameCount++;
            };
            _counterTimer.Start();
        }
        
        //-----Simple blocking example

        private void BlockingButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = true;            
           
            string result = SimulatedSlowHardDriveAccess();
            ResultBlock.Text = result;
            
            IsRunning = false;
        }

        //-----

        //-----Background thread example

        private CoreDispatcher dispatcher = Window.Current.Dispatcher;
        private void BackgroundThreadButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = true;

            Thread backgroundThread = new Thread(
                () => {
                    string result = SimulatedSlowHardDriveAccess();

                    // These operations affect the UI, and therefore, must take place on the UI thread.
                    var dummyVariable = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ResultBlock.Text = result;
                        IsRunning = false;
                    });
                });

            backgroundThread.Start();
        }
        
        //-----

        //------Deadlock example

        private void DeadlockButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = true;

            int returnCode = SomeAsyncMethod().Result;

            IsRunning = false;
        }

        private async Task<int> SomeAsyncMethod()
        {
            await Task.Delay(500); // POW! Deadlock.
            return 0;
        }

        //------

        //-----Correct Async/Await example

        private async void AsyncAwaitButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = true;

            string result = await AccessHardDriveAsync();
            ResultBlock.Text = result;

            IsRunning = false;
        }
        
        //-----

        //-----Remarshaled Call example

        private async void RemarshaledCall(object sender, RoutedEventArgs e)
        {
            IsRunning = true;

            await CallBlockingWork(true);
            await CallBlockingWork(true);
            await CallBlockingWork(true);

            IsRunning = false;
        }

        //-----

        //-----Unmarshaled Call example

        private async void UnmarshaledButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = true;

            await CallBlockingWork(false);
            await CallBlockingWork(false);
            await CallBlockingWork(false);

            IsRunning = false;
        }

        private async Task CallBlockingWork(bool shouldMarshal)
        {
            await DoBlockingWork(shouldMarshal).ConfigureAwait(shouldMarshal);
        }

        private async Task DoBlockingWork(bool shouldMarshal)
        {
            await Task.Delay(1000).ConfigureAwait(shouldMarshal);

            Thread.Sleep(2000);

            await Task.Delay(1000).ConfigureAwait(shouldMarshal);
        }

        //-----

        private async Task<string> AccessHardDriveAsync()
        {
            var result = await Task.Run(() => SimulatedSlowHardDriveAccess());
            return result;
        }

        private string SimulatedSlowHardDriveAccess()
        {
            Thread.Sleep(2500);
            return $"Hard drive says: {Guid.NewGuid()}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
