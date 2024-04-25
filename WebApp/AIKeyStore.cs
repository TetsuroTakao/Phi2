public class AIKeyStore
{
    public const string OpenAIType = "OpenAIType";
    public const string TextAnalyticsType = "TextAnalyticsType";
    public const string SearchServiceType = "SearchServiceType";
    public const string MilvusType = "MilvusType";
    public const string HuggingFaceType = "HuggingFaceType";
    public const string BingType = "BingType";
    public string SettingsName { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string ServiceEndpoint { get; set; } = string.Empty;
    public string LocalEndpoint { get; set; } = string.Empty;
    // OptionalID holds deployment name for OpenAI,DB table name for Milvus
    public string OptionalID { get; set; } = string.Empty;
    public string ModelID { get; set; } = string.Empty;
}