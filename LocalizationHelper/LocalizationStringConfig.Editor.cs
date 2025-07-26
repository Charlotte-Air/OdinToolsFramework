#if UNITY_EDITOR
using CSVFile;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Framework.Utility;
using System.Globalization;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EditorUtility = UnityEditor.EditorUtility;

namespace Framework.LocalizationHelper
{
    public partial class LocalizationStringConfig
    {
        [MenuItem("Tools/导CSV表+拷贝字符集+生成所有语言字体", false, 3000)]
        public static void ReimportLocalizationStringConfigAndCopyFont()
        {
            Instance.Reimport();
            LocalizationTmpConfig.Instance.GenerateTmpFont();
            AssetDatabase.SaveAssets();
            MessageSystem.Send(new CurrentLanguageChangedEvent());
        }
        
        public string GetDataTableAbsolutePath()
        {
            var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            return Path.GetFullPath(DataTablePath, projectPath);
        }
        
        [InfoBox("只会导入已启用的语言", InfoMessageType.Warning)]
        [Button("重新导入本地化字符串表"), PropertyOrder(-1)]
        public void Reimport()
        {
            Debug.Log("开始导入本地化字符串表");
            var absolutePath = GetDataTableAbsolutePath();
            // 判断文件是否存在
            if (!File.Exists(absolutePath))
            {
                Debug.LogError("本地化字符串表文件不存在");
                return;
            }

            var dataTable = new List<List<string>>();
            var csvSettings = new CSVSettings()
            {
                BufferSize = 655360
            };
            using (var readerStream = new StreamReader(absolutePath))
            using (var csvReader = new CSVReader(readerStream, csvSettings))
            {
                dataTable.Add(csvReader.Headers.ToList());
                foreach (var line in csvReader)
                {
                    dataTable.Add(new List<string>(line));
                }
            }
            if(dataTable.Count < 1)
            {
                Debug.LogError("本地化字符串表为空");
                return;
            }
            var header = dataTable[0];
            var headerCount = header.Count;
            var config = LocalizationConfig.Instance;
            var enableLanguages = config.EnableLanguages;
            var enableLanguageInfos = config.EnableLanguageInfos;
            var languageCodeIndexMap = new Dictionary<string, int>();
            var stringMap = new Dictionary<string, List<string>>();
            for (int i = 1; i < headerCount; i++)
            {
                var longDisplayName = header[i];
                Match match = Regex.Match(longDisplayName, @"\(([^)]+)\)$");
                if (!match.Success)
                    continue;
                var languageCode = match.Groups[1].Value;
                if(!enableLanguages.Contains(languageCode))
                    continue;
                CultureInfo cultureInfo = null;
                int cultureInfoIndex = -1;
                foreach (var enableLanguageInfo in enableLanguageInfos)
                {
                    cultureInfoIndex++;
                    if (enableLanguageInfo.GetLanguageCode() == languageCode)
                    {
                        cultureInfo = enableLanguageInfo;
                        break;
                    }
                }
                if(cultureInfo == null)
                    continue;
                languageCodeIndexMap[languageCode] = cultureInfoIndex;
                var dataTableRowNum = dataTable.Count;
                var dataTableColumnNum = header.Count;
                for (int j = 1; j < dataTableRowNum; j++)
                {
                    var key = dataTable[j][0];
                    var value = dataTable[j][i];
                    if (!stringMap.TryGetValue(key, out var localizationStrings))
                    {
                        localizationStrings = new List<string>(dataTableColumnNum);
                        stringMap[key] = localizationStrings;
                    }
                    localizationStrings.Add(value);
                }
            }
            LanguageCodeIndexMap = languageCodeIndexMap;
            StringMap = stringMap;
            Debug.Log("本地化字符串表导入成功 语言数:" + languageCodeIndexMap.Count + "  字符串Key数:" + stringMap.Count);
            EditorUtility.SetDirty(this);
        }
    }
}
#endif