public class KeyService
{
    public AIKeyStore GetValueContainer(bool useAzure = false, bool useVolatile = false) 
        => new AIKeyStore(){ UseAzure = useAzure, UseVolatile = useVolatile};
}
public class AIKeyStore
{
    public bool UseAzure { get; set; } = false;
    public bool UseVolatile { get; set; } = false;
    // public const string OpenAIType = "OpenAIType";
    // public const string TextAnalyticsType = "TextAnalyticsType";
    // public const string SearchServiceType = "SearchServiceType";
    // public const string MilvusType = "MilvusType";
    // public const string HuggingFaceType = "HuggingFaceType";
    // public const string BingType = "BingType";
    public TextAnalytics TextAnalytics { get; set; } = new TextAnalytics();
    public SearchService SearchService { get; set; } = new SearchService();
    public OpenAIType OpenAI { get; set; } = new OpenAIType();
    public MilvusType Milvus { get; set; } = new MilvusType();
    public HuggingFace HuggingFace { get; set; } = new HuggingFace();
    public Bing Bing { get; set; } = new Bing();
}

public class TextAnalytics
{
    public string APIKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string LocalAddress { get; set; } = string.Empty;
}

public class SearchService
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public class OpenAIType
{
    public string APIKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string GenerationService { get; set; } = string.Empty;
    public string Deployment { get; set; } = string.Empty;
}

public class MilvusType
{
    public string Endpoint { get; set; } = string.Empty;
    public string DBName { get; set; } = string.Empty;
}

public class HuggingFace
{
    public string APIKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelID { get; set; } = string.Empty;
}

public class Bing
{
    public string APIKey { get; set; } = string.Empty;
}