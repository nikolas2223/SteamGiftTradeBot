namespace AutoAcceptTrades
{
    using Microsoft.Extensions.Configuration;
    using System.Text.Json;
    using SteamAuthProvider;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Интеграция с функционалом Steam в части обработки запросов на обмен.
    /// Выполняет обработку и автоматическое получение предложений обмена в которых
    /// от аккаунта не требуются вещи (подарочные предложения обмена)
    /// </summary>
    public class SteamClient
    {
        /// <summary>
        /// Клиент для запросов
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Логер стим клиента
        /// </summary>
        private readonly ILogger<SteamClient> _logger;

        /// <summary>
        /// Список аккаунтов стим
        /// </summary>
        private readonly List<SteamAccountConifg> _steamAccountList;

        /// <inheritdoc/>
        public SteamClient(HttpClient httpClient, IConfiguration config, ILogger<SteamClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _steamAccountList = config.GetSection("Steam").Get<List<SteamAccountConifg>>() 
                                ?? throw new KeyNotFoundException("Steam");
        }

        /// <summary>
        /// Выполняет получение (проверку) предложений обмена
        /// </summary>
        /// <returns>Task</returns>
        public async Task CheckAndAcceptGiftTradesAsync()
        {
            var tradeOffersTasks = _steamAccountList.Select(account =>
            {
                var (apiKey, maFilePath) = account;
                return GetAccountTrades(apiKey, maFilePath);
            });

            // Ждём сбора всех трейдов
            var tradeSelectResults = (await Task.WhenAll(tradeOffersTasks))
                .Where(tuple =>
                {
                    var (_, maFilePath) = tuple;
                    return maFilePath is not null;
                });

            if (!tradeSelectResults.Any())
            {
                return;
            }

            var acceptTasks = tradeSelectResults.Select(accountInfo => 
            {
                var(offers, maFilePath) = accountInfo;
                return HandleTradeOffers(offers, maFilePath);
            });

            await Task.WhenAll(acceptTasks);
        }

        /// <summary>
        /// Возвращает информацию о предложениях обмена
        /// </summary>
        /// <param name="apiKey">API Ключ для Steam</param>
        /// <param name="maFilePath">путь до maFile</param>
        /// <returns><see cref="Tuple"> JSON информация о предложениях обмена + путь до maFile аккаунта</returns>
        private async Task<Tuple<JsonElement, string>> GetAccountTrades(string apiKey, string maFilePath)
        {
            var url = $"https://api.steampowered.com/IEconService/GetTradeOffers/v1/?key={apiKey}&get_received_offers=1&active_only=1";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                var offers = json.RootElement
                    .GetProperty("response")
                    .GetProperty("trade_offers_received");

                return new Tuple<JsonElement, string>(offers, maFilePath);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Smth went wrong on getting orders");
            }
            return new Tuple<JsonElement, string>(default, default);
        }

        /// <summary>
        /// Обрабатывает коллекцию предложений обмена
        /// </summary>
        /// <param name="tradeOffers">Предложения обмена</param>
        /// <param name="maFilePath">Путь до maFile</param>
        /// <returns>Task</returns>
        private async Task HandleTradeOffers(JsonElement tradeOffers, string maFilePath)
        {
            foreach (var offer in tradeOffers.EnumerateArray())
            {
                try
                {
                    var tradeId = offer.GetProperty("tradeofferid").GetString();
                    var isOurOffer = offer.GetProperty("is_our_offer").GetBoolean();

                    var itemsToGive = offer.TryGetProperty("items_to_give", out var itemsElement)
                                      ? itemsElement.GetArrayLength()
                                      : 0;

                    if (!string.IsNullOrWhiteSpace(tradeId) && !isOurOffer && itemsToGive == 0)
                    {
                        var loginSecure = SteamTokenProvider.GetSteamLoginSecured(maFilePath);
                        var sessionId = SteamTokenProvider.GetSessionId(maFilePath);

                        _logger.LogInformation("Accepting gift trade {TradeId}", tradeId);
                        await AcceptTrade(tradeId, sessionId, loginSecure);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Smth went wrong while handling trade offer");
                }
            }
        }

        /// <summary>
        /// Приём предложения обмена
        /// </summary>
        /// <param name="tradeOfferId">Id предложения обмена</param>
        /// <param name="sessionId">Id сессии</param>
        /// <param name="steamLoginSecure">secureLogin из информации об авторизации</param>
        /// <returns>Task</returns>
        private async Task AcceptTrade(string tradeOfferId, string sessionId, string steamLoginSecure)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://steamcommunity.com/tradeoffer/{tradeOfferId}/accept");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("sessionid", sessionId),
                new KeyValuePair<string, string>("serverid", "1"),
                new KeyValuePair<string, string>("tradeofferid", tradeOfferId)
            });

            request.Content = content;
            request.Headers.Referrer = new Uri($"https://steamcommunity.com/tradeoffer/{tradeOfferId}/");

            _httpClient.DefaultRequestHeaders.Add("Cookie", $"steamLoginSecure={steamLoginSecure}; sessionid={sessionId};");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Trade {TradeOfferId} accepted successfully.", tradeOfferId);
            }
            else
            {
                _logger.LogInformation("Failed to accept trade {TradeOfferId}: {StatusCode}", tradeOfferId, response.StatusCode);
            }
        }
    }

    /// <summary>
    /// Представляет элемент секции конфига "Steam"
    /// </summary>
    /// <param name="ApiKey">Апи ключ для стима</param>
    /// <param name="MaFilePath">Путь к maFile</param>
    internal record SteamAccountConifg(
        string ApiKey,
        string MaFilePath
    );
}
