using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileNameRegexSearchTool
{
    public class FileNameMatcher
    {
        /// <summary>
        /// 根据正则表达式和/或通配符模式匹配文件名
        /// </summary>
        public static List<string> MatchFilenames(string path, string regexPattern, string filePattern = "*")
        {
            var results = new List<string>();
            if (filePattern == null)
                filePattern = "";

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Path not found: {path}");

            // 解析多个通配符（支持逗号、分号、空格分隔）
            string[] patterns = filePattern.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // 获取所有子文件
            foreach (string filePath in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(filePath);
                bool matchFound = false;

                // 匹配正则表达式
                try
                {
                    if (Regex.IsMatch(fileName, regexPattern))
                    {
                        matchFound = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid regex pattern: " + ex.Message);
                }

                // 匹配通配符
                foreach (string pattern in patterns)
                {
                    if (MatchWildcard(fileName, pattern.Trim()))
                    {
                        matchFound = true;
                        break;
                    }
                }

                if (matchFound)
                {
                    results.Add(filePath);
                }
            }

            return results.Distinct().ToList(); // 去重
        }

        /// <summary>
        /// 简单实现类似 Unix glob 的通配符匹配（支持 * 和 ?）
        /// </summary>
        private static bool MatchWildcard(string input, string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern)
                                        .Replace(@"\*", ".*")
                                        .Replace(@"\?", ".") + "$";

            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }


        //class Program
        //{
        //    static void Main(string[] args)
        //    {
        //        string path = @"E:\Codes\FocusNew\example_dir";  // 替换为你自己的测试目录
        //        string regex = @".*\.json$";
        //        string filePattern = "*.cs";

        //        try
        //        {
        //            var matchedFiles = FileNameMatcher.MatchFilenames(path, regex, filePattern);

        //            Console.WriteLine("Matching files:");
        //            foreach (var file in matchedFiles)
        //            {
        //                Console.WriteLine(file);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.Error.WriteLine("Error: " + ex.Message);
        //        }
        //    }
        //}

        private const string BaseDirectory = @"E:\Codes\FocusNew";
        /// <summary>
        /// 列出指定目录中的文件和子目录。
        /// </summary>
        /// <param name="path">要列出内容的目录路径（可以是相对路径或绝对路径）</param>
        /// <param name="recursive">是否递归查找，默认 false</param>
        /// <returns>返回包含文件路径和类型的列表</returns>
        public static List<FileSystemInfo> list_files(string path, bool recursive = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path 不能为空", nameof(path));
            // 如果是绝对路径，直接使用；否则拼接 BaseDirectory
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(BaseDirectory, path);
            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"指定的目录不存在: {fullPath}");
            var result = new List<FileSystemInfo>();
            // 列出目录项
            AddDirectoryContents(fullPath, result, recursive);
            return result;
        }
        private static void AddDirectoryContents(string dirPath, List<FileSystemInfo> list, bool recursive)
        {
            try
            {
                foreach (var file in Directory.GetFiles(dirPath))
                {
                    list.Add(new FileInfo(file));
                }
                // 添加当前目录下的所有子目录
                foreach (var subDir in Directory.GetDirectories(dirPath))
                {
                    list.Add(new DirectoryInfo(subDir));

                    // 如果是递归模式，继续深入这个子目录
                    if (recursive)
                    {
                        AddDirectoryContents(subDir, list, recursive);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"访问目录失败: {dirPath} - 错误: {ex.Message}");
            }
        }
    }
}
