using System.Diagnostics;

namespace trinova_erp_backend.Middleware
{

    public class RequestLifecycleMiddleware
    {
        private readonly RequestDelegate              _next;
        private readonly ILogger<RequestLifecycleMiddleware> _logger;

        public RequestLifecycleMiddleware(
            RequestDelegate                       next,
            ILogger<RequestLifecycleMiddleware>   logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            bool isSendEmail = context.Request.Path.Value?.Contains("send-email",
                StringComparison.OrdinalIgnoreCase) == true;

            var sw         = Stopwatch.StartNew();
            var traceId    = context.TraceIdentifier;
            var method     = context.Request.Method;
            var path       = context.Request.Path;
            var protocol   = context.Request.Protocol;   
            var requestSize = context.Request.ContentLength;

            if (isSendEmail)
            {
                _logger.LogInformation(
                    "[LIFECYCLE] ── REQUEST STARTED ──────────────────────────────\n" +
                    "  TraceId      : {TraceId}\n"                                    +
                    "  Method       : {Method}\n"                                     +
                    "  Path         : {Path}\n"                                       +
                    "  Protocol     : {Protocol}\n"                                   +
                    "  ContentLength: {ContentLength}\n"                              +
                    "  HasStarted   : {HasStarted} (should be false here)\n"          +
                    "  Time         : {Time}",
                    traceId, method, path, protocol,
                    requestSize.HasValue ? $"{requestSize} bytes" : "(chunked/unknown)",
                    context.Response.HasStarted,
                    DateTime.UtcNow.ToString("O"));
            }

            var ct = context.RequestAborted;
            await using var abortReg = ct.Register(() =>
            {
                if (isSendEmail)
                {
                    _logger.LogWarning(
                        "[LIFECYCLE] ── REQUEST ABORTED (client disconnected) ────────\n" +
                        "  TraceId    : {TraceId}\n"                                      +
                        "  Path       : {Path}\n"                                         +
                        "  Elapsed    : {Elapsed} ms\n"                                   +
                        "  HasStarted : {HasStarted}\n"                                   +
                        "  Protocol   : {Protocol}\n"                                     +
                        "  Implication: HTTP/2 stream was RST before response was written.\n" +
                        "               Check Railway proxy timeout vs SMTP connect time.",
                        traceId, path, sw.ElapsedMilliseconds,
                        context.Response.HasStarted,
                        protocol);
                }
            });

            if (isSendEmail)
            {
                _logger.LogInformation(
                    "[LIFECYCLE] HasStarted BEFORE next: {HasStarted} — TraceId={TraceId}",
                    context.Response.HasStarted, traceId);
            }

            Exception? caughtEx = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                throw;
            }
            finally
            {
                sw.Stop();

                if (isSendEmail)
                {
                    _logger.LogInformation(
                        "[LIFECYCLE] ── RESPONSE COMPLETED ───────────────────────────\n" +
                        "  TraceId       : {TraceId}\n"                                    +
                        "  Protocol      : {Protocol}\n"                                   +
                        "  StatusCode    : {Status}\n"                                     +
                        "  HasStarted    : {HasStarted} (true = response was written)\n"   +
                        "  ContentLength : {ContentLength}\n"                              +
                        "  Elapsed       : {Elapsed} ms\n"                                 +
                        "  Aborted       : {Aborted}\n"                                    +
                        "  Exception     : {Exception}",
                        traceId,
                        protocol,
                        context.Response.StatusCode,
                        context.Response.HasStarted,
                        context.Response.ContentLength.HasValue
                            ? $"{context.Response.ContentLength} bytes"
                            : "(chunked/unknown)",
                        sw.ElapsedMilliseconds,
                        ct.IsCancellationRequested,
                        caughtEx?.GetType().FullName ?? "(none)");

                    // ── Task 4: Railway-specific HTTP/2 warning ──────────────
                    if (protocol == "HTTP/2" && !context.Response.HasStarted)
                    {
                        _logger.LogWarning(
                            "[LIFECYCLE] ── HTTP/2 STREAM NOT COMPLETED ──────────────────\n" +
                            "  TraceId    : {TraceId}\n"                                      +
                            "  Protocol   : HTTP/2\n"                                         +
                            "  HasStarted : false — ASP.NET Core never wrote the response.\n" +
                            "  Diagnosis  : Railway/proxy likely sent GOAWAY or RST_STREAM\n" +
                            "               before this handler returned.\n"                   +
                            "  Common cause: SMTP operation exceeded Railway upstream timeout.",
                            traceId);
                    }
                    else if (protocol == "HTTP/2" && context.Response.HasStarted)
                    {
                        _logger.LogInformation(
                            "[LIFECYCLE] HTTP/2 stream completed normally — " +
                            "response WAS written. TraceId={TraceId}", traceId);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "[LIFECYCLE] {Method} {Path} → {Status} in {Elapsed} ms [{Protocol}] TraceId={TraceId}",
                        method, path, context.Response.StatusCode,
                        sw.ElapsedMilliseconds, protocol, traceId);
                }
            }
        }
    }

    public static class RequestLifecycleMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLifecycleLogging(
            this IApplicationBuilder app) =>
            app.UseMiddleware<RequestLifecycleMiddleware>();
    }
}
