using UnityEngine;
using UnityEditor;

/// <summary>
/// 声明一个自定义编辑器类，用于增强MapGenerator组件在Inspector中的显示和交互
/// CustomEditor属性告知Unity这个编辑器类用于处理MapGenerator类型的对象
/// </summary>
[CustomEditor(typeof(MapGenerator))]
public class MapEditor : Editor 
{
    /// <summary>
    /// 该方法可使挂载了该脚本的自定义组件在Inspector面板显示并进行编辑
    /// </summary>
    public override void OnInspectorGUI ()
    {

        MapGenerator map = target as MapGenerator; // 获取当前编辑的MapGenerator实例

        // 当用户通过 Inspector 修改任意属性时，自动调用 GenerateMap() 重新生成地图（DrawDefaultInspector()用于绘制默认Inspector界面，自动渲染所有可序列化字段，并返回bool值表示是否有属性被修改）
        if (DrawDefaultInspector()) 
        {
            map.GenerateMap ();
        }

        if (GUILayout.Button("Generate Map")) 
        {
            map.GenerateMap ();
        }


    }
}