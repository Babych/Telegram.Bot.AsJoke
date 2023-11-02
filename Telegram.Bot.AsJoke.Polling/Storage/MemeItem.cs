using Azure;
using Azure.Data.Tables;

namespace Telegram.BotAsJoke.Polling.Storage
{
    public class MemeItem : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string FileId { get; set; }

        public string FileUniqueId { get; set; }

        public string ChatId { get; set; }

        public string UserId { get; set; }

        public List<string> Dislikers { get; set; }

        public List<string> Likers { get; set; }

        public string Deleter { get; set; }

        public bool IsDeleted { get; set; }
    }
}
