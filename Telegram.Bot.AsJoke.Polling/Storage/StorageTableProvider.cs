using System.Globalization;

using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Telegram.Bot.Types;
using Telegram.BotAsJoke.Polling.Services;

namespace Telegram.BotAsJoke.Polling.Storage
{
    public interface IStorageTableProvider
    {
        Task<string> PutTextItem(string text, string userId);
        Task<bool?> UpsertUserItem(Message message, bool subscribeForDelivery = false);
        Task<TextItem?> GetTextItem(string textId);
        Task<UserItem?> GetUserItem(string userId);
    }

    public class StorageTableProvider : IStorageTableProvider
    {
        private const string UserList = "UserList";
        private const string BotAsJokeImgContainerName = "botasjokeimgcontainername";
        private const string BotAsJokeTextTableName = "botasjoketexttablename";
        private const string BotAsJokeUserTableName = "botasjokeusertablename";
        private const string BotAsJokeMemeTableName = "botasjokememetablename";

        private readonly TableClient textTableClient;
        private readonly TableClient userTableClient;
        private readonly TableClient memeTableClient;
        private readonly BlobContainerClient blobContainerClient;
        private readonly AsyncRetryPolicy policy;

        public StorageTableProvider()
        {
            try
            {
                var storageConnectionString = BotConfiguration.SaConnectionString;

                BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
                blobContainerClient = blobServiceClient.GetBlobContainerClient(BotAsJokeImgContainerName);
                blobContainerClient.CreateIfNotExists();

                textTableClient = GetTableClient(storageConnectionString, BotAsJokeTextTableName);
                userTableClient = GetTableClient(storageConnectionString, BotAsJokeUserTableName);
                memeTableClient = GetTableClient(storageConnectionString, BotAsJokeMemeTableName);
                policy = Policy
                    .Handle<RequestFailedException>()
                    .WaitAndRetryAsync(new[]
                        {
                            TimeSpan.FromMilliseconds(100),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromMilliseconds(1000)
                        },
                        (exception, retryCount, context) =>
                        {
                            Log.Instance.Trace(
                                $"Retry {retryCount} due to RequestFailedException: {exception.Message}");
                        });
            }
            catch (Exception e)
            {
                Log.Instance.Trace($"StorageTableProvider Failed: {e.Message}");
                throw;

            }
        }

        private TableClient GetTableClient(string storageConnectionString, string tableName)
        {
            try
            {
                var requestOptions = new TableClientOptions();

                // create the table client
                var tableServiceClient = new TableServiceClient(storageConnectionString, requestOptions);

                // single table will be used for all commands
                tableServiceClient.CreateTableIfNotExists(tableName);

                var tableClient = tableServiceClient.GetTableClient(tableName);

                return tableClient;
            }
            catch (Exception e)
            {
                Log.Instance.Trace($"GetMismatchVehiclesTableClient Failed: {e.Message}");
                throw;

            }
        }

        public async Task<string> PutTextItem(string text, string userId)
        {
            try
            {
                var id = $"{Guid.NewGuid()}";
                var dto = new TextItem()
                {
                    PartitionKey = "TextItem",
                    RowKey = id,
                    Timestamp = DateTime.UtcNow,
                    Text = text,
                    AuthorId = userId,

                };

                await policy.ExecuteAsync(async () => await textTableClient.AddEntityAsync(dto));
                return id;
            }
            catch (Exception e)
            {
                Log.Instance.TrackException(e, "PutMismatchedVehicleToSa Failed");
                return null;
            }
        }

        public async Task PutPhotoItem(Message message, bool storeAsMeme = false)
        {
            try
            {
                foreach (var item in message.Photo.Where(x => x.Height > 320))
                {
                    var dto = new MemeItem()
                    {
                        PartitionKey = "Meme",
                        RowKey = item.FileUniqueId,
                        Timestamp = DateTime.UtcNow,
                        FileUniqueId = item.FileUniqueId,
                        FileId = item.FileId,
                        ChatId = message.Chat.Id.ToString(),
                        UserId = message.From.Id.ToString(),
                        IsDeleted = !storeAsMeme,
                    };

                    await policy.ExecuteAsync(async () => await memeTableClient.AddEntityAsync(dto));
                }
            }
            catch (Exception e)
            {
                Log.Instance.TrackException(e, "PutMismatchedVehicleToSa Failed");
            }
        }

        public async Task<List<MemeItem>> GetMemes()
        {
            var filter = "IsDeleted eq false";

            var memes = new List<MemeItem>();
            await foreach (var entity in memeTableClient.QueryAsync<MemeItem>(filter: filter))
            {
                memes.Add(entity);
            }

            return memes;
        }

