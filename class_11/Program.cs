using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using class_11.Models;
using System.Text.Json;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var sqs = new AmazonSQSClient("access_key", "secret_key", RegionEndpoint.EUCentral1);
var queueUrl = "https://sqs.eu-central-1.amazonaws.com/614126170823/sqsexample";

int requestId = 1;

while (true)
{
    Console.WriteLine("1 - Створити заявку");
    Console.WriteLine("2 - Обробити всі заявки");
    Console.WriteLine("3 - Показати приблизну кількість заявок у черзі");
    Console.WriteLine("0 - Вихід");
    Console.Write("Вибір: ");

    string choice = Console.ReadLine() ?? "";

    switch (choice)
    {
        case "1":
            await CreateRequest();
            break;

        case "2":
            await ProcessRequests();
            break;

        case "3":
            await ShowQueueCount();
            break;

        case "0":
            Console.WriteLine("Програму завершено.");
            return;

        default:
            Console.WriteLine("Невірний вибір.");
            break;
    }
}


async Task CreateRequest()
{
    Console.WriteLine();
    Console.WriteLine("===== СТВОРЕННЯ ЗАЯВКИ =====");

    Console.Write("Ім'я користувача: ");
    string userName = Console.ReadLine() ?? "";

    Console.Write("Тема заявки: ");
    string topic = Console.ReadLine() ?? "";

    Console.Write("Опис проблеми: ");
    string description = Console.ReadLine() ?? "";

    Console.Write("Пріоритет (Low / Medium / High): ");
    string priority = Console.ReadLine() ?? "";

    while (priority != "Low" &&
           priority != "Medium" &&
           priority != "High")
    {
        Console.Write("Невірний пріоритет. Введіть Low, Medium або High: ");
        priority = Console.ReadLine() ?? "";
    }

    var request = new SupportRequest
    {
        Id = requestId++,
        UserName = userName,
        Topic = topic,
        Description = description,
        Priority = priority,
        CreatedAt = DateTime.UtcNow
    };

    string json = JsonSerializer.Serialize(request);

    await sqs.SendMessageAsync(new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = json
    });

    Console.WriteLine();
    Console.WriteLine($"Заявка №{request.Id} додана в чергу.");
}


async Task ProcessRequests()
{
    Console.WriteLine();
    Console.WriteLine("===== ОБРОБКА ЗАЯВОК =====");

    bool hasMessages = false;

    while (true)
    {
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 2
        });

        if (response.Messages == null || response.Messages.Count == 0)
            break;

        hasMessages = true;

        foreach (var message in response.Messages)
        {
            try
            {
                var request =
                    JsonSerializer.Deserialize<SupportRequest>(message.Body);

                if (request == null)
                    continue;

                TimeSpan waitingTime = DateTime.UtcNow - request.CreatedAt;

                Console.WriteLine();
                Console.WriteLine("------------------------------");
                Console.WriteLine($"Номер заявки: {request.Id}");
                Console.WriteLine($"Користувач: {request.UserName}");
                Console.WriteLine($"Тема: {request.Topic}");
                Console.WriteLine($"Опис: {request.Description}");
                Console.WriteLine($"Пріоритет: {request.Priority}");
                Console.WriteLine($"Створено: {request.CreatedAt:dd.MM.yyyy HH:mm:ss}");
                Console.WriteLine($"Час очікування: {waitingTime.TotalMinutes:F1} хв.");

                if (request.Priority == "High")
                {
                    Console.WriteLine("!!! Термінова заявка !!!");
                }

                await sqs.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                });

                Console.WriteLine("Заявку оброблено та видалено з черги.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка обробки заявки: {ex.Message}");
            }
        }
    }

    if (!hasMessages)
    {
        Console.WriteLine("Черга порожня.");
    }
}


async Task ShowQueueCount()
{
    var response = await sqs.GetQueueAttributesAsync(
        new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = new List<string>
            {
                "ApproximateNumberOfMessages"
            }
        });

    string count =
        response.Attributes["ApproximateNumberOfMessages"];

    Console.WriteLine();
    Console.WriteLine($"Приблизна кількість заявок у черзі: {count}");
}