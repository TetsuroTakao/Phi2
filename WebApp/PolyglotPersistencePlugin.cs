using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Milvus;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.HuggingFace;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Text;// TextChunker
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.AI.OpenAI;
using Microsoft.ML;
using Microsoft.ML.Tokenizers;
using Json.More;
public class PolyglotPersistencePlugin
{
    #region Fields
    #pragma warning disable SKEXP0001, SKEXP0011, SKEXP0020, SKEXP0025
    ITextEmbeddingGenerationService embeddingGenerator {get; set;} = null!;
    IMemoryStore memoryStore {get; set;} = null!;
    SemanticTextMemory textMemory {get; set;} = null!;
    TextAnalyticsClient textAnalyticsClient {get; set;} = null!;
    OpenAIClient openAIClient {get;set;} = null!;
    EmbeddingsOptions embeddingsOptions {get; set;} = null!;
    string ModelID {get; set;} = null!;
    string debugger {get; set;} = string.Empty;
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
    public PolyglotPersistencePlugin(List<AIKeyStore> settings, bool useAzure=false, bool useeVolatile=false)
    {
        AIKeyStore store;
        if(useAzure)
        {
            store = settings.Where(s => s.SettingsName==AIKeyStore.OpenAIType).FirstOrDefault()!;    
            var searchstore = settings.Where(s => s.SettingsName==AIKeyStore.SearchServiceType).FirstOrDefault()!;
            embeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(deploymentName: store.OptionalID , endpoint: store.ServiceEndpoint, apiKey: store.APIKey, modelId: store.ModelID);
            memoryStore = new AzureAISearchMemoryStore(searchstore.ServiceEndpoint, searchstore.APIKey);
        }
        else
        {
            store = settings.Where(s => s.SettingsName==AIKeyStore.MilvusType).FirstOrDefault()!;
            var searchstore = settings.Where(s => s.SettingsName==AIKeyStore.HuggingFaceType).FirstOrDefault()!;
            var hostname = new Uri(store.LocalEndpoint).Host;
            var portnumber = new Uri(store.LocalEndpoint).Port;
            dbName = store.OptionalID;
            milvusClient = new Milvus.Client.MilvusClient(host: hostname, port: portnumber, ssl:false);
            embeddingGenerator = new HuggingFaceTextEmbeddingGenerationService(searchstore.ModelID, new Uri(searchstore.ServiceEndpoint), $"Bearer {searchstore.APIKey}");
            memoryStore = new MilvusMemoryStore(milvusClient, dbName, vectorSize: 1536, Milvus.Client.SimilarityMetricType.Ip);
        }
        if(useeVolatile)
        {
            #pragma warning disable SKEXP0050
            memoryStore = new VolatileMemoryStore();
        }
        textMemory = new(memoryStore, embeddingGenerator);
        ModelID = store.ModelID;
        AzureKeyCredential credential = new(settings.Where(s => s.SettingsName==AIKeyStore.TextAnalyticsType).FirstOrDefault()!.APIKey);
        var endpoint = useAzure == true ? settings.Where(s => s.SettingsName==AIKeyStore.TextAnalyticsType).FirstOrDefault()!.ServiceEndpoint : settings.Where(s => s.SettingsName==AIKeyStore.TextAnalyticsType).FirstOrDefault()!.LocalEndpoint;
        var options = new TextAnalyticsClientOptions(){Retry = { Delay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(16), MaxRetries = 3, Mode = RetryMode.Exponential}};
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
        textAnalyticsClient = new(new Uri(endpoint), credential, options);
        debugger = $"endpoint:{endpoint}, Key:{credential.Key}";
        openAIClient =new OpenAIClient(new Uri(store.ServiceEndpoint), new AzureKeyCredential(store.APIKey));
        embeddingsOptions = new EmbeddingsOptions(){ DeploymentName = store.ModelID};
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
    #region Persistence Milvus
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
    // public async Task<string> DetectLanguage(string input)
    public string DetectLanguage(string input)
    {
        var res = new List<KeyValuePair<string,string>>();
        try
        {
            // Response<DetectedLanguage> response = await textAnalyticsClient.DetectLanguageAsync(input);
            Response<DetectedLanguage> response = textAnalyticsClient.DetectLanguage(input);
            // DetectedLanguage language = response.Value;
            // res.Add(new KeyValuePair<string,string>("name",$"{language.Name}"));
            // res.Add(new KeyValuePair<string,string>("reliable",$"{language.ConfidenceScore}"));
            res.Add(new KeyValuePair<string,string>("debugger",$"{debugger}"));
        }
        catch (RequestFailedException exception)
        {
            res.Add(new KeyValuePair<string,string>("code",$"{exception.ErrorCode}"));
            res.Add(new KeyValuePair<string,string>("Message",$"{exception.Message}"));
            res.Add(new KeyValuePair<string,string>("InnerException Message",$"{exception.InnerException!.Message}"));
        }
        return string.Join(",", res.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
    public string GetKeyPhrase(string input)
    {
        var res = new List<KeyValuePair<string,string>>();
        try
        {
            Response<KeyPhraseCollection> response = textAnalyticsClient.ExtractKeyPhrases(input);
            foreach (string keyphrase in response.Value)
            {
                res.Add(new KeyValuePair<string,string>("keyphrase",$"{keyphrase}"));
            }
        }
        catch (RequestFailedException exception)
        {
            res.Add(new KeyValuePair<string,string>("code",$"{exception.ErrorCode}"));
            res.Add(new KeyValuePair<string,string>("Message",$"{exception.Message}"));
            res.Add(new KeyValuePair<string,string>("InnerException Message",$"{exception.InnerException!.Message}"));
        }
        return string.Join(",", res.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
    public string GetVector(string input)
    {
        var res = new List<KeyValuePair<string,string>>();
        try
        {
            var lines = TextChunker.SplitPlainTextLines(input, maxTokensPerLine: 10);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: 25);
            using var enumerator = paragraphs.GetEnumerator();
            // IList<string> chunk = new List<string>();
            float[] vectors = null!;
            while (enumerator.MoveNext())
            {
                var paragraph = enumerator.Current;
                embeddingsOptions.Input.Add(paragraph);
            }
            var embeddings = openAIClient.GetEmbeddings(embeddingsOptions);
        // var r = embeddings.Value.Data.Chunk<float>(vectors, 8192).ToArray();
            res.Add(new KeyValuePair<string,string>("Input: ",$"{embeddingsOptions.DeploymentName},"));
            res.Add(new KeyValuePair<string,string>("Input: ",$"{embeddingsOptions.Input.FirstOrDefault()},"));
        // var mlContext = new MLContext();
        // var samples = new List<string>(){"string"};
        // var dataview = mlContext.Data.LoadFromEnumerable(samples);
        // var textPipeline = mlContext.Transforms.Text.FeaturizeText("string", "float[]");
        // var textTransformer = textPipeline.Fit(dataview);
        // var predictionEngine = mlContext.Model.CreatePredictionEngine<string,float[]>(textTransformer);
        // var prediction = predictionEngine.Predict(samples[0]);
        }
         catch (RequestFailedException exception)
        {
            res.Add(new KeyValuePair<string,string>("code",$"{exception.ErrorCode}"));
            res.Add(new KeyValuePair<string,string>("Message",$"{exception.Message}"));
            res.Add(new KeyValuePair<string,string>("InnerException Message",$"{exception.InnerException!.Message}"));
        }
        return string.Join(",", res.Select(kv => $"{kv.Key}:{kv.Value}"));
    }

}