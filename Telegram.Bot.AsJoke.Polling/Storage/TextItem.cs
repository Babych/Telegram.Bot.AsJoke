using Azure;
using Azure.Data.Tables;

namespace Telegram.BotAsJoke.Polling.Storage
{
    public class TextItem : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string Text { get; set; }

        public bool IsDeleted { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }
    }
}
