// Copyright (c) Microsoft. All rights reserved.

using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Blinky
{
    public sealed partial class MainPage : Page
    {
        private const int GREEN_LED_PIN = 47;
        private const int RED_LED_PIN = 35;
        private const int GPIO5_PIN = 5;
        private const int GPIO6_PIN = 6;
        private const int GPIO13_PIN = 13;
        private const int GPIO26_PIN = 26;
        private readonly int[] pindefs = { GPIO5_PIN, GPIO6_PIN, GPIO13_PIN, GPIO26_PIN };

        private int LEDStatus = 0;
        private int counter = 0;

        private GpioPin redPin;
        private GpioPin greenPin;
        private GpioPin[] pins;

        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            if (InitGPIO())
            {
                timer.Start();
            }
        }

        private bool InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return false;
            }

            this.redPin = gpio.OpenPin(RED_LED_PIN);
            this.greenPin = gpio.OpenPin(GREEN_LED_PIN);

            this.greenPin.Write(GpioPinValue.High);
            this.redPin.Write(GpioPinValue.Low);
            this.redPin.SetDriveMode(GpioPinDriveMode.Output);
            this.greenPin.SetDriveMode(GpioPinDriveMode.Output);

            // initialize the output led pins to default to high
            this.pins = new GpioPin[this.pindefs.Length];

            for (int i = 0; i < this.pindefs.Length; i++)
            {
                this.pins[i] = gpio.OpenPin(this.pindefs[i]);
                this.pins[i].Write(GpioPinValue.High);
                this.pins[i].SetDriveMode(GpioPinDriveMode.Output);
            }

            GpioStatus.Text = "GPIO pins initialized correctly.";

            return true;
        }

        private void FlipLED()
        {
            if (this.LEDStatus == 0)
            {
                this.LEDStatus = 1;
                if (this.greenPin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    this.greenPin.Write(GpioPinValue.Low);
                }
                if (this.redPin != null)
                {
                    // to turn off the LED, we need to push the pin 'high'
                    this.redPin.Write(GpioPinValue.High);
                }
                LED.Fill = redBrush;
            }
            else
            {
                LEDStatus = 0;
                if (this.greenPin != null)
                {
                    // to turn off the LED, we need to push the pin 'high'
                    this.greenPin.Write(GpioPinValue.High);
                }
                if (this.redPin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    this.redPin.Write(GpioPinValue.Low);
                }
                LED.Fill = grayBrush;
            }

            // turn off current LED, and drive next LED low
            this.pins[counter].Write(GpioPinValue.High);
            this.counter = (this.counter + 1) % this.pins.Length;
            this.pins[counter].Write(GpioPinValue.Low);
        }

        private void TurnOffLED()
        {
            if (LEDStatus == 1)
            {
                FlipLED();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            FlipLED();
        }

        private void Delay_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (timer == null)
            {
                return;
            }
            if (e.NewValue == Delay.Minimum)
            {
                DelayText.Text = "Stopped";
                timer.Stop();
                TurnOffLED();
            }
            else
            {
                DelayText.Text = e.NewValue + "ms";
                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                timer.Start();
            }
        }
    }
}
