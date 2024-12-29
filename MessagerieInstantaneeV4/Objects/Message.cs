namespace MessagerieInstantaneeV4.Objects;

public class Message
{
    public string IDRECEIVER { get; set; }
    public string IDSENDER { get; set; }
    public string MESSAGE { get; set; }
    public string TIME { get; set; }
    public ListType SourceList { get; set; }
}

public enum ListType
{
    SenderMessages, // Liste 1
    ReceivedMessages // Liste 2
}