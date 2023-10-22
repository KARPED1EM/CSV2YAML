using Csv;
using System.Text;

internal static class Program
{
    private class CsvFile
    {
        public Dictionary<string, Dictionary<int, string>> map = new();
        public Dictionary<int, string> notes = new();
        public List<string> langs = new();
        public int endLine;
    }
    private static void Main()
    {

    InputPath:

        Console.WriteLine("输入要操作的Csv文件：");
        var input = Console.ReadLine();
        if (input is null or "") input = "D:/Desktop/CSV2YAML/String.csv";
        if (!File.Exists(input))
        {
            Console.Clear();
            Console.WriteLine("错误：文件路径无效");
            goto InputPath;
        }

        var originalFile = new CsvFile();
        ReadFile(ref originalFile, input, false);

        Console.ReadLine();
        Console.Clear();

    InputNumToOutput:
        
        originalFile.langs.ForEach(lang => Console.Write(lang + " "));
        Console.WriteLine("\n输入您想要导出的语言序号：(默认：自动导出TONX的语言模板)");
        input = Console.ReadLine() ?? "";
        
        if (input == "")
        {
            Console.Clear();
            WriteYAML(originalFile, "D:/Desktop/TONX/TONX/Resources/Lang/zh_CN.yaml", 13);
            WriteYAML(originalFile, "D:/Desktop/TONX/TONX/Resources/Lang/en_US.yaml", 0);
            WriteYAML(originalFile, "D:/Desktop/TONX/TONX/Resources/Lang/ru_RU.yaml", 5);
            WriteYAML(originalFile, "D:/Desktop/TONX/TONX/Resources/Lang/zh_TW.yaml", 14);
            goto EndOfApp;
        }

        if (!int.TryParse(input, out int lang) || !originalFile.langs.Contains(lang.ToString()))
        {
            Console.Clear();
            Console.WriteLine("错误：无效的序号");
            goto InputNumToOutput;
        }

        Console.WriteLine("\n输入导出的文件名：（默认：output.yaml）");
        input = Console.ReadLine() ?? "";
        input = input == "" ? "output" : input;
        if (!input.ToLower().EndsWith(".yaml"))
            input += ".yaml";

        Console.Clear();
        WriteYAML(originalFile, "D:/Desktop/" + input, lang);

        goto InputNumToOutput;

    EndOfApp:

        Console.WriteLine("按任意键继续...");
        Console.ReadLine();

    }
    private static void WriteYAML(CsvFile file, string path, int lang)
    {
        var sb = new StringBuilder();
        int index = 2;

        foreach ((string key, var trans) in file.map)
        {
        StartFromLoop:
            index++;
            if (file.notes.ContainsKey(index))
            {
                sb.Append(file.notes[index].TrimStart('\"').TrimEnd('\"') + "\n");
                goto StartFromLoop;
            }

            sb.Append(key + ": \"");

            if (trans.TryGetValue(lang, out var value))
                sb.Append(value.TrimStart('\"').TrimEnd('\"'));

            sb.Append("\"\n");
        }

        if (!File.Exists(path)) File.Create(path).Close();
        File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"写到文件：{Path.GetFullPath(path)}");
    }
    private static bool ReadFile(ref CsvFile file, string path, bool cache = false)
    {
        file.map = new();
        file.notes = new();
        file.langs = new();
        FileStream fs = File.OpenRead(path);
        StreamReader sr = new(fs);

        int index = 0;
        while (true)
        {
            index++;
            var line = sr.ReadLine();
            file.endLine = index;
            if (line == null) break;
            if (!line.Equals("")) continue;
            file.notes.Add(index, line);
        }

        var options = new CsvOptions()
        {
            HeaderMode = HeaderMode.HeaderPresent,
            AllowNewLineInEnclosedFieldValues = false,
        };
        fs.Position = 0;
        foreach (var line in CsvReader.ReadFromStream(fs, options))
        {
            file.langs = line.Headers.Where(x => !x.Equals("id")).ToList();
            if (line.Values[0][0] == '#')
            {
                file.notes.Add(line.Index, line.Raw);
                continue;
            }
            try
            {
                var sb = new StringBuilder();
                sb.Append($"\"{line.Values[0]}\"");
                Dictionary<int, string> dic = new();
                for (int i = 1; i < line.ColumnCount; i++)
                {
                    sb.Append($",\"{line.Values[i]}\"");

                    if (line.Headers.Length > i && int.TryParse(line.Headers[i], out var id))
                    {
                        if (file.langs.Contains(id.ToString()) && i <= file.langs.Count)
                            dic[id] = line.Values[i];
                        else
                            Console.WriteLine($"{line.Index}行 => Header: {i} 超出翻译范围");
                    }
                }
                if (!file.map.TryAdd(line.Values[0], dic))
                    Console.WriteLine($"{line.Index}行 => 重复项: \"{line.Values[0]}\"");
                else if (cache) Console.WriteLine(sb.ToString());
                if (dic.Count < file.langs.Count)
                    Console.WriteLine($"{line.Index}行 => {dic.Count}/{file.langs.Count} 断行错误或翻译缺失");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        if (cache)
        {
            Console.WriteLine("--------------------------------");

            foreach (var note in file.notes)
            {
                Console.WriteLine(note.ToString());
            }

            Console.WriteLine("\n--------------------------------\n");

            foreach (var kvp in file.map)
            {
                Console.WriteLine(kvp.Key + " => ");
                foreach (var trans in kvp.Value)
                {
                    Console.WriteLine("\t" + trans.Key.ToString() + " => " + trans.Value);
                }
                Console.WriteLine();
            }
        }
        
        Console.WriteLine($"读取完成：共{file.map.Count}个字符串，{file.langs.Count - 1}个翻译");

        return true;
    }

}