using System;

namespace SAJ.SnaggerTown.Hardware.Slide
{
    /// <summary>
    /// Represents an instance of a Snagger traveling down the slide
    /// </summary>
    internal class SlideRun
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the Snagger's ID
        /// </summary>
        public int SnaggerId { get; set; }

        /// <summary>
        /// Gets or sets the date and time the Snagger tripped the top proximity sensor
        /// </summary>
        public DateTime OccurredOn { get; set; }

        /// <summary>
        /// Gets or sets the time in milliseconds it took for the Snagger to reach the bottom of the slide
        /// </summary>
        public int TimeInMs { get; set; }

        #endregion

        /// <summary>
        /// Generates a JSON encoded string representing the content of the POST request
        /// </summary>
        /// <returns>
        /// A JSON encoded string representing the content of the POST request
        /// </returns>
        public string ToPostRequestContent()
        {
            return "{ \"keyfobNum\": \"" + SnaggerId.ToString() + "\", \"startTimeStamp\": \"" + OccurredOn + "\", \"endTimeStamp\": \"" + OccurredOn.Add(TimeSpan.FromMilliseconds(TimeInMs)) + "\" }";
        }
    }
}