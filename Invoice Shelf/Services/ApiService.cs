using InvoiceShelf.Constants;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Models.Admin.Authentication;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Version = InvoiceShelf.Models.Misc.Version;

namespace InvoiceShelf.Services;

/// <summary>Résultat d'un test de connexion serveur, avec diagnostic précis.</summary>
public record ConnectionTestResult(bool Success, int StatusCode, string? Error, Version? Version);

public class ApiService
{
    // ── Sérialisation JSON ─────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Client HTTP ────────────────────────────────────────────────────────

    private readonly HttpClient _http;

    // ── Cache disque centralisé ────────────────────────────────────────────
    // Tous les résultats de requêtes GET sont mis en cache ici (voir GetCached
    // et GetAllPagesCached) ; toute mutation réussie invalide l'ensemble.
    private readonly ICacheService _cache;

    public ApiService(ICacheService cacheService)
    {
        _cache = cacheService;

        var handler = new SocketsHttpHandler
        {
            ConnectTimeout            = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime  = TimeSpan.FromMinutes(5),
            // Tente l'IPv4 avant l'IPv6 pour éviter les blocages sur les réseaux
            // dont l'IPv6 est annoncé mais cassé (cause n°1 des timeouts mobiles).
            ConnectCallback           = HappyEyeballsConnectAsync
        };

        _http = new HttpClient(handler)
        {
            // Le timeout global est géré par requête via CancellationToken (voir Send<T>),
            // ce qui permet de le modifier à chaud sans recréer le client.
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Établit la connexion TCP en essayant les adresses IPv4 avant les IPv6,
    /// avec un délai court par adresse. Le TLS est ensuite géré par le handler.
    /// Évite les longs timeouts quand une adresse IPv6 est injoignable.
    /// </summary>
    private static async ValueTask<Stream> HappyEyeballsConnectAsync(
        SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        string host = context.DnsEndPoint.Host;
        int    port = context.DnsEndPoint.Port;

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

        // IPv4 (InterNetwork) d'abord, puis IPv6 (InterNetworkV6)
        IEnumerable<IPAddress> ordered = addresses
            .OrderBy(a => a.AddressFamily == AddressFamily.InterNetworkV6 ? 1 : 0);

        Exception? lastError = null;

        foreach (IPAddress address in ordered)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                using var perAddressCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perAddressCts.CancelAfter(TimeSpan.FromSeconds(3)); // Réduit à 3s pour un basculement plus rapide en Release


                await socket.ConnectAsync(new IPEndPoint(address, port), perAddressCts.Token);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex)
            {
                // Log the failure of the specific IP to help debug issues like "Happy Eyeballs" in production
                System.Diagnostics.Debug.WriteLine($"[ApiService] Connection failed for {address} ({address.AddressFamily}): {ex.Message}");
                lastError = ex;
                socket.Dispose();
            }
        }

        throw lastError
            ?? new SocketException((int)SocketError.HostNotFound);
    }

    // ── Configuration ──────────────────────────────────────────────────────

    /// <summary>Timeout appliqué à toutes les requêtes HTTP. Défaut : 30 secondes.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>ID de la société InvoiceShelf (header "company"). Défaut : 1.</summary>
    public int CompanyId { get; set; } = 1;

    private string _baseUrl = string.Empty;

    public void SetBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public string GetBaseUrl() => _baseUrl;

    // ── Cœur : envoi + désérialisation ─────────────────────────────────────

    private readonly record struct ApiResponse<T>(int StatusCode, T? Value, string? Error)
    {
        public bool IsSuccess => StatusCode is >= 200 and < 300;
    }

