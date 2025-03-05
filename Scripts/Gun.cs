using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

[System.Serializable]
public class Gun : MonoBehaviour
{
    /// <summary>
    /// 武器开火模式枚举
    /// </summary>
    public enum FireMode { Auto, Burst, Single };

    /// <summary>
    /// 当前开火模式
    /// </summary>
    public FireMode fireMode;
    /// <summary>
    /// 子弹生成点数组（支持多枪管武器）
    /// </summary>
    public Transform[] projectileSpawn;
    /// <summary>
    /// 子弹预制体
    /// </summary>
    public Projectile projectile;
    /// <summary>
    /// 射击冷却时间（转换为秒使用）
    /// </summary>
    public float msBetweenShots = 100;
    /// <summary>
    /// 弹丸射出速度
    /// </summary>
    public float muzzleVelocity = 35;
    /// <summary>
    /// 弹壳预制体
    /// </summary>
    public Transform shell;
    /// <summary>
    /// 弹壳抛射口位置
    /// </summary>
    public Transform shellEjection;
    /// <summary>
    /// 连发模式每次射击子弹数
    /// </summary>
    public int burstCount;
    /// <summary>
    /// 后坐位移随机范围（前后轴）
    /// </summary>
    public Vector2 kickMinAndMax = new Vector2(.05f, .5f);
    /// <summary>
    /// 后坐角度随机范围（X轴旋转）
    /// </summary>
    public Vector2 recoilAngleMinAndMax = new Vector2(3, 5);
    /// <summary>
    /// 位移后坐恢复时间
    /// </summary>
    public float recoilMoveSettleTime = .1f;
    /// <summary>
    /// 旋转后坐恢复时间
    /// </summary>
    public float recoilRotationSettleTime = .1f;
    /// <summary>
    /// 单个弹匣容量
    /// </summary>
    public int projectilesPerMagazine;
    /// <summary>
    /// 换弹耗时（秒）
    /// </summary>
    public float reloadTime = .3f;
    /// <summary>
    /// 射击音效
    /// </summary>
    public AudioClip shootAudio;
    /// <summary>
    /// 换弹音效
    /// </summary>
    public AudioClip reloadAudio;
    
    /// <summary>
    /// 单发模式扳机释放标记
    /// </summary>
    bool _triggerReleasedSinceLastShot;
    /// <summary>
    /// 枪口闪光组件引用
    /// </summary>
    MuzzleFlash _muzzleFlash;
    /// <summary>
    /// 连发模式剩余可发射数
    /// </summary>
    int _shotsRemainingInBurst;
    /// <summary>
    /// 下次允许射击时间戳
    /// </summary>
    float _nextShotTime;
    /// <summary>
    /// 后坐位移平滑速度（供SmoothDamp使用）
    /// </summary>
    Vector3 _recoilSmoothDampVelocity;
    /// <summary>
    /// 当前后坐旋转角度
    /// </summary>
    float _recoilAngle;
    /// <summary>
    /// 旋转平滑速度
    /// </summary>
    float _recoilRotateSmoothDampVelocity;
    /// <summary>
    /// 当前弹匣剩余弹药
    /// </summary>
    int _projectilesRemainingInMagazine;
    /// <summary>
    /// 换装弹药状态标记
    /// </summary>
    bool _isReloading;

    /// <summary>
    /// 初始化武器组件和状态
    /// </summary>
    void Start()
    {
        _muzzleFlash = GetComponent<MuzzleFlash>();    // 获取枪口闪光效果组件
        _shotsRemainingInBurst = burstCount;           // 初始化连发计数器
        _projectilesRemainingInMagazine = projectilesPerMagazine; // 装填初始弹药
    }

    /// <summary>
    /// 每帧最后执行的后坐力恢复和换弹检测
    /// </summary>
    void LateUpdate()
    {
        // 应用平滑后坐位移恢复（向初始位置移动）
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition, 
            Vector3.zero, 
            ref _recoilSmoothDampVelocity, 
            recoilMoveSettleTime
        );

        // 应用平滑后坐旋转恢复（角度归零）
        _recoilAngle = Mathf.SmoothDamp(
            _recoilAngle, 
            0, 
            ref _recoilRotateSmoothDampVelocity, 
            recoilRotationSettleTime
        );
        transform.localEulerAngles += Vector3.left * _recoilAngle;

