using Microsoft.SemanticKernel;//Kernel
using Microsoft.SemanticKernel.Plugins.Web.Bing;//BingConnector
using Microsoft.SemanticKernel.Plugins.Core;//TimePlugin
using Microsoft.SemanticKernel.Connectors.OpenAI;//OpenAIPromptExecutionSettings
using Microsoft.SemanticKernel.ChatCompletion;//ChatHistory
using Swashbuckle.AspNetCore.Annotations;//SwaggerOperationAttribute
using Microsoft.AspNetCore.Mvc;//FromBody
using Microsoft.Extensions.Options;//IOptionsSnapshot
#region TODO Cleanup
// using Milvus.Client;
// using Swashbuckle.AspNetCore.Swagger;
// using Swashbuckle.AspNetCore.SwaggerGen;
// using Swashbuckle.AspNetCore.SwaggerUI;
#pragma warning disable SKEXP0050, SKEXP0011
#endregion

var webbuilder = WebApplication.CreateBuilder(args);
#region build swagger
webbuilder.Services.AddEndpointsApiExplorer();
webbuilder.Services.AddSwaggerGen(c => {c.SwaggerDoc("v1", new() { Title = "HybridAIWebApp", Version = "v1" }); c.EnableAnnotations();});
webbuilder.Services.AddControllers();
webbuilder.Services.AddLogging();
#endregion

#region  WebApp build
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.OpenAIType, webbuilder.Configuration.GetSection("AI:OpenAI"));
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.BingType, webbuilder.Configuration.GetSection("AI:Bing"));
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.MilvusType, webbuilder.Configuration.GetSection("AI:Milvus"));
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.HuggingFaceType, webbuilder.Configuration.GetSection("AI:HuggingFace"));
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.SearchServiceType, webbuilder.Configuration.GetSection("AI:SearchService"));
webbuilder.Services.Configure<AIKeyStore>(AIKeyStore.TextAnalyticsType, webbuilder.Configuration.GetSection("AI:TextAnalytics"));
#region scope build
// webbuilder.Services.Configure<AIKeyStore>(webbuilder.Configuration.GetSection("AI")).Configure<IServiceProvider>((provider)=>{
//     // Define the using statement to release the scoped service per request when using IOptionsSnapshot to acquire re-bound configuration settings per the Post request.
//     using var scope = provider.CreateScope();// Create scope per request in the root IServiceProvider.
//     var scopedservice = scope.ServiceProvider.GetRequiredService<AIKeyStore>();// This is request-scoped service.
//     scopedservice.UseAzure = true;
//     // scopedservice.GetValueContainer(useAzure: true, useVolatile: false); // Request-scoped service creates a request-scoped configuration container isolated from one another for individual requests.
// }); // Set values from configuration values to the created request-scoped configuration set.
// webbuilder.Services.AddScoped<AIKeyStore>();// Values binded request-scoped option service is added to root service collection.
#endregion
#region TODO Cleanup(include kernel build)
var kernelbuilder = Kernel.CreateBuilder();
// .AddAzureOpenAITextEmbeddingGeneration("PromptFlowDeploy", apiKey!, generationService!)
// .AddAzureOpenAIChatCompletion("PromptFlowDeploy", apiKey!, "text-davinci-002");
// kernelbuilder.Plugins.AddFromType<PolyglotPersistencePlugin>();
// kernelbuilder.Plugins.AddFromObject(new PolyglotPersistencePlugin(apiKey: apiKey!,aiEndpoint: aiEndpoint!, modelID!), nameof(PolyglotPersistencePlugin));
kernelbuilder.Plugins.AddFromType<TimePlugin>();
kernelbuilder.Plugins.AddFromType<TravelExpensisPlugIn>();
Kernel kernel = kernelbuilder.Build();
#endregion
#endregion

