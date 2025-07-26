#if UNITY_EDITOR
using TMPro;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.TextCore;
using Sirenix.OdinInspector;
using TMPro.EditorUtilities;
using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;
using Framework.Utility.WeakReference;

namespace Framework.LocalizationHelper
{
    public partial class LocalizationTmpConfig
    {
        [Flags]
        internal enum GlyphRasterModes
        {
            RASTER_MODE_8BIT = 1,
            RASTER_MODE_MONO = 2,
            RASTER_MODE_NO_HINTING = 4,
            RASTER_MODE_HINTED = 8,
            RASTER_MODE_BITMAP = 16, // 0x00000010
            RASTER_MODE_SDF = 32, // 0x00000020
            RASTER_MODE_SDFAA = 64, // 0x00000040
            RASTER_MODE_MSDF = 256, // 0x00000100
            RASTER_MODE_MSDFA = 512, // 0x00000200
            RASTER_MODE_1X = 4096, // 0x00001000
            RASTER_MODE_8X = 8192, // 0x00002000
            RASTER_MODE_16X = 16384, // 0x00004000
            RASTER_MODE_32X = 32768, // 0x00008000
            RASTER_MODE_COLOR = 65536, // 0x00010000
        }
        
        internal enum FontPackingModes
        {
            Fast = 0,
            Optimum = 4
        };
        
        private static int m_CharacterSetSelectionMode = 7;
        static bool m_IncludeFontFeatures = false;
        private static GlyphRenderMode m_GlyphRenderMode = GlyphRenderMode.SDFAA;
        private static FontPackingModes m_PackingMode = FontPackingModes.Fast;
        static List<uint> m_AvailableGlyphsToAdd = new List<uint>();
        static List<uint> m_MissingCharacters = new List<uint>();
        static List<uint> m_ExcludedCharacters = new List<uint>();
        static Dictionary<uint, uint> m_CharacterLookupMap = new Dictionary<uint, uint>();
        static Dictionary<uint, List<uint>> m_GlyphLookupMap = new Dictionary<uint, List<uint>>();
        static List<Glyph> m_GlyphsToPack = new List<Glyph>();
        static List<Glyph> m_GlyphsPacked = new List<Glyph>();
        private static int m_PointSizeSamplingMode = 0;
        private static int m_PointSize;
        static List<GlyphRect> m_FreeGlyphRects = new List<GlyphRect>();
        static List<GlyphRect> m_UsedGlyphRects = new List<GlyphRect>();
        static List<Glyph> m_FontGlyphTable = new List<Glyph>();
        static List<TMP_Character> m_FontCharacterTable = new List<TMP_Character>();
        static List<Glyph> m_GlyphsToRender = new List<Glyph>();
        static FaceInfo m_FaceInfo;
        static byte[] m_AtlasTextureBuffer;
        private static Texture2D _fontAtlasTexture;
        static int[] fontAtlasResolutions = { 256, 512, 1024, 2048 };
        static private int m_AtlasHeight;
        static private int m_AtlasWidth;
        [Button("强制重新生成字体"), PropertyOrder(-1)]
        public void ForceGenerateTmpFont() => GenerateTmpFont(true);
        public void GenerateTmpFont(bool force = false)
        {
            var stringConfig = LocalizationStringConfig.Instance;
            // 自动收集字符集
            foreach (var tmpFontSetting in TmpFontSettings)
            {
                tmpFontSetting.AutoCharacterSet = stringConfig.GetCharacterSet(tmpFontSetting.LanguageCode);
            }
            
            // 生成TMP字体资产
            {
                m_PackingMode = FastMode ? FontPackingModes.Fast : FontPackingModes.Optimum;
                
                // Initialize font engine
                FontEngineError errorCode = FontEngine.InitializeFontEngine();
                if (errorCode != FontEngineError.Success)
                {
                    Debug.Log("Font Asset Creator - Error [" + errorCode + "] has occurred while Initializing the FreeType Library.");
                }
                
                foreach (var tmpFontSetting in TmpFontSettings)
                {
                    // 判断是不是真的要重新生成字体文件
                    var targetTmpFontAssetPath = GetFontAssetPath(tmpFontSetting);
                    targetTmpFontAssetPath = GetFontAssetPath(targetTmpFontAssetPath, out var tex_DirName, out var tex_FileName, out var tex_Path_NoExt);
                    var targetOutlineMaterialPath = $"{tex_DirName}/{tex_FileName} OutlineMaterial.mat";
                    var targetShadowMaterialPath = $"{tex_DirName}/{tex_FileName} ShadowMaterial.mat";
                    var targetTmpFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetTmpFontAssetPath);
                    var targetOutlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetOutlineMaterialPath);
                    var targetShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetShadowMaterialPath);
                    var finalCharacterSet = tmpFontSetting.FinalCharacterSet;
                    var needUpdateTmpFontAsset = true;
                    if (targetTmpFontAsset != null && targetOutlineMaterial != null && targetShadowMaterial != null)
                    {
                        if(targetTmpFontAsset.creationSettings.characterSequence == finalCharacterSet || force)
                            needUpdateTmpFontAsset = false;
                    }

