<script setup lang="ts">
import {ref, onMounted} from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
    labels: string[]
    values: number[]
}>()

const canvas = ref<HTMLCanvasElement | null>(null)

onMounted(() =>
    new Chart(canvas.value!, {
        type: 'bar',
        data: {
            labels: props.labels,
            datasets: [{
                data: props.values,
                backgroundColor: '#ffffff',
            }],
        },
        options: {
            plugins: { legend: { display: false } },
            scales: { 
                x: {
                    ticks: { color: '#ffffff',
                    },
                    grid: { color: 'rgba(255, 255, 255, 0.2)'}
                },
                y: { beginAtZero: true, ticks: { precision: 0, color: '#ffffff' }, grid: {
                    color: 'rgba(255, 255, 255, 0.2)'
                }}},
        },
    
    })
)
</script>
<template>
    <canvas ref="canvas"></canvas>
</template>