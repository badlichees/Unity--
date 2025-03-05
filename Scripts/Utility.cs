public static class Utility
{
    /// <summary>
    /// Fisher-Yates算法，用于将数组中所有的元素随机打乱且不重复
    /// </summary>
    /// <param name="array">用于存放由prng生成的随机数的泛型数组</param>
    /// <param name="seed">用于确保打乱的唯一性，每个种子对应一个生成（打乱）方案</param>
    /// <returns></returns>
    public static T[] ShuffleArray<T>(T[] array, int seed)
    {
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < array.Length - 1; i++)
        {
            int randomIndex = prng.Next(i, array.Length);
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }
        return array;
    }
}
