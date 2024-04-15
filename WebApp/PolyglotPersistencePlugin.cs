using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Milvus;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.HuggingFace;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using System.Net;
public class PolyglotPersistencePlugin
{
    #region Fields
    #pragma warning disable SKEXP0001, SKEXP0011, SKEXP0020, SKEXP0025
    ITextEmbeddingGenerationService embeddingGenerator {get; set;} = null!;
    IMemoryStore memoryStore {get; set;} = null!;
    SemanticTextMemory textMemory {get; set;} = null!;
    TextAnalyticsClient textAnalyticsClient {get; set;} = null!;
    // string debugger {get; set;} = string.Empty;
    public const string NotFound = "No answer found";
    #endregion
    #region Milvus
    Milvus.Client.MilvusClient milvusClient {get; set;} = null!;
    public string dbName {get; set;} = null!;
    public async Task MilvusCheck()
    {
        var databases = await milvusClient.ListDatabasesAsync();
        if(databases.Where(x => x == dbName).Count() == 0 ) await milvusClient.CreateDatabaseAsync(dbName);
    }
    #endregion
    public PolyglotPersistencePlugin(AIKeyStore store)
    {
        if(store.UseAzure)
        {
            embeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(deploymentName: store.OpenAI.Deployment, endpoint: store.OpenAI.Endpoint, apiKey: store.OpenAI.APIKey, modelId: store.OpenAI.GenerationService);
            memoryStore = new AzureAISearchMemoryStore(store.SearchService.Endpoint, store.SearchService.Key);
        }
        else
        {
            var hostname = new Uri(store.Milvus.Endpoint).Host;
            var portnumber = new Uri(store.Milvus.Endpoint).Port;
            dbName = store.Milvus.DBName;
            milvusClient = new Milvus.Client.MilvusClient(host: hostname, port: portnumber, ssl:false);
            embeddingGenerator = new HuggingFaceTextEmbeddingGenerationService(store.HuggingFace.ModelID, new Uri(store.HuggingFace.Endpoint), $"Bearer {store.HuggingFace.APIKey}");
            memoryStore = new MilvusMemoryStore(milvusClient, dbName, vectorSize: 1536, Milvus.Client.SimilarityMetricType.Ip);
        }
        if(store.UseVolatile)
        {
            #pragma warning disable SKEXP0050
            memoryStore = new VolatileMemoryStore();
        }
        textMemory = new(memoryStore, embeddingGenerator);
        AzureKeyCredential credential = new(store.TextAnalytics.APIKey);
        var endpoint = store.UseAzure == true ? store.TextAnalytics.Endpoint : store.TextAnalytics.LocalAddress;
        var options = new TextAnalyticsClientOptions(){Retry = { Delay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(16), MaxRetries = 3, Mode = RetryMode.Exponential}};
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
        textAnalyticsClient = new(new Uri(endpoint), credential, options);
        // debugger = store.UseAzure.ToString();
    }
    #region TODO Cleanup
    public bool IsDefault { get; set; } = false;
    public string GetState() => IsDefault ? "Azure OpenAI" : "Third Party Milvus";
    public string ChangeState(bool newState)
    {
        this.IsDefault = newState;
        var state = GetState();
        return state;
    }
    #endregion
    #region TODO Cleanup
        // [KernelFunction]
        // [Description("Gets the Kernel memory type.")]
        public async Task<string> MilvusINPUT(string input)
        {
            await MilvusCheck();
            await textMemory.SaveInformationAsync("PromptFlowSample", id: "info2", text: input);
            return "regist database";
        }

        // [KernelFunction]
        // [Description("Changes the state of memory type.")]
        public async Task<string> MilvusOUTPUT(string index)
        {
            await MilvusCheck();
            MemoryQueryResult? lookup = await textMemory.GetAsync("PromptFlowSample", index);
            return lookup?.Metadata.Text ?? "No data found";
        }
        [KernelFunction]
        [Description("類似検索を行う")]
    #endregion
    public async Task<string> SearchGeneric(string inquery, string collection)
    {
        var ans = string.Empty;
        var res = textMemory.SearchAsync(
                        collection: collection,
                        query: inquery,
                        limit: 2,
                        minRelevanceScore: 0.79,
                        withEmbeddings: true
                    );
        if(res.GetAsyncEnumerator().Current == null)
        {
            ans = string.Empty;
        }
        else
        {
            await foreach (var answer in res)
            {
                ans = $"Answer: {answer.Metadata.Text}";
            }
        }
        return ans == string.Empty ? NotFound : ans!;
    }
    
    // 
    // public async Task<string> DetectLanguage(string input)
    public string DetectLanguage(string input)
    {
        var res = new List<KeyValuePair<string,string>>();
        try
        {
            // Response<DetectedLanguage> response = await textAnalyticsClient.DetectLanguageAsync(input);
            Response<DetectedLanguage> response = textAnalyticsClient.DetectLanguageAsync(input).Result;
            DetectedLanguage language = response.Value;
            res.Add(new KeyValuePair<string,string>("name",$"{language.Name}"));
            res.Add(new KeyValuePair<string,string>("score",$"{language.ConfidenceScore}"));
        }
        catch (RequestFailedException exception)
        {
            Console.WriteLine($"Error Code: {exception.ErrorCode}");
            Console.WriteLine($"Message: {exception.Message}");
            res.Add(new KeyValuePair<string,string>("code",$"{exception.ErrorCode}"));
            res.Add(new KeyValuePair<string,string>("Message",$"{exception.Message}"));
        }
        // return res.Where(kv => kv.Key == "name").FirstOrDefault().Value ?? res.Where(kv => kv.Key == "code").FirstOrDefault().Value;
        return string.Join(",", res.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}