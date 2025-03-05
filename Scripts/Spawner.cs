using UnityEngine;
using System.Collections;

/// <summary>
/// 敌人生成系统控制器
/// 功能：波次管理、露营检测、动态生成逻辑
/// </summary>
public class Spawner : MonoBehaviour 
{
    /// <summary>
    /// 波次配置数组
    /// </summary>
    public Wave[] waves;
    /// <summary>
    /// 敌人预制体引用
    /// </summary>
    public Enemy enemy;
    /// <summary>
    /// 
    /// </summary>
    public event System.Action<int> OnNewWave;
    /// <summary>
    /// 是否开启开发者模式
    /// </summary>
    public bool developerMode;
    
    /// <summary>
    /// 玩家实体组件
    /// </summary>
    LivingEntity _playerEntity;    
    /// <summary>
    /// 玩家位置引用
    /// </summary>
    Transform _playerTransform; 
    /// <summary>
    /// 当前波次配置
    /// </summary>
    Wave _currentWave;   
    /// <summary>
    /// 当前波次序号（从1开始）
    /// </summary>
    int _currentWaveNumber;  
    /// <summary>
    /// 本波剩余待生成敌人数
    /// </summary>
    int _enemiesRemainingToSpawn; 
    /// <summary>
    /// 本波存活敌人数
    /// </summary>
    int _enemiesRemainingAlive;
    /// <summary>
    /// 下次生成时间戳 
    /// </summary>
    float _nextSpawnTime;           
    /// <summary>
    /// 地图生成器引用
    /// </summary>
    MapGenerator _map;       
    /// <summary>
    /// 露营行为检测间隔（秒）
    /// </summary>
    float _timeBetweenCampingChecks = 2;   
    /// <summary>
    /// 视为露营行为的移动阈值
    /// </summary>
    float _campThresholdDistance = 1.5f;   
    /// <summary>
    /// 下次露营行为的检测时间
    /// </summary>
    float _nextCampCheckTime;       
    /// <summary>
    /// 上次记录的玩家位置
    /// </summary>
    Vector3 _campPositionOld;      
    /// <summary>
    /// 当前是否判定为露营
    /// </summary>
    bool _isCamping;               
    /// <summary>
    /// 系统状态，是否停止生成（如玩家死亡时）
    /// </summary>
    bool _isDisabled;
    
    void Start() 
    {
        // 获取玩家相关引用
        _playerEntity = FindObjectOfType<Player>();
        _playerTransform = _playerEntity.transform;
        
        // 初始化露营检测
        _nextCampCheckTime = _timeBetweenCampingChecks + Time.time;
        _campPositionOld = _playerTransform.position;
        _playerEntity.OnDeath += OnPlayerDeath; // 订阅玩家死亡事件
        
        // 获取地图生成器，生成第一波敌人
        _map = FindObjectOfType<MapGenerator>();
        NextWave();
    }

    /// <summary>
    /// 每帧更新逻辑：
    /// 1. 露营状态检测
    /// 2. 敌人生成调度
    /// </summary>
    void Update() 
    {
        // 如果生成系统处于激活状态
        if (!_isDisabled) 
        {
            // 若是当前时间大于下次检测露营的时间（即已经到了下次检测露营的时间）
            if (Time.time > _nextCampCheckTime)
            {
                // 刷新下次检测露营的时间点（当前时间加上检测露营的间隔时间）
                _nextCampCheckTime = Time.time + _timeBetweenCampingChecks;
                
                // 计算玩家当前位置与上次记录位置的移动距离：若是小于移动阈值，则_isCamping返回true，表示正在露营；同时更新露营记录位置（作为上次记录的玩家露营位置）
                _isCamping = Vector3.Distance(_playerTransform.position, _campPositionOld) < _campThresholdDistance;
                _campPositionOld = _playerTransform.position;
            }

            // 敌人生成条件判断：
            if ((_enemiesRemainingToSpawn > 0 || _currentWave.infinite) && Time.time > _nextSpawnTime)
            {
                // 待生成敌人数减一，同时刷新下次生成时间点（当前时间加上间隔时间）
                _enemiesRemainingToSpawn--;
                _nextSpawnTime = Time.time + _currentWave.timeBetweenSpawns;
                
                // 启动协程生成单个敌人
                StartCoroutine("SpawnEnemy");
            }
        }
        
        if (developerMode)
        {
            // 按下Enter键
            if (Input.GetKeyDown(KeyCode.Return)) 
            {
                // 停止生成敌人的协程
                StopCoroutine("SpawnEnemy");
                
                // 在场景中遍历每一个enemy并销毁，调用下一波的方法
                foreach (Enemy enemy in FindObjectsOfType<Enemy>())
                {
                    Destroy(enemy.gameObject);
                }
                NextWave();
            }
        }
    }

