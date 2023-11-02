using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace Telegram.BotAsJoke.Polling.Storage
{
    public class UserItem : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string? RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string Chat { get; set; }

        public string User { get; set; }

        public string? UserId { get; set; }

        public string? ChatId { get; set; }

        public bool IsDeleted { get; set; }

        public string DisplayUserName
        {
            get
            {
                var user = JsonConvert.DeserializeObject<User>(User);
                var displayUserName = !string.IsNullOrEmpty(user?.Username)
                    ? $"@{user.Username}"
                    : $"{user?.FirstName} {user?.LastName}";

                return displayUserName;
            }
        }

        public bool IsMamaBot { get; set; }

        public Action Action { get; set; }

        public bool SubscribeForDelivery { get; set; }
    }

    public enum Action
    {
        TextStoreRequested,
        ImageStoreRequested,
    }
}
