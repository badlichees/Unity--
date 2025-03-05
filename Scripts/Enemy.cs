using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class Enemy : LivingEntity
{
    #region 变量声明
    
    /// <summary>
    /// 敌人状态枚举：空闲/追逐/攻击
    /// </summary>
    public enum State { Idle, Chasing, Attacking };
    /// <summary>
    /// 敌人死亡时播放的粒子特效
    /// </summary>
    public ParticleSystem deathEffect;
    public static event System.Action OnDeathStatic;
    
    /// <summary>
    /// 当前敌人状态
    /// </summary>
    State _currentState;
    /// <summary>
    /// 导航网格代理组件
    /// </summary>
    NavMeshAgent _pathfinder;
    /// <summary>
    /// 追踪目标对象的Transform
    /// </summary>
    Transform _target;
    /// <summary>
    /// 目标对象的LivingEntity组件
    /// </summary>
    LivingEntity _targetEntity;
    /// <summary>
    /// 用于动态改变颜色的材质实例
    /// </summary>
    Material _skinMaterial;
    /// <summary>
    /// 材质的原始颜色缓存
    /// </summary>
    Color _originalColor;
    /// <summary>
    /// 触发攻击的最小距离阈值（单位：米）
    /// </summary>
    float _attackDistanceThreshold = .5f;
    /// <summary>
    /// 两次攻击之间的冷却时间（单位：秒）
    /// </summary>
    float _timeBetweenAttacks = 1;
    /// <summary>
    /// 单次攻击造成的伤害值
    /// </summary>
    float _damage = 1;
    /// <summary>
    /// 下次可进行攻击的时间戳
    /// </summary>
    float _nextAttackTime;
    /// <summary>
    /// 本体的碰撞体半径（用于距离计算）
    /// </summary>
    float _myCollisionRadius;
    /// <summary>
    /// 目标的碰撞体半径（用于距离计算）
    /// </summary>
    float _targetCollisionRadius;
    /// <summary>
    /// 是否具有有效追踪目标的标志
    /// </summary>
    bool _hasTarget;
    
    #endregion

    void Awake()
    {
        _pathfinder = GetComponent<NavMeshAgent>();

        // 如果找到了目标（通过标签寻找Player目标）
        if (GameObject.FindGameObjectWithTag ("Player") != null)
        {
            // 调整相关参数
            _hasTarget = true;

            // 设置目标为玩家，并获取相关组件
            _target = GameObject.FindGameObjectWithTag ("Player").transform;
            _targetEntity = _target.GetComponent<LivingEntity> ();

            // 获取敌人和目标（玩家）的胶囊体半径
            _myCollisionRadius = GetComponent<CapsuleCollider> ().radius;
            _targetCollisionRadius = _target.GetComponent<CapsuleCollider> ().radius;
        }
    }
    
    /// <summary>
    /// 初始化敌人组件，启动追踪逻辑
    /// </summary>
    protected override void Start()
    {
        // 调用基方法，实例化相关组件
        base.Start();

        // 如果找到了目标（通过标签寻找Player目标）
        if (_hasTarget)
        {
            // 调整相关参数
            _currentState = State.Chasing;

            // 设置目标为玩家，并获取相关组件，为OnDeath事件订阅目标死亡方法
            _targetEntity.OnDeath += OnTargetDeath;

            // 调用更新路径的协程方法
            StartCoroutine (UpdatePath ());
        }
    }
    
    private void Update()
    {
        if (_hasTarget)
        {
            // 如果当前时间已经超过了下次攻击时间的时间戳
            if (Time.time > _nextAttackTime) 
            {
                // 计算与目标的平方距离
                float sqrDistanceToTarget = (_target.position - transform.position).sqrMagnitude;
                
                // 如果与目标的平方距离小于碰撞体半径总和再加上攻击阈值的平方
                if (sqrDistanceToTarget < Mathf.Pow (_attackDistanceThreshold + _myCollisionRadius + _targetCollisionRadius, 2)) 
                {
                    // 更新下次攻击时间，调用AudioManager的单例实例播放攻击音效，启动攻击协程
                    _nextAttackTime = Time.time + _timeBetweenAttacks;
                    AudioManager.instance.PlaySound("Enemy Attack", transform.position);
                    StartCoroutine (Attack ());
                }
            }
        }
    }

    /// <summary>
    /// 初始化/更新敌人的核心属性配置
    /// </summary>
    /// <param name="moveSpeed">移动速度（单位/秒）</param>
    /// <param name="hitsToKillPlayer">击杀玩家需要攻击的次数（用于反向计算伤害值）</param>
    /// <param name="enemyHealth">敌人的初始生命值</param>
    /// <param name="skinColor">敌人的基础颜色（同时应用于材质和死亡特效）</param>
    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, float enemyHealth, Color skinColor)
    {
        // 设置寻路组件的移动速度
        _pathfinder.speed = moveSpeed;

        // 当存在攻击目标时，根据玩家生命值计算单次攻击伤害
        if (_hasTarget)
        {
            // 伤害计算公式：玩家总生命值 / 击杀所需次数，向上取整（确保整数伤害）
            _damage = Mathf.Ceil(_targetEntity.startingHealth / hitsToKillPlayer);
        }
    
        // 设置敌人的初始生命值
        startingHealth = enemyHealth;
    
        // 配置死亡特效颜色（保留RGB值，强制设置透明度为1）
        deathEffect.startColor = new Color(skinColor.r, skinColor.g, skinColor.b, 1);
    
        // 获取并设置敌人材质的颜色
        _skinMaterial = GetComponent<Renderer>().material;
        _skinMaterial.color = skinColor;
    
        // 保存原始颜色用于重置（例如受击后颜色恢复）
        _originalColor = _skinMaterial.color;
    }
    
    /// <summary>
    /// 处理受到伤害的逻辑，触发死亡效果（覆写了LivingEntity中的虚方法）
    /// </summary>
    /// <param name="damage">受到的伤害值</param>
    /// <param name="hitPoint">命中点世界坐标</param>
    /// <param name="hitDirection">命中方向向量</param>
    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        // 调用单例实例的方法播放受击音效
        AudioManager.instance.PlaySound("Impact", transform.position);
        
        // 如果伤害值大于生命值
        if (damage >= Health) 
        {
            if (OnDeathStatic != null)
            {
                OnDeathStatic();
            }
            AudioManager.instance.PlaySound("Enemy Death", transform.position);
            // 生成死亡特效，并在特效播放完成后销毁
            Destroy(Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)), deathEffect.startLifetime);
        }
        base.TakeHit (damage, hitPoint, hitDirection);
    }

    /// <summary>
    /// 执行攻击动作的协程
    /// </summary>
    IEnumerator Attack()
    {
        // 进入攻击状态并禁用导航
        _currentState = State.Attacking;
        _pathfinder.enabled = false;

        // 初始化敌人攻击前的初始位置、与目标距离间的方向向量、攻击点位（目标点位减去方向上的敌人半径）
        Vector3 originalPosition = transform.position;
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        Vector3 attackPosition = _target.position - directionToTarget * _myCollisionRadius;

        // 攻击动画速度和已播放的百分比
        float attackSpeed = 3;
        float percent = 0;

        // 设置攻击时敌人颜色为红色，声明触发伤害的布尔变量（默认false）
        _skinMaterial.color = Color.red;
        bool hasAppliedDamage = false;

        // 当动画还没播放完
        while (percent <= 1) 
        {
            // 如果动画已经播了50%且尚未申请触发伤害
            if (percent >= .5f && !hasAppliedDamage) 
            {
                // 进行触发伤害的申请，调用目标的受伤方法
                hasAppliedDamage = true;
                _targetEntity.TakeDamage(_damage);
            }

            // 更新动画播放的百分比
            percent += Time.deltaTime * attackSpeed;
            
            // 声明一个插值系数（模拟攻击动作的冲击效果，简化为f(x) = 4x(1 - x)），模拟真实战斗中的"挥砍后坐力"
            // 动能模拟：突进阶段（0-50%）：快速接近目标（加速度）
            // 回撤阶段（50-100%）：缓慢回到原位（减速度）
            // 伤害触发同步：在顶点位置（x=0.5）触发伤害判定，符合「攻击动作峰值造成伤害」的认知
            // 视觉冲击强化：非线性运动比线性移动更有打击感
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        // 更新相关参数值
        _skinMaterial.color = _originalColor;
        _currentState = State.Chasing;
        _pathfinder.enabled = true;
    }
    
    /// <summary>
    /// 定期更新路径的协程（刷新率：0.25秒）
    /// </summary>
    private IEnumerator UpdatePath()
    {
        const float refreshRate = 0.25f;
        while (_hasTarget)
        {
            if (_currentState == State.Chasing)
            {
                // 目标方向向量
                Vector3 directionToTarget = (_target.position - transform.position).normalized;
            
                // 计算安全追踪位置：目标（玩家）位置减去方向向量乘以分别的半径和攻击最小阈值的一半
                // 在保持安全距离的前提下，让敌人精准停在可触发攻击的临界位置
                Vector3 targetPosition = _target.position - directionToTarget * (_myCollisionRadius + _targetCollisionRadius + _attackDistanceThreshold / 2);
                // 如果尚未死亡就将目标点定在targetPosition
                if (!Dead)
                {
                    _pathfinder.SetDestination(targetPosition);
                }
            }
            
            // 每隔refreshRate就调用一次
            yield return new WaitForSeconds(refreshRate);
        }
    }

    /// <summary>
    /// 当目标死亡时触发的回调方法
    /// </summary>
    private void OnTargetDeath()
    {
        _hasTarget = false;
        _currentState = State.Idle;
    }
}