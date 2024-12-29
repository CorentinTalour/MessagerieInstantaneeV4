using MessagerieInstantaneeV4.Objects;

namespace MessagerieInstantaneeV4.Pages;

public partial class Home
{
    private bool _isLoading;
    private List<Message> _receivedMessages;
    private List<Message> _senderMessages;
    private List<Message> _sortedMessages;
    private List<User> _users;
    private string _newMessage = string.Empty;

    // Méthode OnInitializedAsync pour récupérer les utilisateurs
    protected override async Task OnInitializedAsync()
    {
        _isLoading = true; // Afficher un indicateur de chargement
        _users = await MessagerieService.GetUsers(); // Appeler le service pour récupérer les utilisateurs
        _senderMessages = await MessagerieService.RecevoirMessage("MwxjPF4KW1eikS2IS7J5");
        _receivedMessages = await MessagerieService.RecevoirMessage("fAgNpa3azo0UnP4kgBkH");
        CompilationMessage();
        _isLoading = false; // Fin du chargement
    }

    private async Task SendMessage()
    {
        if (_newMessage != string.Empty)
        {
            // Créer une instance de Message avec des valeurs appropriées
            var message = new Message
            {
                IDRECEIVER = "fAgNpa3azo0UnP4kgBkH",
                IDSENDER = "MwxjPF4KW1eikS2IS7J5",
                MESSAGE = _newMessage,
                TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            try
            {
                // Appeler la méthode SendMessage pour envoyer le message
                await MessagerieService.SendMessage(message);
                Console.WriteLine("Message envoyé avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite lors de l'envoi du message : {ex.Message}");
            }
        }
    }

    private void CompilationMessage()
    {
        // Fusionner les deux listes et ajouter l'origine
        var mergeMessages = _senderMessages
            .Select(m => new Message
            {
                IDRECEIVER = m.IDRECEIVER,
                IDSENDER = m.IDSENDER,
                MESSAGE = m.MESSAGE,
                TIME = m.TIME,
                SourceList = ListType.SenderMessages // Marquer comme provenant de la première liste
            })
            .Concat(_receivedMessages
                .Select(m => new Message
                {
                    IDRECEIVER = m.IDRECEIVER,
                    IDSENDER = m.IDSENDER,
                    MESSAGE = m.MESSAGE,
                    TIME = m.TIME,
                    SourceList = ListType.ReceivedMessages // Marquer comme provenant de la deuxième liste
                }))
            .ToList();

        // Trier par date la plus ancienne (TIME -> converti en DateTime)
        _sortedMessages = mergeMessages
            .OrderBy(m => DateTime.Parse(m.TIME)) // Trier par TIME converti en DateTime
            .ToList();
    }
}