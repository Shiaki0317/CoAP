namespace CoapDesktopSender.Core;

public sealed record CoapSendResult(
    bool Ok,

    // Raw combined log (TX/RX, blockwise, options, etc)
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