                    Debug.Log($"开始更新字体：{tmpFontSetting.FontAssetName}");
                    if (needUpdateTmpFontAsset)
                    {
                        int m_Padding = tmpFontSetting.Padding;
                        m_PointSize = tmpFontSetting.PointSize;

                        // Get file path of the source font file.
                        string fontPath = AssetDatabase.GUIDToAssetPath(tmpFontSetting.SourceFont.AssetGuidStr);
                        if (errorCode == FontEngineError.Success)
                        {
                            errorCode = FontEngine.LoadFontFace(fontPath);

                            if (errorCode != FontEngineError.Success)
                            {
                                Debug.Log("Font Asset Creator - Error Code [" + errorCode + "] has occurred trying to load the [" + fontPath + "] " +
                                          "font file. This typically results from the use of an incompatible or corrupted font file.");
                            }
                        }

                        // Define an array containing the characters we will render.
                        if (errorCode == FontEngineError.Success)
                        {
                            uint[] characterSet = null;

                            // Get list of characters that need to be packed and rendered to the atlas texture.
                            List<uint> char_List = new List<uint>();

                            for (int i = 0; i < finalCharacterSet.Length; i++)
                            {
                                uint unicode = finalCharacterSet[i];

                                // Handle surrogate pairs
                                if (i < finalCharacterSet.Length - 1 && char.IsHighSurrogate((char)unicode) &&
                                    char.IsLowSurrogate(finalCharacterSet[i + 1]))
                                {
                                    unicode = (uint)char.ConvertToUtf32(finalCharacterSet[i], finalCharacterSet[i + 1]);
                                    i += 1;
                                }

                                // Check to make sure we don't include duplicates
                                if (char_List.FindIndex(item => item == unicode) == -1) char_List.Add(unicode);
                            }

                            characterSet = char_List.ToArray();

                            GlyphLoadFlags glyphLoadFlags =
                                ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_HINTED) ==
                                GlyphRasterModes.RASTER_MODE_HINTED
                                    ? GlyphLoadFlags.LOAD_RENDER
                                    : GlyphLoadFlags.LOAD_RENDER | GlyphLoadFlags.LOAD_NO_HINTING;

                            glyphLoadFlags =
                                ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_MONO) ==
                                GlyphRasterModes.RASTER_MODE_MONO
                                    ? glyphLoadFlags | GlyphLoadFlags.LOAD_MONOCHROME
                                    : glyphLoadFlags;

