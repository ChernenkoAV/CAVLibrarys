using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Cav.Logger.Telegram;

/// <summary>
/// Настройки логгера
/// </summary>
public sealed class TelegramLoggerConfiguration
{
    /// <summary>
    /// токен бота
    /// </summary>
    public string? BotToken { get; set; }
    /// <summary>
    /// Ид Чата
    /// </summary>
    public string? ChatId { get; set; }
    /// <summary>
    /// Всовывать эмоджи первым символом сообщения. Включено по умолчанию
    /// </summary>
    public bool UseEmoji { get; set; } = true;

    /// <summary>
    /// Функтор указания отключения уведомления для сообщения. 
    /// </summary>
    /// <returns></returns>
    public Func<bool> DisableNotification { get; set; } = () => false;
}

internal sealed class TelegramLogger(
    string categoryName,
    Func<TelegramLoggerConfiguration> getCurrentConfig) : ILogger, IDisposable
{
    private readonly string categoryName = categoryName;
    private readonly Func<TelegramLoggerConfiguration> getCurConfig = getCurrentConfig;

    private QueueMessageWriter qmWriter = new();
    private TelegramMessageFormatter telMesFormatter = new();

#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр

    public bool IsEnabled(LogLevel logLevel)
    {
        var curConf = getCurConfig();

        return logLevel != LogLevel.None &&
            !String.IsNullOrWhiteSpace(curConf.BotToken) &&
            !String.IsNullOrWhiteSpace(curConf.ChatId);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        string message = null!;
        if (formatter != null)
            message = formatter(state, exception);

        if (exception != null)
        {
            if (@String.IsNullOrWhiteSpace(message))
                message += Environment.NewLine;

            message += exception.Message;
        }

        var options = getCurConfig();

        if (!String.IsNullOrWhiteSpace(message))
            message = telMesFormatter.Format(logLevel, categoryName, message, exception, options);

        if (!String.IsNullOrWhiteSpace(message))
            qmWriter.Enqueue(message, options.BotToken!, options.ChatId!, options.DisableNotification?.Invoke() ?? false);
    }

    public void Dispose() => qmWriter.Dispose();
}

[ProviderAlias("Telegram")]
internal sealed class TelegramLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? onChangeToken;
    private TelegramLoggerConfiguration curConfig;
    private readonly ConcurrentDictionary<string, TelegramLogger> loggers =
        new(StringComparer.OrdinalIgnoreCase);

    public TelegramLoggerProvider(
        IOptionsMonitor<TelegramLoggerConfiguration> config)
    {
        curConfig = config.CurrentValue;
        onChangeToken = config.OnChange(updatedConfig => curConfig = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName) =>
        loggers.GetOrAdd(categoryName, name => new TelegramLogger(name, getCurrentConfig));

    private TelegramLoggerConfiguration getCurrentConfig() => curConfig;

    public void Dispose()
    {
        loggers.Clear();
        onChangeToken?.Dispose();
    }
}

/// <summary>
/// Расширения для добавления логера
/// </summary>
public static class TelegramLoggerExtensions
{
    /// <summary>
    /// Добавить логгер
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TelegramLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<TelegramLoggerConfiguration, TelegramLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Добавить логгер с коррекцией настроек
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder builder,
        Action<TelegramLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddTelegramLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}

internal record QueueMessage(string Message, string BotToken, string ChatId, bool DisableNotification);

internal class QueueMessageWriter : IDisposable
{
    private BlockingCollection<QueueMessage> queues = [];

    public QueueMessageWriter()
    {
        var thrd = new Thread(sendMesg)
        {
            IsBackground = true,
            Name = "Telegram sender thread"
        };

        thrd.Start();
    }

    public void Enqueue(string message, string botToken, string chatId, bool disableNotification) =>
        queues.Add(new(message, botToken, chatId, disableNotification));

    private void sendMesg()
    {
        foreach (var qm in queues.GetConsumingEnumerable())
            try
            {
                TelegramLogWriter.Write(qm).GetAwaiter().GetResult();
            }
            finally
            {
            }
    }

    public void Dispose()
    {
        queues.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal static class TelegramLogWriter
{
    public static async Task Write(QueueMessage queueMessage)
    {
        var req = new RestRequest()
        {
            Method = Method.Post
        };

        var dn = queueMessage.DisableNotification.ToString().ToLower();

        if (queueMessage.Message.Length >= 4000)
        {
            req.Resource = "sendDocument";
            req.AlwaysMultipartFormData = true;

            req.AddParameter("file_id", DateTime.Now.Ticks.ToString())
                .AddParameter("chat_id", queueMessage.ChatId)
                .AddParameter("caption", queueMessage.Message[..1000])
                .AddParameter("disable_content_type_detection", "true")
                .AddParameter("disable_notification", dn)
                .AddFile("document", Encoding.UTF8.GetBytes(queueMessage.Message), "error.txt");
        }
        else
        {
            req.Resource = "sendMessage";
            req.AddJsonBody(new
            {
                chat_id = queueMessage.ChatId,
                text = queueMessage.Message,
                disable_notification = dn
            });
        }

        try
        {
            using var client = new RestClient(new RestClientOptions(new Uri($"https://api.telegram.org/bot{queueMessage.BotToken}/")));
            await client.ExecuteAsync(req).ConfigureAwait(false);
        }
        catch { }
    }
}

/// <summary>
/// Форматер сообщения. 
/// </summary>
internal class TelegramMessageFormatter
{
    public virtual string Format(
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        TelegramLoggerConfiguration options)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var sb = new StringBuilder();
#pragma warning disable CA1305 // Укажите IFormatProvider
        sb.AppendLine($"{(options.UseEmoji ? ToEmoji(logLevel) : String.Empty)}{logLevel} {category}");
#pragma warning restore CA1305 // Укажите IFormatProvider

        sb.Append(message);

        if (exception != null)
        {
            sb.AppendLine();
            sb.AppendLine(exception.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Преобразование <see cref="LogLevel"/> в эмодзи
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public virtual string ToEmoji(LogLevel level) =>
        level switch
        {
            LogLevel.Trace => "⚡️",
            LogLevel.Debug => "⚙️",
            LogLevel.Information => "ℹ️",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "🛑",
            LogLevel.Critical => "❌",
            _ => "💤"
        };
}
