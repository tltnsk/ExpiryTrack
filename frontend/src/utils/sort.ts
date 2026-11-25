import { ref, computed, type Ref } from 'vue'

// Sorting for a table
// click a column once goes from A to Z clicking it again goes from Z to A.
// list = the rows, firstKey = the column used before anything is clicked
export function useSort<T>(list: Ref<T[]>, firstKey: keyof T, firstAscending = true) {
  const sortKey = ref(firstKey) as Ref<keyof T>
  const ascending = ref(firstAscending)

  function sortBy(key: keyof T) {
    if (sortKey.value === key) {
      ascending.value = !ascending.value 
    } else {
      sortKey.value = key
      ascending.value = true
    }
  }

  // the small arrow next to the column name
  function arrow(key: keyof T) {
    if (sortKey.value !== key) return '⇅'
    return ascending.value ? '↑' : '↓'
  }

  const sorted = computed(() => {
    return [...list.value].sort((a, b) => {
      const first = a[sortKey.value]
      const second = b[sortKey.value]
      if (first === second) return 0
      if (first === null) return 1 // empty values always at the end
      if (second === null) return -1
      const result = typeof first === 'number' && typeof second === 'number'
        ? first - second
        : String(first).localeCompare(String(second)) // text and dates
      return ascending.value ? result : -result
    })
  })

  return { sorted, sortBy, arrow, sortKey, ascending }
}