        // 自动换弹检测：非换弹状态且弹药耗尽时自动触发
        if (!_isReloading && _projectilesRemainingInMagazine == 0)
        {
            Reload();
        }
    }
    
    /// <summary>
    /// 核心射击逻辑（由输入事件触发）
    /// </summary>
    void Shoot()
    {
        // 射击条件检测：非换弹状态、冷却结束、弹药充足
        if (!_isReloading && Time.time > _nextShotTime && _projectilesRemainingInMagazine > 0) 
        {
            // 连发模式弹药计数检测
            if (fireMode == FireMode.Burst)
            {
                if (_shotsRemainingInBurst == 0) return;
                _shotsRemainingInBurst--;  // 减少连发计数器
            }
            // 单发模式扳机释放检测
            else if (fireMode == FireMode.Single)
            {
                if (!_triggerReleasedSinceLastShot) return;
            }

            // 多枪管循环射击逻辑
            for (int i = 0; i < projectileSpawn.Length; i++)
            {
                if (_projectilesRemainingInMagazine == 0) break; // 弹药耗尽提前终止
                _projectilesRemainingInMagazine--;  // 消耗弹药
                
                // 设置射击冷却时间（毫秒转秒）
                _nextShotTime = Time.time + msBetweenShots / 1000;
                
                // 生成子弹并设置初速度
                Projectile newProjectile = Instantiate(
                    projectile, 
                    projectileSpawn[i].position, 
                    projectileSpawn[i].rotation
                );
                newProjectile.SetSpeed(muzzleVelocity);
            }
            
            // 生成弹壳并激活枪口闪光
            Instantiate(shell, shellEjection.position, shellEjection.rotation);
            _muzzleFlash.Activate();
            
            // 应用随机后坐位移（沿z轴）
            transform.localPosition -= Vector3.forward * Random.Range(
                kickMinAndMax.x, 
                kickMinAndMax.y
            );
            
            // 应用随机后坐旋转（X轴）并限制最大角度
            _recoilAngle += Random.Range(
                recoilAngleMinAndMax.x, 
                recoilAngleMinAndMax.y
            );
            _recoilAngle = Mathf.Clamp(_recoilAngle, 0, 30);

            // 播放射击音效
            AudioManager.instance.PlaySound(shootAudio, transform.position);
        }
    }

    /// <summary>
    /// 扳机持续按下事件处理
    /// </summary>
    public void OnTriggerHold()
    {
        Shoot();
        _triggerReleasedSinceLastShot = false;  // 标记扳机未释放（用于单发模式）
    }

    /// <summary>
    /// 扳机释放事件处理
    /// </summary>
    public void OnTriggerRelease()
    {
        _triggerReleasedSinceLastShot = true;   // 重置单发模式标记
        _shotsRemainingInBurst = burstCount;   // 重置连发计数器
    }

    /// <summary>
    /// 武器瞄准逻辑（换弹时不可瞄准）
    /// </summary>
    /// <param name="aimPoint">目标瞄准点（世界坐标）</param>
    public void Aim(Vector3 aimPoint)
    {
        if (!_isReloading)
        {
            transform.LookAt(aimPoint);  // 直接旋转武器朝向目标点
        }
    }

    /// <summary>
    /// 换弹请求处理（满足条件时启动换弹协程）
    /// </summary>
    public void Reload()
    {
        // 换弹条件：非换弹状态且弹匣未满
        if (!_isReloading && _projectilesRemainingInMagazine != projectilesPerMagazine)
        {
            StartCoroutine(AnimateReload());  // 启动换弹动画协程
            AudioManager.instance.PlaySound(reloadAudio, transform.position);  // 播放换弹音效
        }
    }

    /// <summary>
    /// 换弹动画协程（包含武器旋转动画）
    /// </summary>
    IEnumerator AnimateReload()
    {
        _isReloading = true;  // 进入换弹状态
        
        yield return new WaitForSeconds(0.2f);  // 换弹开始前短暂延迟

        float reloadSpeed = 1f / reloadTime;    // 计算动画速度
        float percent = 0;                       // 动画进度百分比
        Vector3 initialRotation = transform.localEulerAngles;  // 记录初始旋转角度
        float maxReloadAngle = 30;               // 最大换弹旋转角度

        // 换弹动画循环
        while (percent < 1)
        {
            percent += Time.deltaTime * reloadSpeed;
            // 使用二次函数插值实现先快后慢的动画曲线
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            float reloadAngle = Mathf.Lerp(0, maxReloadAngle, interpolation);
            transform.localEulerAngles = initialRotation + Vector3.left * reloadAngle;
            
            yield return null;
        }
        
        _isReloading = false;  // 退出换弹状态
        _projectilesRemainingInMagazine = projectilesPerMagazine;  // 填满弹匣
    }
}