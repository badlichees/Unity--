using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图生成系统核心控制器
/// 功能：动态生成网格地图、障碍物布局、导航处理、地图可达性验证
/// 设计模式：基于种子随机生成、洪水填充算法、对象池模式
/// </summary>
public class MapGenerator : MonoBehaviour
{
    #region 变量声明

    [Tooltip("地图数组，存储多张地图")]
    public Map[] maps;
    [Tooltip("地图编号")]
    public int mapIndex;
    [Tooltip("基础瓦片的预制体，用于生成地图网格")]
    public Transform tilePrefab;
    [Tooltip("障碍物预制体")]
    public Transform obstaclePrefab;
    [Tooltip("作为导航网格的地面")]
    public Transform navmeshFloor;
    
    public Transform mapFloor;
    [Tooltip("用于遮盖超出地图范围的导航网格的遮罩")]
    public Transform navmeshMaskPrefab;
    [Tooltip("最大地图尺寸")]
    public Vector2 maxMapSize;
    [Tooltip("瓦片尺寸")]
    public float tileSize;
    
    /// <summary>
    /// 当前地图
    /// </summary>
    Map _currentMap;
    /// <summary>
    /// 瓦片外边框比例
    /// 0: 瓦片紧密排列 1: 仅显示中心点
    /// </summary>
    [Range(0, 1)] public float outlinePercent;
    /// <summary>
    /// 存储所有瓦片坐标的列表（按生成顺序排列）
    /// </summary>
    List<Coord> _allTileCoords;
    /// <summary>
    /// 随机排序后的瓦片坐标队列，用于保证障碍物分布的随机性
    /// </summary>
    Queue<Coord> _shuffledTileCoords;
    /// <summary>
    /// 存储所有生成瓦片的二维数组，按坐标索引（[x,y]对应地图坐标）
    /// </summary>
    Transform[,] _tileMap;
    /// <summary>
    /// 随机排序后的可通行瓦片坐标队列，用于生成玩家出生点等需要安全位置的功能
    /// </summary>
    Queue<Coord> _shuffledOpenTileCoords;
    
    #endregion
   
    void Awake()
    {
        FindObjectOfType<Spawner> ().OnNewWave += OnNewWave;
    }
    
    /// <summary>
    /// 生成新一波敌人时调用的方法：地图编号为当前波数减一，并创建新地图
    /// </summary>
    /// <param name="waveNumber">当前波数</param>
    void OnNewWave(int waveNumber)
    {
        mapIndex = waveNumber - 1;
        GenerateMap ();
    }
    
