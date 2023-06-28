using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valour.Shared;

namespace LoanBroker.Models.NonDBO;

public class SVTransaction
{
    public long FromSvid { get; set; }
    public long ToSvid { get; set; }
    public decimal Amount { get; set; }
    public string ApiKey { get; set; }
    public string Detail { get; set; }
    public int TranType { get; set; }
    
    public SVTransaction(long fromsvid, long tosvid, decimal amount, string apiKey, string detail, int trantype)
    {
        FromSvid = fromsvid;
        ToSvid = tosvid;
        Amount = amount;
        ApiKey = apiKey;
        Detail = detail;
        TranType = trantype;
    }

    public async Task<TaskResult> ExecuteAsync(HttpClient client)
    {
#if DEBUG
        var baseurl = "https://localhost:7186/";
#else
        var baseurl = "https://spookvooper.com/";
#endif
        var detail = Detail.Replace(" ", "%20");
        var url = $"{baseurl}/api/eco/transaction/send?fromid={FromSvid}&toid={ToSvid}&amount={Amount}&apikey={ApiKey}&detail={detail}&trantype={TranType}";
        return JsonSerializer.Deserialize<TaskResult>(await client.GetStringAsync(url));
    }
}
