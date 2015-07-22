/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VisualSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VisualSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.WritableSource = new WriteableBitmap(NumSamples, GraphHeight);

            this.DataContext = this;

            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromSeconds(1 / 60.0);
            m_timer.Tick += delegate { Update(); };
            m_timer.Start();
        }

        const int NumSamples = 512;
        const int GraphHeight = 128;

        DispatcherTimer m_timer;

        public WriteableBitmap WritableSource { get; set; }
        public float BitmapUpdateDuration { get; set; }
        public float PolyLineUpdateDuration { get; set; }
        public string BitmapUpdateDurationString { get { return string.Format("Bitmap Update {0:0.000} ms", BitmapUpdateDuration); } }
        public string PolyLineUpdateDurationString { get { return string.Format("Poly Line Update {0:0.000} ms", PolyLineUpdateDuration); } }

        private void Update()
        {
            var measureTime = new Stopwatch();

            m_timer.Stop();
            try
            {
                float[] samples = new float[NumSamples];

                int t0 = Environment.TickCount / 16;
                for (int x = 0; x < NumSamples; ++x)
                {
                    samples[x] = (float)Math.Sin((t0 + x) / 16.0);
                }
                float min = -1;
                float max = 1;
                float scaleToRange = (GraphHeight - 1) / (max - min);

                //
                // bitmap test
                //

                measureTime.Start();
                {
                    var buffer = new byte[NumSamples * GraphHeight * sizeof(int)];

                    int prevY = (int)(-(min + samples[0]) * scaleToRange);
                    for (int x = 1, pX = sizeof(int); x < NumSamples; ++x, pX += sizeof(int))
                    {
                        int y = (int)(-(min + samples[x]) * scaleToRange);

                        int startY, stopY;
                        if (prevY < y)
                        {
                            startY = prevY;
                            stopY = y;
                        }
                        else
                        {
                            startY = y;
                            stopY = prevY;
                        }

                        for (int iy = startY, pY = startY * NumSamples * sizeof(int); iy <= stopY; ++iy, pY += NumSamples * sizeof(int))
                        {
                            buffer[pY + pX + 0] = 0;
                            buffer[pY + pX + 1] = 0;
                            buffer[pY + pX + 2] = 0xFF;
                            buffer[pY + pX + 3] = 0xFF;
                        }

                        prevY = y;
                    }

                    using (Stream stream = WritableSource.PixelBuffer.AsStream())
                    {
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    WritableSource.Invalidate();
                }
                this.BitmapUpdateDuration = measureTime.ElapsedTicks * 1000.0f / Stopwatch.Frequency;

                //
                // poly line test
                //

                measureTime.Restart();
                {
                    var points = new PointCollection();
                    for (int x = 0; x < NumSamples; ++x)
                    {
                        int y = (int)(-(min + samples[x]) * scaleToRange);
                        points.Add(new Point(x, y));
                    }
                    PolyLines.Points = points;
                }
                this.PolyLineUpdateDuration = measureTime.ElapsedTicks * 1000.0f / Stopwatch.Frequency;
            }
            finally
            {
                m_timer.Start();
            }

            RaisePropertyChanged("BitmapUpdateDurationString");
            RaisePropertyChanged("PolyLineUpdateDurationString");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
