using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Windows;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Reflection;
using SharpCompress.Archives;
using SharpCompress.Common;
using MaaBATapAssistant.ViewModels;
using MaaBATapAssistant.Models;

namespace MaaBATapAssistant.Utils;

/* 更新策略：
 * 先检测软件版本，如果有更新则进行全文件更新
 * 如果软件版本已是最新，则检测资源文件版本，如果有更新则只更新资源文件
 * 启动软件时会自动进行一次资源文件更新的检测，如果有更新则提示用户
 * 执行每日凌晨3点/4点重启游戏时会自动检测+更新资源文件
 * 注：更新资源文件时不包括ocr模型文件
 */
public static class UpdateTool
{
    /// <summary>
    /// 先检查软件新版本，再检查资源文件新版本
    /// 如果软件有新版本则不检查资源文件。因为更新软件会同时更新资源文件
    /// </summary>
    /// <param name="needPrintLog">是否需要打印Log到主界面</param>
    /// <returns>是否发现新版本</returns>
    public static void CheckBothNewVersion(bool needPrintLog, out bool _appCanUpdate, out bool _resourcesCanUpdate)
    {
        _appCanUpdate = false;
        _resourcesCanUpdate = false;
        CheckNewVersion(true, needPrintLog, out bool appIsUpToDate, out bool appCanUpdate, true);
        if (appIsUpToDate)
        {
            CheckNewVersion(false, needPrintLog, out bool resourcesIsUpToDate, out bool resourcesCanUpdate, true);
            if (appIsUpToDate && resourcesIsUpToDate)
            {
                ProgramDataModel.Instance.UpdateInfo = "当前已是最新版本";
                if (needPrintLog)
                    Utility.PrintLog(ProgramDataModel.Instance.UpdateInfo);
            }
            else if (resourcesCanUpdate)
            {
                _resourcesCanUpdate = true;
            }
        }
        else
        {
            if (appCanUpdate)
                _appCanUpdate = true;
        }
    }

