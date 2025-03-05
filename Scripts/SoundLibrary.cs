using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效资源库管理系统
/// 功能：
/// - 通过组ID分类管理多组音频资源
/// - 支持随机获取同组音效
/// - 编辑器可视化配置音效组
/// </summary>
public class SoundLibrary : MonoBehaviour 
{
    /// <summary>
    /// 在编辑器中配置的声音组集合（支持拖拽赋值）
    /// </summary>
    public SoundGroup[] soundGroups;

    /// <summary>
    /// 音效组快速查询字典（组ID -> 音频剪辑数组）
    /// </summary>
    private Dictionary<string, AudioClip[]> _groupDictionary = new Dictionary<string, AudioClip[]>();

    /// <summary>
    /// 初始化音效资源库
    /// </summary>
    void Awake() 
    {
        // 将编辑器配置的SoundGroup转换为字典存储
        foreach (SoundGroup soundGroup in soundGroups) 
        {
            // 注意：若存在重复groupID会抛出ArgumentException
            _groupDictionary.Add(soundGroup.groupID, soundGroup.group);
        }
    }

    /// <summary>
    /// 根据音效组名称随机获取一个clip
    /// </summary>
    /// <param name="name">音效组ID（需与编辑器配置的groupID完全匹配）</param>
    /// <returns>随机音频剪辑，未找到时返回null</returns>
    public AudioClip GetClipFromName(string name)
    {
        if (_groupDictionary.ContainsKey(name))
        {
            AudioClip[] sounds = _groupDictionary[name];
            // 从数组中随机选择（包含两端索引值）
            return sounds[Random.Range(0, sounds.Length)];
        }
        
        return null;
    }

    /// <summary>
    /// 可序列化的音效组结构（用于编辑器配置）
    /// 示例用法：
    /// - groupID: "PlayerFootsteps"
    /// - group: [footstep1, footstep2, footstep3]
    /// </summary>
    [System.Serializable]
    public class SoundGroup
    {
        /// <summary>
        /// 音效组唯一标识符（区分大小写）
        /// </summary>
        public string groupID;

        /// <summary>
        /// 拖拽添加同类型音效文件（至少需要1个音频文件）
        /// </summary>
        public AudioClip[] group;
    }
}