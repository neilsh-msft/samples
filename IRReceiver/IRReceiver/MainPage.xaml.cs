using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Exmple application to read IR television remote signals usign TSOP38238 receiver (active low)
namespace IRReceiver
{
    /// <summary>
    /// Page with graph to show IR receiver output
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private const int GREEN_LED_PIN = 47;
        private const int GPIO16_PIN = 16;

        private bool polled = false;
        private bool polling = false;

        private GpioPin irInput;
        private GpioPin statusLed;
        private Stopwatch stopwatch;
        private DispatcherTimer timer;
        private int graphHeight;
        private int graphWidth;
        private int nsPerTick;

        private int scale = 216000;
        
        private struct Sample
        {
            public long tick;
            public bool value;
        };

        private int current;
        private readonly int numSamples = 500;
        private Sample[] samples;

        public MainPage()
        {
            this.InitializeComponent();
            this.graphHeight = (int)this.PolyLines.ActualHeight;
            this.graphWidth = (int)this.PolyLines.ActualWidth;

            this.WritableSource = new WriteableBitmap(this.graphWidth, this.graphHeight);
            this.DataContext = this;

            this.samples = new Sample[this.numSamples];
            this.current = 0;

            this.stopwatch = Stopwatch.StartNew();
            
            if (this.InitGPIO())
            {
                this.nsPerTick = (int)(1E9 / Stopwatch.Frequency);
                this.Status.Text = String.Format("Clock resolution: {0} nanoseconds", this.nsPerTick);

                this.timer = new DispatcherTimer();
                this.timer.Interval = TimeSpan.FromMilliseconds(500);
                this.timer.Tick += delegate { UpdateBitmap(); };
                this.timer.Start();
            }
        }

        private WriteableBitmap WritableSource { get; set; }

        private bool InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                this.Status.Text = "There is no GPIO controller on this device.";
                return false;
            }

            this.statusLed = gpio.OpenPin(GREEN_LED_PIN);
            this.statusLed.Write(GpioPinValue.Low);
            this.statusLed.SetDriveMode(GpioPinDriveMode.Output);

            this.irInput = gpio.OpenPin(GPIO16_PIN);
            this.irInput.SetDriveMode(GpioPinDriveMode.Input);
//            this.irInput.DebounceTimeout = TimeSpan.FromMilliseconds(0.05);

            this.irInput.ValueChanged += IrInput_ValueChanged;
            if (!this.polled)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }

            this.Status.Text = "GPIO pins initialized correctly.";

            return true;
        }

        private void IrInput_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            this.polling = true;

            try
            {

                if (this.polled)
                {
                    this.irInput.ValueChanged -= IrInput_ValueChanged;
                    this.IrInput_Poll();
                    this.irInput.ValueChanged += IrInput_ValueChanged;
                }
                else
                {
                    // TSOP 38238 is an active low device
                    if (args.Edge == GpioPinEdge.RisingEdge)
                    {
                        this.samples[this.current].tick = this.stopwatch.ElapsedTicks;
                        this.samples[this.current].value = false;
                        statusLed.Write(GpioPinValue.Low);
                    }
                    else
                    {
                        this.samples[this.current].tick = this.stopwatch.ElapsedTicks;
                        this.samples[this.current].value = true;
                        statusLed.Write(GpioPinValue.High);
                    }

                    this.current = (this.current + 1) % this.numSamples;
                }
            }
            finally
            {
                this.polling = false;
            }
        }

        private void IrInput_Poll()
        {
            GpioPinValue lastValue = this.irInput.Read(), currentValue;
            long lastTick = this.stopwatch.ElapsedTicks, currentTick;

            while (true)
            {
                currentValue = this.irInput.Read();
                currentTick = this.stopwatch.ElapsedTicks;

                if (lastValue != currentValue)
                {
                    this.samples[this.current].tick = currentTick;
                    this.samples[this.current].value = currentValue == GpioPinValue.Low; // active low input
                    this.current = (this.current + 1) % this.numSamples;

                    lastValue = currentValue;
                    lastTick = currentTick;
                }

                if (currentValue == GpioPinValue.High && lastValue == GpioPinValue.High &&
                    currentTick - lastTick > (10000000 / this.nsPerTick))
                {
                    // more than 10 ms since last event
                    return;
                }
            }
        }

        private void UpdateBitmap()
        {
            if (this.polling)
            {
                return;
            }

            int current = this.current;

            Sample previous = this.samples[(current - 1 + this.numSamples) % this.numSamples];
            long startTick = previous.tick;

            var points = new PointCollection();
            for (int i = 1; i < this.numSamples; i++)
            {
                Sample s = this.samples[(current - i + this.numSamples) % this.numSamples];
                int y = s.value ? 10 : this.graphHeight - 10;
                long x = ((startTick - previous.tick) * this.nsPerTick) / scale;

                points.Add(new Point(this.graphWidth - x - 1, y));

                x = ((startTick - s.tick) * this.nsPerTick) / scale;

                if (x > this.graphWidth)
                {
                    points.Add(new Point(0, y));
                    break;
                }
                points.Add(new Point(this.graphWidth - x - 1, y));

                previous = s;
            }
            PolyLines.Points = points;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
