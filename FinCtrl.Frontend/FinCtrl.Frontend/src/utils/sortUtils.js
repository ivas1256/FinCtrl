/**
 * Сортировка вставками (Insertion Sort)
 *
 * Суть алгоритма: массив делится на отсортированную и неотсортированную части.
 * На каждом шаге берётся первый элемент из неотсортированной части
 * и вставляется на правильную позицию в отсортированную часть.
 *
 * Сложность: O(n²) в худшем случае, O(n) в лучшем (если массив уже отсортирован)
 *
 * @param {number[]} arr - Массив чисел для сортировки
 * @returns {number[]} - Отсортированный массив (мутирует исходный)
 */
function insertionSort(arr) {
  if (!Array.isArray(arr)) {
    throw new TypeError('Аргумент должен быть массивом');
  }

  for (let i = 1; i < arr.length; i++) {
    const key = arr[i]; // элемент, который вставляем
    let j = i - 1;

    // Сдвигаем элементы вправо, пока не найдём место для key
    while (j >= 0 && arr[j] > key) {
      arr[j + 1] = arr[j];
      j--;
    }

    arr[j + 1] = key;
  }

  return arr;
}

/**
 * Сортировка вставками с кастомной функцией сравнения
 *
 * @param {*[]} arr - Массив для сортировки
 * @param {function(a, b): number} compareFn - Функция сравнения (как для Array.sort)
 * @returns {*[]}
 */
function insertionSortWithComparator(arr, compareFn = (a, b) => a - b) {
  if (!Array.isArray(arr)) {
    throw new TypeError('Аргумент должен быть массивом');
  }
  if (typeof compareFn !== 'function') {
    throw new TypeError('compareFn должна быть функцией');
  }

  for (let i = 1; i < arr.length; i++) {
    const key = arr[i];
    let j = i - 1;

    while (j >= 0 && compareFn(arr[j], key) > 0) {
      arr[j + 1] = arr[j];
      j--;
    }

    arr[j + 1] = key;
  }

  return arr;
}

export { insertionSort, insertionSortWithComparator };