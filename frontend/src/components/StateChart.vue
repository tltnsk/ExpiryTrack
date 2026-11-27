<script setup lang="ts">
import {ref, onMounted, computed} from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
    values: number[]
    aspectRatio?: number
    legendPosition?: 'right' | 'bottom'
}>()

const canvas = ref<HTMLCanvasElement | null>(null)

const hasData = computed(() => props.values.some((value) => value > 0))

onMounted(() => {
    if (!canvas.value) return // no canvas when there's no data
    new Chart(canvas.value!, {
        type: 'doughnut',
        data: {
            labels: ['Active', 'Expiring soon', 'Expired', 'Cancelled'],
            datasets: [{
                data: props.values,
                backgroundColor: ['#2f61e2', '#6c91ec', '#1d3f99', '#c6d6f9']
            }],
        },
        options: {
            aspectRatio: props.aspectRatio ?? 2,
            plugins: { legend: { position: props.legendPosition ?? 'right'}},
        },
    
    })
}
)
</script>
<template>
    <p v-if="!hasData" class="no-data">Nothing to show yet.</p>
    <canvas v-else ref="canvas"></canvas>
</template>
<style scoped>
.no-data {
    padding: 24px 0;
    color: #6b6b6b;
    text-align: center;
}
</style>