#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;


namespace Framework.ConfigHelper.Editor
{
    public class ConfigHelperWindow : OdinMenuEditorWindow
    {
        [MenuItem("Custom/配置")]
        private static void OpenWindow()
        {
            GetWindow<ConfigHelperWindow>().Show();
        }
        
        static bool isAsyncBuilding = false;
        static async UniTaskVoid AsyncBuildMenuTree()
        {
            if(isAsyncBuilding)
                return;
            isAsyncBuilding = true;
            await UniTask.DelayFrame(10);
            var window = GetWindow<ConfigHelperWindow>();
            if (window != null)
            {
                window.ForceMenuTreeRebuild();
            }
            isAsyncBuilding = false;
        }
        
        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();

            var addList = new List<(System.Object instance, ConfigMenuAttribute config)>();
            var configGenericType = typeof(GameConfig<>);
            var editorConfigGenericType = typeof(EditorConfig<>);
            var toolGenericType = typeof(SimpleTool<>);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (IsSubclassOfRawGeneric(configGenericType, type))
                    {
                        PropertyInfo property_Instance = configGenericType.MakeGenericType(type).GetProperty("Instance");
                        var instance = property_Instance.GetValue(null, null);
                        if (instance == null) // 说明资产加载还没准备好
                        {
                            AsyncBuildMenuTree().Forget();
                            return tree;
                        }
                        addList.Add((instance, type.GetCustomAttribute<ConfigMenuAttribute>()));

                        if (instance!=null)
                        {
                            PropertyInfo property_GameConfigTypes = configGenericType.MakeGenericType(type).GetProperty("GameConfigTypes");
                            if (property_GameConfigTypes!=null)
                            {
                                // Debug.Log($"{type.Name}-GetValue -{property_activePlatforms.Name}");
                                var property_GameConfigTypes_value = property_GameConfigTypes.GetValue(instance);
                                if ( property_GameConfigTypes_value != null )
                                {
                                    var gameConfigTypes = (GameConfigType[])property_GameConfigTypes_value;
                                    if ( gameConfigTypes.Length>0 )
                                    {
                                        for (int i = 0; i < gameConfigTypes.Length; i++)
                                        {
                                            var activePlatform = gameConfigTypes[i];
                                            var configMenuAttribute = type.GetCustomAttribute<ConfigMenuAttribute>();
                                            configMenuAttribute.category = configMenuAttribute.category ?? instance.GetType().Name;
                                            configMenuAttribute.category = $"{configMenuAttribute.category}({activePlatform.ToString()})";
                                            
                                            MethodInfo method_GetInstanceByPlatform = configGenericType.MakeGenericType(type).GetMethod("GetInstanceByType", BindingFlags.Public | BindingFlags.Static);
                                            var resultInstance = method_GetInstanceByPlatform.Invoke(this, new object[]{ activePlatform } );
                                            if (resultInstance == null) // 说明资产加载还没准备好
                                            {
                                                AsyncBuildMenuTree().Forget();
                                                return tree;
                                            }
                                            addList.Add(( resultInstance, configMenuAttribute ));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError($"{type.Name}-No ActivePlatforms");
                            }
                        }
                        else
                        {
                            Debug.LogError($"{type.Name}-No Instance");
                        }

                    }
                    else if(IsSubclassOfRawGeneric(editorConfigGenericType, type))
                    {
                        PropertyInfo property_Instance = editorConfigGenericType.MakeGenericType(type).GetProperty("Instance");
                        var instance = property_Instance.GetValue(null, null);
                        if (instance == null) // 说明资产加载还没准备好
                        {
                            AsyncBuildMenuTree().Forget();
                            return tree;
                        }
                        addList.Add((instance, type.GetCustomAttribute<ConfigMenuAttribute>()));
                    }
                    else if(IsSubclassOfRawGeneric(toolGenericType, type))
                    {
                        PropertyInfo property_Instance = toolGenericType.MakeGenericType(type).GetProperty("Instance");
                        var instance = property_Instance.GetValue(null, null);
                        if (instance == null) // 说明资产加载还没准备好
                        {
                            AsyncBuildMenuTree().Forget();
                            return tree;
                        }
                        addList.Add((instance, type.GetCustomAttribute<ConfigMenuAttribute>()));
                    }
                }
            }
            
            addList.Sort((a, b) =>
            {
                var aOrder = a.config?.order ?? 0;
                var bOrder = b.config?.order ?? 0;
                var aCategory = a.config?.category ?? a.instance.GetType().Name;
                var bCategory = b.config?.category ?? b.instance.GetType().Name;
                
                if (aOrder != bOrder)
                {
                    return aOrder.CompareTo(bOrder);
                }
                else
                {
                    return aCategory.CompareTo(bCategory);
                }
            });
            
            var addCategoryList = new HashSet<string>();
            foreach (var (instance, config) in addList)
            {
                var category = config?.category ?? instance.GetType().Name;
                if(!addCategoryList.Add(category))
                    throw new Exception($"ConfigMenuAttribute category[{category}]重复！");
                tree.Add(category, instance);
            }
            
            return tree;
        }
        
        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            if(generic == null || toCheck == null) return false;
            if (generic == toCheck) return false;
            
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}

#endif