#region  memory build
#region Milvus + HuggingFace（tokenizer + vectorizer）
// var apiKeyHuggingFace = webbuilder.Configuration["AI:HuggingFace:APIKey"];
// var aiEndpointHuggingFace = webbuilder.Configuration["AI:HuggingFace:Endpoint"];
// var modelIDHuggingFace = webbuilder.Configuration["AI:HuggingFace:ModelID"];
// var aiEndpointMilvus = webbuilder.Configuration["AI:Milvus:Endpoint"];
// var dbnameMilvus = webbuilder.Configuration["AI:Milvus:DBName"];
#endregion
#endregion

#region  OpenAI build
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    MaxTokens = 200,
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};
ChatHistory history = [];
history.AddSystemMessage(@"あなたは人々が出発元の駅名と到着先の駅名を指定して、片道の料金情報を見つけるのに役立つ仮想アシスタントです。自分の範囲外のことを話してはいけません。 返答は非常に簡潔かつ要点を絞ったものでなければなりません。人々と仲良くしましょう。");
#endregion

#region logger build
// var webapplogger = factory.CreateLogger("Program");
#endregion
var webapp = webbuilder.Build();

#region swagger UI build
webapp.UseSwagger();
webapp.UseSwaggerUI();
#endregion
// error handling
if (!webapp.Environment.IsDevelopment())
{
    webapp.UseExceptionHandler("/oops");
}

#region  middleware
webapp.MapGet("/", () => "出張旅費精算書作成Web").WithMetadata(new SwaggerOperationAttribute(summary: "ルートURL", description: "既定のURL"));
webapp.MapGet("/oops", () => "Oops! An error happened.").WithMetadata(new SwaggerOperationAttribute(summary: "エラーURL", description: "エラーハンドリングしたときに表示されるURL"));
#endregion
// Use IOptionsSnapshot to define scope for security
webapp.MapPost("/CreateTransportationPlan", ([FromBody] string query, IOptionsSnapshot<AIKeyStore> settings) => {
    var result = new List<string>();
    var keyStore = new List<AIKeyStore>();
    keyStore.Add(settings.Get(AIKeyStore.TextAnalyticsType));
    keyStore.Add(settings.Get(AIKeyStore.OpenAIType));
    keyStore.Add(settings.Get(AIKeyStore.BingType));
    keyStore.Add(settings.Get(AIKeyStore.MilvusType));
    keyStore.Add(settings.Get(AIKeyStore.HuggingFaceType));
    keyStore.Add(settings.Get(AIKeyStore.SearchServiceType));
    #region TODO Cleanup
    // result.Add("keyStore.UseAzure:" + keyStore.UseAzure);
    // result.Add("KeyStore.OpenAI.Deployment:" + keyStore.OpenAI.Deployment);
    // result.Add("KeyStore.OpenAI.APIKey:" + keyStore.OpenAI.APIKey);
    // result.Add("KeyStore.OpenAI.Endpoint:" + keyStore.OpenAI.Endpoint);
    // result.Add("KeyStore.OpenAI.GenerationService:" + keyStore.OpenAI.GenerationService);
    // result.Add("KeyStore.HuggingFace.APIKey:" + keyStore.HuggingFace.APIKey);
    // result.Add("KeyStore.HuggingFace.Endpoint:" + keyStore.HuggingFace.Endpoint);
    // result.Add("KeyStore.HuggingFace.ModelID:" + keyStore.HuggingFace.ModelID);
    // result.Add("KeyStore.Milvus.Endpoint:" + keyStore.Milvus.Endpoint);
    // result.Add("KeyStore.Milvus.DBName:" + keyStore.Milvus.DBName);
    // result.Add("KeyStore.SearchService.Endpoint:" + keyStore.SearchService.Endpoint);
    // result.Add("KeyStore.SearchService.Key:" + keyStore.SearchService.Key);
    // result.Add("KeyStore.TextAnalytics.APIKey:" + keyStore.TextAnalytics.APIKey);
    // result.Add("KeyStore.TextAnalytics.Endpoint:" + keyStore.TextAnalytics.Endpoint);
    // result.Add("KeyStore.Bing.APIKey:" + keyStore.Bing.APIKey);

    #endregion
    var db = new PolyglotPersistencePlugin(keyStore, false, false);
    var searchResult = db.SearchGeneric(query,keyStore.Where(s => s.SettingsName==AIKeyStore.BingType).FirstOrDefault()!.OptionalID).Result;
    if(searchResult==PolyglotPersistencePlugin.NotFound){
        #region bing build
        using ILoggerFactory factory = LoggerFactory.Create(provider => provider.AddConsole());
        var apiKeyBing = keyStore.Where(s => s.SettingsName==AIKeyStore.BingType).FirstOrDefault()!.APIKey;
        var bingConnector = new BingConnector(apiKeyBing!, factory);
        #endregion
        foreach (var item in bingConnector.SearchAsync(query, 10).Result)
        {
            result.Add(item);
        }
    }
    else
    {
        result.Add(searchResult);
    }
    return result;
});