        public async Task<MemeItem> GetMeme(string fileUniqueId)
        {
            var filter = $"FileUniqueId eq {fileUniqueId}";

            var memes = new List<MemeItem>();
            await foreach (var entity in memeTableClient.QueryAsync<MemeItem>(filter: filter))
            {
                memes.Add(entity);
            }

            return memes.FirstOrDefault();
        }

        public async Task<List<UserItem>> GetListOfSubscribers()
        {
            try
            {
                var filter = "SubscribeForDelivery eq true";

                var users = new List<UserItem>();
                await foreach (var entity in userTableClient.QueryAsync<UserItem>(filter: filter))
                {
                    users.Add(entity);
                }

                return users;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task DeleteMeme(string fileUniqueId, string userId)
        {
            var meme = await GetMeme(fileUniqueId);
            meme.IsDeleted = true;
            meme.Deleter = userId;
            await memeTableClient.UpdateEntityAsync(meme, meme.ETag);
        }

        public async Task LikeMeme(string fileUniqueId, string userId)
        {
            var meme = await GetMeme(fileUniqueId);
            meme.Likers.Add(userId);

            await memeTableClient.UpdateEntityAsync(meme, meme.ETag);
        }
        public async Task DislikeMeme(string fileUniqueId, string userId)
        {
            var meme = await GetMeme(fileUniqueId);
            meme.Dislikers.Add(userId);

            await memeTableClient.UpdateEntityAsync(meme, meme.ETag);
        }

        /// <summary>
        /// Returns if user before was subscribed for the first time.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="subscribeForDelivery"></param>
        /// <returns></returns>
        public async Task<bool?> UpsertUserItem(Message message, bool subscribeForDelivery = false)
        {
            try
            {
                var userItem = await GetUserItem(message.Chat.Id.ToString(CultureInfo.InvariantCulture));
                if (userItem == null)
                {
                    var dto = new UserItem()
                    {
                        PartitionKey = UserList,
                        RowKey = message.Chat.Id.ToString(CultureInfo.InvariantCulture),
                        Timestamp = DateTime.UtcNow,
                        UserId = message.From?.Id.ToString(CultureInfo.InvariantCulture),
                        ChatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture),
                        Chat = JsonConvert.SerializeObject(message.Chat),
                        User = JsonConvert.SerializeObject(message.From),
                        SubscribeForDelivery = subscribeForDelivery
                    };

                    await policy.ExecuteAsync(async () => await userTableClient.AddEntityAsync(dto));
                    return true;
                }

                userItem.SubscribeForDelivery = subscribeForDelivery;
                await policy.ExecuteAsync(async () => await userTableClient.UpsertEntityAsync(userItem));

                return false;
            }
            catch (Exception e)
            {
                Log.Instance.TrackException(e, "PutMismatchedVehicleToSa Failed");
            }

            return null;
        }

        public async Task UpsertMamaBot(Message message, bool isMamaBot)
        {
            try
            {
                var userItem = await GetUserItem(message.Chat.Id.ToString(CultureInfo.InvariantCulture));
                if (userItem == null)
                {
                    var dto = new UserItem()
                    {
                        PartitionKey = UserList,
                        RowKey = message.Chat.Id.ToString(CultureInfo.InvariantCulture),
                        ChatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture),
                        Chat = JsonConvert.SerializeObject(message.Chat),
                        User = JsonConvert.SerializeObject(message.From),
                        Timestamp = DateTime.UtcNow,
                        IsMamaBot = isMamaBot
                    };

                    await policy.ExecuteAsync(async () => await userTableClient.AddEntityAsync(dto));
                    return;
                }

                userItem.IsMamaBot = isMamaBot;
                await policy.ExecuteAsync(async () => await userTableClient.UpsertEntityAsync(userItem));
            }
            catch (Exception e)
            {
                Log.Instance.TrackException(e, "PutMismatchedVehicleToSa Failed");
            }
        }

        public async Task<TextItem?> GetTextItem(string textId)
        {
            NullableResponse<TextItem> textItem =
                await textTableClient.GetEntityIfExistsAsync<TextItem>("TextItem", textId);

            return !textItem.HasValue ? null : textItem.Value;
        }

        public async Task<UserItem?> GetUserItem(string chatId)
        {
            NullableResponse<UserItem> userItem =
                await userTableClient.GetEntityIfExistsAsync<UserItem>(UserList, chatId);

            if (!userItem.HasValue)
            {
                return null;
            }

            return userItem?.Value;
        }

        public async Task<bool> IsMamaBot(string chatId)
        {
            try
            {
                NullableResponse<UserItem> result =
                    await userTableClient.GetEntityIfExistsAsync<UserItem>(UserList, chatId);

                if (!result.HasValue)
                {
                    return false;
                }

                return result.Value?.IsMamaBot ?? false;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
