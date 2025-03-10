// ReSharper disable once CheckNamespace
namespace BD.WTTS.Services;

partial class ProxyService
{
    const ProxyMode defaultProxyMode = ProxyMode.Hosts;
    const ushort httpsPort = 443;
    ProxyMode proxyMode = defaultProxyMode;

    /// <summary>
    /// 启动代理服务
    /// </summary>
    /// <returns></returns>
    async Task<OperateProxyServiceResult> StartProxyServiceAsync()
    {
        IReadOnlyCollection<AccelerateProjectDTO>? proxyDomains = EnableProxyDomains;
        IReadOnlyCollection<ScriptDTO>? scripts = default;
        bool isEnableScript = ProxySettings.IsEnableScript.Value;
        bool isOnlyWorkSteamBrowser = default;
        ushort proxyPort = default;
        string? proxyIp = ProxySettings.SystemProxyIp.Value;
        proxyMode = defaultProxyMode;
        bool isProxyGOG = default;
        bool onlyEnableProxyScript = ProxySettings.OnlyEnableProxyScript.Value;
        bool enableHttpProxyToHttps = ProxySettings.EnableHttpProxyToHttps.Value;
        bool socks5ProxyEnable = ProxySettings.Socks5ProxyEnable.Value ||
            (OperatingSystem.IsAndroid() && // Android VPN 模式使用 tun2socks
                ProxySettings.ProxyModeValue == ProxyMode.VPN);
        ushort socks5ProxyPortId = ProxySettings.Socks5ProxyPortId.Value;
        if (!ModelValidatorProvider.IsPortId(socks5ProxyPortId))
            socks5ProxyPortId = IProxySettings.DefaultSocks5ProxyPortId;
        bool twoLevelAgentEnable = ProxySettings.TwoLevelAgentEnable.Value;
        ExternalProxyType twoLevelAgentProxyType = ProxySettings.TwoLevelAgentProxyType.Value;
        if (!twoLevelAgentProxyType.IsDefined())
            twoLevelAgentProxyType = IReverseProxyService.Constants.DefaultTwoLevelAgentProxyType;
        string? twoLevelAgentIp = ReverseProxySettings.GetTwoLevelAgentIp(ProxySettings.TwoLevelAgentIp.Value);
        ushort twoLevelAgentPortId = ProxySettings.TwoLevelAgentPortId.Value;
        if (!ModelValidatorProvider.IsPortId(twoLevelAgentPortId))
            twoLevelAgentPortId = IProxySettings.DefaultTwoLevelAgentPortId;
        string? twoLevelAgentUserName = ProxySettings.TwoLevelAgentUserName.Value;
        string? twoLevelAgentPassword = ProxySettings.TwoLevelAgentPassword.Value;
        string? proxyDNS = ProxySettings.ProxyMasterDns.Value;

        Lazy<IPAddress> proxyIp_ = new(() => ReverseProxySettings.GetProxyIp(proxyIp));
        void SetProxyIp(IPAddress proxyIPAddress)
        {
            proxyIp_ = new(proxyIPAddress);
            proxyIp = proxyIPAddress.ToString();
        }

        if (isEnableScript)
        {
            await EnableProxyScripts.ContinueWith(e =>
            {
                scripts = e.Result?.ToImmutableArray();
            });
        }

#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
#if MACCATALYST
                    if (OperatingSystem.IsMacOS())
#endif
        {
            isOnlyWorkSteamBrowser = ProxySettings.IsOnlyWorkSteamBrowser.Value;
            proxyMode = ProxySettings.ProxyMode.Value;
            if (!proxyMode.IsDefined())
                proxyMode = defaultProxyMode;
            isProxyGOG = ProxySettings.IsProxyGOG.Value;
        }
#endif
        // macOS\Linux 上目前因权限问题仅支持 0.0.0.0(IPAddress.Any)
        if ((OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            && IPAddress.IsLoopback(proxyIp_.Value))
        {
            SetProxyIp(IPAddress.Any);
        }

        //reverseProxyService.HostProxyPortId = ProxySettings.HostProxyPortId;

        this.RaisePropertyChanged(nameof(EnableProxyDomains));
        this.RaisePropertyChanged(nameof(EnableProxyScripts));

        switch (proxyMode)
        {
            case ProxyMode.Hosts:
                var inUsePort = SocketHelper.IsUsePort(proxyIp_.Value, httpsPort);
                if (inUsePort)
                {
                    string? error_CommunityFix_StartProxyFaild443 = default;
                    if (OperatingSystem.IsWindows())
                    {
#pragma warning disable CA1416 // 验证平台兼容性
                        var p = SocketHelper.GetProcessByTcpPort(httpsPort);
#pragma warning restore CA1416 // 验证平台兼容性
                        if (p != null)
                        {
                            error_CommunityFix_StartProxyFaild443 =
                                Strings.CommunityFix_StartProxyFaild443___.Format(
                                    httpsPort, p.ProcessName, p.Id);
                        }
                    }
                    error_CommunityFix_StartProxyFaild443 ??=
                        Strings.CommunityFix_StartProxyFaild443_.Format(httpsPort);
                    if (OperatingSystem.IsLinux())
                    {
                        var processPath = Environment.ProcessPath;
                        processPath.ThrowIsNull();
                        var urlUnixHostAccess = string.Format(
                            Constants.Urls.OfficialWebsite_UnixHostAccess_,
                            WebUtility.UrlEncode(processPath));
                        Browser2.Open(urlUnixHostAccess);
                    }
                    return error_CommunityFix_StartProxyFaild443;
                }
                break;
            case ProxyMode.System:
                if (OperatingSystem.IsWindows() &&
                    proxyIp_.Value.Equals(IPAddress.Any))
                {
                    SetProxyIp(IPAddress.Loopback);
                }
                if (!platformService.SetAsSystemProxy(true,
                    proxyIp_.Value,
                    proxyPort))
                {
                    return Strings.CommunityFix_SetAsSystemProxyFail;
                }
                break;
            case ProxyMode.PAC:
                if (OperatingSystem.IsWindows() &&
                    proxyIp_.Value.Equals(IPAddress.Any))
                {
                    SetProxyIp(IPAddress.Loopback);
                }
                var pacUrl = $"http://{proxyIp_.Value}:{proxyPort}/pac";
                if (!platformService.SetAsSystemPACProxy(true, pacUrl))
                {
                    return Strings.CommunityFix_SetAsSystemPACProxyFail;
                }
                break;
        }

        ReverseProxySettings reverseProxySettings = new(proxyDomains, scripts,
            isEnableScript, isOnlyWorkSteamBrowser, proxyPort,
            proxyIp, proxyMode, isProxyGOG, onlyEnableProxyScript,
            enableHttpProxyToHttps, socks5ProxyEnable, socks5ProxyPortId,
            twoLevelAgentEnable, twoLevelAgentProxyType, twoLevelAgentIp,
            twoLevelAgentPortId, twoLevelAgentUserName, twoLevelAgentPassword, proxyDNS);
        byte[] reverseProxySettings_ = Serializable.SMP2(reverseProxySettings);
        var startProxyResult = await reverseProxyService.StartProxyAsync(reverseProxySettings_);
        if (startProxyResult)
        {
            switch (proxyMode)
            {
                case ProxyMode.Hosts: // 启动加速服务后修改 Hosts 文件
                    if (proxyDomains.Any_Nullable())
                    {
                        var localhost = IPAddress.Any.Equals(proxyIp_.Value) ?
                            IPAddress.Loopback.ToString() : proxyIp!;
                        var hosts = proxyDomains.SelectMany(s =>
                        {
                            if (s == null) return default!;

                            return s.ListeningDomainNamesArray.Select(host =>
                            {
                                if (host.Contains(' '))
                                {
                                    var h = host.Split(' ');
                                    return new KeyValuePair<string, string>(h[1], h[0]);
                                }
                                return new KeyValuePair<string, string>(host, localhost);
                            });
                        }).ToDictionaryIgnoreRepeat(x => x.Key, y => y.Value);
                        if (isEnableScript)
                        {
                            hosts.TryAdd(IReverseProxyService.Constants.LocalDomain, localhost);
                        }
                        var updateHostsResult = hostsFileService.UpdateHosts(hosts);
                        if (updateHostsResult.ResultType != OperationResultType.Success)
                        {
                            if (OperatingSystem2.IsMacOS())
                            {
                                Browser2.Open(Constants.Urls.OfficialWebsite_UnixHostAccess);
                                //platformService.RunShell($" \\cp \"{Path.Combine(IOPath.CacheDirectory, "hosts")}\" \"{platformService.HostsFilePath}\"");
                            }
                            await reverseProxyService.StopProxyAsync();
                            return Strings.OperationHostsError_.Format(updateHostsResult.Message);
                        }
                    }
                    break;
            }
            _StartAccelerateTime = DateTimeOffset.Now;
            StartTimer();
            return default;
        }
        else
        {
            var errorString = startProxyResult.Code switch
            {
                StartProxyResultCode.Exception => startProxyResult.Exception?.ToString() ?? nameof(StartProxyResultCode.Exception),
                _ => $"StartProxyFail, ErrCode: {startProxyResult.Code}",
            };
            return errorString;
        }
    }

    /// <summary>
    /// 停止代理服务
    /// </summary>
    async Task<OperateProxyServiceResult> StopProxyServiceAsync(bool isExit = false)
    {
        switch (proxyMode) // 先停止接入代理流量
        {
            case ProxyMode.Hosts:
                var needClear = hostsFileService.ContainsHostsByTag();
                if (needClear)
                {
                    var removeHostsResult = hostsFileService.RemoveHostsByTag();
                    if (removeHostsResult.ResultType != OperationResultType.Success)
                    {
                        // 退出时候不打开浏览器
                        if (!isExit && (OperatingSystem.IsMacOS() ||
                            (OperatingSystem.IsLinux() && !platformService.IsAdministrator)))
                        {
                            Browser2.Open(Constants.Urls.OfficialWebsite_UnixHostAccess);
                        }

                        // 还原 Hosts 失败阻止停止代理，因停止可能会导致写入部分 hosts 文件中的域名无法访问
                        if (!isExit)
                        {
                            return Strings.OperationHostsError_.Format(removeHostsResult.Message);
                        }
                    }
                }
                break;
            case ProxyMode.System:
                platformService.SetAsSystemProxy(false);
                break;
            case ProxyMode.PAC:
                platformService.SetAsSystemPACProxy(false);
                break;
        }
        StopTimer(); // 停止 UI 计时器
        await reverseProxyService.StopProxyAsync(); // 停止代理服务
        return default;
    }

    /// <summary>
    /// 操作代理服务返回结果
    /// </summary>
    /// <param name="Error"></param>
    readonly record struct OperateProxyServiceResult(string? Error)
    {
        public static implicit operator OperateProxyServiceResult(string error) => new(error);

        /// <summary>
        /// 将返回结果使用 Toast 显示并且返回当前应设置的代理状态
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OnStartedShowToastReturnProxyStatus()
        {
            string? text;
            bool proxyStatus;
            if (string.IsNullOrWhiteSpace(Error))
            {
                text = Strings.CommunityFix_StartProxySuccess;
                proxyStatus = true;
            }
            else
            {
                text = Error;
                proxyStatus = false;
            }
            Toast.Show(text);
            return proxyStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OnStopedShowToastReturnProxyStatus()
        {
            bool proxyStatus = default;
            if (!string.IsNullOrWhiteSpace(Error))
            {
                Toast.Show(Error);
                proxyStatus = true;
            }
            return proxyStatus;
        }
    }
}