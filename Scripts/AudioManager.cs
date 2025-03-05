using UnityEngine;
using System.Collections;

/// <summary>
/// 音频管理系统（单例模式）
/// 功能：全局音量控制、音乐交叉淡入淡出、2D/3D音效播放、音量设置持久化
/// 特性：DontDestroyOnLoad 跨场景存活、自动跟随玩家位置更新音频监听器
/// </summary>
public class AudioManager : MonoBehaviour 
{
    /// <summary>
    /// 音频通道类型枚举
    /// </summary>
    public enum AudioChannel { Master, SoundEffect, Music };
    /// <summary>
    /// 主音量百分比（影响所有声道）
    /// </summary>
    public float MasterVolumePercent { get; private set; }   
    /// <summary>
    /// 音效独立音量百分比
    /// </summary>
    public float SoundEffectVolumePercent  { get; private set; }  
    /// <summary>
    /// 音乐独立音量百分比
    /// </summary>
    public float MusicVolumePercent  { get; private set; }    
    /// <summary>
    /// 单例实例
    /// </summary>
    public static AudioManager instance; 

    /// <summary>
    /// // 2D音效专用源（无需空间定位）
    /// </summary>
    AudioSource _soundEffect2DSource; 
    /// <summary>
    /// 双音乐源数组（用于交叉淡入淡出）
    /// </summary>
    AudioSource[] _musicSources;  
    /// <summary>
    /// 当前激活的音乐源索引
    /// </summary>
    int _activeMusicSourceIndex;
    /// <summary>
    /// 场景中的音频监听器
    /// </summary>
    Transform _audioListener;   
    /// <summary>
    /// 玩家位置（用于更新监听器位置）
    /// </summary>
    Transform _playerTransform;
    /// <summary>
    /// 音效名称与AudioClip的映射库
    /// </summary>
    SoundLibrary _library;               

