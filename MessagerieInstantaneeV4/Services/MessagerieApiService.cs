using System.Net.Http.Json;
using MessagerieInstantaneeV4.Objects;

namespace MessagerieInstantaneeV4.Services;

public class MessagerieApiService
{
    #region Fields

    private readonly HttpClient _httpClient;

    #endregion

    #region Constructors

    public MessagerieApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #endregion

    #region Methods

    public async Task<List<User>> GetUsers()
    {
        try
        {
            // Utilisation de HttpClient pour une requête GET
            var users = await _httpClient.GetFromJsonAsync<List<User>>("User");

            if (users == null)
                throw new Exception("Impossible d'obtenir la liste des utilisateurs depuis l'API.");

            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            throw;
        }
    }

    public async Task SendMessage(Message message)
    {
        try
        {
            // Utilisation de HttpClient pour une requête POST
            var response = await _httpClient.PostAsJsonAsync("Message/send", message);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            throw;
        }
    }

    public async Task<List<Message>> RecevoirMessage(string id)
    {
        try
        {
            if (id == null)
                throw new Exception("Impossible d'obtenir la liste des messages depuis l'API. L'ID est nul.");

            // Appel API pour récupérer les messages en spécifiant explicitement le type List<Message>
            var response = await _httpClient.GetFromJsonAsync<List<Message>>($"Message/ReceiveFrom/{id}");

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            throw;
        }
    }

    public async Task SendLogs(Log log)
    {
        try
        {
            // Utilisation de HttpClient pour une requête POST
            var response = await _httpClient.PostAsJsonAsync("Log", log);

            response.EnsureSuccessStatusCode();

            //return await response.Content.ReadFromJsonAsync<List<Log>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            throw;
        }
    }

    #endregion
}