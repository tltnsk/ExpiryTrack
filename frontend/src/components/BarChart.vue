<script setup lang="ts">
import {ref, onMounted, computed} from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
    labels: string[]
    values: number[]
    aspectRatio?: number
}>()

const canvas = ref<HTMLCanvasElement | null>(null)

const hasData = computed(() => props.values.some((value) => value > 0))

onMounted(() =>{
    if (!canvas.value) return // no canvas when there's no data
    new Chart(canvas.value!, {
        type: 'bar',
        data: {
            labels: props.labels,
            datasets: [{
                data: props.values,
                backgroundColor: '#111111',
                borderRadius: 6,
            }],
        },
        options: {
            aspectRatio: props.aspectRatio ?? 2,
            plugins: { legend: { display: false } },
            scales: { 
                x: {
                    ticks: { color: '#111111',
                    },
                    grid: { color: 'rgba(0, 0, 0, 0.08)'}
                },
                y: { beginAtZero: true, ticks: { precision: 0, color: '#111111' }, grid: {
                    color: 'rgba(0, 0, 0, 0.08)'
                }}},
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