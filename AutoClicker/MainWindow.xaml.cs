using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AutoClicker
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool stateWorking = false;

        ObservableCollection<ObservablePoint> points = new ObservableCollection<ObservablePoint>();
        System.Drawing.Point currentPoint;

        EventWaitHandle eventWait = new EventWaitHandle(false, EventResetMode.ManualReset);
        Timer timer;
        int maxSecond = 0;

        public MainWindow()
        {
            InitializeComponent();

            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.MouseMove += HookManager_MouseMove;

            repeatCount.Text = "1";
            timeSec.Text = "10";
            timeSleepSec.Text = "1";

            timer = new Timer(OnTimerCallback);


            var textHelp = "Ctrl+Alt+F2 - Включение/Выключение\n" +
                "Crl+Alt+F3 - Запись координат\n" +
                "Ctrl+Alt+F4 - Запуск";
            help.Text = textHelp;
        }
        ~MainWindow()
        {
            HookManager.KeyDown -= HookManager_KeyDown;
            HookManager.MouseMove -= HookManager_MouseMove;
        }

        private void HookManager_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            xText.Text = $"X:{e.X}";
            yText.Text = $"Y:{e.Y}";
            currentPoint = e.Location;
        }

        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                if (e.Control && e.Alt && e.KeyCode == System.Windows.Forms.Keys.F2 && !stateWorking)
                {
                    startBtn.Content = "Выключить";
                    logText.Text = "Рабочий режим Вкл\n";
                    stateWorking = true;
                    points.Clear();
                }
                else if (e.Control && e.Alt && e.KeyCode == System.Windows.Forms.Keys.F2 && stateWorking)
                {
                    startBtn.Content = "Включить";
                    logText.Text = "Рабочий режим Выкл\n"+ logText.Text;
                    stateWorking = false;
                    points.Clear();
                }

                if (!stateWorking) return;
                // RECORD POINT
                if (e.Control && e.Alt && e.KeyCode == System.Windows.Forms.Keys.F3)
                {
                    if (currentPoint == null) return;

                    points.Add(new ObservablePoint(currentPoint.X, currentPoint.Y));
                    logText.Text = $"Записаны координаты X({currentPoint.X}):Y({currentPoint.Y})\n"+ logText.Text;
                }

                if (e.Control && e.Alt && e.KeyCode == System.Windows.Forms.Keys.F4)
                {
                    DoWork();
                }
            }catch(Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}","Ошибка выполнения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void On_Close(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private async void DoWork()
        {
            if (string.IsNullOrWhiteSpace(repeatCount.Text) || !int.TryParse(repeatCount.Text, out int maxCount)) return;

            await Task.Run(() =>
            {
                logText.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { logText.Text = $"Выполнение...\n"+ logText.Text; }));
                string temp = "1";
                repeatCount.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { temp = repeatCount.Text; }));

                while (stateWorking)
                {
                    string repeatCountText = "";
                    string timeSecText = "";
                    string timeSleepSecText = "";

                    repeatCount.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { repeatCountText = repeatCount.Text; }));
                    timeSec.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { timeSecText = timeSec.Text; }));
                    timeSleepSec.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { timeSleepSecText = timeSleepSec.Text; }));

                    if (!string.IsNullOrWhiteSpace(repeatCountText) && int.TryParse(repeatCountText, out int count))
                    {
                        if (--count < 0) break;

                        logText.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { logText.Text = $"Попытка {maxCount - count} из {maxCount}\n"+ logText.Text; }));

                        for (int i = 0; i < points.Count; ++i)
                        {
                            if (!stateWorking) break;

                            var pos = points[i];
                            logText.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { logText.Text = $"{i+1}/{points.Count}\n" +
                                $"---Перемещаем мышь X({pos.X}):Y({pos.Y}) и кликаем\n"+ logText.Text; }));
                            WinAPI.SetCursorPos((int)pos.X, (int)pos.Y);
                            WinAPI.mouse_event((int)(WinAPI.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
                            WinAPI.mouse_event((int)(WinAPI.MouseEventFlags.LEFTUP), 0, 0, 0, 0);

                            if (!string.IsNullOrWhiteSpace(timeSleepSecText) && int.TryParse(timeSleepSecText, out int sleepSecond))
                            {
                                if (i < points.Count - 1) Thread.Sleep(TimeSpan.FromSeconds(sleepSecond));
                            }
                            else if (i < points.Count - 1) Thread.Sleep(TimeSpan.FromSeconds(1));
                        }

                        if (!string.IsNullOrWhiteSpace(timeSecText) && int.TryParse(timeSecText, out int second))
                        {
                            if (count > 0)
                            {
                                logText.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { logText.Text = $"Ждем {second} секунд\n" + logText.Text; }));
                                maxSecond = second;
                                timer.Change(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
                                eventWait.Reset();
                                eventWait.WaitOne();
                                //Thread.Sleep(TimeSpan.FromSeconds(second));
                            }
                        }

                        repeatCount.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { repeatCount.Text = $"{count}"; }));
                    }
                    else break;
                }

                repeatCount.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { repeatCount.Text = $"{maxCount}"; }));
                logText.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { logText.Text = $"Выполнено\n"+ logText.Text; }));
            });
        }

        private void OnTimerCallback(object state)
        {
            if (--maxSecond <= 0 || !stateWorking)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { this.Title = "AutoClicker"; }));
                
                eventWait.Set();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { this.Title = $"{maxSecond} сек"; }));
            }
        }

        private void On_Btn(object sender, RoutedEventArgs e)
        {
            if (stateWorking)
            {
                startBtn.Content = "Включить";
                logText.Text = "Рабочий режим Выкл\n" + logText.Text;
                stateWorking = false;
                points.Clear();
            }
            else if (!stateWorking)
            {
                startBtn.Content = "Выключить";
                logText.Text = "Рабочий режим Вкл\n";
                stateWorking = true;
                points.Clear();
            }
        }
    }

    public class WinAPI
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }
    }

    public class ObservablePoint : INotifyPropertyChanged
    {
        private double _x, _y;

        public double X { get { return _x; } set { _x = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X))); } }
        public double Y { get { return _y; } set { _y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y))); } }

        public ObservablePoint()
        {
            X = Y = 0;
        }
        public ObservablePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
