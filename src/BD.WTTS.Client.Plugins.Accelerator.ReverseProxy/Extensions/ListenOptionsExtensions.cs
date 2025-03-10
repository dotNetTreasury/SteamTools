#if !DISABLE_ASPNET_CORE && (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

static partial class ListenOptionsExtensions
{
    /// <summary>
    /// 使用流量分析中间件
    /// </summary>
    /// <param name="listen"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ListenOptions UseFlowAnalyze(this ListenOptions listen)
    {
        var flowAnalyzer = listen.ApplicationServices.GetRequiredService<IFlowAnalyzer>();
        listen.Use(next => async context =>
        {
            var oldTransport = context.Transport;
            try
            {
                await using var loggingDuplexPipe = new FlowAnalyzeDuplexPipe(context.Transport, flowAnalyzer);
                context.Transport = loggingDuplexPipe;
                await next(context);
            }
            finally
            {
                context.Transport = oldTransport;
            }
        });
        return listen;
    }

    /// <summary>
    /// 使用 Tls 中间件
    /// </summary>
    /// <param name="listen"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ListenOptions UseTls(this ListenOptions listen)
    {
        var certService = listen.ApplicationServices.GetRequiredService<CertService>();
        //certService.CreateCaCertIfNotExists();
        //certService.InstallAndTrustCaCert();
        return listen.UseTls(https => https.ServerCertificateSelector = (ctx, domain) => certService.GetOrCreateServerCert(domain));
    }

    /// <summary>
    /// 使用 Tls 中间件
    /// </summary>
    /// <param name="listen"></param>
    /// <param name="configureOptions">https配置</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ListenOptions UseTls(this ListenOptions listen, Action<HttpsConnectionAdapterOptions> configureOptions)
    {
        var invadeMiddleware = listen.ApplicationServices.GetRequiredService<TlsInvadeMiddleware>();
        var restoreMiddleware = listen.ApplicationServices.GetRequiredService<TlsRestoreMiddleware>();

        listen.Use(next => context => invadeMiddleware.InvokeAsync(next, context));
        listen.UseHttps(configureOptions);
        listen.Use(next => context => restoreMiddleware.InvokeAsync(next, context));
        return listen;
    }
}
#endif
