namespace Domain.Enums
{
    /// <summary>Where the file behind a loan document lives.</summary>
    public enum DocumentStorage
    {
        /// <summary>Only a reference: the file is hosted elsewhere and the document's FileUrl points to it.</summary>
        External = 0,
        Local = 1,
        Azure = 2
    }
}
