# SteamGiftTradeBot
Простой фоновый сервис на .NET, который автоматически принимает только подарочные предложения обмена через Steam Web API.

## 📌 Возможности

* 🔁 Принимает только **подарочные трейды** — то есть обмены, в которых бот **не передаёт никаких предметов**, а получает их;
* 🕒 Проверка входящих предложений происходит с заданным интервалом (настраивается в `appsettings.json`);
* 👥 Поддерживает несколько аккаунтов одновременно.

---

## ⚙️ Требования

1. **Steam API ключ**
   Получить можно по адресу: [https://steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)

2. **maFile от Steam Desktop Authenticator (SDA)**

3. **Конфигурация `appsettings.json`**, например:

```json
{
  "Steam": [
    {
      "ApiKey": "BE811EC3332B0A155867725524C355CF",
      "MaFilePath": "maFiles\\1.maFile"
    },
    {
      "ApiKey": "d10a1144-2770-11f0-b9ca-0c9a3c2d6161",
      "MaFilePath": "maFiles\\2.maFile"
    }
  ],
  "Polling": {
      "IntervalMinutes": 1
  }
}
```
