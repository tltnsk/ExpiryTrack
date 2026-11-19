<script setup lang="ts">
import {ref, onMounted} from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
    labels: string[]
    values: number[]
    aspectRatio?: number
}>()

const canvas = ref<HTMLCanvasElement | null>(null)

onMounted(() =>
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
)
</script>
<template>
    <canvas ref="canvas"></canvas>
</template>