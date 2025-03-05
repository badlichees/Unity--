using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏菜单管理系统（主菜单/选项菜单）
/// 功能：
/// - 场景切换管理
/// - 画面设置（分辨率/全屏）
/// - 音量控制（主音量/音乐/音效）
/// - 玩家偏好存储（PlayerPrefs）
/// </summary>
public class Menu : MonoBehaviour 
{
    /// <summary>
    /// 主菜单父物体（包含开始/选项/退出按钮）
    /// </summary>
    public GameObject mainMenuHolder;
    /// <summary>
    /// 选项菜单父物体（包含画面/声音设置）
    /// </summary>
    public GameObject optionsMenuHolder;
    /// <summary>
    /// 音量调节滑块数组[0:主音量 1:音乐 2:音效]
    /// </summary>
    public Slider[] volumeSliders;
    /// <summary>
    /// 分辨率选项开关数组（需与screenWidths顺序对应）
    /// </summary>
    public Toggle[] resolutionToggles;
    /// <summary>
    /// 全屏模式开关
    /// </summary>
    public Toggle fullscreenToggle;
    /// <summary>
    /// 预设分辨率宽度集合（高度根据16:9比例自动计算）
    /// </summary>
    public int[] screenWidths;

    /// <summary>
    /// 当前激活的分辨率索引（对应screenWidths数组）
    /// </summary>
    private int _activeScreenResolutionIndex;

    /// <summary>
    /// 初始化玩家偏好设置
    /// 注意：在Start而非Awake中初始化，确保AudioManager已就绪
    /// </summary>
    void Start() 
    {
        // 加载存储的分辨率偏好（默认0）
        _activeScreenResolutionIndex = PlayerPrefs.GetInt("Screen Resolution Index", 0);
        
        // 加载全屏状态（0为窗口模式，1为全屏）
        bool isFullScreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        // 初始化音量滑块（需确保AudioManager已存在）
        volumeSliders[0].value = AudioManager.instance.MasterVolumePercent;
        volumeSliders[1].value = AudioManager.instance.MusicVolumePercent;
        volumeSliders[2].value = AudioManager.instance.SoundEffectVolumePercent;

        // 设置分辨率选项开关状态
        for (int i = 0; i < resolutionToggles.Length; i++)
        {
            resolutionToggles[i].isOn = (i == _activeScreenResolutionIndex);
        }

        // 应用全屏状态
        fullscreenToggle.isOn = isFullScreen;
    }

    /// <summary>
    /// 开始游戏按钮事件（加载游戏场景）
    /// </summary>
    public void Play() 
    {
        SceneManager.LoadScene("Game"); // 需确保Build Settings中包含该场景
    }

    /// <summary>
    /// 退出游戏按钮事件
    /// 注意：编辑器模式下不会生效，仅在编译后有效
    /// </summary>
    public void Quit() 
    {
        Application.Quit();
    }

    /// <summary>
    /// 打开选项菜单（隐藏主菜单）
    /// </summary>
    public void OptionsMenu() 
    {
        mainMenuHolder.SetActive(false);
        optionsMenuHolder.SetActive(true);
    }

    /// <summary>
    /// 返回主菜单（隐藏选项菜单）
    /// </summary>
    public void MainMenu() 
    {
        mainMenuHolder.SetActive(true);
        optionsMenuHolder.SetActive(false);
    }

    /// <summary>
    /// 设置屏幕分辨率
    /// </summary>
    /// <param name="i">分辨率选项索引（对应screenWidths数组）</param>
    public void SetScreenResolution(int i)
    {
        if (resolutionToggles[i].isOn) 
        {
            _activeScreenResolutionIndex = i;
            float aspectRatio = 16f / 9f; // 固定16:9宽高比
            int height = (int)(screenWidths[i] / aspectRatio);
            
            Screen.SetResolution(screenWidths[i], height, Screen.fullScreen);
            
            // 存储偏好设置
            PlayerPrefs.SetInt("Screen Resolution Index", i);
            PlayerPrefs.Save(); // 立即写入磁盘
        }
    }

    /// <summary>
    /// 切换全屏模式
    /// </summary>
    /// <param name="activeFullscreen">是否启用全屏</param>
    public void SetFullscreen(bool activeFullscreen)
    {
        // 禁用分辨率选项交互（全屏时不可选）
        foreach (var toggle in resolutionToggles)
        {
            toggle.interactable = !activeFullscreen;
        }

        if (activeFullscreen)
        {
            // 获取所有支持的分辨率（按升序排列）
            Resolution[] allResolutions = Screen.resolutions;
            
            // 选择最后一个（最大）分辨率
            Resolution maxResolution = allResolutions[allResolutions.Length - 1];
            
            // 设置为最大分辨率并启用全屏
            Screen.SetResolution(maxResolution.width, maxResolution.height, true);
        }
        else
        {
            // 恢复之前存储的分辨率
            SetScreenResolution(_activeScreenResolutionIndex);
        }

        // 保存用户偏好
        PlayerPrefs.SetInt("Fullscreen", activeFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 设置主音量（通过AudioManager）
    /// </summary>
    public void SetMasterVolume(float value)  
    {
        AudioManager.instance.SetVolume(value, AudioManager.AudioChannel.Master);
    }

    /// <summary>
    /// 设置音乐音量（通过AudioManager）
    /// </summary>
    public void SetMusicVolume(float value) 
    {
        AudioManager.instance.SetVolume(value, AudioManager.AudioChannel.Music);
    }

    /// <summary>
    /// 设置音效音量（通过AudioManager）
    /// </summary>
    public void SetSoundEffectsVolume(float value) 
    {
        AudioManager.instance.SetVolume(value, AudioManager.AudioChannel.SoundEffect);
    }
}