// webapp.MapPost("/DetectLanguage", ([FromBody] string query, IOptionsSnapshot<AIKeyStore> settings) => {
webapp.MapPost("/DetectLanguage", ([FromBody] string query, IOptionsSnapshot<AIKeyStore> settings) => {
    var result = string.Empty;
    var keyStore = new List<AIKeyStore>();
    keyStore.Add(settings.Get(AIKeyStore.TextAnalyticsType));
    keyStore.Add(settings.Get(AIKeyStore.OpenAIType));
    keyStore.Add(settings.Get(AIKeyStore.BingType));
    keyStore.Add(settings.Get(AIKeyStore.MilvusType));
    keyStore.Add(settings.Get(AIKeyStore.SearchServiceType));
    keyStore.Add(settings.Get(AIKeyStore.HuggingFaceType));
    // keyStore.UseAzure = true;
    var db = new PolyglotPersistencePlugin(keyStore, true, false);
    // var searchResult = db.DetectLanguage(query).Result;
    try
    {
        result = db.DetectLanguage(query);
    }
    catch (System.Exception e)
    {
        result = e.Message + e.InnerException?.Message;
    }
    return  $"DetectLanguage result = [{result}]";
});

webapp.MapPost("/KeyPhrase", ([FromBody] string query, IOptionsSnapshot<AIKeyStore> settings) => {
    var result = string.Empty;
    var keyStore = new List<AIKeyStore>();
    keyStore.Add(settings.Get(AIKeyStore.TextAnalyticsType));
    keyStore.Add(settings.Get(AIKeyStore.OpenAIType));
    keyStore.Add(settings.Get(AIKeyStore.BingType));
    keyStore.Add(settings.Get(AIKeyStore.MilvusType));
    keyStore.Add(settings.Get(AIKeyStore.SearchServiceType));
    keyStore.Add(settings.Get(AIKeyStore.HuggingFaceType));
    // keyStore.UseAzure = true;
    var db = new PolyglotPersistencePlugin(keyStore, true, false);
    // var searchResult = db.DetectLanguage(query).Result;
    try
    {
        result = db.GetKeyPhrase(query);
    }
    catch (System.Exception e)
    {
        result = e.Message + e.InnerException?.Message;
    }
    return  $"DetectLanguage result = [{result}]";
});

webapp.MapPost("/GetVector", ([FromBody] string query, IOptionsSnapshot<AIKeyStore> settings) => {
    var result = string.Empty;
    var keyStore = new List<AIKeyStore>();
    keyStore.Add(settings.Get(AIKeyStore.TextAnalyticsType));
    keyStore.Add(settings.Get(AIKeyStore.OpenAIType));
    keyStore.Add(settings.Get(AIKeyStore.BingType));
    keyStore.Add(settings.Get(AIKeyStore.MilvusType));
    keyStore.Add(settings.Get(AIKeyStore.SearchServiceType));
    keyStore.Add(settings.Get(AIKeyStore.HuggingFaceType));
    var db = new PolyglotPersistencePlugin(keyStore, true, false);
    try
    {
        result = db.GetVector(query);
    }
    catch (System.Exception e)
    {
        result = e.Message + e.InnerException?.Message;
    }
    return  $"GetVector result = [{result}]";
});

webapp.Run();