                            // pack glyphs in the given texture space.
                            {
                                // Clear the various lists used in the generation process.
                                m_AvailableGlyphsToAdd.Clear();
                                m_MissingCharacters.Clear();
                                m_ExcludedCharacters.Clear();
                                m_CharacterLookupMap.Clear();
                                m_GlyphLookupMap.Clear();
                                m_GlyphsToPack.Clear();
                                m_GlyphsPacked.Clear();

                                // Check if requested characters are available in the source font file.
                                for (int i = 0; i < characterSet.Length; i++)
                                {
                                    uint unicode = characterSet[i];
                                    uint glyphIndex;

                                    if (FontEngine.TryGetGlyphIndex(unicode, out glyphIndex))
                                    {
                                        // Skip over potential duplicate characters.
                                        if (m_CharacterLookupMap.ContainsKey(unicode)) continue;

                                        // Add character to character lookup map.
                                        m_CharacterLookupMap.Add(unicode, glyphIndex);

                                        // Skip over potential duplicate glyph references.
                                        if (m_GlyphLookupMap.ContainsKey(glyphIndex))
                                        {
                                            // Add additional glyph reference for this character.
                                            m_GlyphLookupMap[glyphIndex].Add(unicode);
                                            continue;
                                        }

                                        // Add glyph reference to glyph lookup map.
                                        m_GlyphLookupMap.Add(glyphIndex, new List<uint>() { unicode });

                                        // Add glyph index to list of glyphs to add to texture.
                                        m_AvailableGlyphsToAdd.Add(glyphIndex);
                                    }
                                    else
                                    {
                                        // Add Unicode to list of missing characters.
                                        m_MissingCharacters.Add(unicode);
                                    }
                                }

                                // Pack available glyphs in the provided texture space.
                                if (m_AvailableGlyphsToAdd.Count > 0)
                                {
                                    int packingModifier =
                                        ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                                        GlyphRasterModes.RASTER_MODE_BITMAP
                                            ? 0
                                            : 1;

                                    // 自动计算合适的图集分辨率，直到2048*2048，还塞不下就走自动调整Point Size算法
                                    var atlasWidthIndex = 0;
                                    var atlasHeightIndex = 0;
                                    m_AtlasHeight = fontAtlasResolutions[0];
                                    m_AtlasWidth = fontAtlasResolutions[0];
                                    var atlasResolutionLength = fontAtlasResolutions.Length;
                                    while (true)
                                    {
                                        m_AtlasWidth = fontAtlasResolutions[atlasWidthIndex];
                                        m_AtlasHeight = fontAtlasResolutions[atlasHeightIndex];

                                        // 开始计算图集
                                        {
                                            FontEngine.SetFaceSize(m_PointSize);

                                            m_GlyphsToPack.Clear();
                                            m_GlyphsPacked.Clear();

                                            m_FreeGlyphRects.Clear();
                                            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                                m_AtlasHeight - packingModifier));
                                            m_UsedGlyphRects.Clear();

                                            for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                            {
                                                uint glyphIndex = m_AvailableGlyphsToAdd[i];
                                                Glyph glyph;

                                                if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags,
                                                        out glyph))
                                                {
                                                    if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                                    {
                                                        m_GlyphsToPack.Add(glyph);
                                                    }
                                                    else
                                                    {
                                                        m_GlyphsPacked.Add(glyph);
                                                    }
                                                }
                                            }

                                            TryPackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_Padding,
                                                (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth,
                                                m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);
                                        }

