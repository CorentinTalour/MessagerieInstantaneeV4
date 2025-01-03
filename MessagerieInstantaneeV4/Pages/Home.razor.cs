using Google.Cloud.Firestore;
using MessagerieInstantaneeV4.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MessagerieInstantaneeV4.Pages;

public partial class Home
{
    private static Timer _messageRetrievalTimer; // Timer pour récupérer les nouveaux messages
    private static FirestoreDb _firestoreDb;
    private bool _isLoading;
    private string _newMessage = string.Empty;
    private List<Message> _receivedMessages;
    private List<Message> _senderMessages;
    private List<Message> _sortedMessages;
    private List<User> _users;

    private List<string> messages = new();

    [Inject] private IJSRuntime JS { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        // Appeler le JavaScript pour activer la persistance hors ligne
        //await JsRuntime2.InvokeVoidAsync("enableFirestorePersistence");

        _users = await MessagerieService.GetUsers();
        _senderMessages = await MessagerieService.RecevoirMessage("MwxjPF4KW1eikS2IS7J5");
        _receivedMessages = await MessagerieService.RecevoirMessage("fAgNpa3azo0UnP4kgBkH");
        CompilationMessage();

        _isLoading = false;

        // Démarrer le Timer pour récupérer les nouveaux messages toutes les 2 secondes
        // _messageRetrievalTimer = new Timer(async _ =>
        // {
        //     Console.WriteLine("Synchoronisation des messages...");
        //     await synchroMessage();
        //     Console.WriteLine("Synchronisation terminée.");
        // }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // Démarre immédiatement, puis se répète toutes les 2 secondes
    }

    private async Task SendMessage()
    {
        if (_newMessage != string.Empty)
        {
            var message = new Message
            {
                IDRECEIVER = "fAgNpa3azo0UnP4kgBkH",
                IDSENDER = "MwxjPF4KW1eikS2IS7J5",
                MESSAGE = _newMessage,
                TIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            try
            {
                await MessagerieService.SendMessage(message);
                Console.WriteLine("Message envoyé avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite lors de l'envoi du message : {ex.Message}");
            }
        }
    }

    private async Task<bool> IsInternetAvailable()
    {
        try
        {
            return await JS.InvokeAsync<bool>("isInternetAvailable");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la vérification de la connexion : {ex.Message}");
            return false;
        }
    }

    private async Task WaitForInternet()
    {
        while (!await IsInternetAvailable())
        {
            Console.WriteLine("En attente de la reconnexion Internet...");
            await Task.Delay(2000); // Attendre 2 secondes avant de vérifier à nouveau
        }

        Console.WriteLine("Connexion Internet rétablie !");
    }

    private async Task synchroMessage()
    {
        Console.WriteLine("Synchronisation des messages...");

        // Vérification de la connexion Internet
        if (!await IsInternetAvailable())
        {
            Console.WriteLine("Aucune connexion Internet. Tentatives de reconnection...");
            await WaitForInternet();
        }

        try
        {
            // Tentative de récupération des messages
            _senderMessages = await MessagerieService.RecevoirMessage("MwxjPF4KW1eikS2IS7J5");
            _receivedMessages = await MessagerieService.RecevoirMessage("fAgNpa3azo0UnP4kgBkH");
            CompilationMessage();
            Console.WriteLine("Synchronisation terminée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des messages: {ex.Message}");
        }

        StateHasChanged(); // Mettre à jour l'interface utilisateur
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