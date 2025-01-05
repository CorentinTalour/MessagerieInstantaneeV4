using System.Text.Json;
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

        _users = await MessagerieService.GetUsers();
        _senderMessages = await MessagerieService.RecevoirMessage("MwxjPF4KW1eikS2IS7J5");
        _receivedMessages = await MessagerieService.RecevoirMessage("fAgNpa3azo0UnP4kgBkH");
        CompilationMessage();

        _isLoading = false;

        // 1. Timer pour vérifier la connexion et envoyer les messages en cache toutes les 5 secondes
        _messageRetrievalTimer = new Timer(async _ =>
        {
            Console.WriteLine("Vérification de la connexion et envoi des messages en cache...");
            await CheckAndSendCachedMessages();
            Console.WriteLine("Vérification terminée.");
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // Démarre immédiatement, puis se répète toutes les 5 secondes

        // 2. Timer pour récupérer les nouveaux messages toutes les 20 secondes
        var messageRetrievalTimer = new Timer(async _ =>
            {
                Console.WriteLine("Synchronisation des messages...");
                await synchroMessage();
                Console.WriteLine("Synchronisation terminée.");
            }, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(20)); // Démarre immédiatement, puis se répète toutes les 20 secondes
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

            var log = new Log
            {
                idButton = "btnSendMessage",
                time = DateTime.UtcNow
            };

            try
            {
                if (await IsInternetAvailable())
                {
                    // Envoyer le message et le log si Internet est disponible
                    await MessagerieService.SendMessage(message);
                    Console.WriteLine("Message envoyé avec succès");

                    await MessagerieService.SendLogs(log);
                    Console.WriteLine("Log envoyé avec succès");
                }
                else
                {
                    // Mettre le message et le log en cache si Internet est indisponible
                    await CacheMessage(message);
                    await CacheLog(log);

                    Console.WriteLine("Message et log mis en cache (hors ligne)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            }
        }
    }

    private async Task CacheMessage(Message message)
    {
        // Récupérer la liste des messages stockés dans localStorage
        var cachedMessagesJson = await JS.InvokeAsync<string>("localStorage.getItem", "pendingMessages");

        var cachedMessages = new List<Message>();

        // Si des messages sont déjà stockés, les désérialiser
        if (!string.IsNullOrEmpty(cachedMessagesJson))
            cachedMessages = JsonSerializer.Deserialize<List<Message>>(cachedMessagesJson);

        // Ajouter le nouveau message à la liste
        cachedMessages.Add(message);

        // Ré-sérialiser la liste et la stocker dans localStorage
        var newCache = JsonSerializer.Serialize(cachedMessages);
        await JS.InvokeVoidAsync("localStorage.setItem", "pendingMessages", newCache);

        Console.WriteLine("Message ajouté au cache.");
    }

    private async Task CacheLog(Log log)
    {
        // Récupérer la liste des logs stockés dans localStorage
        var cachedLogsJson = await JS.InvokeAsync<string>("localStorage.getItem", "pendingLogs");

        var cachedLogs = new List<Log>();

        // Si des logs sont déjà stockés, les désérialiser
        if (!string.IsNullOrEmpty(cachedLogsJson))
            cachedLogs = JsonSerializer.Deserialize<List<Log>>(cachedLogsJson);

        // Ajouter le nouveau log à la liste
        cachedLogs.Add(log);

        // Ré-sérialiser la liste et la stocker dans localStorage
        var newCache = JsonSerializer.Serialize(cachedLogs);
        await JS.InvokeVoidAsync("localStorage.setItem", "pendingLogs", newCache);

        Console.WriteLine("Log ajouté au cache.");
    }

    private async Task CheckAndSendCachedMessages()
    {
        // Récupérer les messages en cache depuis localStorage
        var cachedMessagesJson = await JS.InvokeAsync<string>("localStorage.getItem", "pendingMessages");
        var cachedLogsJson = await JS.InvokeAsync<string>("localStorage.getItem", "pendingLogs");

        var cachedMessages = new List<Message>();
        var cachedLogs = new List<Log>();

        if (!string.IsNullOrEmpty(cachedMessagesJson))
            cachedMessages = JsonSerializer.Deserialize<List<Message>>(cachedMessagesJson);

        if (!string.IsNullOrEmpty(cachedLogsJson))
            cachedLogs = JsonSerializer.Deserialize<List<Log>>(cachedLogsJson);

        // Envoyer les messages en cache
        if (cachedMessages.Any())
        {
            var messagesToSend = new List<Message>(cachedMessages);
            foreach (var cachedMessage in messagesToSend)
                try
                {
                    await MessagerieService.SendMessage(cachedMessage);
                    Console.WriteLine("Message envoyé depuis le cache");

                    cachedMessages.Remove(cachedMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'envoi du message depuis le cache : {ex.Message}");
                }

            // Mettre à jour ou supprimer le cache des messages
            if (cachedMessages.Count > 0)
            {
                var updatedCache = JsonSerializer.Serialize(cachedMessages);
                await JS.InvokeVoidAsync("localStorage.setItem", "pendingMessages", updatedCache);
            }
            else
            {
                await JS.InvokeVoidAsync("localStorage.removeItem", "pendingMessages");
            }
        }

        // Envoyer les logs en cache
        if (cachedLogs.Any())
        {
            var logsToSend = new List<Log>(cachedLogs);
            foreach (var cachedLog in logsToSend)
                try
                {
                    await MessagerieService.SendLogs(cachedLog);
                    Console.WriteLine("Log envoyé depuis le cache");

                    cachedLogs.Remove(cachedLog);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'envoi du log depuis le cache : {ex.Message}");
                }

            // Mettre à jour ou supprimer le cache des logs
            if (cachedLogs.Count > 0)
            {
                var updatedCache = JsonSerializer.Serialize(cachedLogs);
                await JS.InvokeVoidAsync("localStorage.setItem", "pendingLogs", updatedCache);
            }
            else
            {
                await JS.InvokeVoidAsync("localStorage.removeItem", "pendingLogs");
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