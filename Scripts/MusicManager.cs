using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏音乐管理系统（单例模式设计）
/// 功能：
/// 1. 根据场景自动切换背景音乐
/// 2. 支持音乐淡入淡出过渡
/// 3. 自动循环播放场景音乐
/// 注意：需配合AudioManager音乐播放器使用
/// </summary>
public class MusicManager : MonoBehaviour 
{
    /// <summary>
    /// 游戏主场景背景音乐（如战场/探索场景）
    /// </summary>
    public AudioClip mainTheme;
    
    /// <summary>
    /// 菜单界面背景音乐（如开始菜单/暂停菜单）
    /// </summary>
    public AudioClip menuTheme;

    /// <summary>
    /// 当前跟踪的场景名称
    /// </summary>
    string _sceneName;

    /// <summary>
    /// 初始化时立即检测初始场景
    /// 注意：在编辑器模式下可能需要手动调用场景加载事件
    /// </summary>
    void Start() 
    {
        // 强制触发首次场景检测
        OnLevelWasLoaded();
    }

    /// <summary>
    /// 场景加载完成回调（旧版API）
    /// 注意：在Unity 5.4+建议改用SceneManager.sceneLoaded
    /// </summary>
    /// <param name="sceneIndex">已废弃的场景索引参数</param>
    void OnLevelWasLoaded() 
    {
        // 获取当前激活场景的真实名称
        string newSceneName = SceneManager.GetActiveScene().name;
        
        // 场景变更检测机制（避免重复播放）
        if (newSceneName != _sceneName) 
        {
            _sceneName = newSceneName;
            
            // 延迟0.2秒等待场景初始化完成
            // 避免与其他场景加载操作产生竞争条件
            Invoke("PlayMusic", 0.2f);
        }
    }

    /// <summary>
    /// 核心音乐播放逻辑
    /// 特性：
    /// - 自动匹配场景对应音乐
    /// - 带2秒淡入效果的播放
    /// - 音乐结束后自动循环
    /// </summary>
    void PlayMusic()
    {
        AudioClip clipToPlay = null;

        // 场景-音乐映射逻辑（可扩展为字典配置）
        if (_sceneName == "Main Menu")
        {
            clipToPlay = menuTheme;
        }
        else if (_sceneName == "Game")
        {
            clipToPlay = mainTheme;
        }

        if (clipToPlay != null)
        {
            // 通过音频管理器播放（参数2表示2秒淡入时间）
            AudioManager.instance.PlayMusic(clipToPlay, 2);

            // 计算音乐时长并设置循环调用
            Invoke("PlayMusic", clipToPlay.length);
        }
    }
}