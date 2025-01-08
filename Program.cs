// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using Azure.Messaging.ServiceBus;

// Azure CLI setup
/* myLocation=<myLocation>
myNameSpaceName=az204svcbus$RANDOM 
myResourceGroup=az204-svcbus-rg-$RANDOM
az group create --name az204-svcbus-rg --location $myLocation
az servicebus namespace create --resource-group az204-svcbus-rg --name $myNameSpaceName --location $myLocation
az servicebus queue create --resource-group az204-svcbus-rg --namespace-name $myNameSpaceName --name az204-queue
az group delete --name az204-svcbus-rg --no-wait
*/

string connectionString = "your-actual-secret-key";
string queueName = "az204-queue";

ServiceBusClient client = new ServiceBusClient(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

// Create a batch
using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

for (int i = 0; i < 3; i++)
{
    // Try adding a message to the batch
    if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
    {
        throw new Exception($"Exception {i} has occurred.");
    }
}

try
{
    // Send the batch of messages to the client
    await sender.SendMessagesAsync(messageBatch);
    Console.WriteLine("A batch of messages has been published.");
}
finally
{
    // Ensure that resources are cleaned up
    await sender.DisposeAsync();
    await client.DisposeAsync();
}

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

client = new ServiceBusClient(connectionString);

// Create message processor
ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

try
{
    // Add message and error handlers
    processor.ProcessMessageAsync += ProcessMessageHandler;
    processor.ProcessErrorAsync += ProcessMessageErrorHandler;

    // Start processing
    await processor.StartProcessingAsync();

    Console.WriteLine("Wait...");
    Console.ReadKey();

    Console.WriteLine("\nStopping reciever...");
    // Stop processing
    await processor.StopProcessingAsync();
    Console.WriteLine("Stopped.");
}
catch (System.Exception)
{
    // Clean up resources
    await processor.DisposeAsync();
    await client.DisposeAsync();
}

async Task ProcessMessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    await args.CompleteMessageAsync(args.Message);
}

Task ProcessMessageErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

