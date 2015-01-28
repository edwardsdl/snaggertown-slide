using System;
using System.IO.Ports;
using System.Threading;
using AngrySquirrel.Netduino.HidDecoder;
using AngrySquirrel.Netduino.NtpClient;
using AngrySquirrel.Netduino.ProximitySensor;
using AngrySquirrel.Netduino.RestClient;
using AngrySquirrel.Netduino.SerialLcd;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace SAJ.SnaggerTown.Hardware.Slide
{
	/// <summary>Calculates and records the velocity of Snaggers as they travel down the slide</summary>
	public class Program
	{
		#region Properties
        
        private const readonly string Api = "devsnaggertown";
        
        private const readonly int ApiPort = 8081;

		/// <summary>Gets or sets the lower proximity sensor</summary>
		private static ProximitySensor LowerProximitySensor { get; set; }

		/// <summary>Gets or sets the Serial LCD</summary>
		private static ISerialLcd SerialLcd { get; set; }

		/// <summary>Gets or sets the slide run</summary>
		private static SlideRun SlideRun { get; set; }

		/// <summary>Gets or sets the upper proximity sensor</summary>
		private static ProximitySensor UpperProximitySensor { get; set; }

		#endregion

		#region Public Methods and Operators

		/// <summary>Program entry point</summary>
		public static void Main()
		{
			SlideRun = new SlideRun();

			InitializeSerialLcd();
			InitializeClock();
			InitializeHidDecoder();
			InitializeLowerProximitySensor();
			InitializeUpperProximitySensor();

			DisplayMessage("Ready");

			Thread.Sleep(Timeout.Infinite);
		}

		#endregion

		#region Methods

		/// <summary>Calculates the average velocity of the Snagger as they travelled down the slide</summary>
		/// <param name="timeInMs">The amount of time in milliseconds it took for the Snagger to reach the bottom of the slide</param>
		/// <returns>The average velocity of the Snagger as they travelled down the slide in miles per hour</returns>
		private static double CalculateVelocity(int timeInMs)
		{
			const double SlideLengthInFeet = 31;
			const double FeetPerMile = 5280;
			const double Miles = SlideLengthInFeet / FeetPerMile;

			const double MillisecondsPerHour = 3600000;
			var hour = timeInMs / MillisecondsPerHour;

			return Miles / hour;
		}

		/// <summary>Displays a message indicating a module is initializing</summary>
		/// <param name="module">The module which is initializing</param>
		private static void DisplayInitializationMessage(string module)
		{
			DisplayMessage("- Initializing -", module);
		}

		/// <summary>Displays a message on the LCD</summary>
		/// <param name="messageFirstLine">The message to be displayed on the first line of the LCD</param>
		/// <param name="messageSecondLine">The message to be displayed on the second line of the LCD</param>
		private static void DisplayMessage(string messageFirstLine = "", string messageSecondLine = "")
		{
			if (SerialLcd != null)
			{
				SerialLcd.Clear();
				SerialLcd.Write(messageFirstLine);
				SerialLcd.Write(0, 1, messageSecondLine);
			}
		}

		private static void InitializeClock()
		{
			DisplayInitializationMessage("Clock");

			try
			{
				var dateTime = NtpClient.GetDateTime();
				Utility.SetLocalTime(dateTime.AddHours(-5));
			}
			catch
			{
				DisplayMessage("Failed to", "initialize clock!");
			}
		}

		/// <summary>Initializes the HID RFID decoder</summary>
		private static void InitializeHidDecoder()
		{
			DisplayInitializationMessage("HID Decoder");

			var data0 = new InterruptPort(Pins.GPIO_PIN_D2, false, ResistorModes.Disabled, InterruptModes.InterruptEdgeLow);
			var data1 = new InterruptPort(Pins.GPIO_PIN_D3, false, ResistorModes.Disabled, InterruptModes.InterruptEdgeLow);

			var hidDecoder = new HidDecoder(new HidDecoderParams(data0, data1));
			hidDecoder.CardDecoded += OnCardDecoded;
		}

		/// <summary>Initializes the lower proximity sensor</summary>
		private static void InitializeLowerProximitySensor()
		{
			DisplayInitializationMessage("Lower Sensor");

			var lowerProximitySensorInput = new AnalogInput(Cpu.AnalogChannel.ANALOG_1);

			LowerProximitySensor = new ProximitySensor(lowerProximitySensorInput)
				{
					IsEnabled = true, 
					ObjectDetectionTrigger =
						(maximumReadableDistance, distance) => distance < maximumReadableDistance, 
				};
			LowerProximitySensor.ObjectDetected += OnObjectDetected;
		}

		/// <summary>Initializes the serial LCD</summary>
		private static void InitializeSerialLcd()
		{
			var serialLcdOutput = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One);
			serialLcdOutput.Open();

			SerialLcd = new SerialLcd(serialLcdOutput);
			SerialLcd.Clear();
		}

		/// <summary>Initializes the upper proximity sensor</summary>
		private static void InitializeUpperProximitySensor()
		{
			DisplayInitializationMessage("Upper Sensor");

			var upperProximitySensorInput = new AnalogInput(Cpu.AnalogChannel.ANALOG_0);

			UpperProximitySensor = new ProximitySensor(upperProximitySensorInput)
				{
					IsEnabled = true, 
					ObjectDetectionTrigger =
						(maximumReadableDistance, distance) => distance < maximumReadableDistance, 
				};
			UpperProximitySensor.ObjectDetected += OnObjectDetected;
		}

		/// <summary>Handles the <see cref="HidDecoder.CardDecoded"/> event</summary>
		/// <param name="sender">The sender of the <see cref="HidDecoder.CardDecoded"/> event</param>
		/// <param name="cardDecodedEventArgs">The event arguments containing decoded card data information</param>
		private static void OnCardDecoded(object sender, CardDecodedEventArgs cardDecodedEventArgs)
		{
            SlideRun.SnaggerId = cardDecodedEventArgs.CardData.CardNumber;
			DisplayMessage("Ready to Slide!");
        }

		/// <summary>Handles the <see cref="ProximitySensor.ObjectDetected"/> event</summary>
		/// <param name="sender">The sender of the <see cref="ProximitySensor.ObjectDetected"/> event</param>
		/// <param name="objectDetectedEventArgs">The event arguments containing distance information about a detected object</param>
		private static void OnObjectDetected(object sender, ObjectDetectedEventArgs objectDetectedEventArgs)
		{
			if (ShouldRecordUpperSensorEvent(sender))
			{
				DisplayMessage("-   Recorded   -", "Upper sensor");
				SlideRun.OccurredOn = objectDetectedEventArgs.DateTime;
			}

			if (ShouldRecordLowerSensorEvent(sender))
			{
				DisplayMessage("-   Recorded   -", "Lower sensor");

				SlideRun.TimeInMs = objectDetectedEventArgs.DateTime.Subtract(SlideRun.OccurredOn)
					.Milliseconds;

				var restClient = new RestClient(Api, ApiPort)
					{
						AcceptHeader = "text/plain", 
						ContentTypeHeader = "application/json"
					};
				restClient.Post("/slideruns", SlideRun.ToPostRequestContent());

				SlideRun = new SlideRun();

				Thread.Sleep(500);
				DisplayMessage("-   Duration   -", SlideRun.TimeInMs.ToString());

				Thread.Sleep(500);
				DisplayMessage("-   Velocity   -", CalculateVelocity(SlideRun.TimeInMs) + " mph");
			}
		}

		/// <summary>Determines whether a lower sensor event should be recorded</summary>
		/// <param name="sender">The sender of the <see cref="ProximitySensor.ObjectDetected"/> event</param>
		/// <returns>A value indicating whether the sensor event should be recorded</returns>
		private static bool ShouldRecordLowerSensorEvent(object sender)
		{
			var isLowerSensorEvent = sender == LowerProximitySensor;
			var haveRecordedUpperSensorEvent = SlideRun.OccurredOn != default(DateTime);
			var isDuplicateLowerSensorEvent = SlideRun.TimeInMs != 0;

			return isLowerSensorEvent && haveRecordedUpperSensorEvent && !isDuplicateLowerSensorEvent;
		}

		/// <summary>Determines whether an upper sensor event should be recorded</summary>
		/// <param name="sender">The sender of the <see cref="ProximitySensor.ObjectDetected"/> event</param>
		/// <returns>A value indicating whether the sensor event should be recorded</returns>
		private static bool ShouldRecordUpperSensorEvent(object sender)
		{
			return sender == UpperProximitySensor;
		}

		#endregion
	}
}