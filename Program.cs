using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using FileNameRegexSearchTool;
using System.Xml.Linq;
using DiffMatchPatch;
using System.Runtime.Intrinsics.Arm;

namespace ProblemsResponse
{
    internal class Program
    {
        const string apiUrl = "http://git..com/api/v1/";
        const string owner = "USERS";
        const string repo = "Problems";
        const string token = "";
        const string aiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
        const string aiKey = "sk-";
        const string aiModel = "qwen3-235b-a22b";
        static string rootPath = @"E:\Codes\FocusNew";
        private static int isProcessing = 0; // 0 = idle, 1 = processing

        private static async Task ReplyToIssueAsync(HttpClient client, string apiUrl, string owner, string repo, int issueNumber, string message)
        {
            var commentBody = new { body = message.Replace("<replace_in_file>", @"```
<replace_in_file>").Replace("</replace_in_file>", @"</replace_in_file>
```").Replace(rootPath, string.Empty, StringComparison.OrdinalIgnoreCase) };
            var commentJson = JsonSerializer.Serialize(commentBody);
            var commentContent = new StringContent(commentJson, System.Text.Encoding.UTF8, "application/json");

            var commentResponse = await client.PostAsync(
                $"{apiUrl}repos/{owner}/{repo}/issues/{issueNumber}/comments",
                commentContent);

            commentResponse.EnsureSuccessStatusCode();
            Console.WriteLine("已成功回复issue");
        }

        private static async Task CloseIssueAsync(HttpClient client, string apiUrl, string owner, string repo, int issueNumber)
        {
            var closeBody = new { state = "closed" };
            var closeJson = JsonSerializer.Serialize(closeBody);
            var closeContent = new StringContent(closeJson, Encoding.UTF8, "application/json");

            var closeResponse = await client.PatchAsync(
                $"{apiUrl}repos/{owner}/{repo}/issues/{issueNumber}",
                closeContent);

            closeResponse.EnsureSuccessStatusCode();
            Console.WriteLine("已关闭issue");
        }
        private static async Task CheckAndProcessIssues()
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);

            var response = await client.GetAsync($"{apiUrl}repos/{owner}/{repo}/issues?state=open");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API响应: {responseContent}");

            var issues = await JsonSerializer.DeserializeAsync<List<Issue>>(
                await response.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var oldestOpenIssue = issues?
                .Where(i => i.IsPullRequest == null || !i.IsPullRequest.Value)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefault();

            if (oldestOpenIssue != null)
            {
                Console.WriteLine($"最早未关闭的issue: #{oldestOpenIssue.Number} - {oldestOpenIssue.Title}");
                Console.WriteLine($"创建时间: {oldestOpenIssue.CreatedAt}");
                Console.WriteLine($"链接: {oldestOpenIssue.HtmlUrl}");

                // 调用AI接口获取回复
                var aiService = new AIService(aiUrl, aiKey, aiModel);

                try
                {
                    var loops = 0;
                    do
                    {
                        //var aiResponse = await aiService.GetAIResponseAsync($"请帮我回复这个issue: {oldestOpenIssue.Body}");
                        var aiResponse = await aiService.GetAIResponseAsync($"{oldestOpenIssue.Body}");
                    __NEW__: { }
                        Console.WriteLine($"AI回复: {aiResponse}");

                        await ReplyToIssueAsync(client, apiUrl, owner, repo, oldestOpenIssue.Number, aiResponse);

                        #region 自动化编程
                        //if (aiResponse.IndexOf("请确认") != -1 && aiResponse.IndexOf("继续") != -1 && aiResponse.IndexOf("修复") != -1)
                        //{
                        //    Console.WriteLine("自动进行修复确认");
                        //    aiResponse = await aiService.GetAIResponseAsync("继续");
                        //    goto __NEW__;
                        //}

                        //// 如果你希望我根据这些优化建议生成修改后的完整代码，请告诉我！

                        //if (aiResponse.IndexOf("你希望") != -1 && aiResponse.IndexOf("代码") != -1 && aiResponse.IndexOf("生成") != -1 && aiResponse.IndexOf("代码") != -1 && (aiResponse.IndexOf("优化") != -1 || aiResponse.IndexOf("建议") != -1))
                        //{
                        //    Console.WriteLine("自动进行修复确认");
                        //    aiResponse = await aiService.GetAIResponseAsync("使用工具进行修改");
                        //    goto __NEW__;
                        //}
                        ////是否继续？
                        //if (aiResponse.IndexOf("是否继续") != -1)
                        //{
                        //    Console.WriteLine("自动继续");
                        //    aiResponse = await aiService.GetAIResponseAsync("继续");
                        //    goto __NEW__;
                        //}
                        //// 优化已完成
                        //if (aiResponse.IndexOf("已完成") != -1 && (aiResponse.IndexOf("优化") != -1 || aiResponse.IndexOf("修改") != -1))
                        //{
                        //    Console.WriteLine("自动验证");
                        //    aiResponse = await aiService.GetAIResponseAsync("重新读取确认无误");
                        //    goto __NEW__;
                        //}
                        #endregion


                        // 步骤1: 提取XML
                        string xmlContent = ExtractXml(aiResponse);
                        if (string.IsNullOrEmpty(xmlContent))
                        {
                            Console.WriteLine("未找到有效的 XML");
                            break;
                        }
                        xmlContent = xmlContent.Replace("<diff>", "<diff><![CDATA[").Replace("</diff>", "]]></diff>");

                        // 步骤2: 解析XML
                        XmlDocument doc = new XmlDocument();
                        try
                        {
                            doc.LoadXml(xmlContent);
                        }
                        catch (XmlException ex)
                        {
                            try
                            {
                                //var settings = new XmlReaderSettings
                                //{
                                //    CheckCharacters = false
                                //};
                                //using (var reader = XmlReader.Create(new StringReader(xmlContent), settings))
                                //{
                                //doc = new XmlDocument();
                                doc.LoadXml(xmlContent);
                                //}
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine("XML 格式错误：" + ex.Message);
                                aiResponse = await aiService.GetAIResponseAsync("XML 格式错误：" + ex.Message);
                                goto __NEW__;
                            }
                        }
                        // 获取根元素名（方法名）
                        XmlElement root = doc.DocumentElement;
                        string methodName = root.Name;
                        Console.WriteLine("方法名：" + methodName);
                        Console.WriteLine("参数列表：");
                        // 遍历所有直接子节点，读取参数
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                string key = node.Name;
                                string value = node.InnerText;
                                Console.WriteLine($"  {key} = {value}");
                            }
                        }

                        // 根据方法名调用对应方法
                        if (methodName == "read_file")
                        {
                            string path = root.SelectSingleNode("path")?.InnerText;
                            if (!string.IsNullOrEmpty(path))
                            {
                                string fileContent = read_file(path);
                                Console.WriteLine($"文件内容:\n{fileContent}");
                                aiResponse = await aiService.GetAIResponseAsync(fileContent);
                                goto __NEW__;
                            }
                            else
                            {
                                Console.WriteLine("缺少path参数");
                            }
                        }
                        else if (methodName == "search_files")
                        {
                            string path = root.SelectSingleNode("path")?.InnerText;
                            string regex = root.SelectSingleNode("regex")?.InnerText;
                            string filePattern = root.SelectSingleNode("filePattern")?.InnerText;

                            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(regex))
                            {
                                string searchResult = search_file(path, regex, filePattern);
                                Console.WriteLine($"搜索结果(完整路径|最后修改时间):\n{searchResult}");
                                aiResponse = await aiService.GetAIResponseAsync(searchResult);
                                goto __NEW__;
                            }
                            else
                            {
                                Console.WriteLine("缺少path或regex参数");
                            }
                        }
                        else if (methodName == "replace_in_file")
                        {
                            string path = root.SelectSingleNode("path")?.InnerText;
                            string diff = root.SelectSingleNode("diff")?.InnerText;
                            diff = diff.Replace(">>>>>>> REPLACE", "+++++++ REPLACE");

                            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(diff))
                            {
                                if (!IsAbsolutePath(path))
                                {
                                    path = Path.Combine(rootPath, path);
                                }
                                var replacer = new FileReplacer();

                                var result = replacer.ReplaceInFile(path, diff);

                                var req = (result.Success ? "替换成功" : "替换失败") + "," + result.Message;
                                if (result.Message.IndexOf("未找到有效的") != -1)
                                {
                                    req += ", 请确认范例与格式\r\n" +
"""
# Tool Define

## replace_in_file
Description: Request to replace sections of content in an existing file using SEARCH/REPLACE blocks that define exact changes to specific parts of the file. This tool should be used when you need to make targeted changes to specific parts of a file.
Parameters:
- path: (required) The path of the file to modify (relative to the current working directory ${cwd.toPosix()})
- diff: (required) One or more SEARCH/REPLACE blocks following this exact format:
  \`\`\`
  ------- SEARCH
  [exact content to find]
  =======
  [new content to replace with]
  +++++++ REPLACE
  \`\`\`
  Critical rules:
  1. SEARCH content must match the associated file section to find EXACTLY:
     * Match character-for-character including whitespace, indentation, line endings
     * Include all comments, docstrings, etc.
  2. SEARCH/REPLACE blocks will ONLY replace the first match occurrence.
     * Including multiple unique SEARCH/REPLACE blocks if you need to make multiple changes.
     * Include *just* enough lines in each SEARCH section to uniquely match each set of lines that need to change.
     * When using multiple SEARCH/REPLACE blocks, list them in the order they appear in the file.
  3. Keep SEARCH/REPLACE blocks concise:
     * Break large SEARCH/REPLACE blocks into a series of smaller blocks that each change a small portion of the file.
     * Include just the changing lines, and a few surrounding lines if needed for uniqueness.
     * Do not include long runs of unchanging lines in SEARCH/REPLACE blocks.
     * Each line must be complete. Never truncate lines mid-way through as this can cause matching failures.
  4. Special operations:
     * To move code: Use two SEARCH/REPLACE blocks (one to delete from original + one to insert at new location)
     * To delete code: Use empty REPLACE section
Usage:
<replace_in_file>
<path>File path here</path>
<diff>
Search and replace blocks here
</diff> 
</replace_in_file>

# Example

<replace_in_file>
<path>src/components/App.tsx</path>
<diff>
------- SEARCH
import React from 'react';
=======
import React, { useState } from 'react';
+++++++ REPLACE

------- SEARCH
function handleSubmit() {
  saveData();
  setLoading(false);
}

=======
+++++++ REPLACE

------- SEARCH
return (
  <div>
=======
function handleSubmit() {
  saveData();
  setLoading(false);
}

return (
  <div>
+++++++ REPLACE
</diff>
</replace_in_file>
""";
                                }
                                if (result.Message.IndexOf("未找到匹配内容") != -1)
                                {
                                    req += "; 以下在重新提供完整文件, 包在content标签内" +
                                        "<content>" +
$"""
{File.ReadAllText(path)}
"""
                                        + "</content>";
                                }
                                aiResponse = await aiService.GetAIResponseAsync(req);

                                goto __NEW__;
                            }
                            else
                            {
                                Console.WriteLine("缺少path或diff参数");
                                aiResponse = await aiService.GetAIResponseAsync("缺少path或diff参数");
                                goto __NEW__;
                            }


                        }
                        break;
                    } while (++loops <= 5);

                __END__: { }
                    Console.WriteLine("AI回复处理完成。");


                    await CloseIssueAsync(client, apiUrl, owner, repo, oldestOpenIssue.Number);
                }
                catch (Exception ex)
                {
                    await ReplyToIssueAsync(client, apiUrl, owner, repo, oldestOpenIssue.Number, $"AI请求异常: {ex.Message}");
                    await CloseIssueAsync(client, apiUrl, owner, repo, oldestOpenIssue.Number);

                    Console.WriteLine($"AI请求异常: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("没有找到未关闭的issue");
            }
        }

        /// <summary>
        /// 应用 Unified Diff 到原始文本
        /// </summary>
        /// <param name="originalText">原始文本</param>
        /// <param name="unifiedDiff">Unified Diff 字符串</param>
        /// <returns>修改后的文本</returns>
        public static string ApplyUnifiedDiff(string originalText, string unifiedDiff)
        {
            var originalLines = originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var diffLines = unifiedDiff.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var resultLines = new List<string>(originalLines);
            int currentPosition = 0;

            foreach (var diffLine in diffLines)
            {
                if (string.IsNullOrWhiteSpace(diffLine) || diffLine.StartsWith("---") || diffLine.StartsWith("+++"))
                    continue;

                if (diffLine.StartsWith("@@"))
                {
                    // 解析位置信息，例如: @@ -1,3 +1,4 @@
                    var hunkHeader = diffLine.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (hunkHeader.Length >= 3)
                    {
                        int.TryParse(hunkHeader[1].Substring(1), out currentPosition); // 获取原始文件起始行
                        currentPosition--; // 转换为0-based索引
                    }
                    continue;
                }

                if (currentPosition >= 0 && currentPosition < resultLines.Count)
                {
                    switch (diffLine[0])
                    {
                        case ' ': // 未修改的行
                            currentPosition++;
                            break;
                        case '-': // 删除的行
                            resultLines.RemoveAt(currentPosition);
                            break;
                        case '+': // 添加的行
                            resultLines.Insert(currentPosition, diffLine.Substring(1));
                            currentPosition++;
                            break;
                    }
                }
            }

            return string.Join(Environment.NewLine, resultLines);
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("启动Gitea issue检查服务，每10秒检查一次...");

            var timer = new System.Threading.Timer(async _ =>
            {
                if (System.Threading.Interlocked.CompareExchange(ref isProcessing, 1, 0) == 0)
                {
                    try
                    {
                        await CheckAndProcessIssues();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"检查issue时出错: {ex.Message}");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref isProcessing, 0);
                    }
                }
                else
                {
                    Console.WriteLine("上次任务尚未完成，跳过本次检查");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            timer.Dispose();
        }

        static string search_file(string path, string regex, string filePattern = null)
        {
            try
            {
                if (!IsAbsolutePath(path))
                {
                    path = Path.Combine(rootPath, path);
                }
                var matchedFiles = FileNameMatcher.MatchFilenames(path, regex, filePattern);
                if (matchedFiles.Count == 0)
                {
                    return "warn, no files found";
                }
                StringBuilder sb1 = new StringBuilder();
                foreach (var file in matchedFiles)
                {
                    sb1.AppendLine(file + $"|{new FileInfo(file).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")}");
                }

                return sb1.ToString();
            }
            catch (Exception ex)
            {
                return "error, " + ex.Message;
            }
        }

        // 提取 XML 内容的方法
        static string ExtractXml(string input)
        {
            //string pattern = @"(<\s*[a-zA-Z][^>]*>.*?<\s*\/\s*[a-zA-Z][^>]*>)";
            //Regex regex = new Regex(pattern, RegexOptions.Singleline);
            //Match match = regex.Match(input);
            //if (match.Success)
            //{
            //    return match.Groups[1].Value.Trim();
            //}
            //return null;
            // 匹配最外层的完整 XML 标签块（从 <tag 开始到 </tag> 结束）
            string pattern = @"<(\w+)[^>]*>([\s\S]*?)</\1>";
            Regex regex = new Regex(pattern, RegexOptions.Singleline);
            Match match = regex.Match(input);
            if (match.Success)
            {
                return match.Value.Trim();
            }
            return null;
        }

        static bool IsAbsolutePath(string path)
        {
            return Path.IsPathRooted(path);
        }
        static string AddLineNumbersToFileContent(string content)
        {
            return content;
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int lineNumberWidth = lines.Length.ToString().Length; // 动态计算位数，如最多999，宽度为3
            var result = lines
                .Select((line, index) =>
                    $"{(index + 1).ToString().PadLeft(lineNumberWidth)}|{line}");
            return string.Join(Environment.NewLine, result);
        }
        static string read_file(string path)
        {

            //try
            //{
            //    var result = FileSearcher.SearchFiles(rootPath, @"public.*void.*QueryData$", "*.cs");
            //    StringBuilder sb1 = new StringBuilder();
            //    foreach (var item in result)
            //    {
            //        sb1.AppendLine(item.ToString());
            //    }
            //}
            //catch (Exception ex)
            //{
            //}

            var readFile = path;
            if (!IsAbsolutePath(readFile))
            {
                readFile = Path.Combine(rootPath, readFile);
            }
            if (!File.Exists(readFile))
            {
                return "error, file not found";
            }
            var readAllText = AddLineNumbersToFileContent(File.ReadAllText(readFile));
            return readAllText;
        }
    }



    public class User
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    public class Issue
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("user")]
        public User? Reporter { get; set; }

        [JsonPropertyName("pull_request")]
        public bool? IsPullRequest { get; set; }
    }
}
