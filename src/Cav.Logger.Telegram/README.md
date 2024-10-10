## Логгер в телеграмм
Простейший логгер в телеграм для Microsoft DI. Сборка имеет строгое имя. При отправке большого текста он отправляется файлом. Рекомендуется использовать только в дев/тест/стэйджинг контурах.
 
### Конфигурация
 
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.Hosting.Lifetime": "None"
        },
        "Telegram": {
            "LogLevel": {
                "Default": "Trace"
                },
            "BotToken": "bot_token",
            "ChatId": "chat_id"
        }
    }
}
```
 
Корректно отрабатывается конфигурация "общая" и непосредственно логгера.

## Telegram logger
The simplest telegram logger for Microsoft DI. The assembly has a strong name. When sending large text, it is sent as a file. It is recommended to use only in dev/test/staging circuits.
 
### Configuration
 
```json
{
     "Logging": {
         "LogLevel": {
             "Default": "Information",
             "Microsoft.Hosting.Lifetime": "None"
         },
         "Telegram": {
             "LogLevel": {
                 "Default": "Trace"
                 },
             "BotToken": "bot_token",
             "ChatId": "chat_id"
         }
     }
}
```
 
The "general" configuration and the logger itself are correctly worked out.