    /// <summary>
    /// 初始化单例和音频系统
    /// </summary>
    void Awake() 
    {
        // 单例冲突处理
        if (instance != null) 
        {
            Destroy(gameObject);
        } 
        else 
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // 该方法让传入的GameObject在切换场景时不被摧毁，实现跨场景保留

            // 初始化声音资源库
            _library = GetComponent<SoundLibrary>();

            // 创建双音乐源（用于交叉淡入淡出）
            _musicSources = new AudioSource[2];
            for (int i = 0; i < 2; i++) 
            {
                GameObject newMusicSource = new GameObject($"Music Source {i + 1}"); // 在字符串前使用“$”表示该字符串是一个插值字符串使之可以直接嵌入表达式
                _musicSources[i] = newMusicSource.AddComponent<AudioSource>(); // 为新创建的GameObject添加AudioSource组件并将该组件复制给对应的数组元素
                newMusicSource.transform.parent = transform;  // 将创建的newMusicSource作为该组件的子物体管理
                _musicSources[i].loop = true;  // 音乐默认循环
            }

            // 创建2D音效专用源相关逻辑（添加组件、设置父物体）
            GameObject newSoundEffect2DSource = new GameObject("2D SoundEffect Source");
            _soundEffect2DSource = newSoundEffect2DSource.AddComponent<AudioSource>();
            newSoundEffect2DSource.transform.parent = transform;

            // 获取音频监听器和玩家位置，“?.”为空值条件运算符，若没有找到组件则会直接报空，不会报错
            _audioListener = FindObjectOfType<AudioListener>()?.transform;
            _playerTransform = FindObjectOfType<Player>()?.transform;

            // 加载保存的音量设置（默认全音量）（为键值对格式，若key存在则返回对应的浮点数值；若不存在则返回默认值）
            MasterVolumePercent = PlayerPrefs.GetFloat("Master Volume", 1);
            SoundEffectVolumePercent = PlayerPrefs.GetFloat("SoundEffect Volume", 1);
            MusicVolumePercent = PlayerPrefs.GetFloat("Music Volume", 1);
        }
    }

    /// <summary>
    /// 每帧更新音频监听器位置（跟随玩家）
    /// </summary>
    void Update() 
    {
        if (_playerTransform != null && _audioListener != null) 
        {
            _audioListener.position = _playerTransform.position;  // 3D音效空间定位
        }
    }

    /// <summary>
    /// 设置指定通道的音量并保存设置
    /// </summary>
    /// <param name="volumePercent">0-1的音量百分比</param>
    /// <param name="channel">目标音频通道</param>
    public void SetVolume(float volumePercent, AudioChannel channel)
    {
        // 设置指定通道音量
        switch (channel) 
        {
            case AudioChannel.Master:
                MasterVolumePercent = volumePercent;
                break;
            case AudioChannel.SoundEffect:
                SoundEffectVolumePercent = volumePercent;
                break;
            case AudioChannel.Music:
                MusicVolumePercent = volumePercent;
                break;
        }

        // 更新音乐源实际音量（主音量叠加）
        _musicSources[0].volume = MusicVolumePercent * MasterVolumePercent;
        _musicSources[1].volume = MusicVolumePercent * MasterVolumePercent;

        // 保存到PlayerPrefs
        PlayerPrefs.SetFloat("Master Volume", MasterVolumePercent);
        PlayerPrefs.SetFloat("SoundEffect Volume", SoundEffectVolumePercent);
        PlayerPrefs.SetFloat("Music Volume", MusicVolumePercent);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 播放音乐（带交叉淡入淡出效果）
    /// </summary>
    /// <param name="clip">音乐片段</param>
    /// <param name="fadeDuration">淡入淡出时间（秒）</param>
    public void PlayMusic(AudioClip clip, float fadeDuration = 1) 
    {
        // 切换激活的音乐源索引（0和1交替切换）
        _activeMusicSourceIndex = 1 - _activeMusicSourceIndex;
        
        // 将传入的AudioClip赋值给新激活的音频源并播放
        _musicSources[_activeMusicSourceIndex].clip = clip;
        _musicSources[_activeMusicSourceIndex].Play();

        // 启动协程实现音量渐变
        StartCoroutine(AnimateMusicCrossfade(fadeDuration));
    }

    /// <summary>
    /// 在指定位置播放3D音效
    /// </summary>
    public void PlaySound(AudioClip clip, Vector3 position)
    {
        if (clip != null)
        {
            // 使用静态方法播放带空间定位的音效
            AudioSource.PlayClipAtPoint(
                clip, 
                position, 
                SoundEffectVolumePercent * MasterVolumePercent  // 叠加音量计算
            );
        }
    }

    /// <summary>
    /// 通过名称在指定位置播放3D音效
    /// </summary>
    public void PlaySound(string soundName, Vector3 position) 
    {
        // 在指定位置调用sound库中的clip并在指定position播放
        PlaySound(_library.GetClipFromName(soundName), position);
    }

    /// <summary>
    /// 播放2D音效（无视位置）
    /// </summary>
    public void PlaySound2D(string soundName) 
    {
        // 该方法用于播放一次音频（传入soundName）
        _soundEffect2DSource.PlayOneShot(
            _library.GetClipFromName(soundName),
            SoundEffectVolumePercent * MasterVolumePercent  // 叠加音量计算
        ); 
    }

    /// <summary>
    /// 音乐交叉淡入淡出协程
    /// </summary>
    /// <param name="duration">音乐过渡时间</param>
    IEnumerator AnimateMusicCrossfade(float duration) 
    {
        float percent = 0; // 播放百分比

        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / duration;  // 计算插值比例
            
            // 渐入新音乐源（使用线性插值）
            _musicSources[_activeMusicSourceIndex].volume = Mathf.Lerp(
                0, 
                MusicVolumePercent * MasterVolumePercent, 
                percent
            );
            
            // 渐出旧音乐源（使用线性插值）
            _musicSources[1 - _activeMusicSourceIndex].volume = Mathf.Lerp(
                MusicVolumePercent * MasterVolumePercent, 
                0, 
                percent
            );

            yield return null; // 每帧执行一次协程
        }
    }
}