    private async Task<ApiResponse<T>> Send<T>(
        HttpMethod method,
        string path,
        object? body = null,
        bool authenticated = false)
    {
        using var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (authenticated)
        {
            string? token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Aucun token trouvé. Veuillez vous reconnecter.");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // InvoiceShelf multi-sociétés : header "company" requis sur les endpoints authentifiés.
            request.Headers.Add("company", CompanyId.ToString());
        }

        if (body is not null)
        {
            string json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var cts = new CancellationTokenSource(Timeout);

        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, cts.Token);
            int code = (int)response.StatusCode;

            string content = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
                return new ApiResponse<T>(code, default, content);

            // Toute mutation réussie (POST/PUT/DELETE...) rend le cache GET
            // potentiellement obsolète : on invalide tout le cache disque pour
            // garantir la cohérence des listes et fiches au prochain affichage.
            if (method != HttpMethod.Get)
                await _cache.ClearAllAsync();

            if (string.IsNullOrWhiteSpace(content))
                return new ApiResponse<T>(code, default, null);

            try
            {
                T? value = JsonSerializer.Deserialize<T>(content, JsonOptions);
                return new ApiResponse<T>(code, value, null);
            }
            catch (JsonException ex)
            {
                return new ApiResponse<T>(code, default, $"Réponse JSON invalide : {ex.Message}");
            }
        }
        catch (TaskCanceledException) when (cts.IsCancellationRequested)
        {
            // 408 = le timeout configuré a été atteint
            return new ApiResponse<T>(408, default, $"Délai dépassé ({Timeout.TotalSeconds}s).");
        }
        catch (HttpRequestException ex)
        {
            // 0 = pas de réponse réseau (DNS, TLS, hôte injoignable, IPv6…)
            return new ApiResponse<T>(0, default, ex.Message);
        }
    }

    // ── Pagination ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parcourt automatiquement toutes les pages d'un endpoint de liste paginé
    /// (invoices, estimates, expenses, customers, payments...) et agrège les
    /// résultats. L'API InvoiceShelf pagine ces endpoints côté serveur (souvent
    /// 15/25 éléments par page) ; un simple GET sans ceci ne renvoyait que la
    /// première page (ex. dépenses récentes manquantes).
    /// Si la réponse ne contient pas de "meta" (endpoint non paginé), une seule
    /// page est lue, comme avant.
    /// </summary>
    private async Task<(List<TItem> Items, bool Complete)> GetAllPages<TResponse, TItem>(string path)
        where TResponse : IPaginatedResponse<TItem>
    {
        char separator = path.Contains('?') ? '&' : '?';

        // Garde-fou : évite de tirer un nombre déraisonnable de pages en parallèle
        // si le serveur renvoie un "meta" incohérent (last_page qui explose, etc.).
        const int maxPages = 200;

        // Première page : sert aussi à connaître le nombre total de pages via meta.last_page.
        ApiResponse<TResponse> first = await Send<TResponse>(HttpMethod.Get, $"{path}{separator}page=1", authenticated: true);
        if (!first.IsSuccess || first.Value is null)
            return (new List<TItem>(), false); // Échec réseau/HTTP : liste incomplète.

        PaginationMeta? meta = first.Value.Meta;
        if (meta is null || meta.CurrentPage >= meta.LastPage)
            return (new List<TItem>(first.Value.Data), true);

        int lastPage = Math.Min(meta.LastPage, maxPages);
        bool truncatedByGuard = lastPage < meta.LastPage;

        // Pages restantes récupérées en parallèle (limitées à 5 requêtes concurrentes)
        // au lieu d'une boucle séquentielle : sur un gros volume (des dizaines de
        // pages), ça faisait plusieurs dizaines de secondes de chargement.
        List<TItem>?[] pageData = new List<TItem>?[lastPage + 1];
        pageData[1] = first.Value.Data;
        bool allPagesOk = true;

        Console.WriteLine($"[ApiCache] {path} : {lastPage} page(s) à récupérer (1 déjà lue, {lastPage - 1} en parallèle, max 5 concurrentes).");

        using SemaphoreSlim throttle = new SemaphoreSlim(5);

        IEnumerable<Task> tasks = Enumerable.Range(2, lastPage - 1).Select(async page =>
        {
            await throttle.WaitAsync();
            System.Diagnostics.Stopwatch pageSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                ApiResponse<TResponse> res = await Send<TResponse>(HttpMethod.Get, $"{path}{separator}page={page}", authenticated: true);
                pageSw.Stop();
                if (!res.IsSuccess || res.Value is null)
                {
                    Console.WriteLine($"[ApiCache]   page {page} ÉCHEC en {pageSw.ElapsedMilliseconds} ms (HTTP {res.StatusCode} : {res.Error}).");
                    allPagesOk = false;
                    return;
                }
                Console.WriteLine($"[ApiCache]   page {page} OK en {pageSw.ElapsedMilliseconds} ms ({res.Value.Data.Count} élément(s)).");
                pageData[page] = res.Value.Data;
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);

        List<TItem> results = new List<TItem>();
        for (int page = 1; page <= lastPage; page++)
        {
            if (pageData[page] is null)
            {
                allPagesOk = false;
                continue;
            }
            results.AddRange(pageData[page]!);
        }

        return (results, allPagesOk && !truncatedByGuard);
    }

    // ── Cache des requêtes GET ──────────────────────────────────────────────

    /// <summary>Clé de cache dérivée du chemin de la requête GET (unique par endpoint).</summary>
    private static string GetCacheKey(string path) => $"get{path}";

    /// <summary>
    /// Lecture GET avec mise en cache disque systématique du résultat :
    /// - cache frais et forceRefresh=false → renvoyé sans appel réseau ;
    /// - sinon appel réseau, résultat écrit en cache si succès ;
    /// - en cas d'échec réseau, repli sur la dernière valeur connue, même périmée.
    /// </summary>
    private async Task<T?> GetCached<T>(string path, Func<Task<T?>> fetch, bool forceRefresh) where T : class
    {
        string key = GetCacheKey(path);

        if (!forceRefresh)
        {
            CacheResult<T> cached = await _cache.GetAsync<T>(key);
            if (cached.IsFresh && cached.Value is not null)
            {
                Console.WriteLine($"[ApiCache] HIT  {key}");
                return cached.Value;
            }

            Console.WriteLine($"[ApiCache] MISS {key} (HasValue={cached.HasValue}, IsExpired={cached.IsExpired}) → réseau.");
        }
        else
        {
            Console.WriteLine($"[ApiCache] SKIP {key} (forceRefresh) → réseau.");
        }

        T? value = await fetch();
        if (value is not null)
        {
            await _cache.SetAsync(key, value);
            return value;
        }

        CacheResult<T> stale = await _cache.GetAsync<T>(key);
        return stale.CanServeStale ? stale.Value : null;
    }

    /// <summary>Variante de <see cref="GetCached{T}"/> pour les listes paginées.</summary>
    private async Task<List<TItem>> GetAllPagesCached<TResponse, TItem>(string path, bool forceRefresh)
        where TResponse : IPaginatedResponse<TItem>
    {
        string key = GetCacheKey(path);

        if (!forceRefresh)
        {
            CacheResult<List<TItem>> cached = await _cache.GetAsync<List<TItem>>(key);
            if (cached.IsFresh && cached.Value is not null)
            {
                Console.WriteLine($"[ApiCache] HIT  {key} ({cached.Value.Count} élément(s), écrit le {cached.CachedAt}).");
                return cached.Value;
            }

            Console.WriteLine($"[ApiCache] MISS {key} (HasValue={cached.HasValue}, IsExpired={cached.IsExpired}) → réseau paginé.");
        }
        else
        {
            Console.WriteLine($"[ApiCache] SKIP {key} (forceRefresh) → réseau paginé.");
        }

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        (List<TItem> items, bool complete) = await GetAllPages<TResponse, TItem>(path);
        sw.Stop();
        Console.WriteLine($"[ApiCache] Réseau paginé {key} : {items.Count} élément(s), complete={complete}, {sw.ElapsedMilliseconds} ms.");

        if (complete)
        {
            await _cache.SetAsync(key, items);
            return items;
        }

        // Liste incomplète (échec réseau en cours de pagination) : on préfère
        // la dernière liste complète connue, même périmée, si elle existe.
        CacheResult<List<TItem>> stale = await _cache.GetAsync<List<TItem>>(key);
        return stale.CanServeStale && stale.Value is not null ? stale.Value : items;
    }

    // ── Test de connexion (diagnostic) ─────────────────────────────────────

    public async Task<ConnectionTestResult> TestConnection()
    {
        try
        {
            var res = await Send<Version>(HttpMethod.Get, ApiUri.Version);
            bool ok = res.IsSuccess && res.Value is not null;
            return new ConnectionTestResult(ok, res.StatusCode, res.Error, res.Value);
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, 0, ex.Message, null);
        }
    }

    public async Task<Version?> GetVersion() => (await TestConnection()).Version;

    // ── Authentification ────────────────────────────────────────────────────

    public async Task<string?> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new ArgumentException("Les identifiants ne peuvent pas être vides.");

        var body = new LoginRequest { Username = username, Password = password };
        var res  = await Send<LoginAnswer>(HttpMethod.Post, ApiUri.Login, body);

        return res.IsSuccess ? res.Value?.Token : null;
    }

    public async Task Logout()
        => await Send<LogoutStatus>(HttpMethod.Post, ApiUri.Logout, authenticated: true);

    public async Task<bool> CheckToken()
    {
        try
        {
            var res = await Send<JsonElement>(HttpMethod.Get, ApiUri.CheckToken, authenticated: true);
            return res.IsSuccess;
        }
        catch { return false; }
    }

    public async Task<UserProfile?> GetMe(bool forceRefresh = false)
        => await GetCached<UserProfile>(ApiUri.Me, async () =>
        {
            try
            {
                ApiResponse<UserProfileResponse> res = await Send<UserProfileResponse>(HttpMethod.Get, ApiUri.Me, authenticated: true);
                return res.IsSuccess ? res.Value?.Data : null;
            }
            catch { return null; }
        }, forceRefresh);

    // ── Factures ─────────────────────────────────────────────────────────────

    public async Task<List<Invoice>> GetInvoices(bool forceRefresh = false)
        => await GetAllPagesCached<Invoices, Invoice>(ApiUri.AllInvoices, forceRefresh);

    public async Task<Invoice?> GetInvoice(int id, bool forceRefresh = false)
        => await GetCached<Invoice>(ApiUri.Invoice(id), async () =>
        {
            try
            {
                ApiResponse<InvoiceDetail> res = await Send<InvoiceDetail>(HttpMethod.Get, ApiUri.Invoice(id), authenticated: true);
                return res.IsSuccess ? res.Value?.Data : null;
            }
            catch { return null; }
        }, forceRefresh);

    /// <summary>Récupère le prochain numéro de facture auto-généré par le serveur (ex. "INV-000012").</summary>
    public async Task<string?> GetNextInvoiceNumber()
    {
        try
        {
            var res = await Send<NextNumberResponse>(HttpMethod.Get, ApiUri.NextNumber("invoice"), authenticated: true);
            return res.IsSuccess ? res.Value?.NextNumber : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Crée une nouvelle facture (brouillon). Retourne la facture créée si succès,
    /// ou (null, message d'erreur) sinon.
    /// </summary>
    public async Task<(Invoice? Invoice, string? Error)> CreateInvoice(CreateInvoiceRequest request)
    {
        ApiResponse<InvoiceDetail> res = await Send<InvoiceDetail>(HttpMethod.Post, ApiUri.AllInvoices, request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de la création de la facture (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }

    /// <summary>
    /// Met à jour une facture existante (InvoicesRequest côté API accepte le
    /// même payload que la création, y compris "items" qui remplace entièrement
    /// les lignes existantes). Réservé aux factures encore modifiables
    /// (brouillon ici, mais le serveur autorise plus largement selon le réglage
    /// "retrospective_edits" — voir Invoice::allow_edit).
    /// </summary>
    public async Task<(Invoice? Invoice, string? Error)> UpdateInvoice(int id, CreateInvoiceRequest request)
    {
        ApiResponse<InvoiceDetail> res = await Send<InvoiceDetail>(HttpMethod.Put, ApiUri.Invoice(id), request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de la mise à jour de la facture (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }

    // ── Catalogue d'articles ────────────────────────────────────────────────

    /// <summary>Liste les articles du catalogue de la société (pour pré-remplir une ligne de facture).</summary>
    public async Task<List<CatalogItem>> GetCatalogItems(bool forceRefresh = false)
        => await GetAllPagesCached<CatalogItems, CatalogItem>(ApiUri.AllItems, forceRefresh);

    // ── Templates PDF ────────────────────────────────────────────────────────

    /// <summary>Liste les templates PDF disponibles pour les factures (natifs et personnalisés).</summary>
    public async Task<List<InvoiceTemplate>> GetInvoiceTemplates(bool forceRefresh = false)
        => await GetCached<List<InvoiceTemplate>>(ApiUri.InvoiceTemplates, async () =>
        {
            try
            {
                ApiResponse<InvoiceTemplatesResponse> res = await Send<InvoiceTemplatesResponse>(HttpMethod.Get, ApiUri.InvoiceTemplates, authenticated: true);
                return res.IsSuccess ? res.Value?.InvoiceTemplates : null;
            }
            catch { return null; }
        }, forceRefresh) ?? [];

    // ── Champs personnalisés ─────────────────────────────────────────────────

    /// <summary>
    /// Liste les définitions de champs personnalisés configurées côté serveur
    /// pour un type de modèle donné (ex. "Invoice"). Ces champs sont créés par
    /// l'administrateur dans InvoiceShelf (Réglages > Champs personnalisés) ;
    /// l'app ne fait qu'afficher/collecter les valeurs à l'endroit approprié.
    /// </summary>
    public async Task<List<CustomField>> GetCustomFields(string modelType, bool forceRefresh = false)
        => await GetCached<List<CustomField>>(ApiUri.CustomFields(modelType), async () =>
        {
            try
            {
                ApiResponse<CustomFieldsResponse> res = await Send<CustomFieldsResponse>(HttpMethod.Get, ApiUri.CustomFields(modelType), authenticated: true);
                return res.IsSuccess ? res.Value?.Data : null;
            }
            catch { return null; }
        }, forceRefresh) ?? [];

    // ── Devis (Estimates) ──────────────────────────────────────────────────────

    public async Task<List<Estimate>> GetEstimates(bool forceRefresh = false)
        => await GetAllPagesCached<Estimates, Estimate>(ApiUri.AllEstimates, forceRefresh);

    public async Task<Estimate?> GetEstimate(int id, bool forceRefresh = false)
        => await GetCached<Estimate>(ApiUri.Estimate(id), async () =>
        {
            try
            {
                ApiResponse<EstimateDetail> res = await Send<EstimateDetail>(HttpMethod.Get, ApiUri.Estimate(id), authenticated: true);
                return res.IsSuccess ? res.Value?.Data : null;
            }
            catch { return null; }
        }, forceRefresh);

    /// <summary>Récupère le prochain numéro de devis auto-généré par le serveur (ex. "EST-000012").</summary>
    public async Task<string?> GetNextEstimateNumber()
    {
        try
        {
            ApiResponse<NextNumberResponse> res = await Send<NextNumberResponse>(HttpMethod.Get, ApiUri.NextNumber("estimate"), authenticated: true);
            return res.IsSuccess ? res.Value?.NextNumber : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Crée un nouveau devis (brouillon). Retourne le devis créé si succès,
    /// ou (null, message d'erreur) sinon.
    /// </summary>
    public async Task<(Estimate? Estimate, string? Error)> CreateEstimate(CreateEstimateRequest request)
    {
        ApiResponse<EstimateDetail> res = await Send<EstimateDetail>(HttpMethod.Post, ApiUri.AllEstimates, request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de la création du devis (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }

    /// <summary>
    /// Met à jour un devis existant (EstimatesRequest côté API accepte le même
    /// payload que la création, y compris "items" qui remplace entièrement les
    /// lignes existantes). Réservé aux devis encore modifiables (brouillon).
    /// </summary>
    public async Task<(Estimate? Estimate, string? Error)> UpdateEstimate(int id, CreateEstimateRequest request)
    {
        ApiResponse<EstimateDetail> res = await Send<EstimateDetail>(HttpMethod.Put, ApiUri.Estimate(id), request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de la mise à jour du devis (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }

    /// <summary>Liste les templates PDF disponibles pour les devis (natifs et personnalisés).</summary>
    public async Task<List<EstimateTemplate>> GetEstimateTemplates(bool forceRefresh = false)
        => await GetCached<List<EstimateTemplate>>(ApiUri.EstimateTemplates, async () =>
        {
            try
            {
                ApiResponse<EstimateTemplatesResponse> res = await Send<EstimateTemplatesResponse>(HttpMethod.Get, ApiUri.EstimateTemplates, authenticated: true);
                return res.IsSuccess ? res.Value?.EstimateTemplates : null;
            }
            catch { return null; }
        }, forceRefresh) ?? [];

    // ── Clients ──────────────────────────────────────────────────────────────

    public async Task<List<Customer>> GetCustomers(bool forceRefresh = false)
        => await GetAllPagesCached<Customers, Customer>(ApiUri.AllCustomers, forceRefresh);

    public async Task<Customer?> GetCustomer(int id, bool forceRefresh = false)
        => await GetCached<Customer>(ApiUri.Customer(id), async () =>
        {
            try
            {
                ApiResponse<CustomerDetail> res = await Send<CustomerDetail>(HttpMethod.Get, ApiUri.Customer(id), authenticated: true);
                return res.IsSuccess ? res.Value?.Data : null;
            }
            catch { return null; }
        }, forceRefresh);

    // ── Paiements ─────────────────────────────────────────────────────────────

    public async Task<List<Payment>> GetPayments(bool forceRefresh = false)
        => await GetAllPagesCached<Payments, Payment>(ApiUri.AllPayments, forceRefresh);

    /// <summary>Liste les modes de paiement configurés sur la société (ex. "Espèces", "Virement"...).</summary>
    public async Task<List<PaymentMethod>> GetPaymentMethods(bool forceRefresh = false)
        => await GetAllPagesCached<PaymentMethods, PaymentMethod>(ApiUri.AllPaymentMethods, forceRefresh);

    /// <summary>Récupère le prochain numéro de paiement auto-généré par le serveur (ex. "PAY-000012").</summary>
    public async Task<string?> GetNextPaymentNumber()
    {
        try
        {
            var res = await Send<NextNumberResponse>(HttpMethod.Get, ApiUri.NextNumber("payment"), authenticated: true);
            return res.IsSuccess ? res.Value?.NextNumber : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Enregistre un nouveau paiement (ex. depuis la page de détail d'une facture).
    /// Retourne le paiement créé si succès, ou (null, message d'erreur) sinon.
    /// </summary>
    public async Task<(Payment? Payment, string? Error)> CreatePayment(CreatePaymentRequest request)
    {
        ApiResponse<PaymentDetail> res = await Send<PaymentDetail>(HttpMethod.Post, ApiUri.AllPayments, request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de l'enregistrement du paiement (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }

    // ── Dépenses ──────────────────────────────────────────────────────────────

    public async Task<List<Expense>> GetExpenses(bool forceRefresh = false)
        => await GetAllPagesCached<Expenses, Expense>(ApiUri.AllExpenses, forceRefresh);

    /// <summary>Liste les catégories de dépenses configurées sur la société.</summary>
    public async Task<List<ExpenseCategory>> GetExpenseCategories(bool forceRefresh = false)
        => await GetAllPagesCached<ExpenseCategories, ExpenseCategory>(ApiUri.AllExpenseCategories, forceRefresh);

    /// <summary>
    /// Identifiant de la devise de la société (réglage "currency"), requis par
    /// l'API pour créer une dépense. Retourne null si indisponible.
    /// </summary>
    public async Task<int?> GetCompanyCurrencyId(bool forceRefresh = false)
    {
        CompanyCurrencySetting? setting = await GetCached<CompanyCurrencySetting>(ApiUri.CompanySettings("currency"), async () =>
        {
            try
            {
                ApiResponse<CompanyCurrencySetting> res = await Send<CompanyCurrencySetting>(HttpMethod.Get, ApiUri.CompanySettings("currency"), authenticated: true);
                return res.IsSuccess && !string.IsNullOrWhiteSpace(res.Value?.Currency) ? res.Value : null;
            }
            catch { return null; }
        }, forceRefresh);

        if (setting is null || string.IsNullOrWhiteSpace(setting.Currency))
            return null;
        return int.TryParse(setting.Currency, out int currencyId) ? currencyId : null;
    }

    /// <summary>
    /// Crée une nouvelle dépense. Retourne la dépense créée si succès,
    /// ou (null, message d'erreur) sinon.
    /// </summary>
    public async Task<(Expense? Expense, string? Error)> CreateExpense(CreateExpenseRequest request)
    {
        ApiResponse<ExpenseDetail> res = await Send<ExpenseDetail>(HttpMethod.Post, ApiUri.AllExpenses, request, authenticated: true);
        if (!res.IsSuccess)
            return (null, res.Error ?? $"Échec de la création de la dépense (HTTP {res.StatusCode}).");
        if (res.Value?.Data is null)
            return (null, res.Error ?? $"Réponse vide ou invalide du serveur (HTTP {res.StatusCode}).");
        return (res.Value.Data, null);
    }
}