    /// <summary>
    /// 地图生成脚本的核心方法
    /// </summary>
    public void GenerateMap()
    {
        // 初始化当前地图、瓦片坐标、基于随机种子的随机数生成器，实例化地图的碰撞盒尺寸
        _currentMap = maps[mapIndex];
        _tileMap = new Transform[_currentMap.mapSize.x, _currentMap.mapSize.y];
        System.Random prng = new System.Random(_currentMap.seed);
        
        // 将所有瓦片的坐标实例化并存储在一个列表中（使用双循环遍历将所有的瓦片坐标都添加进_allTileCoords这个列表中）
        // 并且使用Utility中写好的Fisher-Yates算法将存储在_allTileCoords中的所有瓦片的坐标打乱后压入_shuffledTileCoords这个队列中
        _allTileCoords = new List<Coord>();
        for (int x = 0; x < _currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < _currentMap.mapSize.y; y++)
            {
                _allTileCoords.Add(new Coord(x,y));
            }
        }
        _shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(_allTileCoords.ToArray(), _currentMap.seed));
        
        // 设置一个专门用于放置生成的障碍物和瓦片的游戏物体，并将该地图游戏物体设置为其父物体
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }
        Transform mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = transform;

        // 使用双重for循环遍历所有瓦片的坐标，确定好每个瓦片的生成坐标，并且可以通过改变outlinePercent的大小来调整瓦片大小
        for (int x = 0; x < _currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < _currentMap.mapSize.y; y++)
            {
                Vector3 tilePosition = CoordToPosition(x, y);
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(90, 0, 0));
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
                newTile.parent = mapHolder;
                _tileMap[x,y] = newTile;
            }
        }
        
        // 实例化MapIsFullyAccessible()方法中的两个参数
        bool[,] obstacleMap = new bool[_currentMap.mapSize.x, _currentMap.mapSize.y];
        int currentObstacleCount = 0;
        
        // 实例化所有瓦片坐标的列表
        List<Coord> allOpenCoords = new List<Coord> (_allTileCoords);
        
        // 生成障碍物的模块，障碍物数量有多少，就循环多少次
        int obstacleCount = (int)(_currentMap.mapSize.x * _currentMap.mapSize.y * _currentMap.obstaclePercent);
        for (int i = 0; i < obstacleCount; i++)
        {
            // 获取到随机坐标后将其作为地图中障碍物的坐标，为当前障碍物数量加一
            Coord randomCoord = GetRandomCoord();
            obstacleMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;
            
            // 若是随机坐标不为地图中心点且地图可通行
            if (randomCoord != _currentMap.MapCenter && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
            {
                // 使用随机插值生成一个障碍物高度
                float obstacleHeight = Mathf.Lerp(_currentMap.minimumObstacleHeight,_currentMap.maximumObstacleHeight,(float)prng.NextDouble());
                
                // 将二维坐标转换为三维位置并作为障碍物位置，在该位置处生成新障碍物（归到mapHolder之下），调整障碍物的缩放大小
                Vector3 obstaclePosition = CoordToPosition(randomCoord.x,randomCoord.y);
                Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity);
                newObstacle.parent = mapHolder;
                newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1 - outlinePercent) * tileSize);

                // 实例化障碍物的渲染器和材质
                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                
                // 根据当前障碍物的垂直位置计算颜色混合比例（范围0-1）：当瓦片位于地图最下端时，colorPercent=0 使用前景色；最上端时，colorPercent=1 使用背景色
                float colorPercent = randomCoord.y / (float)_currentMap.mapSize.y;
                
                // 在预设的前景色和背景色之间进行线性插值，实现从下至上的渐变色效果（注：此设计可使障碍物颜色与地图整体色调形成纵深感的视觉过渡）
                obstacleMaterial.color = Color.Lerp(_currentMap.foregroundColor, _currentMap.backgroundColor, colorPercent);
                
                // 将生成的新材质赋给障碍物渲染器（需使用sharedMaterial保证材质实例独立），避免直接修改原始预制体材质，确保不同障碍物可拥有独立材质属性
                obstacleRenderer.sharedMaterial = obstacleMaterial;
                
                allOpenCoords.Remove(randomCoord);
            }
            else
            {
                // 否则将该坐标的障碍物区块标记设置为false，减掉一开始加上的当前障碍物数量
                obstacleMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }
        
        // 初始化随机开放瓦片坐标队列（过滤掉障碍物坐标后的可通行区域）
        _shuffledOpenTileCoords = new Queue<Coord> (Utility.ShuffleArray (allOpenCoords.ToArray (), _currentMap.seed));
        
        // 生成上下左右四条边的四个遮罩，并且放入mapHolder之下，设置好遮罩的大小
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (_currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maxMapSize.x - _currentMap.mapSize.x) / 2f, 1, _currentMap.mapSize.y) * tileSize;
        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (_currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - _currentMap.mapSize.x) / 2f, 1, _currentMap.mapSize.y) * tileSize;
        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (_currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - _currentMap.mapSize.y) / 2f) * tileSize;
        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (_currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - _currentMap.mapSize.y) / 2f) * tileSize;
        
        // 设置导航网格的大小
       navmeshFloor.localScale = new Vector3 (maxMapSize.x, maxMapSize.y) * tileSize;
       mapFloor.localScale = new Vector3(_currentMap.mapSize.x * tileSize,  _currentMap.mapSize.y * tileSize);
    }

    /// <summary>
    /// 该方法通过传入的两个参数，判断随机生成的地图是否完全可通行（即不存在封闭区域）
    /// </summary>
    /// <param name="obstacleMap">用于标记不可通行的障碍物区块，当返回true时，代表传入的坐标处存在障碍物</param>
    /// <param name="currentObstacleCount">当前障碍物数量</param>
    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
    {
        // 访问记录数组，用于记录某一瓦片区块是否已经通过洪水填充处理过
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)];
       
        // 用于存储已被处理过的瓦片坐标
        Queue<Coord> queue = new Queue<Coord>();
        
        // 先将中心坐标标记处理
        queue.Enqueue(_currentMap.MapCenter);
        mapFlags[_currentMap.MapCenter.x, _currentMap.MapCenter.y] = true;
        
        // 可通行瓦片区域数量
        int accessibleTileCount = 1; 
        
        // 直到从queue中处理完最后一个瓦片为止
        while (queue.Count > 0)
        {
            // 取出最外层的瓦片
            Coord tile = queue.Dequeue(); 

            // 双层for循环进行洪水填充（从中心点向外扩散）
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // 相邻格子的坐标
                    int neighborX = tile.x + x;
                    int neighborY = tile.y + y;
                    
                    // 将填充坐标限制在十字范围内
                    if (x == 0 || y == 0) 
                    {
                        // 防止数组越界的条件
                        if (neighborX >= 0 && neighborX < obstacleMap.GetLength(0) &&
                            neighborY >= 0 && neighborY < obstacleMap.GetLength(1))
                        {
                            // 当该片瓦片未被处理且该片瓦片上没有障碍物
                            if (!mapFlags[neighborX, neighborY] && !obstacleMap[neighborX, neighborY])
                            {
                                // 标记该点，同时将处理过的坐标放入queue，可通行瓦片数加一
                                mapFlags[neighborX, neighborY] = true;
                                queue.Enqueue(new Coord(neighborX, neighborY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }
        
        // 声明一个可通行瓦片数量的理论值，若是该理论值与算法得出的实际值相同，则返回true，表示该地图可完全通行
        int targetAccessibleTileCount = _currentMap.mapSize.x * _currentMap.mapSize.y - currentObstacleCount;
        return targetAccessibleTileCount == accessibleTileCount;
    }
    
    /// <summary>
    /// 用于设置瓦片和障碍物的坐标（目的是更好体现出面向对象思想）
    /// </summary>
    Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3 (-_currentMap.mapSize.x / 2f + 0.5f + x, 0, -_currentMap.mapSize.y / 2f + 0.5f + y) * tileSize;
    }

    /// <summary>
    /// 在队列中取出一个随机坐标
    /// </summary>
    public Coord GetRandomCoord()
    {
        Coord randomCoord = _shuffledTileCoords.Dequeue();
        _shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }
    
    /// <summary>
    /// 根据世界坐标获取对应的瓦片Transform
    /// </summary>
    /// <param name="position">世界空间中的位置坐标</param>
    /// <returns>对应坐标位置的瓦片Transform引用</returns>
    public Transform GetTileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / tileSize + (_currentMap.mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / tileSize + (_currentMap.mapSize.y - 1) / 2f);
        x = Mathf.Clamp (x, 0, _tileMap.GetLength (0) -1);
        y = Mathf.Clamp (y, 0, _tileMap.GetLength (1) -1);
        return _tileMap [x, y];
    }
    
    /// <summary>
    /// 获取随机可通行瓦片的位置（保证该位置没有障碍物）
    /// </summary>
    /// <returns>随机安全位置的瓦片Transform引用</returns>
    public Transform GetRandomOpenTile() 
    {
        Coord randomCoord = _shuffledOpenTileCoords.Dequeue ();
        _shuffledOpenTileCoords.Enqueue (randomCoord);
        return _tileMap[randomCoord.x,randomCoord.y];
    }
    
    /// <summary>
    /// 声明一个坐标结构体用于存储瓦片坐标
    /// </summary>
    [System.Serializable]
    public struct Coord
    {
        /// <summary>
        /// 水平坐标（对应二维数组第一维度）
        /// </summary>
        public int x;
        
        /// <summary>
        /// 垂直坐标（对应二维数组第二维度）
        /// </summary>
        public int y;
        
        /// <summary>
        /// 构造函数，用于创建并初始化Coord结构体的实例
        /// </summary>
        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        
        /// <summary>
        /// 使用运算符重载使该结构可以进行坐标相等性比较
        /// </summary>
        public static bool operator ==(Coord c1, Coord c2) 
        {
            return c1.x == c2.x && c1.y == c2.y;
        }
        
        /// <summary>
        /// 使用运算符重载使该结构可以进行坐标不等性比较
        /// </summary>
        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }
    }

    /// <summary>
    /// 地图类，包含多个属性
    /// </summary>
    [System.Serializable]
    public class Map
    {
        /// <summary>
        /// 地图尺寸（以瓦片数量为单位）
        /// X: 水平方向瓦片数 Y: 垂直方向瓦片数
        /// </summary>
        public Coord mapSize;
        /// <summary>
        /// 障碍物覆盖率
        /// 0: 无障碍物 1: 完全被障碍物覆盖
        /// </summary>
        [Range(0, 1)] public float obstaclePercent;
        /// <summary>
        /// 随机数种子，相同种子会产生相同地图布局
        /// </summary>
        public int seed;
        /// <summary>
        /// 障碍物最小高度
        /// </summary>
        public float minimumObstacleHeight;
        /// <summary>
        /// 障碍物最大高度
        /// </summary>
        public float maximumObstacleHeight;
        /// <summary>
        /// 近端颜色（用于障碍物颜色渐变）
        /// </summary>
        public Color foregroundColor;
        /// <summary>
        /// 远端颜色（用于障碍物颜色渐变）
        /// </summary>
        public Color backgroundColor;
        /// <summary>
        /// 地图中心坐标（洪水填充算法的起点），该位置保证永远畅通
        /// </summary>
        public Coord MapCenter
        {
            get
            {
                return new Coord(mapSize.x / 2, mapSize.y / 2);
            }
        }
    }
}
