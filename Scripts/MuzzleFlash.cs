using UnityEngine;

/// <summary>
/// 枪口闪光效果控制系统
/// 功能：
/// - 武器开火时播放随机闪光样式
/// - 自动计时隐藏效果
/// - 多渲染器同步更新
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    /// <summary>
    /// 闪光效果父物体（控制整体显隐）
    /// </summary>
    public GameObject flashHolder;
    /// <summary>
    /// 闪光持续时间（秒）
    /// </summary>
    public float flashTime = 0.1f;
    /// <summary>
    /// 可选闪光样式精灵集合
    /// </summary>
    public Sprite[] flashSprites;
    /// <summary>
    /// 需要更新材质的渲染器组件（通常为多个方位的子物体）
    /// </summary>
    public SpriteRenderer[] spriteRenderers;
    

    /// <summary>
    /// 初始化隐藏枪口闪光
    /// </summary>
    void Start()
    {
        DeActivate(); // 确保初始状态为隐藏
    }
    
    /// <summary>
    /// 激活枪口闪光效果
    /// 执行流程：
    /// 1. 显示闪光父物体
    /// 2. 随机选择闪光样式
    /// 3. 同步更新所有渲染器材质
    /// 4. 启动自动关闭计时器
    /// </summary>
    public void Activate()
    {
        flashHolder.SetActive(true);
        
        // 随机选择闪光样式（确保数组不为空）
        int flashSpriteIndex = Random.Range(0, flashSprites.Length);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sprite = flashSprites[flashSpriteIndex];
        }
        
        // 设置自动关闭（使用字符串方法名需注意重构风险）
        Invoke("DeActivate", flashTime); 
    }

    /// <summary>
    /// 隐藏枪口闪光效果
    /// 注意：保留私有访问权限防止外部误调用
    /// </summary>
    void DeActivate()
    {
        flashHolder.SetActive(false);
    }
}