# SteamGiftTradeBot

A simple background .NET service that automatically accepts **gift trade offers only** via the Steam Web API.

## 📌 Features

* 🔁 Accepts only **gift trades** — trades where the bot **does not give any items**, only receives them;
* 🕒 Checks incoming trade offers at a configurable interval (set in `appsettings.json`);
* 👥 Supports multiple accounts simultaneously.

---

## ⚙️ Requirements

1. **Steam API Key**  
   You can obtain one at: [https://steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)

2. **maFile from Steam Desktop Authenticator (SDA)**

3. **`appsettings.json` configuration**, for example:

```json
{
  "Steam": [
    {
      "ApiKey": "BE811EC3332A0A155867725524C355CF",
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
