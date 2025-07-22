using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class FileReplacer
{
    public record ReplacementResult(bool Success, string Message, string? FilePath);

    public ReplacementResult ReplaceInFile(string filePath, string diffContent)
    {
        string NormalizeCodeText(string code)
        {
            return code;
            //   .Replace("\r\n", "\n")      // 统一换行符
            //   .Replace("\t", "    ")      // 制表符转4个空格
            //   .Trim();                  // 去除首尾空白
            //  .Replace(" ", "");          // 去除所有空格（可选）
        }
        try
        {
            // 统一换行符为\n
            diffContent = diffContent.Replace("\r\n", "\n").Replace("\r", "\n");

            // 验证文件存在
            if (!File.Exists(filePath))
            {
                return new ReplacementResult(false, $"文件不存在: {filePath}", null);
            }

            // 解析diff内容
            var replacements = ParseDiffContent(diffContent);
            if (!replacements.Any())
            {
                return new ReplacementResult(false, "未找到有效的SEARCH/REPLACE区块", null);
            }

            // 读取文件内容
            var fileContent = File.ReadAllText(filePath).Replace("\r\n", "\n").Replace("\r", "\n");
            var newContent = NormalizeCodeText(fileContent);
            var appliedCount = 0;

            // 按顺序应用每个替换
            foreach (var (search, replace) in replacements)
            {
                var regex = new Regex(Regex.Escape(NormalizeCodeText(search)));
                //var regex = new Regex(NormalizeCodeText(search));
                var match = regex.Match(newContent);
                var ss = newContent.IndexOf(search);
                if (match.Success)
                {
                    newContent = regex.Replace(newContent, replace, 1); // 只替换第一个匹配项
                    appliedCount++;
                }
                else
                {
                    return new ReplacementResult(false, $"未找到匹配内容:\n{search}", null);
                }
            }

            // 如果没有实际修改
            if (newContent == fileContent)
            {
                return new ReplacementResult(false, "文件内容未改变", null);
            }

            // 写入文件
            File.WriteAllText(filePath, newContent, Encoding.UTF8);
            return new ReplacementResult(true, $"成功应用 {appliedCount} 处替换", filePath);
        }
        catch (Exception ex)
        {
            return new ReplacementResult(false, $"处理文件时出错: {ex.Message}", null);
        }
    }

    private List<(string search, string replace)> ParseDiffContent(string diffContent)
    {
        // 统一换行符为\n
        diffContent = diffContent.Replace("\r\n", "\n").Replace("\r", "\n");
        var replacements = new List<(string, string)>();
        var pattern = @"-{7} SEARCH\n(.*?)\n={7}\n(.*?)\n\+{7} REPLACE";
        var matches = Regex.Matches(diffContent, pattern, RegexOptions.Singleline);
        //var pattern = @"-{7} SEARCH\r?\n(.*?)\r?\n={7}\r?\n(.*?)\r?\n\+{7} REPLACE";
        //var matches = Regex.Matches(diffContent, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            if (match.Groups.Count == 3)
            {
                var search = match.Groups[1].Value;
                var replace = match.Groups[2].Value;
                //replacements.Add((search, replace));

                // 保留原始空白字符，不进行trim
                replacements.Add((search, replace));
            }
        }

        return replacements;
    }
}

// 使用示例
public class Program
{
    //    public static void Main()
    //    {
    //        var replacer = new FileReplacer();

    //        // 示例diff内容
    //        var diff = @"
    //------- SEARCH
    //public class OldClass
    //{
    //    // 旧注释
    //    public void OldMethod()
    //=======
    //public class NewClass
    //{
    //    // 新注释
    //    public void NewMethod()
    //+++++++ REPLACE

    //------- SEARCH
    //    Console.WriteLine(""Hello"");
    //=======
    //    Console.WriteLine(""Hello World"");
    //+++++++ REPLACE";

    //        var result = replacer.ReplaceInFile("Example.cs", diff);

    //        Console.WriteLine(result.Success ? "替换成功" : "替换失败");
    //        Console.WriteLine(result.Message);
    //        if (result.Success)
    //        {
    //            Console.WriteLine($"修改的文件: {result.FilePath}");
    //        }
    //    }
}
