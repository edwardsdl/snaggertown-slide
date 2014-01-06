namespace SAJ.SnaggerTown.Hardware.Slide
{
	/// <summary>Represents a Snagger's record returned from the SnaggerTown API</summary>
	public class Snagger
	{
		#region Constructors and Destructors

		/// <summary>Initializes a new instance of the <see cref="Snagger"/> class</summary>
		/// <param name="collectionResponseItem">A collection response item returned from the SnaggerTown API</param>
		public Snagger(string collectionResponseItem)
		{
			var fields = collectionResponseItem.Split('|');
			SnaggerId = int.Parse(fields[0]);
			Name = fields[1];
			Rfid = int.Parse(fields[2]);
			XboxGamertag = fields[3];
		}

		#endregion

		#region Public Properties

		/// <summary>Gets or sets the name of the Snagger</summary>
		public string Name { get; set; }

		/// <summary>Gets or sets the Snagger's RFID card number</summary>
		public int Rfid { get; set; }

		/// <summary>Gets or sets the Snagger's ID</summary>
		public int SnaggerId { get; set; }

		/// <summary>Gets or sets the Snagger's Xbox gamer tag</summary>
		public string XboxGamertag { get; set; }

		#endregion
	}
}