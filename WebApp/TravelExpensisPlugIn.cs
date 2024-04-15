using Microsoft.SemanticKernel;
using System.ComponentModel;

public class TravelExpensisPlugIn()
{
    readonly HttpClient client = new HttpClient();

    [KernelFunction, Description("出発元の駅から到着先の駅までの交通費の料金を計算する")]
    [return: Description("片道の料金を返す")]
    public async Task<string> TrackFlightAsync(
    [Description("出発元の駅コード")] string source,
    [Description("到着先の駅コード")] string destination,
    [Description("候補の最大数")] int limit)
    {
        string url = $"https://roote.ekispert.net/result?arr_code={destination}&dep_code={source}&sort=time";
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }

}