                                        if (m_GlyphsToPack.Count > 0) // 无法塞下全部字符，扩大分辨率
                                        {
                                            if (atlasWidthIndex < atlasResolutionLength - 1 &&
                                                atlasHeightIndex < atlasResolutionLength - 1)
                                            {
                                                if (atlasWidthIndex >= atlasHeightIndex)
                                                    atlasHeightIndex++;
                                                else
                                                    atlasWidthIndex++;
                                            }
                                            else if (atlasWidthIndex < atlasResolutionLength - 1)
                                                atlasHeightIndex++;
                                            else if (atlasHeightIndex < atlasResolutionLength - 1)
                                                atlasWidthIndex++;
                                            else
                                                break;

                                            m_AtlasWidth = fontAtlasResolutions[atlasWidthIndex];
                                            m_AtlasHeight = fontAtlasResolutions[atlasHeightIndex];
                                        }
                                        else // 全部字符都塞下了
                                            break;
                                    }

                                    // 最后使用自动调整Point Size算法最大化利用空间提升清晰度
                                    {
                                        // Estimate min / max range for auto sizing of point size.
                                        int minPointSize = 0;
                                        int maxPointSize = (int)Mathf.Sqrt((m_AtlasWidth * m_AtlasHeight) / m_AvailableGlyphsToAdd.Count) * 3;

                                        m_PointSize = (maxPointSize + minPointSize) / 2;

                                        bool optimumPointSizeFound = false;
                                        for (int iteration = 0;
                                             iteration < 15 && optimumPointSizeFound == false;
                                             iteration++)
                                        {
                                            FontEngine.SetFaceSize(m_PointSize);

                                            m_GlyphsToPack.Clear();
                                            m_GlyphsPacked.Clear();

                                            m_FreeGlyphRects.Clear();
                                            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                                m_AtlasHeight - packingModifier));
                                            m_UsedGlyphRects.Clear();

                                            for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                            {
                                                uint glyphIndex = m_AvailableGlyphsToAdd[i];
                                                Glyph glyph;

                                                if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags,
                                                        out glyph))
                                                {
                                                    if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                                    {
                                                        m_GlyphsToPack.Add(glyph);
                                                    }
                                                    else
                                                    {
                                                        m_GlyphsPacked.Add(glyph);
                                                    }
                                                }
                                            }

                                            TryPackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_Padding,
                                                (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth,
                                                m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);

                                            //Debug.Log("Glyphs remaining to add [" + m_GlyphsToAdd.Count + "]. Glyphs added [" + m_GlyphsAdded.Count + "].");

                                            if (m_GlyphsToPack.Count > 0)
                                            {
                                                if (m_PointSize > minPointSize)
                                                {
                                                    maxPointSize = m_PointSize;
                                                    m_PointSize = (m_PointSize + minPointSize) / 2;

                                                    //Debug.Log("Decreasing point size from [" + maxPointSize + "] to [" + m_PointSize + "].");
                                                }
                                            }
                                            else
                                            {
                                                if (maxPointSize - minPointSize > 1 && m_PointSize < maxPointSize)
                                                {
                                                    minPointSize = m_PointSize;
                                                    m_PointSize = (m_PointSize + maxPointSize) / 2;

                                                    //Debug.Log("Increasing point size from [" + minPointSize + "] to [" + m_PointSize + "].");
                                                }
                                                else
                                                {
                                                    //Debug.Log("[" + iteration + "] iterations to find the optimum point size of : [" + m_PointSize + "].");
                                                    optimumPointSizeFound = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    int packingModifier =
                                        ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                                        GlyphRasterModes.RASTER_MODE_BITMAP
                                            ? 0
                                            : 1;

                                    FontEngine.SetFaceSize(m_PointSize);

                                    m_GlyphsToPack.Clear();
                                    m_GlyphsPacked.Clear();

                                    m_FreeGlyphRects.Clear();
                                    m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                        m_AtlasHeight - packingModifier));
                                    m_UsedGlyphRects.Clear();
                                }

                                m_FontCharacterTable.Clear();
                                m_FontGlyphTable.Clear();
                                m_GlyphsToRender.Clear();

                                // Handle Results and potential cancellation of glyph rendering
                                if (m_GlyphRenderMode == GlyphRenderMode.SDF32 && m_PointSize > 512 ||
                                    m_GlyphRenderMode == GlyphRenderMode.SDF16 && m_PointSize > 1024 ||
                                    m_GlyphRenderMode == GlyphRenderMode.SDF8 && m_PointSize > 2048)
                                {
                                    int upSampling = 1;
                                    switch (m_GlyphRenderMode)
                                    {
                                        case GlyphRenderMode.SDF8:
                                            upSampling = 8;
                                            break;
                                        case GlyphRenderMode.SDF16:
                                            upSampling = 16;
                                            break;
                                        case GlyphRenderMode.SDF32:
                                            upSampling = 32;
                                            break;
                                    }

                                    Debug.Log("Glyph rendering has been aborted due to sampling point size of [" + m_PointSize + "] x SDF [" + upSampling + "] " +
                                              "up sampling exceeds 16,384 point size. Please revise your generation settings to make sure the sampling point size x SDF up sampling mode does not exceed 16,384.");
                                }

                                // Add glyphs and characters successfully added to texture to their respective font tables.
                                foreach (Glyph glyph in m_GlyphsPacked)
                                {
                                    uint glyphIndex = glyph.index;

                                    m_FontGlyphTable.Add(glyph);

                                    // Add glyphs to list of glyphs that need to be rendered.
                                    if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                        m_GlyphsToRender.Add(glyph);

                                    foreach (uint unicode in m_GlyphLookupMap[glyphIndex])
                                    {
                                        // Create new Character
                                        m_FontCharacterTable.Add(new TMP_Character(unicode, glyph));
                                    }
                                }
                                
                                foreach (Glyph glyph in m_GlyphsToPack)
                                {
                                    foreach (uint unicode in m_GlyphLookupMap[glyph.index])
                                    {
                                        m_ExcludedCharacters.Add(unicode);
                                    }
                                }

                                // Get the face info for the current sampling point size.
                                m_FaceInfo = FontEngine.GetFaceInfo();
                            }

                            // render glyphs in texture buffer.
                            {
                                // Allocate texture data
                                m_AtlasTextureBuffer = new byte[m_AtlasWidth * m_AtlasHeight];

                                // Render and add glyphs to the given atlas texture.
                                if (m_GlyphsToRender.Count > 0)
                                {
                                    RenderGlyphsToTexture(m_GlyphsToRender, m_Padding, m_GlyphRenderMode,
                                        m_AtlasTextureBuffer, m_AtlasWidth, m_AtlasHeight);
                                }
                            }
                        }
                    }
                    
                    // 生成TMP字体资产
                    if (needUpdateTmpFontAsset)
                    {
                        CreateFontAtlasTexture(m_AtlasWidth, m_AtlasHeight);

                        // If dynamic make readable ...
                        _fontAtlasTexture.Apply(false, false);

                        //保存字体资产文件
                        var fontAsset = SaveFontAssetFile(tmpFontSetting);
                        tmpFontSetting.TmpFontAsset = new AssetPathRef(fontAsset);
                    }
                    else
                    {
                        tmpFontSetting.TmpFontAsset = new AssetPathRef(targetTmpFontAsset);
                        tmpFontSetting.TmpFontOutlineMaterial = new AssetPathRef(targetOutlineMaterial);
                        tmpFontSetting.TmpFontShadowMaterial = new AssetPathRef(targetShadowMaterial);
                        Debug.Log($"字体文件无需更新：{tmpFontSetting.FontAssetName}");
                    }
                    Debug.Log($"完成更新字体：{tmpFontSetting.FontAssetName}");
                }
                FontEngine.DestroyFontEngine();
            }
            EditorUtility.SetDirty(this);
            UpdateTmpFontGenerateSetting();
        }
        
        private static void CreateFontAtlasTexture(int atlasWidth, int atlasHeight)
        {
            if (_fontAtlasTexture != null)
                DestroyImmediate(_fontAtlasTexture);

            _fontAtlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false, true);

            Color32[] colors = new Color32[atlasWidth * atlasHeight];

            for (int i = 0; i < colors.Length; i++)
            {
                byte c = m_AtlasTextureBuffer[i];
                colors[i] = new Color32(c, c, c, c);
            }

            // Clear allocation of
            m_AtlasTextureBuffer = null;

            if ((m_GlyphRenderMode & GlyphRenderMode.RASTER) == GlyphRenderMode.RASTER ||
                (m_GlyphRenderMode & GlyphRenderMode.RASTER_HINTED) == GlyphRenderMode.RASTER_HINTED)
                _fontAtlasTexture.filterMode = FilterMode.Point;

            _fontAtlasTexture.SetPixels32(colors, 0);
            _fontAtlasTexture.Apply(false, false);
        }
        
        private static TMP_FontAsset SaveFontAssetFile(TmpFontGenerateSetting fontGenerateAssetSetting)
        {
            string fullPath = GetFontAssetPath(fontGenerateAssetSetting);
            if (string.IsNullOrEmpty(fullPath))
                return null;

            if (((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                throw new NotImplementedException("暂不支持位图字体");
            }
            else
            {
                return Save_SDF_FontAsset(fullPath, fontGenerateAssetSetting);
            }
        }

        static string GetFontAssetPath(TmpFontGenerateSetting fontGenerateAssetSetting)
        {
            string outputPath = Path.GetFullPath(Instance.TmpFontOutputPath);
            string fontAssetFileName = fontGenerateAssetSetting.FontAssetName;
            if (outputPath.Length <= 0 || fontAssetFileName.Length <= 0)
            {
                return null;
            }

            string fullPath = Path.Combine(outputPath, string.Concat(fontAssetFileName, ".asset"));
            return fullPath;
        }
        
        static string GetFontAssetPath(string filePath, out string tex_DirName, out string tex_FileName, out string tex_Path_NoExt)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            string dataPath = Application.dataPath;
            string relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            tex_DirName = Path.GetDirectoryName(relativeAssetPath);
            tex_FileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            tex_Path_NoExt = tex_DirName + "/" + tex_FileName;
            return tex_Path_NoExt + ".asset";
        }
        
        private static readonly int OutlineUseExternalData = Shader.PropertyToID("_OutlineUseExternalData");
        private static readonly int UnderlayUseExternalData = Shader.PropertyToID("_UnderlayUseExternalData");
        static TMP_FontAsset Save_SDF_FontAsset(string filePath, TmpFontGenerateSetting fontGenerateAssetSetting)
        {
            var fontAssetPath = GetFontAssetPath(filePath, out var tex_DirName, out var tex_FileName, out var tex_Path_NoExt);
            // Check if TextMeshPro font asset already exists. If not, create a new one. Otherwise update the existing one.
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (fontAsset == null)
            {
                //Debug.Log("Creating TextMeshPro font asset!");
                fontAsset = CreateInstance<TMP_FontAsset>(); // Create new TextMeshPro Font Asset.
                AssetDatabase.CreateAsset(fontAsset, tex_Path_NoExt + ".asset");

                // Set version number of font asset
                SetFontAssetProperty(fontAsset, "version", "1.1.0");

                // Reference to source font file GUID.
                SetFontAssetField(fontAsset, "m_SourceFontFile_EditorRef", fontGenerateAssetSetting.SourceFont.EditorLoad<Font>());
                SetFontAssetField(fontAsset, "m_SourceFontFileGUID", fontGenerateAssetSetting.SourceFont.AssetGuidStr);

                //Set Font Asset Type
                SetFontAssetProperty(fontAsset, "atlasRenderMode", m_GlyphRenderMode);

                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                SetFontAssetProperty(fontAsset, "glyphTable", m_FontGlyphTable);

                // Add CharacterTable[] to font asset.
                SetFontAssetProperty(fontAsset, "characterTable", m_FontCharacterTable);

                // Sort glyph and character tables.
                FontAsset_SortAllTables(fontAsset);

                // Get and Add Kerning Pairs to Font Asset
                // if (m_IncludeFontFeatures)
                //     SetFontAssetProperty(fontAsset, "fontFeatureTable", GetKerningTable());

                // Add Font Atlas as Sub-Asset
                fontAsset.atlasTextures = new Texture2D[] { _fontAtlasTexture };
                _fontAtlasTexture.name = tex_FileName + " Atlas";
                SetFontAssetProperty(fontAsset, "atlasWidth", m_AtlasWidth);
                SetFontAssetProperty(fontAsset, "atlasHeight", m_AtlasHeight);
                SetFontAssetProperty(fontAsset, "atlasPadding", fontGenerateAssetSetting.Padding);

                AssetDatabase.AddObjectToAsset(_fontAtlasTexture, fontAsset);

                // Create new Material and Add it as Sub-Asset
                Shader default_Shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                // 描边材质
                {
                    Material tmp_material = new Material(default_Shader);

                    tmp_material.name = tex_FileName + " OutlineMaterial";
                    tmp_material.SetTexture(ShaderUtilities.ID_MainTex, _fontAtlasTexture);
                    tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, _fontAtlasTexture.width);
                    tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, _fontAtlasTexture.height);

                    int spread = fontGenerateAssetSetting.Padding + 1;
                    tmp_material.SetFloat(ShaderUtilities.ID_GradientScale,
                        spread); // Spread = Padding for Brute Force SDF.

                    tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                    tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
                    tmp_material.SetInt(OutlineUseExternalData, 1);
                    tmp_material.EnableKeyword("OUTLINE_ON");
                    tmp_material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);

                    fontAsset.material = tmp_material;

                    AssetDatabase.CreateAsset(tmp_material, $"{tex_DirName}/{tmp_material.name}.mat");
                    fontGenerateAssetSetting.TmpFontOutlineMaterial = new AssetPathRef(tmp_material);
                }
                // 阴影材质
                {
                    Material tmp_material = new Material(default_Shader);

                    tmp_material.name = tex_FileName + " ShadowMaterial";
                    tmp_material.SetTexture(ShaderUtilities.ID_MainTex, _fontAtlasTexture);
                    tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, _fontAtlasTexture.width);
                    tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, _fontAtlasTexture.height);

                    int spread = fontGenerateAssetSetting.Padding + 1;
                    tmp_material.SetFloat(ShaderUtilities.ID_GradientScale,
                        spread); // Spread = Padding for Brute Force SDF.

                    tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                    tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
                    tmp_material.EnableKeyword("UNDERLAY_ON");
                    tmp_material.SetInt(UnderlayUseExternalData, 1);

                    AssetDatabase.CreateAsset(tmp_material, $"{tex_DirName}/{tmp_material.name}.mat");
                    fontGenerateAssetSetting.TmpFontShadowMaterial = new AssetPathRef(tmp_material);
                }
            }
            else
            {
                // Find all Materials referencing this font atlas.
                Material[] material_references = TMP_EditorUtility.FindMaterialReferences(fontAsset);

                // Set version number of font asset
                SetFontAssetProperty(fontAsset, "version", "1.1.0");

                // Special handling to remove legacy font asset data
                var m_glyphInfoList = GetFontAssetField<List<TMP_Glyph>>(fontAsset, "m_glyphInfoList");
                if (m_glyphInfoList != null && m_glyphInfoList.Count > 0)
                    SetFontAssetField(fontAsset, "m_glyphInfoList", null);

                //Set Font Asset Type
                SetFontAssetProperty(fontAsset, "atlasRenderMode", m_GlyphRenderMode);
                
                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                SetFontAssetProperty(fontAsset, "glyphTable", m_FontGlyphTable);

                // Add CharacterTable[] to font asset.
                SetFontAssetProperty(fontAsset, "characterTable", m_FontCharacterTable);

                // Sort glyph and character tables.
                FontAsset_SortAllTables(fontAsset);

                // Get and Add Kerning Pairs to Font Asset
                // Check and preserve existing adjustment pairs.
                // if (m_IncludeFontFeatures)
                // fontAsset.fontFeatureTable = GetKerningTable();

                // Destroy Assets that will be replaced.
                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                {
                    for (int i = 1; i < fontAsset.atlasTextures.Length; i++)
                        DestroyImmediate(fontAsset.atlasTextures[i], true);
                }

                SetFontAssetField(fontAsset, "m_AtlasTextureIndex", 0);
                SetFontAssetProperty(fontAsset, "atlasWidth", m_AtlasWidth);
                SetFontAssetProperty(fontAsset, "atlasHeight", m_AtlasHeight);
                SetFontAssetProperty(fontAsset, "atlasPadding", fontGenerateAssetSetting.Padding);

                // Make sure remaining atlas texture is of the correct size
                Texture2D tex = fontAsset.atlasTextures[0];
                tex.name = tex_FileName + " Atlas";

                // Make texture readable to allow resizing
                bool isReadableState = tex.isReadable;
                if (isReadableState == false)
                    SetAtlasTextureIsReadable(tex, true);

                if (tex.width != m_AtlasWidth || tex.height != m_AtlasHeight)
                {
                    tex.Reinitialize(m_AtlasWidth, m_AtlasHeight);
                    tex.Apply(false);
                }

                // Copy new texture data to existing texture
                Graphics.CopyTexture(_fontAtlasTexture, tex);

                // Apply changes to the texture.
                tex.Apply(false);

                // Special handling due to a bug in earlier versions of Unity.
                _fontAtlasTexture.hideFlags = HideFlags.None;
                fontAsset.material.hideFlags = HideFlags.None;

                // Update the Texture reference on the Material
                for (int i = 0; i < material_references.Length; i++)
                {
                    material_references[i].SetFloat(ShaderUtilities.ID_TextureWidth, tex.width);
                    material_references[i].SetFloat(ShaderUtilities.ID_TextureHeight, tex.height);

                    int spread = fontGenerateAssetSetting.Padding + 1;
                    material_references[i].SetFloat(ShaderUtilities.ID_GradientScale, spread);

                    material_references[i].SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                    material_references[i].SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
                }
            }

            // Saving File for Debug
            //var pngData = destination_Atlas.EncodeToPNG();
            //File.WriteAllBytes("Assets/Textures/Debug Distance Field.png", pngData);

            // Set texture to non-readable
            SetAtlasTextureIsReadable(fontAsset.atlasTexture, false);

            // Add list of GlyphRects to font asset.
            SetFontAssetProperty(fontAsset, "freeGlyphRects", m_FreeGlyphRects);
            SetFontAssetProperty(fontAsset, "usedGlyphRects", m_UsedGlyphRects);

            // Save Font Asset creation settings
            // m_SelectedFontAsset = fontAsset;
            // m_LegacyFontAsset = null;
            fontAsset.creationSettings = SaveFontCreationSettings(fontGenerateAssetSetting);

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(fontAsset)); // Re-import font asset to get the new updated version.

            fontAsset.ReadFontAssetDefinition();

            AssetDatabase.Refresh();

            _fontAtlasTexture = null;

            // NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);

            return fontAsset;
        }

        private static FontAssetCreationSettings SaveFontCreationSettings(TmpFontGenerateSetting fontGenerateAssetSetting)
        {
            FontAssetCreationSettings settings = new FontAssetCreationSettings();

            //settings.sourceFontFileName = m_SourceFontFile.name;
            settings.sourceFontFileGUID = fontGenerateAssetSetting.SourceFont.AssetGuidStr;
            settings.pointSizeSamplingMode = 0;
            settings.pointSize = m_PointSize;
            settings.padding = fontGenerateAssetSetting.Padding;
            settings.packingMode = (int)m_PackingMode;
            settings.atlasWidth = m_AtlasWidth;
            settings.atlasHeight = m_AtlasHeight;
            settings.characterSetSelectionMode = m_CharacterSetSelectionMode;
            settings.characterSequence = fontGenerateAssetSetting.FinalCharacterSet;
            // settings.referencedFontAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_ReferencedFontAsset));
            // settings.referencedTextAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_CharactersFromFile));
            // settings.fontStyle = (int)m_FontStyle;
            // settings.fontStyleModifier = m_FontStyleValue;
            settings.renderMode = (int)m_GlyphRenderMode;
            settings.includeFontFeatures = m_IncludeFontFeatures;

            return settings;
        }

        private static MethodInfo _tryPackGlyphsInAtlasMethod;
        static bool TryPackGlyphsInAtlas(List<Glyph> glyphsToAdd,List<Glyph> glyphsAdded,int padding,GlyphPackingMode packingMode,GlyphRenderMode renderMode,int width,int height,List<GlyphRect> freeGlyphRects,List<GlyphRect> usedGlyphRects)
        {
            _tryPackGlyphsInAtlasMethod ??= typeof(FontEngine).GetMethod("TryPackGlyphsInAtlas", BindingFlags.Static | BindingFlags.NonPublic);
            if (_tryPackGlyphsInAtlasMethod == null)
            {
                Debug.LogError("TryPackGlyphsInAtlasMethod is null");
                return false;
            }
            var result = _tryPackGlyphsInAtlasMethod.Invoke(null, new object[] {glyphsToAdd, glyphsAdded, padding, packingMode, renderMode, width, height, freeGlyphRects, usedGlyphRects});
            return result != null && (bool)result;
        }

        private static MethodInfo _renderGlyphsToTextureMethod;
        static FontEngineError RenderGlyphsToTexture
        (
            List<Glyph> glyphs,
            int padding,
            GlyphRenderMode renderMode,
            byte[] texBuffer,
            int texWidth,
            int texHeight)
        {
            if (_renderGlyphsToTextureMethod == null)
            {
                Type[] parameterTypes = new Type[] {
                    typeof(List<Glyph>),
                    typeof(int),
                    typeof(GlyphRenderMode),
                    typeof(byte[]),
                    typeof(int),
                    typeof(int)
                };
                _renderGlyphsToTextureMethod ??= typeof(FontEngine).GetMethod("RenderGlyphsToTexture", BindingFlags.Static | BindingFlags.NonPublic, null, parameterTypes, null);
            }
            if (_renderGlyphsToTextureMethod == null)
            {
                Debug.LogError("RenderGlyphsToTextureMethod is null");
                return FontEngineError.Invalid_Library;
            }
            var result = _renderGlyphsToTextureMethod.Invoke(null, new object[] {glyphs, padding, renderMode, texBuffer, texWidth, texHeight});
            return result != null ? (FontEngineError)result : FontEngineError.Invalid_Library;
        }
        
        private static void SetFontAssetProperty(TMP_FontAsset fontAsset, string propertyName, object value)
        {
            var t = typeof(TMP_FontAsset);
            var property = typeof(TMP_FontAsset).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                Debug.LogError($"TMP_FontAsset does not have property {propertyName}");
                return;
            }
            property.SetValue(fontAsset, value);
        }
        
        private static void SetFontAssetField(TMP_FontAsset fontAsset, string fieldName, object value)
        {
            var field = typeof(TMP_FontAsset).GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError($"TMP_FontAsset does not have field {fieldName}");
                return;
            }
            field.SetValue(fontAsset, value);
        }
        
        private static T GetFontAssetField<T>(TMP_FontAsset fontAsset, string fieldName)
        {
            var field = typeof(TMP_FontAsset).GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError($"TMP_FontAsset does not have field {fieldName}");
                return default;
            }
            return (T)field.GetValue(fontAsset);
        }
        
        private static void FontAsset_SortAllTables(TMP_FontAsset fontAsset)
        {
            var method = typeof(TMP_FontAsset).GetMethod("SortAllTables", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError("TMP_FontAsset does not have method SortAllTables");
                return;
            }
            method.Invoke(fontAsset, null);
        }

        private static Type _fontEngineEditorUtilitiesType;
        private static MethodInfo _setAtlasTextureIsReadableMethodInfo;

        static void SetAtlasTextureIsReadable(Texture2D texture, bool isReadable)
        {
            if (_fontEngineEditorUtilitiesType == null)
            {
                Assembly editorAssembly = Assembly.Load("UnityEditor.TextCoreFontEngineModule");
                _fontEngineEditorUtilitiesType = editorAssembly.GetType("UnityEditor.TextCore.LowLevel.FontEngineEditorUtilities");
                if (_fontEngineEditorUtilitiesType != null)
                {
                    _setAtlasTextureIsReadableMethodInfo = _fontEngineEditorUtilitiesType.GetMethod("SetAtlasTextureIsReadable", BindingFlags.Static | BindingFlags.NonPublic);
                }
            }
            if (_setAtlasTextureIsReadableMethodInfo != null)
            {
                _setAtlasTextureIsReadableMethodInfo.Invoke(null, new object[] {texture, isReadable});
                return;
            }
            Debug.LogError("SetAtlasTextureIsReadableMethodInfo is null");
        }
    }
}
#endif