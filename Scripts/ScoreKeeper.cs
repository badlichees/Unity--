using UnityEngine;

/// <summary>
/// 游戏积分管理系统（单例模式候选）
/// 功能：
/// - 实时追踪玩家得分
/// - 连击奖励机制
/// - 玩家死亡事件处理
/// - 自动事件订阅管理
/// </summary>
public class ScoreKeeper : MonoBehaviour
{
    /// <summary>
    /// 当前游戏总积分（静态可读属性）
    /// </summary>
    public static int Score { get; private set; }

    /// <summary>
    /// 最后一次敌人击杀时间戳
    /// </summary>
   float _lastEnemyKillTime;
    /// <summary>
    /// 当前连击次数（影响积分倍数）
    /// </summary>
    int _streakCount;
    /// <summary>
    /// 连击有效时间间隔（单位：秒）
    /// </summary>
    float _streakExpiryTime = 1;
    

    /// <summary>
    /// 初始化事件监听
    /// </summary>
    void Start() 
    {
        // 订阅敌人死亡事件
        Enemy.OnDeathStatic += OnEnemyKilled;
        
        // 查找玩家对象并订阅死亡事件
        FindObjectOfType<Player>().OnDeath += OnPlayerDeath;
    }

    /// <summary>
    /// 敌人击杀事件处理器
    /// 包含连击计算逻辑：
    /// - 在有效时间内连续击杀获得2^n倍奖励
    /// - 连击超时则重置倍数
    /// </summary>
    void OnEnemyKilled() 
    {
        // 连击有效性检测（是否在有效时间内）
        bool withinStreakWindow = Time.time < _lastEnemyKillTime + _streakExpiryTime;

        // 更新连击状态
        _streakCount = withinStreakWindow ? _streakCount + 1 : 0;

        // 刷新最后击杀时间
        _lastEnemyKillTime = Time.time;

        // 计算得分：基础5分 + 2的连击次方（指数增长奖励）
        Score += 5 + (int)Mathf.Pow(2, _streakCount);
    }

    /// <summary>
    /// 玩家死亡事件处理
    /// 安全取消事件订阅防止空引用
    /// </summary>
    void OnPlayerDeath() 
    {
        Enemy.OnDeathStatic -= OnEnemyKilled;
    }
}