    /// <param name="checkApp">true为检查软件更新，false为检查资源文件更新</param>
    public static void CheckNewVersion(bool checkApp, bool needPrintLog, out bool isUpToDate, out bool canUpdate, bool asyncUpdateResources)
    {
        isUpToDate = false;
        canUpdate = false;
        // 获取并检查Version是否非空
        Version? localAppVersion = null;
        Version? localResourceVersion = null;
        if (checkApp)
        {
            localAppVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (localAppVersion is null)
            {
                ProgramDataModel.Instance.UpdateInfo = "获取当前软件版本失败";
                if (needPrintLog)
                    Utility.PrintLog(ProgramDataModel.Instance.UpdateInfo);
                Utility.MyDebugWriteLine("获取当前软件版本失败，LocalVersion为空");
                return;
            }
        }
        else
        {
            localResourceVersion = new(ProgramDataModel.Instance.ResourcesVersion);
            if (localResourceVersion is null)
            {
                ProgramDataModel.Instance.UpdateInfo = "获取当前资源文件版本失败";
                if (needPrintLog)
                    Utility.PrintLog(ProgramDataModel.Instance.UpdateInfo);
                Utility.MyDebugWriteLine("获取当前资源文件版本失败，LocalResourceVersion为空");
                return;
            }
        }
        // 获取版本号
        string apiUrl;
        if (checkApp)
        {
            apiUrl = ProgramDataModel.Instance.DownloadSource == EDownloadSource.Gitee ? MyConstant.AppGiteeApiUrl : MyConstant.AppGitHubApiUrl;
        }
        else
        {
            apiUrl = ProgramDataModel.Instance.DownloadSource == EDownloadSource.Gitee ? MyConstant.ResourcesGiteeApiUrl : MyConstant.ResourcesGitHubApiUrl;
        }
        if (!GetLatestVersionAndDownloadUrl(apiUrl, MyConstant.PlatformTag, out string latestVersionString, out _))
        {
            if (checkApp)
            {
                if (needPrintLog)
                    Utility.PrintLog("检查软件新版本时网络请求出错");
                Utility.MyDebugWriteLine("检查软件新版本时网络请求出错");
            }
            else
            {
                if (needPrintLog)
                    Utility.PrintLog("检查资源文件新版本时网络请求出错");
                Utility.MyDebugWriteLine("检查资源文件新版本时网络请求出错");
            }
            return;
        }
        // 比较版本号大小
        Version latestVersion = new(RemoveFirstLetterV(latestVersionString));
        if (checkApp)
        {
            Utility.MyDebugWriteLine($"App - LocalVersion:{localAppVersion} | LatestVersion:{latestVersion}");
            if (localAppVersion != null && localAppVersion.CompareTo(latestVersion) >= 0)
            {
                isUpToDate = true;
            }
            else
            {
                canUpdate = true;
                ProgramDataModel.Instance.UpdateInfo = $"发现软件新版本{latestVersionString}，是否更新？";
                if (needPrintLog)
                    Utility.PrintLog($"发现软件新版本{latestVersionString}，请前往设置页面手动更新");
                Utility.MyDebugWriteLine($"发现软件新版本 {latestVersionString}");
            }
        }
        else
        {
            Utility.MyDebugWriteLine($"Resource - LocalVersion:{localResourceVersion} | LatestVersion:{latestVersion}");
            if (localResourceVersion != null && localResourceVersion.CompareTo(latestVersion) >= 0)
            {
                isUpToDate = true;
                if (needPrintLog)
                    Utility.PrintLog("当前资源文件已是最新版本");
            }
            else
            {
                canUpdate = true;
                ProgramDataModel.Instance.UpdateInfo = $"发现资源文件新版本{latestVersionString}，是否更新？";
                if (needPrintLog)
                {
                    // 需要打印Log则表示是在主界面调用，则触发自动更新资源文件
                    if (ProgramDataModel.Instance.SettingsData.IsAutoUpdateResources)
                    {
                        Utility.PrintLog($"发现资源文件新版本{latestVersionString}");
                        if (asyncUpdateResources)
                        {
                            Task.Run(async () =>
                            {
                                await UpdateResource(true);
                            });
                        }
                        else
                        {
                            UpdateResource(true).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        Utility.PrintLog($"发现资源文件新版本{latestVersionString}，请前往设置页面手动更新");
                    }
                }
                else
                {
                    Utility.MyDebugWriteLine($"发现资源文件新版本{latestVersionString}");
                }
                
            }
        }
    }

    /// <summary>
    /// 通过api获取最新的版本号及其下载链接，不论是否为pre-release版本，此方法获取的json体积更小，更节省资源。
    /// <para>读取tag_name作为版本号，上传Release的时候请注意正确填写tag。</para>
    /// <para>读取browser_download_url作为下载链接，请注意上传的文件要为.zip .7z .rar等压缩文件</para>
    /// <para>platformTag字段为文件名中含有的平台标识，根据自己的需求填写</para>
    /// </summary>
    /// <param name="apiUrl">Api链接，不要带/latest后缀</param>
    /// <returns>获取失败返回false</returns>
    private static bool GetLatestVersionAndDownloadUrl(string apiUrl, string platformTag, out string latestVersionString, out string downloadUrl)
    {
        ProgramDataModel.Instance.IsCheckingNewVersion = true;
        ProgramDataModel.Instance.UpdateInfo = "正在检查更新...";
        Utility.MyDebugWriteLine($"正在检查更新,apiUrl = {apiUrl}");
        apiUrl += "/latest"; //获取最新版本，不论是否为pre-release
        latestVersionString = string.Empty;
        downloadUrl = string.Empty;
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

        try
        {
            using var response = httpClient.GetAsync(apiUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                using var read = response.Content.ReadAsStringAsync();
                read.Wait();
                string jsonString = read.Result;
                JObject json = JObject.Parse(jsonString);
                if (json == null)
                {
                    Utility.MyDebugWriteLine("获取的Json为空");
                    ProgramDataModel.Instance.IsCheckingNewVersion = false;
                    return false;
                }
                var tagNameToken = json["tag_name"];
                if (tagNameToken == null)
                {
                    Utility.MyDebugWriteLine("获取的tag_name为空");
                    ProgramDataModel.Instance.IsCheckingNewVersion = false;
                    return false;
                }
                latestVersionString = tagNameToken.ToString();

                if (json["assets"] is JArray assetsJsonArray && assetsJsonArray.Count > 0)
                {
                    foreach (var assetJsonObject in assetsJsonArray)
                    {
                        string? browserDownloadUrl = assetJsonObject["browser_download_url"]?.ToString();
                        if (!string.IsNullOrEmpty(browserDownloadUrl))
                        {
                            if (browserDownloadUrl.Contains(platformTag))
                            {
                                if (browserDownloadUrl.EndsWith(".zip") || browserDownloadUrl.EndsWith(".7z") || browserDownloadUrl.EndsWith(".rar"))
                                {
                                    downloadUrl = browserDownloadUrl;
                                    break;
                                }
                            }
                            else
                            {
                                ProgramDataModel.Instance.UpdateInfo = "ERR:下载链接中找不到对应的PlatformTag";
                                Utility.MyDebugWriteLine("下载链接中找不到对应的PlatformTag");
                            }
                        }
                    }
                }
            }
            else
            {
                ProgramDataModel.Instance.UpdateInfo = $"检查新版本时网络请求出错-{response.StatusCode}";
                Utility.MyDebugWriteLine($"检查新版本时网络请求出错: {response.StatusCode} - {response.ReasonPhrase}");
                ProgramDataModel.Instance.IsCheckingNewVersion = false;
                return false;
            }
        }
        catch (Exception e)
        {
            ProgramDataModel.Instance.UpdateInfo = "检查新版本时网络请求出错";
            Utility.MyDebugWriteLine($"检查新版本时网络请求出错: {e.Message}");
            ProgramDataModel.Instance.IsCheckingNewVersion = false;
            return false;
        }
        finally
        {
            httpClient.Dispose();
        }
        ProgramDataModel.Instance.IsCheckingNewVersion = false;
        return true;
    }

    /// <summary>去除版本号前的v或者V</summary>
    private static string RemoveFirstLetterV(string input)
    {
        if (!string.IsNullOrEmpty(input) && (input[0] == 'v' || input[0] == 'V'))
        {
            return input.Substring(1);
        }
        return input;
    }

    public static async Task<string> UpdateApp()
    {
        string apiUrl = ProgramDataModel.Instance.DownloadSource == EDownloadSource.Gitee ? MyConstant.AppGiteeApiUrl : MyConstant.AppGitHubApiUrl;
        if (!GetLatestVersionAndDownloadUrl(apiUrl, MyConstant.PlatformTag, out string latestVersionString, out string downloadUrl))
        {
            ProgramDataModel.Instance.HasNewVersion = false;
            Utility.MyDebugWriteLine("检查软件新版本号出错! - UpdateApp()");
            return string.Empty;
        }
        Utility.MyDebugWriteLine($"开始下载更新 - 由v{MyConstant.AppVersion}更新至{latestVersionString}");
        Utility.MyDebugWriteLine("DownloadUrl:" + downloadUrl);
        // 创建临时文件存放路径temp\
        var tempFileDirectory = @".\temp";
        if (Directory.Exists(tempFileDirectory))
        {
            Directory.Delete(tempFileDirectory, true);
            Directory.CreateDirectory(tempFileDirectory);
        }
        else
        {
            Directory.CreateDirectory(tempFileDirectory);
        }
        string tempFileName = GetFileNameFromUrl(downloadUrl);
        // 下载+解压文件到temp\
        MainViewModel.Instance.ProgramData.IsDownloadingFiles = true;
        MainViewModel.Instance.ProgramData.UpdateInfo = $"正在下载{tempFileName}";
        if (!await DownloadAndExtractFile(downloadUrl, tempFileDirectory, tempFileName))
        {
            MainViewModel.Instance.ProgramData.UpdateInfo = "文件下载失败！";
            Utility.MyDebugWriteLine("文件下载失败");
            MainViewModel.Instance.ProgramData.IsDownloadingFiles = false;
            return string.Empty;
        }
        MainViewModel.Instance.ProgramData.UpdateInfo = $"文件下载完成，是否重启并更新软件";
        Utility.MyDebugWriteLine($"{tempFileName}下载+解压完成");
        MainViewModel.Instance.ProgramData.IsDownloadingFiles = false;
        MainViewModel.Instance.ProgramData.IsReadyForApplyUpdate = true;
        MainViewModel.Instance.SetUpdateWindowTopmost(true);
        return tempFileName;
    }

    // 资源文件更新，下载+解压+覆盖文件
    public static async Task UpdateResource(bool needPrintLog)
    {
        string apiUrl = ProgramDataModel.Instance.DownloadSource == EDownloadSource.Gitee ? MyConstant.ResourcesGiteeApiUrl : MyConstant.ResourcesGitHubApiUrl;
        if (!GetLatestVersionAndDownloadUrl(apiUrl, MyConstant.PlatformTag, out string latestVersionString, out string downloadUrl))
        {
            ProgramDataModel.Instance.HasNewVersion = false;
            if (needPrintLog)
                Utility.PrintLog("检查资源文件新版本号出错");
            Utility.MyDebugWriteLine("检查资源文件新版本号出错! - UpdateResource()");
            return;
        }
        Utility.MyDebugWriteLine($"开始下载资源文件更新 - 由v{ProgramDataModel.Instance.ResourcesVersion}更新至{latestVersionString}");
        Utility.MyDebugWriteLine("DownloadUrl:" + downloadUrl);
        // 创建临时文件存放路径temp\
        var tempFileDirectory = @".\temp";
        if (Directory.Exists(tempFileDirectory))
        {
            Directory.Delete(tempFileDirectory, true);
            Directory.CreateDirectory(tempFileDirectory);
        }
        else
        {
            Directory.CreateDirectory(tempFileDirectory);
        }
        string tempFileName = GetFileNameFromUrl(downloadUrl);
        // 下载+解压文件到temp\
        MainViewModel.Instance.ProgramData.IsDownloadingFiles = true;
        if (needPrintLog)
            Utility.PrintLog($"正在下载并自动更新资源文件{latestVersionString}...");
        MainViewModel.Instance.ProgramData.UpdateInfo = $"正在下载并自动更新资源文件{latestVersionString}...";
        if (!await DownloadAndExtractFile(downloadUrl, tempFileDirectory, tempFileName))
        {
            if (needPrintLog)
                Utility.PrintLog($"资源文件文件下载失败");
            MainViewModel.Instance.ProgramData.UpdateInfo = "资源文件文件下载失败！";
            Utility.MyDebugWriteLine("资源文件文件下载失败");
            MainViewModel.Instance.ProgramData.IsDownloadingFiles = false;
            return;
        }
        MainViewModel.Instance.ProgramData.UpdateInfo = $"资源文件下载完成，正在自动更新...";
        Utility.MyDebugWriteLine($"{tempFileName}下载+解压完成，自动更新资源文件");

        // 生成update.bat文件执行文件删除+复制。不处理model文件夹
        var utf8Bytes = Encoding.UTF8.GetBytes(AppContext.BaseDirectory);
        var utf8BaseDirectory = Encoding.UTF8.GetString(utf8Bytes);
        var batFilePath = Path.Combine(utf8BaseDirectory, "temp", "update.bat");
        var extractedPath = $"\"{utf8BaseDirectory}temp\\{Path.GetFileNameWithoutExtension(tempFileName)}\\*.*\"";
        var targetPath = $"\"{utf8BaseDirectory}resources\"";
        await using (StreamWriter sw = new(batFilePath))
        {
            await sw.WriteLineAsync("@echo off");
            await sw.WriteLineAsync($"cd /d {targetPath}");
            await sw.WriteLineAsync("for /d %%D in (*) do (");
            await sw.WriteLineAsync("    if exist \"%%D\\model\" (");
            await sw.WriteLineAsync("        for /d %%F in (\"%%D\\*\") do (");
            await sw.WriteLineAsync("            if /i not \"%%~nxF\" == \"model\" (");
            await sw.WriteLineAsync("                rmdir /s /q \"%%F\"");
            await sw.WriteLineAsync("            )");
            await sw.WriteLineAsync("        )");
            await sw.WriteLineAsync("        for %%G in (\"%%D\\*.*\") do ( ");
            await sw.WriteLineAsync("            del /q \"%%G\"");
            await sw.WriteLineAsync("        )");
            await sw.WriteLineAsync("    ) else (");
            await sw.WriteLineAsync("        rmdir /s /q \"%%D\"");
            await sw.WriteLineAsync("    )");
            await sw.WriteLineAsync(")");
            await sw.WriteLineAsync($"xcopy /e /y {extractedPath} {targetPath}");
            await sw.WriteLineAsync("rd /s /q \"..\\temp\"");
        }
        var psi = new ProcessStartInfo(batFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        if (needPrintLog)
            Utility.PrintLog($"资源文件更新完成");
        MainViewModel.Instance.ProgramData.UpdateInfo = "资源文件更新完成";
        Utility.MyDebugWriteLine($"资源文件更新完成");
        MainViewModel.Instance.ProgramData.IsDownloadingFiles = false;
        MainViewModel.Instance.ProgramData.HasNewVersion = false;
        ProgramDataModel.Instance.ResourcesVersion = RemoveFirstLetterV(latestVersionString);
    }

    private static string GetFileNameFromUrl(string downloadUrl)
    {
        // 下载地址最后部分内容则为文件名，如果不符合规则，则使用默认文件名与格式TempFile.zip
        string tempFileName = "TempFile.zip";
        int lastIndex = downloadUrl.LastIndexOf('/');
        if (lastIndex != -1 && lastIndex < downloadUrl.Length - 1)
        {
            tempFileName = downloadUrl.Substring(lastIndex + 1);
        }
        return tempFileName;
    }

    /// <summary>
    /// 下载并解压文件，解压支持.zip .rar .7z
    /// </summary>
    private static async Task<bool> DownloadAndExtractFile(string url, string tempFileDirectory, string tempFileName)
    {
        string tempFilePath = Path.Combine(tempFileDirectory, tempFileName);
        Utility.MyDebugWriteLine(tempFilePath);
        MainViewModel.Instance.ProgramData.DownloadProgress = 0;
        MainViewModel.Instance.ProgramData.DownloadedSizeInfo = "";
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            long? contentLength = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead = 0;
            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                totalRead += bytesRead;

                if (contentLength.HasValue)
                {
                    double percentage = ((double)totalRead / contentLength.Value) * 100;
                    string totalReadMB = ((double)totalRead / 1024f / 1024f).ToString("0.00");
                    string contentLengthMB = ((double)contentLength / 1024f / 1024f).ToString("0.00");
                    MainViewModel.Instance.ProgramData.DownloadProgress = percentage;
                    MainViewModel.Instance.ProgramData.DownloadedSizeInfo = $"已下载 {totalReadMB}MB / {contentLengthMB}MB";
                }
                // 保存文件
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
            fileStream.Close();

            // 解压文件
            var extractDir = Path.Combine(tempFileDirectory, Path.GetFileNameWithoutExtension(tempFileName));
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, true);
            }
            if (!File.Exists(tempFilePath))
            {
                Utility.MyDebugWriteLine("找不到已下载的文件!");
                return false;
            }
            switch (Path.GetExtension(tempFilePath))
            {
                case ".zip":
                    ZipFile.ExtractToDirectory(tempFilePath, extractDir);
                    break;
                case ".rar":
                case ".7z":
                    Directory.CreateDirectory(extractDir);
                    var archive = ArchiveFactory.Open(tempFilePath);
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            if (!Directory.Exists(extractDir))
                            {
                                Directory.CreateDirectory(extractDir);
                            }
                            entry.WriteToDirectory(extractDir, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                    break;
            }
        }
        catch (HttpRequestException httpEx)
        {
            Utility.MyDebugWriteLine($"HTTP请求出现异常: {httpEx.Message}");
            return false;
        }
        catch (IOException ioEx)
        {
            Utility.MyDebugWriteLine($"文件操作出现异常: {ioEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Utility.MyDebugWriteLine($"出现未知异常: {ex.Message}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 应用更新。替换文件+重启软件
    /// </summary>
    public static async void ApplyUpdate(string tempFileName)
    {
        Utility.MyDebugWriteLine("开始应用更新");
        if (!Directory.Exists(Path.Combine(@".\temp", Path.GetFileNameWithoutExtension(tempFileName))))
        {
            Utility.MyGrowlError("解压的文件不存在!");
            return;
        }
        // 把不提醒公告的设置改为false
        MainViewModel.Instance.ProgramData.SettingsData.DoNotShowAnnouncementAgain = false;
        SettingsViewModel.UpdateConfigJsonFile();
        
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null)
        {
            Utility.MyDebugWriteLine("GetEntryAssembly 失败");
            return;
        }
        // 生成update.bat文件来实现复制文件+启动应用
        var currentExeFileName = assembly.GetName().Name + ".exe";
        var utf8Bytes = Encoding.UTF8.GetBytes(AppContext.BaseDirectory);
        var utf8BaseDirectory = Encoding.UTF8.GetString(utf8Bytes);
        var batFilePath = Path.Combine(utf8BaseDirectory, "temp", "update.bat");
        var extractedPath = $"\"{utf8BaseDirectory}temp\\{Path.GetFileNameWithoutExtension(tempFileName)}\\*.*\"";
        var targetPath = $"\"{utf8BaseDirectory}\"";
        await using (StreamWriter sw = new(batFilePath))
        {
            await sw.WriteLineAsync("@echo off");
            await sw.WriteLineAsync("chcp 65001");
            await sw.WriteLineAsync("ping 127.0.0.1 -n 3 > nul");
            await sw.WriteLineAsync($"cd /d {targetPath}");
            await sw.WriteLineAsync("for /d %%D in (*) do (");
            await sw.WriteLineAsync("    if /i not \"%%D\"==\"config\" if /i not \"%%D\"==\"debug\" if /i not \"%%D\"==\"images\" if /i not \"%%D\"==\"temp\" (");
            await sw.WriteLineAsync("        rd /s /q \"%%D\"");
            await sw.WriteLineAsync("    )");
            await sw.WriteLineAsync(")");
            await sw.WriteLineAsync("del /q \"*\"");
            await sw.WriteLineAsync($"xcopy /e /y {extractedPath} {targetPath}");
            await sw.WriteLineAsync($"start /d \"{utf8BaseDirectory}\" {currentExeFileName}");
            await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
            await sw.WriteLineAsync("rd /s /q \"temp\"");
        }
        var psi = new ProcessStartInfo(batFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        Application.Current.Shutdown();
    }
}
