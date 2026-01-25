namespace CoapDesktopSender.Core;

public sealed record CoapSendResult(
    bool Ok,

    // TX/RX/Blockwise/Options など総合ログ
    string Log,

    // Payload logs
    string? TextLog,
    string? CborLog,
    string? BinaryLog,

    // Inspector
    string? RequestSummary,
    string? ResponseSummary,
    string? RequestOptions,
    string? ResponseOptions
);