    /// <summary>
    /// 敌人生成协程：
    /// 1. 选择生成点（普通模式/露营模式）
    /// 2. 地块闪烁效果
    /// 3. 实例化敌人并注册死亡事件
    /// </summary>
    IEnumerator SpawnEnemy()
    {
        const float spawnDelay = 1; // 生成前延迟（秒）
        const float tileFlashSpeed = 4; // 地块闪烁速度

        // 获取生成地块
        Transform spawnTile = _map.GetRandomOpenTile();
        
        // 如果玩家在露营，则在玩家所在位置生成
        if (_isCamping) 
        {
            spawnTile = _map.GetTileFromPosition(_playerTransform.position);
        }

        
        Material tileMaterial = spawnTile.GetComponent<Renderer>().material;// 获取地块材质
        Color initialColor = Color.white; // 初始颜色
        Color flashColour = Color.red; // 闪烁目标颜色
        
        float spawnTimer = 0; // 生成计时器

        // 地块闪烁动画循环
        while (spawnTimer < spawnDelay)
        {
            // 使用PingPong函数实现颜色闪烁效果（若第一个参数超过了第二个参数，则为返回状态，如此往复）
            tileMaterial.color = Color.Lerp(initialColor, flashColour, Mathf.PingPong(spawnTimer * tileFlashSpeed, 1)); // 在0-1之间往复插值

            spawnTimer += Time.deltaTime;
            yield return null; // 等待下一帧
        }

        // 实例化敌人并初始化敌人属性（在地块上方1单位高度）
        Enemy spawnedEnemy = Instantiate(enemy, spawnTile.position + Vector3.up, Quaternion.identity);
        spawnedEnemy.OnDeath += OnEnemyDeath; // 注册敌人死亡事件
        spawnedEnemy.SetCharacteristics(_currentWave.moveSpeed, _currentWave.hitsToKillPlayer, _currentWave.enemyHealth, _currentWave.skinColor);
    }

    /// <summary>
    /// 玩家死亡事件处理，停止所有生成逻辑
    /// </summary>
    void OnPlayerDeath() 
    {
        _isDisabled = true; 
    }

    /// <summary>
    /// 敌人死亡事件处理：
    /// 1. 更新存活计数器
    /// 2. 检测是否开启下一波
    /// </summary>
    void OnEnemyDeath() 
    {
        _enemiesRemainingAlive--;

        // 当本波所有敌人被消灭
        if (_enemiesRemainingAlive == 0) 
        {
            NextWave(); // 进入下一波
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ResetPlayerPosition()
    {
        _playerTransform.position = _map.GetTileFromPosition(Vector3.zero).position + Vector3.up * 3;
    }
    
    /// <summary>
    /// 波次推进逻辑：
    /// 1. 递增波次计数器
    /// 2. 加载新波次配置
    /// 3. 重置生成计数器
    /// </summary>
    void NextWave() 
    {
        if (_currentWaveNumber > 0)
        {
            AudioManager.instance.PlaySound2D("Level Complete");
        }
        
        // 波次从1开始
        _currentWaveNumber++;
        
        // 检查是否还有剩余波次
        if (_currentWaveNumber - 1 < waves.Length) 
        {
            // 加载当前波次配置
            _currentWave = waves[_currentWaveNumber - 1]; 

            // 初始化生成计数器
            _enemiesRemainingToSpawn = _currentWave.enemyCount;
            _enemiesRemainingAlive = _enemiesRemainingToSpawn;
            
            // 安全性检测，若不为空才调用
            if (OnNewWave != null)
            {
                OnNewWave(_currentWaveNumber);
            }
        
            ResetPlayerPosition();
        }
    }

    /// <summary>
    /// 波次配置数据结构
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        /// <summary>
        /// 敌人数量是否为无限
        /// </summary>
        public bool infinite;
        /// <summary>
        /// 本波敌人总数
        /// </summary>
        public int enemyCount;
        /// <summary>
        /// 敌人生成间隔（秒）
        /// </summary>
        public float timeBetweenSpawns;
        /// <summary>
        /// 敌人移动速度
        /// </summary>
        public float moveSpeed;
        /// <summary>
        /// 需要打几次才能杀死玩家
        /// </summary>
        public int hitsToKillPlayer;
        /// <summary>
        /// 敌人生命值
        /// </summary>
        public float enemyHealth;
        /// <summary>
        /// 敌人的颜色
        /// </summary>
        public Color skinColor;
    }
}