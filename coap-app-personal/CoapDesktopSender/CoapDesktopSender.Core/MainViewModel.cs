using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CoapDesktopSender.Core;

public partial class MainViewModel : ObservableObject
{
    private readonly CoapSender _sender = new();

    public ObservableCollection<string> Methods { get; } = new(new[] { "GET", "POST", "PUT", "DELETE" });
    public ObservableCollection<string> Formats { get; } = new(new[]
    {
        "text/plain",
        "application/octet-stream",
        "application/cbor"
    });

    // ===== Basic request =====
    [ObservableProperty] private string uriText = "coap://127.0.0.1:5683/test";
    [ObservableProperty] private string method = "POST";

    // CON/NON
    [ObservableProperty] private bool useCon = true;
    [ObservableProperty] private bool useNon = false;

    // Observe
    [ObservableProperty] private bool useObserve = false;

    // Content-Format / Accept（将来用）
    [ObservableProperty] private bool setContentFormat = true;
    [ObservableProperty] private string contentFormatName = "application/cbor";
    [ObservableProperty] private bool setAccept = false;
    [ObservableProperty] private string acceptFormatName = "application/cbor";

    // ===== Timeout / Retry =====
    [ObservableProperty] private int timeoutMs = 3000;     // ユーザ設定タイムアウト（ms）
    [ObservableProperty] private int maxRetries = 1;       // 0=再送なし、1=最大2回送る（初回+再送1回）

    // ===== Payload mode =====
    [ObservableProperty] private bool isPayloadNone = false;
    [ObservableProperty] private bool isPayloadText = true;
    [ObservableProperty] private bool isPayloadHex = false;
    [ObservableProperty] private bool isPayloadCborJson = false;

    [ObservableProperty] private string payloadText = "hello";
    [ObservableProperty] private string payloadHex = "DE AD BE EF";
    [ObservableProperty] private string payloadJson = "{\"a\":1,\"b\":\"x\"}";

    private PayloadMode CurrentPayloadMode =>
        IsPayloadText ? PayloadMode.TextUtf8 :
        IsPayloadHex ? PayloadMode.HexBinary :
        IsPayloadCborJson ? PayloadMode.CborFromJson :
        IsPayloadNone ? PayloadMode.None :
        PayloadMode.TextUtf8;

    // ===== Blockwise =====
    [ObservableProperty] private bool enableBlock1 = false;
    [ObservableProperty] private int block1Num = 0;
    [ObservableProperty] private bool block1More = false;
    [ObservableProperty] private int block1Szx = 2; // 64 bytes

    [ObservableProperty] private bool enableBlock2 = false;
    [ObservableProperty] private int block2Num = 0;
    [ObservableProperty] private bool block2More = false;
    [ObservableProperty] private int block2Szx = 2;

    // ===== Inspector / Logs =====
    [ObservableProperty] private string requestSummary = "";
    [ObservableProperty] private string requestOptions = "";
    [ObservableProperty] private string responseSummary = "";
    [ObservableProperty] private string responseOptions = "";

    [ObservableProperty] private string trafficLog = "";

    [ObservableProperty] private string responseTextLog = "";
    [ObservableProperty] private string responseCborLog = "";
    [ObservableProperty] private string responseBinaryLog = "";

    [RelayCommand]
    private void ClearLog()
    {
        TrafficLog = "";
        RequestSummary = "";
        RequestOptions = "";
        ResponseSummary = "";
        ResponseOptions = "";
        ResponseTextLog = "";
        ResponseCborLog = "";
        ResponseBinaryLog = "";
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        try
        {
            // CON/NON 整合
            if (UseCon && UseNon) UseNon = false;
            if (!UseCon && !UseNon) UseCon = true;

            var uri = new Uri(UriText);

            var model = new CoapSendModel
            {
                UriText = UriText,
                Method = Method,
                PayloadMode = CurrentPayloadMode,
                PayloadText = PayloadText,
                PayloadHex = PayloadHex,
                PayloadJson = PayloadJson,
                UseCon = UseCon,
                UseObserve = UseObserve,
                SetContentFormat = SetContentFormat,
                ContentFormatName = ContentFormatName,
                SetAccept = SetAccept,
                AcceptFormatName = AcceptFormatName
            };

            byte[] payloadBytes = PayloadBuilder.Build(model) ?? Array.Empty<byte>();

            // ここでユーザ設定の timeout/retry を渡す
            var result = await _sender.SendAsync(
                uri: uri,
                method: Method,
                payload: payloadBytes,
                confirmable: UseCon,
                timeoutMs: TimeoutMs,
                maxRetries: MaxRetries,
                enableBlock1: EnableBlock1,
                block1Num: Block1Num,
                block1More: Block1More,
                block1Szx: Block1Szx,
                enableBlock2: EnableBlock2,
                block2Num: Block2Num,
                block2More: Block2More,
                block2Szx: Block2Szx,
                ct: CancellationToken.None
            );

            TrafficLog = result.Log;

            ResponseTextLog = result.TextLog ?? "";
            ResponseCborLog = result.CborLog ?? "";
            ResponseBinaryLog = result.BinaryLog ?? "";

            RequestSummary = result.RequestSummary ?? "";
            RequestOptions = result.RequestOptions ?? "";
            ResponseSummary = result.ResponseSummary ?? "";
            ResponseOptions = result.ResponseOptions ?? "";
        }
        catch (Exception ex)
        {
            TrafficLog = ex.ToString();
        }
    }
}
