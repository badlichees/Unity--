using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour 
{
    /// <summary>
    /// 游戏结束后的渐变遮罩面板
    /// </summary>
    public Image fadePlane;
    /// <summary>
    /// 游戏结束的UI内容根节点
    /// </summary>
    public GameObject gameOverUI;
    /// <summary>
    /// 新波次横幅的矩形变换组件（用于动画效果）
    /// </summary>
    public RectTransform newWaveBanner;
    /// <summary>
    /// 新波次标题文本组件（显示当前波次数）
    /// </summary>
    public Text newWaveTitle;
    /// <summary>
    /// 新波次敌人数量统计文本组件
    /// </summary>
    public Text newWaveEnemyCount;
    /// <summary>
    /// 实时得分显示文本组件（格式化为6位数字）
    /// </summary>
    public Text scoreUI;
    /// <summary>
    /// 玩家血条矩形变换组件（通过缩放X轴实现血条效果）
    /// </summary>
    public RectTransform healthBar;
    /// <summary>
    /// 游戏结束界面最终得分显示文本
    /// </summary>
    public Text gameOverScoreUI;

    /// <summary>
    /// 生成器实例
    /// </summary>
    Spawner _spawner;
    /// <summary>
    /// 玩家实例
    /// </summary>
    Player _player;
    
    /// <summary>
    /// 初始化玩家引用并注册死亡事件
    /// </summary>
    void Start ()
    {
        _player = FindObjectOfType<Player>();
        _player.OnDeath += OnGameOver;
    }

    /// <summary>
    /// 初始化敌人生成器并注册新波次事件
    /// </summary>
    void Awake()
    {
        _spawner = FindObjectOfType<Spawner> ();
        _spawner.OnNewWave += OnNewWave;
    }

    /// <summary>
    /// 每帧更新UI状态：
    /// 1. 更新得分显示
    /// 2. 计算并更新血条比例
    /// </summary>
    void Update()
    {
        scoreUI.text = "当前得分：" + ScoreKeeper.Score.ToString("D6");
        float healthPercent = 0;
        if (_player != null)
        {
            // 计算生命值百分比（当前生命/初始生命）
            healthPercent = _player.Health / _player.startingHealth;
        }
        // 通过缩放X轴实现血条长度变化
        healthBar.localScale = new Vector3(healthPercent, 1, 1);
    }
    
    /// <summary>
    /// 新波次事件处理：更新UI横幅信息并触发动画
    /// </summary>
    /// <param name="waveNumber">当前波次索引（从1开始）</param>
    void OnNewWave(int waveNumber)
    {
        // 波次数值转中文数字（支持前5波）
        string[] numbers = { "一", "二", "三", "四", "五" };
        newWaveTitle.text = "—第" + numbers[waveNumber - 1] + "波—";
        
        // 处理敌人数量显示逻辑：无限模式显示"无限"，否则显示具体数值
        string enemyCountString = (_spawner.waves[waveNumber - 1].infinite) 
            ? "无限" 
            : _spawner.waves[waveNumber - 1].enemyCount.ToString();
        newWaveEnemyCount.text = "敌人数量：" + enemyCountString;

        // 重启横幅动画协程
        StopCoroutine("AnimateNewWaveBanner");
        StartCoroutine("AnimateNewWaveBanner");
    }

    /// <summary>
    /// 游戏结束处理流程：
    /// 1. 显示鼠标光标
    /// 2. 启动屏幕渐变效果
    /// 3. 同步最终得分
    /// 4. 隐藏游戏HUD元素
    /// 5. 显示游戏结束界面
    /// </summary>
    void OnGameOver()
    {
        Cursor.visible = true;
        StartCoroutine(Fade(Color.clear, new Color(0, 0, 0, .95f), 1));
        gameOverScoreUI.text = scoreUI.text;
        scoreUI.gameObject.SetActive(false);
        healthBar.transform.parent.gameObject.SetActive(false);
        gameOverUI.SetActive (true);
    }

    /// <summary>
    /// 新波次横幅动画协程：
    /// 实现从屏幕外滑入->停留->滑出的动画效果
    /// </summary>
    IEnumerator AnimateNewWaveBanner()
    {
        float animatePercent = 0;
        float speed = 3f;            // 动画移动速度
        float delayTime = 1.5f;      // 停留持续时间
        int direction = 1;           // 移动方向（1=进入，-1=退出）
        float endDelayTime = Time.time + 1 / speed + delayTime;

        while (animatePercent >= 0)
        {
            animatePercent += Time.deltaTime * speed * direction;

            // 当动画完成进入阶段时，等待指定时间后切换方向
            if (animatePercent >= 1)
            {
                animatePercent = 1;
                if (Time.time >= endDelayTime)
                {
                    direction = -1;
                }
            }
            
            // 使用插值计算Y轴位置（从-180到0）
            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-180,0,animatePercent);
            yield return null;
        }
    }

    /// <summary>
    /// 屏幕渐变效果协程
    /// </summary>
    /// <param name="from">起始颜色（通常为透明）</param>
    /// <param name="to">目标颜色（通常为半透明黑色）</param>
    /// <param name="time">渐变持续时间（秒）</param>
    IEnumerator Fade(Color from, Color to, float time) 
    {
        float speed = 1 / time; 
        float percent = 0; 

        while (percent < 1)
        {
            percent += Time.deltaTime * speed;
            // 使用Color.Lerp实现平滑颜色过渡
            fadePlane.color = Color.Lerp(from, to, percent);
            yield return null;
        }
    }
    
    /// <summary>
    /// 重新开始游戏（加载游戏场景）
    /// </summary>
    public void StartNewGame()
    {
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// 返回主菜单（加载主菜单场景）
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}