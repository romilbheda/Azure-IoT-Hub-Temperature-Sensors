using Sensors.Dht;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TemperatureSensor.Client.Common;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TemperatureSensor.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : BindablePage
    {
        private DispatcherTimer _dispatchTimer;

        private GpioPin _temperaturePin;

        private IDht _dhtInterface;

        private List<int> _retryCount;

        private DateTimeOffset _startedAt;

        public MainPage()
        {
            this.InitializeComponent();

            // call the method to initialize variables and hardware components
            InitHardware();

            // set interval of timer to 1 second
            _dispatchTimer.Interval = TimeSpan.FromSeconds(1);

            // invoke a method at each tick (as per interval of your timer)
            _dispatchTimer.Tick += _dispatchTimer_Tick;

            // initialize pin (GPIO pin on which you have set your temperature sensor)
            _temperaturePin = GpioController.GetDefault().OpenPin(4, GpioSharingMode.Exclusive);

            // create instance of a DHT11 
            _dhtInterface = new Dht11(_temperaturePin, GpioPinDriveMode.Input);

            // start the timer
            _dispatchTimer.Start();

            // set start date time
            _startedAt = DateTimeOffset.Now;
        }

        // method to initialize variables and hardware components
        private void InitHardware()
        {
            _dispatchTimer = new DispatcherTimer();

            _temperaturePin = null;

            _dhtInterface = null;

            _retryCount = new List<int>();

            _startedAt = DateTimeOffset.Parse("1/1/1");
        }

        private async void _dispatchTimer_Tick(object sender, object e)
        {
            try
            {
                DhtReading reading = new DhtReading();

                int val = this.TotalAttempts;

                this.TotalAttempts++;

                reading = await _dhtInterface.GetReadingAsync().AsTask();

                _retryCount.Add(reading.RetryCount);
                this.OnPropertyChanged(nameof(AverageRetriesDisplay));
                this.OnPropertyChanged(nameof(TotalAttempts));
                this.OnPropertyChanged(nameof(PercentSuccess));

                if (reading.IsValid) // if we are able to capture value, display those
                {
                    this.TotalSuccess++;
                    this.Temperature = Convert.ToSingle(reading.Temperature);
                    this.Humidity = Convert.ToSingle(reading.Humidity);
                    this.LastUpdated = DateTimeOffset.Now;
                    this.OnPropertyChanged(nameof(SuccessRate));
                }
                else // log if the reading is not in valid state
                {
                    Debug.WriteLine(string.Format("IsValid: {0}, RetryCount: {1}, TimedOut: {2}, Humidity: {3}, Temperature: {4}", reading.IsValid, reading.RetryCount, reading.TimedOut, reading.Humidity, reading.Temperature));
                }

                this.OnPropertyChanged(nameof(LastUpdatedDisplay)); // show when the data was last updated
                this.OnPropertyChanged(nameof(DateTimeDisplay));
            }
            catch(Exception ex) // log any exception that occurs
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #region application properties

        public string PercentSuccess
        {
            get
            {
                string returnValue = string.Empty;

                int attempts = this.TotalAttempts;

                if (attempts > 0)
                {
                    returnValue = string.Format("{0:0.0}%", 100f * (float)this.TotalSuccess / (float)attempts);
                }
                else
                {
                    returnValue = "0.0%";
                }

                return returnValue;
            }
        }

        private int _totalAttempts = 0;

        public int TotalAttempts
        {
            get
            {
                return _totalAttempts;
            }
            set
            {
                this.SetProperty(ref _totalAttempts, value);
                this.OnPropertyChanged(nameof(PercentSuccess));
            }
        }

        private int _totalSuccess = 0;

        public int TotalSuccess
        {
            get
            {
                return _totalSuccess;
            }
            set
            {
                this.SetProperty(ref _totalSuccess, value);
                this.OnPropertyChanged(nameof(PercentSuccess));
            }
        }

        private float _humidity = 0f;

        public float Humidity
        {
            get
            {
                return _humidity;
            }

            set
            {
                this.SetProperty(ref _humidity, value);
                this.OnPropertyChanged(nameof(HumidityDisplay));
            }
        }

        public string HumidityDisplay
        {
            get
            {
                return string.Format("{0:0.0}% RH", this.Humidity);
            }
        }

        private float _temperature = 0f;

        public float Temperature
        {
            get
            {
                return _temperature;
            }
            set
            {
                SetProperty(ref _temperature, value);
                this.OnPropertyChanged(nameof(TemperatureDisplay));
            }
        }

        public string TemperatureDisplay
        {
            get
            {
                return string.Format("{0:0.0} °C", this.Temperature);
            }
        }

        private DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;

        public DateTimeOffset LastUpdated
        {
            get
            {
                return _lastUpdated;
            }
            set
            {
                this.SetProperty(ref _lastUpdated, value);
                this.OnPropertyChanged(nameof(LastUpdatedDisplay));

            }
        }

        private DateTime _dateTime = DateTime.Now;

        public DateTime DateTimeUpdate
        {
            get
            {
                return _dateTime;
            }
            set
            {
                this.SetProperty(ref _dateTime, value);
                this.OnPropertyChanged(nameof(DateTimeDisplay));
            }
        }

        public string LastUpdatedDisplay
        {
            get
            {
                string returnValue = string.Empty;

                TimeSpan elapsed = DateTimeOffset.Now.Subtract(this.LastUpdated);

                if (this.LastUpdated == DateTimeOffset.MinValue)
                {
                    returnValue = "never";
                }
                else if (elapsed.TotalSeconds < 60d)
                {
                    int seconds = (int)elapsed.TotalSeconds;

                    if (seconds < 2)
                    {
                        returnValue = "just now";
                    }
                    else
                    {
                        returnValue = string.Format("{0:0} {1} ago", seconds, seconds == 1 ? "second" : "seconds");
                    }
                }
                else if (elapsed.TotalMinutes < 60d)
                {
                    int minutes = (int)elapsed.TotalMinutes == 0 ? 1 : (int)elapsed.TotalMinutes;
                    returnValue = string.Format("{0:0} {1} ago", minutes, minutes == 1 ? "minute" : "minutes");
                }
                else if (elapsed.TotalHours < 24d)
                {
                    int hours = (int)elapsed.TotalHours == 0 ? 1 : (int)elapsed.TotalHours;
                    returnValue = string.Format("{0:0} {1} ago", hours, hours == 1 ? "hour" : "hours");
                }
                else
                {
                    returnValue = "a long time ago";
                }

                return returnValue;
            }
        }

        public string DateTimeDisplay
        {
            get
            {
                string returnValue = string.Empty;

                returnValue = DateTime.Now.ToString();

                return returnValue;
            }
        }

        public int AverageRetries
        {
            get
            {
                int returnValue = 0;

                if (_retryCount.Count() > 0)
                {
                    returnValue = (int)_retryCount.Average();
                }

                return returnValue;
            }
        }

        public string AverageRetriesDisplay
        {
            get
            {
                return string.Format("{0:0}", this.AverageRetries);
            }
        }

        public string SuccessRate
        {
            get
            {
                string returnValue = string.Empty;

                double totalSeconds = DateTimeOffset.Now.Subtract(_startedAt).TotalSeconds;
                double rate = this.TotalSuccess / totalSeconds;

                if (rate < 1)
                {
                    returnValue = string.Format("{0:0.00} seconds/reading", 1d / rate);
                }
                else
                {
                    returnValue = string.Format("{0:0.00} readings/sec", rate);
                }

                return returnValue;
            }
        }

        #endregion
    }
}
