namespace BD.WTTS;

public static class RegexHelper
{
    /// <summary>
    /// 如果输入正则表达式字符串是枚举，则将其替换为'expanded'正则表达式
    /// </summary>
    public static string ExpandRegex(string regex)
    {
        Dictionary<string, string> regexDictionary = new()
        {
                { "EMAIL_REGEX", "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])" },
                { "WIN_FILEPATH_REGEX", @"[a-zA-Z]:[\\\/](?:[a-zA-Z0-9]+[\\\/])*([a-zA-Z0-9]+\.[a-zA-Z]*)" }
        };

        return regexDictionary.ContainsKey(regex) ? regexDictionary[regex] : regex;
    }

    /// <summary>
    /// 获取注册表文件的字符串内容，或与正则表达式/通配符匹配的文件的路径。
    /// </summary>
    /// <param name="accFile"></param>
    /// <param name="regex"></param>
    /// <returns></returns>
    public static string? RegexSearchFileOrFolder(string accFile, string regex)
    {
        accFile = PathHelper.ExpandEnvironmentVariables(accFile);
        regex = ExpandRegex(regex);
        // The "file" is a registry key
        if (OperatingSystem.IsWindows() && accFile.StartsWith("REG:"))
        {
            var res = RegistryKeyHelper.ReadRegistryKey(accFile[4..]);
            if (res == null)
                return null;
            switch (res)
            {
                case string s:
                    return s;
                case byte[] bytes:
                    return HashStringHelper.GetSha256HashString(bytes);
                default:
                    Log.Warn(nameof(RegexHelper), $"REG was read, and was returned something that is not a string or byte array! {accFile}.");
                    return res?.ToString();
            }
        }

        // Handle wildcards
        if (accFile.Contains('*'))
        {
            var folder = PathHelper.ExpandEnvironmentVariables(Path.GetDirectoryName(accFile) ?? "");
            var file = Path.GetFileName(accFile);

            // Handle "...\\*" folder.
            // as well as "...\\*.log" or "...\\file_*", etc.
            // This is NOT recursive - Specify folders manually in JSON
            return Directory.Exists(folder) ? PathHelper.RegexSearchFolder(folder, regex, file) : "";
        }

        var fullPath = PathHelper.ExpandEnvironmentVariables(accFile);
        // Is folder? Search folder.
        if (Directory.Exists(fullPath))
            return PathHelper.RegexSearchFolder(fullPath, regex);

        // Is file? Search file
        var m = Regex.Match(File.ReadAllText(fullPath!), regex);
        return m.Success ? m.Value : "";
    }
}
