namespace GameForum.Services
{
    public class QuickSortService
    {
        public int[] Sort(int[] array)
        {
            if (array == null || array.Length <= 1)
                return array;

            var result = (int[])array.Clone();
            QuickSort(result, 0, result.Length - 1);
            return result;
        }

        private void QuickSort(int[] array, int left, int right)
        {
            if (left < right)
            {
                var pivotIndex = Partition(array, left, right);
                QuickSort(array, left, pivotIndex - 1);
                QuickSort(array, pivotIndex + 1, right);
            }
        }

        private int Partition(int[] array, int left, int right)
        {
            var pivot = array[right];
            var i = left - 1;

            for (int j = left; j < right; j++)
            {
                if (array[j] <= pivot)
                {
                    i++;
                    Swap(array, i, j);
                }
            }

            Swap(array, i + 1, right);
            return i + 1;
        }

        private void Swap(int[] array, int i, int j)
        {
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
